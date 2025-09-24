using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// クリア結果の各UIを順次出す簡易プレゼンター
/// - 各要素ごとに: 遅延(delay) → duration で from→to に移動
/// - イージング可、必要時のみ SetActive(true)
/// - 非スケール時間を使用（結果画面で timeScale=0 でも動く）
/// </summary>
public class ResultDataUI : MonoBehaviour
{
    [Serializable]
    public class ClearUI
    {
        [Tooltip("表示対象のUIルート")]
        public GameObject ui;
        [Tooltip("開始位置（ローカル）")]
        public Vector3 from;
        [Tooltip("終了位置（ローカル）")]
        public Vector3 to;

        [Tooltip("表示を開始するまでの遅延秒")]
        public float delay = 0f;
        [Tooltip("移動にかける時間（秒）")]
        public float duration = 0.4f;

        [Tooltip("進行に対する補間（0→1）")]
        public AnimationCurve easing = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("任意：フェード")]
        [Tooltip("CanvasGroup があれば 0→1 にフェード")]
        public bool fadeIfCanvasGroup = true;

        // ---- ランタイム ----
        [NonSerialized] public bool started;
        [NonSerialized] public bool finished;
        [NonSerialized] public float startTimeUnscaled; // 開始時刻（非スケール秒）
        [NonSerialized] public CanvasGroup cg;          // あれば利用
    }

    [Header("表示キュー")]
    public List<ClearUI> clearUIs = new List<ClearUI>();

    // 非スケール時間の基準（有効化時に0）
    private float _t0;

    private void OnEnable()
    {
        // === 初期化（結果画面に入った瞬間） ===
        _t0 = Time.realtimeSinceStartup; // 非スケール時間の代替

        if (clearUIs == null || clearUIs.Count == 0) return;

        foreach (var ui in clearUIs)
        {
            if (ui.ui == null) continue;

            // 位置を from にセット、いったん非表示
            var tr = ui.ui.transform as RectTransform;
            tr.localPosition = ui.from;
            ui.ui.SetActive(false);

            // 可能なら CanvasGroup を取得（フェード用）
            ui.cg = ui.ui.GetComponent<CanvasGroup>();
            if (ui.fadeIfCanvasGroup && ui.cg != null)
            {
                ui.cg.alpha = 0f;
            }

            ui.started = false;
            ui.finished = false;
            ui.startTimeUnscaled = 0f;
        }
    }

    private void Update()
    {
        if (clearUIs == null || clearUIs.Count == 0)
        {
            enabled = false; // 監視不要なら自動停止
            return;
        }

        // 非スケール秒（結果UIは timeScale の影響を受けない前提）
        float now = Time.realtimeSinceStartup;
        float elapsed = now - _t0;

        bool anyAlive = false;

        // for を使い、要素のランタイム状態を書き戻せるようにする
        for (int i = 0; i < clearUIs.Count; i++)
        {
            var item = clearUIs[i];
            if (item == null || item.ui == null) continue;

            if (item.finished) continue; // 完了済み

            // まだ開始時刻に達していない
            if (elapsed < item.delay)
            {
                anyAlive = true;
                continue;
            }

            // 開始時処理（1回だけ）
            if (!item.started)
            {
                item.ui.SetActive(true);
                item.started = true;
                item.startTimeUnscaled = now;
            }

            // 進行度（0→1）
            float t = item.duration <= 0f ? 1f : Mathf.Clamp01((now - item.startTimeUnscaled) / item.duration);
            float eval = item.easing != null ? item.easing.Evaluate(t) : t;

            // 線形ではなく curve 評価で from→to を補間（毎フレーム from を使うのではなく、固定の起点を使う）
            var tr = item.ui.transform as RectTransform;
            tr.localPosition = Vector3.LerpUnclamped(item.from, item.to, eval);

            // フェード（任意）
            if (item.fadeIfCanvasGroup && item.cg != null)
            {
                item.cg.alpha = eval;
            }

            if (t >= 1f)
            {
                item.finished = true;
            }
            else
            {
                anyAlive = true;
            }
        }

        // 全部終わったら Update を止めて無駄な負荷を避ける
        if (!anyAlive)
        {
            enabled = false;
        }
    }

    /// <summary>
    /// 「次へ」ボタン
    /// </summary>
    public void OnNextButtonClick()
    {
        GameManager.Instance?.ToEndRoll();
    }
}
