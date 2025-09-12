using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ダッシュ時に一定間隔でスキンメッシュのスナップショット（残像）を生成する
/// ・LungeManager の開始/終了イベントに連動
/// ・各スナップショットは 0.1 秒でフェードアウトして破棄
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(LungeManager))]
public class DashAfterimageSpawner : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("残像化する SkinnedMeshRenderer。未指定なら子階層から自動収集")]
    public SkinnedMeshRenderer[] sources;

    [Tooltip("残像用の透明マテリアル（α有効）。URP/Lit などの Transparent を推奨")]
    public Material ghostMaterial;

    [Header("生成パラメータ")]
    [Tooltip("生成間隔（秒）。例：0.05")]
    public float spawnInterval = 0.05f;

    [Tooltip("残像の寿命（秒）。例：0.10")]
    public float lifeTime = 0.10f;

    [Range(0f, 1f)]
    [Tooltip("生成直後の不透明度（0-1）。残りはフェードアウト")]
    public float initialAlpha = 0.6f;

    [Tooltip("フェードカーブ（Time=0→1 に対する α 乗算）。未設定なら線形")]
    public AnimationCurve alphaCurve;

    [Header("描画オプション")]
    [Tooltip("影の受け取りを無効にする（視認性とコスト対策）")]
    public bool disableReceiveShadows = true;

    [Tooltip("影の投影を無効にする（視認性とコスト対策）")]
    public bool disableCastShadows = true;

    // 日本語：内部
    private LungeManager _lm;
    private PlayerMovement _player; // PlayableGraph の Evaluate を使うため（任意）
    private Coroutine _loopCo;

    private void Awake()
    {
        _lm = GetComponent<LungeManager>();
        _player = GetComponent<PlayerMovement>();

        if (alphaCurve == null || alphaCurve.length == 0)
        {
            // 日本語：未設定時は線形（1→0）
            alphaCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        }
    }

    private void OnEnable()
    {
        _lm.OnLungeStart += HandleLungeStart;
        _lm.OnLungeFinish += HandleLungeEnd;
        _lm.OnLungeBlocked += HandleLungeEnd;
        _lm.OnLungeTooSteep += HandleLungeEnd;
    }

    private void OnDisable()
    {
        _lm.OnLungeStart -= HandleLungeStart;
        _lm.OnLungeFinish -= HandleLungeEnd;
        _lm.OnLungeBlocked -= HandleLungeEnd;
        _lm.OnLungeTooSteep -= HandleLungeEnd;

        if (_loopCo != null) StopCoroutine(_loopCo);
        _loopCo = null;
    }

    private void HandleLungeStart()
    {
        // 日本語：ソースが未設定なら自動収集
        if (sources == null || sources.Length == 0)
        {
            sources = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        }

        // 日本語：生成ループ開始
        if (_loopCo != null) StopCoroutine(_loopCo);
        _loopCo = StartCoroutine(CoSpawnLoop());
    }

    private void HandleLungeEnd()
    {
        // 日本語：生成ループ停止（既存残像は各自で寿命管理）
        if (_loopCo != null) StopCoroutine(_loopCo);
        _loopCo = null;
    }

    private IEnumerator CoSpawnLoop()
    {
        // 日本語：開始時に即 1 回生成し、その後は spawnInterval ごと
        while (_lm != null && _lm.IsLunging)
        {
            SpawnGhostNow();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnGhostNow()
    {
        if (ghostMaterial == null || sources == null) return;

        // 日本語：PlayableGraph を使っている場合、評価を一度呼んでポーズを安定化
        if (_player != null) _player.EvaluateGraphOnce();

        for (int i = 0; i < sources.Length; ++i)
        {
            var smr = sources[i];
            if (smr == null || !smr.gameObject.activeInHierarchy) continue;

            // 日本語：現在ポーズをベイク
            var baked = new Mesh();
            smr.BakeMesh(baked); // SkinnedMeshRenderer からスナップショット作成（Unity 6 公式API）

            // 日本語：残像 GameObject を組み立て
            var go = new GameObject($"Ghost_{smr.name}");
            go.layer = gameObject.layer; // レイヤー継承（必要に応じて変更）

            // 日本語：親無しのワールド配置（頂点は SMR の Transform 基準）
            go.transform.SetPositionAndRotation(smr.transform.position, smr.transform.rotation);
            go.transform.localScale = Vector3.one; // BakeMesh はスキン変形後頂点なので 1 で描画してOK

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = baked;

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = ghostMaterial;
            mr.receiveShadows = !disableReceiveShadows;
#if UNITY_6000_0_OR_NEWER
            // 日本語：投影フラグ（Editor/RenderPipeline によって挙動差あり）
            if (disableCastShadows) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
#endif

            // 日本語：フェード担当のコンポーネントを付与
            var fade = go.AddComponent<DashGhostInstance>();
            fade.Init(lifeTime, initialAlpha, alphaCurve, () =>
            {
                // 日本語：Mesh の明示破棄（リーク防止）
                if (mf != null && mf.sharedMesh != null)
                {
                    Destroy(mf.sharedMesh);
                }
                Destroy(go);
            });
        }
    }
}
