using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WeaponType �� WeaponItem �̌y�ʃ��]���o
/// �EResources/WeaponItems �ȉ��̑S WeaponItem ����x�����ǂݍ��ݎ�����
/// �E�ǉ��̃f�[�^�x�[�X���Y��s�v�ɂ���ŏ��\��
/// </summary>
public static class WeaponItemResolver
{
    private static bool _built = false;
    private static Dictionary<WeaponType, WeaponItem> _map;

    /// <summary>
    /// ����A�N�Z�X���� Resources/WeaponItems �𑖍����Ď������\�z
    /// </summary>
    private static void BuildIfNeeded()
    {
        if (_built) return;

        _map = new Dictionary<WeaponType, WeaponItem>(8);
        // Assets/Resources/WeaponItems/*.asset �ɔz�u����z��
        var all = Resources.LoadAll<WeaponItem>("WeaponItems");
        for (int i = 0; i < all.Length; ++i)
        {
            var w = all[i];
            if (w == null) continue;
            if (_map.ContainsKey(w.weaponType))
            {
                Debug.LogWarning($"[WeaponItemResolver] WeaponType �d��: {w.weaponType}. �㏟���ŏ㏑�����܂��B");
            }
            _map[w.weaponType] = w;
        }
        _built = true;
#if UNITY_EDITOR
        Debug.Log($"[WeaponItemResolver] Loaded WeaponItems: {_map.Count}");
#endif
    }

    /// <summary>
    /// WeaponType ���� WeaponItem ���擾
    /// </summary>
    public static bool TryGet(WeaponType type, out WeaponItem item)
    {
        BuildIfNeeded();
        if (_map != null && _map.TryGetValue(type, out item)) return true;
        item = null;
        return false;
    }
}
