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
    [Tooltip("��������i�C���f�b�N�X��UI��̔ԍ��ƈ�v�j")]
    public List<WeaponInstance> weapons = new List<WeaponInstance>();

    [Tooltip("�E��iMain�j�̌��ݑ����C���f�b�N�X�B-1 �͖�����")]
    public int mainIndex = -1;

    [Tooltip("����iSub�j�̌��ݑ����C���f�b�N�X�B-1 �͖�����")]
    public int subIndex = -1;

    // ==== �������[�e�B���e�B ====
    private bool IsUsableIndex(int idx)
    {
        return idx >= 0 && idx < weapons.Count && weapons[idx] != null && !weapons[idx].IsBroken;
    }

    // ���E���h���r�������iexclude ��1�������O�Bexclude < 0 �͏��O�Ȃ��j
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
        int next = FindNextUsable(mainIndex, exclude: -1);

        SetHandIndex(HandType.Main, next);
        if (next == -1) return false;

        // �E��ƍ��肪�����ɂȂ�����A����𓦂����i�E��D��j
        if (subIndex == mainIndex)
        {
            int newSub = FindNextUsable(subIndex, exclude: mainIndex);
            SetHandIndex(HandType.Sub, newSub);
        }
        return true;
    }

    public bool TrySwitchLeft()
    {
        // ����͉E��g�p���̕�����X�L�b�v
        int next = FindNextUsable(subIndex, exclude: mainIndex);

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
        weapons.Add(new WeaponInstance(weapon));
        UIEvents.OnRightWeaponSwitch?.Invoke(weapons, mainIndex, mainIndex); // UI�X�V
    }

    // �C���x���g������ removeIndex ���폜���A�莝����������
    public void RemoveAtAndRecover(int removeIndex)
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
            int newSub = FindNextUsable(subIndex, exclude: mainIndex);
            SetHandIndex(HandType.Sub, newSub);
        }
    }

    // ==== �ϋv����i�j�󎞂͎��� Remove & Recover�j====
    public void ConsumeDurability(HandType hand, float cost)
    {
        int idx = GetHandIndex(hand);
        if (!IsUsableIndex(idx)) return;

        WeaponInstance inst = weapons[idx];
        int before = inst.currentDurability;
        inst.Use(cost);

        // --- �ϋv�x�X�V�C�x���g ---
        UIEvents.OnDurabilityChanged?.Invoke(
            hand, idx, inst.currentDurability, inst.template.maxDurability
        );

        if (inst.IsBroken)
        {
            // �j�� �� �폜 & �����ؑցi�E��D�惋�[��������Ŏ��s�j
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
    public int mainIndex = -1; // ���݂̃��C������C���f�b�N�X
    public int subIndex = -1; // ���݂̃T�u����C���f�b�N�X

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
        return null; // ���킪�Ȃ��ꍇ
    }

    public int SwitchWeapon(HandType handType)
    {
        if (weapons.Count == 0)
        {
            Debug.LogWarning("No weapons available to switch.");
            return 0; // ���킪�Ȃ��ꍇ�͉������Ȃ�
        }
        if (handType == HandType.Main)
        {
            mainIndex = (mainIndex + 1) % weapons.Count; // ���C�������؂�ւ�
            return 1;
        }
        else if (handType == HandType.Sub)
        {
            subIndex = (subIndex + 1) % weapons.Count; // �T�u�����؂�ւ�
            return -1;
        }
        else
        {
            Debug.LogWarning("Invalid hand type specified for weapon switch.");
            return 0; // �����ȃn���h�^�C�v�̏ꍇ�͉������Ȃ�
        }
    }
    public int SwitchRightWeapon()
    {
        if (weapons.Count == 0)
        {
            Debug.LogWarning("No weapons available to switch.");
            return -1; // ���킪�Ȃ��ꍇ�͉������Ȃ�
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
            mainIndex = -1; // ���C��������O��
        }
        else if (handType == HandType.Sub)
        {
            subIndex = -1; // �T�u������O��
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
            // weapons ����폜
            weapons.RemoveAt(index);

            if (weapons.Count == 0)
            {
                mainIndex = -1; // �S�Ă̕��킪�폜���ꂽ�ꍇ�A���C������̃C���f�b�N�X�����Z�b�g
                subIndex = -1; // �T�u����̃C���f�b�N�X�����Z�b�g
                return;
            }

            if (mainIndex == index)
            {
                mainIndex %= weapons.Count; // ���C������̃C���f�b�N�X�����Z�b�g
            }
            else if (main != null)
            {
                mainIndex = GetIndex(main); // ���C������̃C���f�b�N�X���Đݒ�
            }

            if (subIndex == index)
            {
                subIndex %= weapons.Count;
            }
            else if (sub != null)
            {
                subIndex = GetIndex(sub); // �T�u����̃C���f�b�N�X���Đݒ�
            }
        }
    }
    private int GetIndex(WeaponInstance weapon)
    {
        return weapons.IndexOf(weapon);
    }
}*/