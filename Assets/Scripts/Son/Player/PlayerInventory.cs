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
        currentDurability = weapon.maxDurability;
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

    // ==== 内部ユーティリティ ====
    private bool IsUsableIndex(int idx)
    {
        return idx >= 0 && idx < weapons.Count && weapons[idx] != null && !weapons[idx].IsBroken;
    }

    // ラウンドロビン検索（exclude を1つだけ除外。exclude < 0 は除外なし）
    private int FindNextUsable(int startIdx, int exclude)
    {
        int n = weapons.Count;
        if (n == 0) return -1;

        int start = Mathf.Clamp(startIdx, -1, n - 1);
        for (int step = 1; step <= n; ++step)
        {
            int i = (start + step) % n;
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
        int next = FindNextUsable(mainIndex, exclude: -1);

        SetHandIndex(HandType.Main, next);
        if (next == -1) return false;

        // 右手と左手が同じになったら、左手を逃がす（右手優先）
        if (subIndex == mainIndex)
        {
            int newSub = FindNextUsable(subIndex, exclude: mainIndex);
            SetHandIndex(HandType.Sub, newSub);
        }
        return true;
    }

    public bool TrySwitchLeft()
    {
        // 左手は右手使用中の武器をスキップ
        int next = FindNextUsable(subIndex, exclude: mainIndex);

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
        weapons.Add(new WeaponInstance(weapon));
        UIEvents.OnRightWeaponSwitch?.Invoke(weapons, mainIndex, mainIndex); // UI更新
    }

    // インベントリから removeIndex を削除し、手持ちを自動回復
    public void RemoveAtAndRecover(int removeIndex)
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
            int newSub = FindNextUsable(subIndex, exclude: mainIndex);
            SetHandIndex(HandType.Sub, newSub);
        }
    }

    // ==== 耐久消費（破壊時は自動 Remove & Recover）====
    public void ConsumeDurability(HandType hand, float cost)
    {
        int idx = GetHandIndex(hand);
        if (!IsUsableIndex(idx)) return;

        WeaponInstance inst = weapons[idx];
        int before = inst.currentDurability;
        inst.Use(cost);

        // --- 耐久度更新イベント ---
        UIEvents.OnDurabilityChanged?.Invoke(
            hand, idx, inst.currentDurability, inst.template.maxDurability
        );

        if (inst.IsBroken)
        {
            // 破壊 → 削除 & 自動切替（右手優先ルールを内部で実行）
            RemoveAtAndRecover(idx);
        }
    }

    public WeaponInstance GetWeapon(HandType hand)
    {
        int idx = GetHandIndex(hand);
        return IsUsableIndex(idx) ? weapons[idx] : null;
    }
}




/*public class PlayerWeaponInventory
{
    public List<WeaponInstance> weapons = new List<WeaponInstance>();
    public int mainIndex = -1; // 現在のメイン武器インデックス
    public int subIndex = -1; // 現在のサブ武器インデックス

    public void AddWeapon(WeaponItem weapon)
    {
        if (weapon != null)
        {
            weapons.Add(new WeaponInstance(weapon));
        }
    }
    public void RemoveWeapon(int index)
    {
        if (index >= 0 && index < weapons.Count)
        {
            weapons.RemoveAt(index);
        }
    }
    public WeaponInstance GetWeapon(HandType handType)
    {
        if (handType == HandType.Main && mainIndex >= 0 && mainIndex < weapons.Count)
        {
            return weapons[mainIndex];
        }
        else if (handType == HandType.Sub && subIndex >= 0 && subIndex < weapons.Count)
        {
            return weapons[subIndex];
        }
        return null; // 武器がない場合
    }

    public int SwitchWeapon(HandType handType)
    {
        if (weapons.Count == 0)
        {
            Debug.LogWarning("No weapons available to switch.");
            return 0; // 武器がない場合は何もしない
        }
        if (handType == HandType.Main)
        {
            mainIndex = (mainIndex + 1) % weapons.Count; // メイン武器を切り替え
            return 1;
        }
        else if (handType == HandType.Sub)
        {
            subIndex = (subIndex + 1) % weapons.Count; // サブ武器を切り替え
            return -1;
        }
        else
        {
            Debug.LogWarning("Invalid hand type specified for weapon switch.");
            return 0; // 無効なハンドタイプの場合は何もしない
        }
    }
    public int SwitchRightWeapon()
    {
        if (weapons.Count == 0)
        {
            Debug.LogWarning("No weapons available to switch.");
            return -1; // 武器がない場合は何もしない
        }
        else
        {
            mainIndex = (mainIndex + 1) % weapons.Count;
            return mainIndex;
        }
    }
    public void UnequipWeapon(HandType handType)
    {
        if (handType == HandType.Main)
        {
            mainIndex = -1; // メイン武器を外す
        }
        else if (handType == HandType.Sub)
        {
            subIndex = -1; // サブ武器を外す
        }
    }
    public void DestroyWeapon(WeaponInstance target)
    {
        int index = GetIndex(target);
        if (index >= 0)
        {
            WeaponInstance main = null;
            WeaponInstance sub = null;
            if (mainIndex != -1 && mainIndex < weapons.Count) main = weapons[mainIndex];
            if (subIndex != -1 && subIndex < weapons.Count) sub = weapons[subIndex];
            // weapons から削除
            weapons.RemoveAt(index);

            if (weapons.Count == 0)
            {
                mainIndex = -1; // 全ての武器が削除された場合、メイン武器のインデックスをリセット
                subIndex = -1; // サブ武器のインデックスもリセット
                return;
            }

            if (mainIndex == index)
            {
                mainIndex %= weapons.Count; // メイン武器のインデックスをリセット
            }
            else if (main != null)
            {
                mainIndex = GetIndex(main); // メイン武器のインデックスを再設定
            }

            if (subIndex == index)
            {
                subIndex %= weapons.Count;
            }
            else if (sub != null)
            {
                subIndex = GetIndex(sub); // サブ武器のインデックスを再設定
            }
        }
    }
    private int GetIndex(WeaponInstance weapon)
    {
        return weapons.IndexOf(weapon);
    }
}*/