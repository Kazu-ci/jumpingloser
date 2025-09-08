using System;
using System.Collections.Generic;
using UnityEngine;
using static EventBus;
public enum HandType
{
    Main, // �E��
    Sub   // ����
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
    [Tooltip("��������i�C���f�b�N�X��UI��̔ԍ��ƈ�v�j")]
    public List<WeaponInstance> weapons = new List<WeaponInstance>();

    [Tooltip("�E��iMain�j�̌��ݑ����C���f�b�N�X�B-1 �͖�����")]
    public int mainIndex = -1;

    [Tooltip("����iSub�j�̌��ݑ����C���f�b�N�X�B-1 �͖�����")]
    public int subIndex = -1;

    private Dictionary<WeaponType,int> typeToIndex = new Dictionary<WeaponType, int>();

    // ==== �������[�e�B���e�B ====
    private bool IsUsableIndex(int idx)
    {
        return idx >= 0 && idx < weapons.Count && weapons[idx] != null && !weapons[idx].IsBroken;
    }

    // dir: +1 = �O�����ɏ���, -1 = ������ɏ���
    private int FindNextUsable(int startIdx, int exclude, int dir = 1)
    {
        // �� ���S�K�[�h
        int n = weapons.Count;
        if (n == 0) return -1;
        if (dir == 0) dir = +1;

        // startIdx �� [-1, n-1] �ɐ��K���i-1 �́u���̈ʒu�̒��O�v�݂����Ɉ����j
        int start = Mathf.Clamp(startIdx, -1, n - 1);

        // n ��܂Ō��ɂ���
        for (int step = 1; step <= n; ++step)
        {
            // ����C���f�b�N�X�v�Z�i�����␳����j
            int i = (start + step * dir) % n;
            if (i < 0) i += n;

            if (exclude >= 0 && i == exclude) continue;
            if (IsUsableIndex(i)) return i;
        }
        return -1;
    }

    private int GetHandIndex(HandType hand) => (hand == HandType.Main) ? mainIndex : subIndex;

    // ��̃C���f�b�N�X��ݒ肵�AUI �C�x���g�ɓ]��
    private void SetHandIndex(HandType hand, int to)
    {
        int from = GetHandIndex(hand);
        if (hand == HandType.Main) mainIndex = to; else subIndex = to;

        if (from != to)
        {
            // --- ������UIEvents�֒ʒm ---
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

    // ==== �����ؑ� ====
    public bool TrySwitchRight()
    {
        // �E��͍�������O���Ȃ�
        //int next = FindNextUsable(mainIndex, exclude: -1);
        int next = -1;
        if (weapons.Count > 0) next = (mainIndex + 1) % (weapons.Count);

        SetHandIndex(HandType.Main, next);
        if (next == -1) return false;

        // �E��ƍ��肪�����ɂȂ�����A����𓦂����i�E��D��j
        if (subIndex == mainIndex)
        {
            int newSub = FindNextUsable(subIndex, exclude: mainIndex, -1);
            SetHandIndex(HandType.Sub, newSub);
        }
        return true;
    }

    public bool TrySwitchLeft()
    {
        // ����͉E��g�p���̕�����X�L�b�v
        int next = FindNextUsable(subIndex, exclude: mainIndex, -1);

        SetHandIndex(HandType.Sub, next);
        if (next == -1) return false;
        return true;
    }

    public void Unequip(HandType hand)
    {
        SetHandIndex(hand, -1);
    }

    // ==== �����Ǘ� ====
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
            UIEvents.OnRightWeaponSwitch?.Invoke(weapons, mainIndex, mainIndex); // UI�X�V
        }
        
    }

    // �C���x���g������ removeIndex ���폜���A�莝����������
   /* public void RemoveAtAndRecover(int removeIndex)
    {
        if (removeIndex < 0 || removeIndex >= weapons.Count) return;

        WeaponInstance removed = weapons[removeIndex];
        WeaponInstance mainRef = (mainIndex >= 0 && mainIndex < weapons.Count) ? weapons[mainIndex] : null;
        WeaponInstance subRef = (subIndex >= 0 && subIndex < weapons.Count) ? weapons[subIndex] : null;

        bool wasMain = (removed != null && mainRef == removed);
        bool wasSub = (removed != null && subRef == removed);

        // --- ����: �j��C�x���g�i�폜�O index �� WeaponItem�j---
        UIEvents.OnWeaponDestroyed?.Invoke(removeIndex, removed?.template);

        weapons.RemoveAt(removeIndex);

        if (weapons.Count == 0)
        {
            SetHandIndex(HandType.Main, -1);
            SetHandIndex(HandType.Sub, -1);
            return;
        }

        // �Q�Ƃōă}�b�v
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

        // �O�̂��ߍŏI����`�F�b�N�i�E��D��ō���𓦂����j
        if (mainIndex >= 0 && subIndex == mainIndex)
        {
            int newSub = FindNextUsable(subIndex, exclude: mainIndex, -1);
            SetHandIndex(HandType.Sub, newSub);
        }
    }*/

    // ==== �ϋv����i�j�󎞂͎��� Remove & Recover�j====
    // �d�l�ύX�F����󂹂��ɑϋv0�ŕ��u�ł���悤
    public void ConsumeDurability(HandType hand, float cost)
    {
        int idx = GetHandIndex(hand);
        if (!IsUsableIndex(idx)) return;

        WeaponInstance inst = weapons[idx];
        int before = inst.currentDurability;
        if (before < cost && cost > 0) return;
        inst.Use(cost);

        // --- �ϋv�x�X�V�C�x���g ---
        UIEvents.OnDurabilityChanged?.Invoke(
            hand, idx, inst.currentDurability, inst.template.maxDurability
        );

        /*if (inst.IsBroken)
        {
            // �j�� �� �폜 & �����ؑցi�E��D�惋�[��������Ŏ��s�j
            RemoveAtAndRecover(idx);
            // --- �j��C�x���g ---
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




