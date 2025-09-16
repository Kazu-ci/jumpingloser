using System;
using System.Collections.Generic;
using UnityEngine;
using static EventBus;
public enum HandType
{
    Main, // 右手
    Sub   // 左手
}
[System.Serializable]
public class WeaponInstance
{
    public WeaponItem template;
    public int currentDurability;

    public WeaponInstance(WeaponItem weapon)
    {
        template = weapon;
        currentDurability = 1;
    }

    public bool Use(int cost)
    {
        if (currentDurability < cost) return false;
        currentDurability -= Mathf.CeilToInt(cost);
        currentDurability = Mathf.Max(0, currentDurability);
        currentDurability = Mathf.Min(currentDurability, template.maxDurability);

        return true;
    }

    public bool IsBroken => currentDurability <= 0;
}


[Serializable]
public class PlayerWeaponInventory
{
    [Tooltip("所持武器（インデックスがUI上の番号と一致）")]
    public List<WeaponInstance> weapons = new List<WeaponInstance>();

    [Tooltip("右手（Main）の現在装備インデックス。-1 は未装備")]
    public int mainIndex = -1;

    [Tooltip("左手（Sub）の現在装備インデックス。-1 は未装備")]
    public int subIndex = -1;

    private Dictionary<WeaponType,int> typeToIndex = new Dictionary<WeaponType, int>();

    // ==== 内部ユーティリティ ====
    private bool IsUsableIndex(int idx)
    {
        return idx >= 0 && idx < weapons.Count && weapons[idx] != null && !weapons[idx].IsBroken;
    }

    // dir: +1 = 前方向に巡回, -1 = 後方向に巡回
    private int FindNextUsable(int startIdx, int exclude, int dir = 1)
    {
        // ※ 安全ガード
        int n = weapons.Count;
        if (n == 0) return -1;
        if (dir == 0) dir = +1;

        // startIdx は [-1, n-1] に正規化（-1 は「今の位置の直前」みたいに扱う）
        int start = Mathf.Clamp(startIdx, -1, n - 1);

        // n 回まで見にいく
        for (int step = 1; step <= n; ++step)
        {
            // 巡回インデックス計算（負数補正あり）
            int i = (start + step * dir) % n;
            if (i < 0) i += n;

            if (exclude >= 0 && i == exclude) continue;
            if (IsUsableIndex(i)) return i;
        }
        return -1;
    }

    private int GetHandIndex(HandType hand) => (hand == HandType.Main) ? mainIndex : subIndex;

    // 手のインデックスを設定し、UI イベントに転送
    private void SetHandIndex(HandType hand, int to)
    {
        int from = GetHandIndex(hand);
        if (hand == HandType.Main) mainIndex = to; else subIndex = to;

        if (from != to)
        {
            // --- ここでUIEventsへ通知 ---
            if (hand == HandType.Main)
            {
                UIEvents.OnRightWeaponSwitch?.Invoke(weapons, from, to);
            }
            else
            {
                UIEvents.OnLeftWeaponSwitch?.Invoke(weapons, from, to);
            }
        }
    }

    // ==== 装備切替 ====
    public bool TrySwitchRight(int index = 1)
    {
        int next = -1;
        if (weapons.Count > 0) next = FindNextUsable(mainIndex, exclude: -1, index);
        SetHandIndex(HandType.Main, next);

        if (next == -1) return false;
        return true;
    }

    public void Unequip(HandType hand)
    {
        SetHandIndex(hand, -1);
    }

    // ==== 所持管理 ====
    public void AddWeapon(WeaponItem weapon)
    {
        if (weapon == null) return;
        if(typeToIndex.ContainsKey(weapon.weaponType))
        {
            int idx = typeToIndex[weapon.weaponType];
            weapons[idx].currentDurability += weapons[idx].template.addDurabilityOnPickup;
            weapons[idx].currentDurability = Mathf.Min(weapons[idx].currentDurability, weapons[idx].template.maxDurability);
            UIEvents.OnDurabilityChanged?.Invoke(HandType.Main, idx, weapons[idx].currentDurability, weapons[idx].template.maxDurability);
            return;
        }
        else
        {
            weapons.Add(new WeaponInstance(weapon));
            typeToIndex[weapon.weaponType] = weapons.Count - 1;
            UIEvents.OnRightWeaponSwitch?.Invoke(weapons, mainIndex, mainIndex); // UI更新
        }
        
    }

    // ==== 耐久消費（破壊時は自動 Remove & Recover）====
    // 仕様変更：武器壊せずに耐久0で放置できるよう
    public bool ConsumeDurability(HandType hand, int cost)
    {
        int idx = GetHandIndex(hand);
        bool res = true;
        if (!IsUsableIndex(idx)) res = false;

        WeaponInstance inst = weapons[idx];
        if(res) res = inst.Use(cost);

        // --- 耐久度更新イベント ---
        if (res)
        {
            UIEvents.OnDurabilityChanged?.Invoke(
            hand, idx, inst.currentDurability, inst.template.maxDurability
        );
        } 
        else { UIEvents.OnWeaponUseFailed?.Invoke(); }

        return res;
    }

    public WeaponInstance GetWeapon(HandType hand)
    {
        int idx = GetHandIndex(hand);
        //return IsUsableIndex(idx) ? weapons[idx] : null;
        if(idx >= 0 && idx < weapons.Count)
        {
            return weapons[idx];
        }
        else
        {
            return null;
        }
    }
}




