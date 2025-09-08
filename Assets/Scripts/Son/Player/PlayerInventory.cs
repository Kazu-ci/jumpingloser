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
        currentDurability = (int)(weapon.maxDurability / 2);
    }

    public void Use(float cost)
    {
        currentDurability -= Mathf.CeilToInt(cost);
        currentDurability = Mathf.Max(0, currentDurability);
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
    public bool TrySwitchRight()
    {
        // 右手は左手を除外しない
        //int next = FindNextUsable(mainIndex, exclude: -1);
        int next = -1;
        if (weapons.Count > 0) next = (mainIndex + 1) % (weapons.Count);

        SetHandIndex(HandType.Main, next);
        if (next == -1) return false;

        // 右手と左手が同じになったら、左手を逃がす（右手優先）
        if (subIndex == mainIndex)
        {
            int newSub = FindNextUsable(subIndex, exclude: mainIndex, -1);
            SetHandIndex(HandType.Sub, newSub);
        }
        return true;
    }

    public bool TrySwitchLeft()
    {
        // 左手は右手使用中の武器をスキップ
        int next = FindNextUsable(subIndex, exclude: mainIndex, -1);

        SetHandIndex(HandType.Sub, next);
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

    // インベントリから removeIndex を削除し、手持ちを自動回復
   /* public void RemoveAtAndRecover(int removeIndex)
    {
        if (removeIndex < 0 || removeIndex >= weapons.Count) return;

        WeaponInstance removed = weapons[removeIndex];
        WeaponInstance mainRef = (mainIndex >= 0 && mainIndex < weapons.Count) ? weapons[mainIndex] : null;
        WeaponInstance subRef = (subIndex >= 0 && subIndex < weapons.Count) ? weapons[subIndex] : null;

        bool wasMain = (removed != null && mainRef == removed);
        bool wasSub = (removed != null && subRef == removed);

        // --- 総線: 破壊イベント（削除前 index と WeaponItem）---
        UIEvents.OnWeaponDestroyed?.Invoke(removeIndex, removed?.template);

        weapons.RemoveAt(removeIndex);

        if (weapons.Count == 0)
        {
            SetHandIndex(HandType.Main, -1);
            SetHandIndex(HandType.Sub, -1);
            return;
        }

        // 参照で再マップ
        mainIndex = (mainRef != null) ? weapons.IndexOf(mainRef) : -1;
        subIndex = (subRef != null) ? weapons.IndexOf(subRef) : -1;

        if (wasMain)
        {
            if (!TrySwitchRight())
            {
                SetHandIndex(HandType.Main, -1);
            }
        }

        if (wasSub)
        {
            if (!TrySwitchLeft())
            {
                SetHandIndex(HandType.Sub, -1);
            }
        }

        // 念のため最終同一チェック（右手優先で左手を逃がす）
        if (mainIndex >= 0 && subIndex == mainIndex)
        {
            int newSub = FindNextUsable(subIndex, exclude: mainIndex, -1);
            SetHandIndex(HandType.Sub, newSub);
        }
    }*/

    // ==== 耐久消費（破壊時は自動 Remove & Recover）====
    // 仕様変更：武器壊せずに耐久0で放置できるよう
    public void ConsumeDurability(HandType hand, float cost)
    {
        int idx = GetHandIndex(hand);
        if (!IsUsableIndex(idx)) return;

        WeaponInstance inst = weapons[idx];
        int before = inst.currentDurability;
        if (before < cost && cost > 0) return;
        inst.Use(cost);

        // --- 耐久度更新イベント ---
        UIEvents.OnDurabilityChanged?.Invoke(
            hand, idx, inst.currentDurability, inst.template.maxDurability
        );

        /*if (inst.IsBroken)
        {
            // 破壊 → 削除 & 自動切替（右手優先ルールを内部で実行）
            RemoveAtAndRecover(idx);
            // --- 破壊イベント ---
            PlayerEvents.OnWeaponBroke?.Invoke(hand);
        }*/
    }

    public WeaponInstance GetWeapon(HandType hand)
    {
        int idx = GetHandIndex(hand);
        //return IsUsableIndex(idx) ? weapons[idx] : null;
        if(idx> 0 && idx < weapons.Count)
        {
            return weapons[idx];
        }
        else
        {
            return null;
        }
    }
}




