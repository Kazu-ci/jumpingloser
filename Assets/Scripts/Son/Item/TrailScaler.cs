using UnityEngine;

[RequireComponent(typeof(TrailRenderer))]
public class TrailScaler : MonoBehaviour
{
    // === Trail の太さを指定（Inspector で設定可能） ===
    [Tooltip("Trail の全体的な太さ倍率（デフォルト1）")]
    public float width = 5.0f;

    // === Trail の先頭～末尾までの太さカーブ ===
    [Tooltip("Trail の長さに沿った太さ変化（X=位置, Y=倍率）")]
    public AnimationCurve widthCurve = AnimationCurve.Linear(0, 1, 1, 1);

    // TrailRenderer への参照
    private TrailRenderer trail;

    private void Awake()
    {
        // TrailRenderer コンポーネントを取得
        trail = GetComponent<TrailRenderer>();
    }

    private void Start()
    {
        // Start 時に設定を適用
        ApplyTrailWidth();
    }

    /// <summary>
    /// TrailRenderer に太さ設定を適用する関数
    /// </summary>
    public void ApplyTrailWidth()
    {
        trail.widthMultiplier = width;  // 全体倍率
        trail.widthCurve = widthCurve;  // 太さカーブ
    }
}
