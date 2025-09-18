using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WeaponType → WeaponItem の軽量リゾルバ
/// ・Resources/WeaponItems 以下の全 WeaponItem を一度だけ読み込み辞書化
/// ・追加のデータベース資産を不要にする最小構成
/// </summary>
public static class WeaponItemResolver
{
    private static bool _built = false;
    private static Dictionary<WeaponType, WeaponItem> _map;

    /// <summary>
    /// 初回アクセス時に Resources/WeaponItems を走査して辞書を構築
    /// </summary>
    private static void BuildIfNeeded()
    {
        if (_built) return;

        _map = new Dictionary<WeaponType, WeaponItem>(8);
        // Assets/Resources/WeaponItems/*.asset に配置する想定
        var all = Resources.LoadAll<WeaponItem>("WeaponItems");
        for (int i = 0; i < all.Length; ++i)
        {
            var w = all[i];
            if (w == null) continue;
            if (_map.ContainsKey(w.weaponType))
            {
                Debug.LogWarning($"[WeaponItemResolver] WeaponType 重複: {w.weaponType}. 後勝ちで上書きします。");
            }
            _map[w.weaponType] = w;
        }
        _built = true;
#if UNITY_EDITOR
        Debug.Log($"[WeaponItemResolver] Loaded WeaponItems: {_map.Count}");
#endif
    }

    /// <summary>
    /// WeaponType から WeaponItem を取得
    /// </summary>
    public static bool TryGet(WeaponType type, out WeaponItem item)
    {
        BuildIfNeeded();
        if (_map != null && _map.TryGetValue(type, out item)) return true;
        item = null;
        return false;
    }
}
