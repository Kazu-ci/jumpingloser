using UnityEngine;
using static EventBus;
using System.Collections.Generic;
using DG.Tweening;

public class TempUI_Loading : MonoBehaviour
{
    public static TempUI_Loading Instance { get; private set; }
    public GameObject loadingUI;

    [Header("どの状態でローディングUIを出すか")]
    public List<GameState> loadingStates = new List<GameState>();

    [Header("LOADING の各文字オブジェクト（順番通り）")]
    public List<GameObject> Loadings = new List<GameObject>();

    [Header("アニメ設定")]
    public float jumpPower = 30f;      // ジャンプ高さ(px)
    public float jumpDuration = 0.30f; // 1文字の合計時間（上り0.5/下り0.5）
    public float letterDelay = 0.05f;  // 隣の文字への遅延
    public float waveInterval = 0.20f; // 1波と次波の間隔

    private Sequence waveSeq;                  // 再利用するメインシーケンス
    private readonly List<RectTransform> _rts = new(); // キャッシュ
    private Vector2[] _basePos;                // 初期位置

    private void Awake()
    {
        // シングルトン保証
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // DOTween容量を先に確保（GC回避の一助）
        DOTween.SetTweensCapacity(200, 50);
    }

    private void OnEnable()
    {
        SystemEvents.OnGameStateChange += HandlePreLoading;
        SystemEvents.OnSceneLoadComplete += HandleLoadingComplete;

        // --- 一度だけ準備（レターと初期座標を確定） ---
        CacheRectsAndBasePositions();
        BuildSequenceIfNeeded(); // シーケンスも一度だけ作る
    }

    private void OnDisable()
    {
        SystemEvents.OnGameStateChange -= HandlePreLoading;
        SystemEvents.OnSceneLoadComplete -= HandleLoadingComplete;

        // 無効化時は停止＆初期位置へ
        PauseAndReset();
    }

    private void HandlePreLoading(GameState state)
    {
        if (!loadingUI) return;

        if (loadingStates.Contains(state))
        {
            loadingUI.SetActive(true);
            // --- 再生（Killせず再始動） ---
            if (waveSeq != null)
            {
                // ※ 有効化直後に瞬時再生開始
                waveSeq.Restart();
            }
        }
        else
        {
            loadingUI.SetActive(false);
            PauseAndReset();
        }
    }

    private void HandleLoadingComplete()
    {
        if (!loadingUI) return;
        loadingUI.SetActive(false);
        PauseAndReset();
    }

    // ==== 内部処理 ====

    /// <summary>
    /// 子文字RectTransformと初期座標をキャッシュ
    /// </summary>
    private void CacheRectsAndBasePositions()
    {
        _rts.Clear();
        for (int i = 0; i < Loadings.Count; i++)
        {
            if (Loadings[i] == null) continue;
            var rt = Loadings[i].GetComponent<RectTransform>();
            if (rt != null) _rts.Add(rt);
        }

        _basePos = new Vector2[_rts.Count];
        for (int i = 0; i < _rts.Count; i++)
        {
            _basePos[i] = _rts[i].anchoredPosition;
        }
    }

    /// <summary>
    /// 波シーケンスを一度だけ構築（AutoKillしない）
    /// </summary>
    private void BuildSequenceIfNeeded()
    {
        if (waveSeq != null) return;

        // 既存があれば止める（基本来ない）
        waveSeq = DOTween.Sequence()
            // Unscaled更新でローディング中のTime.timeScale影響を受けない
            .SetUpdate(true)
            // 破棄しないで使い回す
            .SetAutoKill(false);

        // 1波の長さ（全員分のジャンプ＋最後の間隔）
        float half = jumpDuration * 0.5f;

        // --- 各文字のジャンプを「挿入」して1本の波にする ---
        for (int i = 0; i < _rts.Count; i++)
        {
            var rt = _rts[i];
            float startAt = i * letterDelay; // 文字ごとの開始オフセット

            // ※ 基準Y（毎回計算ではなくローカル変数に退避）
            float baseY = _basePos[i].y;

            // 上り→下りの連結を一つのSequenceに
            var oneJump = DOTween.Sequence()
                // 上り：OutQuadでフワッと
                .Append(rt.DOAnchorPosY(baseY + jumpPower, half).SetEase(Ease.OutQuad))
                // 下り：InQuadでスッと落ちる
                .Append(rt.DOAnchorPosY(baseY, half).SetEase(Ease.InQuad));

            // メインに時間挿入（このおかげで全員が1本の波時間軸に乗る）
            waveSeq.Insert(startAt, oneJump);
        }

        // 波の終端に待機を足して、無限ループ
        float waveLength = (_rts.Count > 0 ? (_rts.Count - 1) * letterDelay : 0f) + jumpDuration + waveInterval;
        waveSeq.AppendInterval(Mathf.Max(0f, waveLength - waveSeq.Duration()));
        waveSeq.SetLoops(-1, LoopType.Restart);

        // 最初は停止しておく（必要なときにRestart）
        waveSeq.Pause();
    }

    /// <summary>
    /// シーケンスを止めて全文字を初期位置に戻す
    /// </summary>
    private void PauseAndReset()
    {
        if (waveSeq != null) waveSeq.Pause();

        if (_rts != null && _basePos != null)
        {
            int n = Mathf.Min(_rts.Count, _basePos.Length);
            for (int i = 0; i < n; i++)
            {
                if (_rts[i] != null)
                    _rts[i].anchoredPosition = _basePos[i]; // 位置を確実にリセット
            }
        }
    }
}
