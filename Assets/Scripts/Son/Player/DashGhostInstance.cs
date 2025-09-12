using System;
using UnityEngine;

/// <summary>
/// 残像 1 体の寿命・フェード管理
/// ・MaterialPropertyBlock で α を更新（インスタンス化コスト回避）
/// ・寿命で自動破棄（コールバック）
/// </summary>
[DisallowMultipleComponent]
public class DashGhostInstance : MonoBehaviour
{
    private float _life;
    private float _age;
    private float _initialAlpha;
    private AnimationCurve _curve;
    private Action _onDispose;

    private MeshRenderer _mr;
    private MaterialPropertyBlock _mpb;

    // 日本語：URP/Lit などの代表的カラー名を順に探す
    private static readonly int[] _colorIDs = new int[]
    {
        Shader.PropertyToID("_BaseColor"), // URP
        Shader.PropertyToID("_Color")      // Built-in/Standard
    };

    public void Init(float life, float initialAlpha, AnimationCurve curve, Action onDispose)
    {
        _life = Mathf.Max(0.01f, life);
        _initialAlpha = Mathf.Clamp01(initialAlpha);
        _curve = curve;
        _onDispose = onDispose;

        _mr = GetComponent<MeshRenderer>();
        if (_mr == null) _mr = gameObject.AddComponent<MeshRenderer>();
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        // 日本語：開始時に初期 α を反映
        ApplyAlpha(1f);
    }

    private void Update()
    {
        _age += Time.deltaTime;
        float t01 = Mathf.Clamp01(_age / _life);
        ApplyAlpha(1f - _curve.Evaluate(t01)); // 日本語：カーブは 1→0 想定（線形ならそのまま）

        if (_age >= _life)
        {
            _onDispose?.Invoke();
        }
    }

    private void ApplyAlpha(float factor)
    {
        if (_mr == null) return;

        _mr.GetPropertyBlock(_mpb);

        // 日本語：最初に見つかったカラープロパティに α を適用
        for (int i = 0; i < _colorIDs.Length; ++i)
        {
            if (_mr.sharedMaterial != null && _mr.sharedMaterial.HasProperty(_colorIDs[i]))
            {
                Color c = _mr.sharedMaterial.GetColor(_colorIDs[i]);
                c.a = _initialAlpha * Mathf.Clamp01(factor);
                _mpb.SetColor(_colorIDs[i], c);
                break;
            }
        }
        _mr.SetPropertyBlock(_mpb);
    }
}
