using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using static EventBus;
using TMPro;
using System.Collections;

public class TempUI : MonoBehaviour
{
    [Header("�E��UI�i�T���v���j")]
    public TextMeshProUGUI weaponDurableText; // ���݉E�葕���̑ϋv�����\��
    public float weaponIconLength = 100f;     // �ׂ̃A�C�R���܂ł̃��[�J��X����
    public GameObject weaponPrefab;           // 1�̕���A�C�R��UI�v���n�u�iImage��z��j
    public GameObject weaponList;             // �E���̋�I�u�W�F�N�g�i�S�A�C�R���̐e�j

    public GameObject dashEnableIcon;       // �_�b�V���\�A�C�R��
    public GameObject dashDisableIcon;

    public GameObject AimIcon;
    private GameObject lockTarget = null;

    private Coroutine slideCo;

    // �E�茻�݃C���f�b�N�X
    private int currentRightIndex = -1;
    // ���߂̏������탊�X�g
    private List<WeaponInstance> lastWeaponsRef;

    private void OnEnable()
    {
        UIEvents.OnRightWeaponSwitch += ChangeRightWeapon;
        UIEvents.OnDurabilityChanged += OnDurabilityChanged;   // �茳�̑ϋvUI���f
        UIEvents.OnWeaponDestroyed += OnWeaponDestroyed;     // ���O�̂�

        PlayerEvents.OnAimTargetChanged += SwitchLockIcon;
        UIEvents.OnDashUIChange += SwitchDashIcon;

        if (AimIcon != null)
            AimIcon.SetActive(false);
        TryRenderFistOnly();
    }

    private void OnDisable()
    {
        UIEvents.OnRightWeaponSwitch -= ChangeRightWeapon;
        UIEvents.OnDurabilityChanged -= OnDurabilityChanged;
        UIEvents.OnWeaponDestroyed -= OnWeaponDestroyed;
        PlayerEvents.OnAimTargetChanged -= SwitchLockIcon;
        UIEvents.OnDashUIChange -= SwitchDashIcon;
    }

    private void Update()
    {
        if(lockTarget != null && AimIcon != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(lockTarget.transform.position + Vector3.up * -0f);
            AimIcon.transform.position = screenPos;
        }

    }
    // ���b�N�I���A�C�R���\��
    private void SwitchLockIcon(GameObject target)
    {
        lockTarget = target;
        if(lockTarget != null)
        {
            if (AimIcon != null)
                AimIcon.SetActive(true);
        }
        else
        {
            if (AimIcon != null)
                AimIcon.SetActive(false);
        }
    }
    private void SwitchDashIcon(bool enable)
    {
        if(dashEnableIcon != null)
            dashEnableIcon.SetActive(enable);
        if (dashDisableIcon != null)
            dashDisableIcon.SetActive(!enable);
    }
    // ====== �����\���i�f��̂݁j======
    private void TryRenderFistOnly()
    {
        if (weaponList == null || weaponPrefab == null) return;

        var listRT = weaponList.transform as RectTransform;
        if (listRT == null) return;

        // �����A�C�R���N���A�i���S�̂��߁j
        for (int i = listRT.childCount - 1; i >= 0; --i)
            Destroy(listRT.GetChild(i).gameObject);

        // �f��A�C�R���iindex = -1�j
        var go = Instantiate(weaponPrefab, weaponList.transform);
        go.name = "Icon_-1_Fist";
        var rt = go.transform as RectTransform;
        if (rt != null)
        {
            // ���S�� -1�i�f��j�ɍ��킹��BX=0 �������B
            rt.anchoredPosition = new Vector2(0f, 0f);
        }

        // �ϋv�́u���v
        if (weaponDurableText != null)
            weaponDurableText.text = "��";

        // ���݉E��C���f�b�N�X = �f��
        currentRightIndex = -1;
    }

    // ====== �ؑփn���h���i�E��T���v���j======
    private void ChangeRightWeapon(List<WeaponInstance> weapons, int from, int to)
    {
        // --- �Q�ƃK�[�h ---
        if (weaponList == null) { Debug.LogWarning("TempUI: No UI weaponList"); return; }
        if (weaponPrefab == null) { Debug.LogWarning("TempUI: No UI weaponPrefab"); return; }

        lastWeaponsRef = weapons; // �Q�ƕێ�

        RectTransform listRT = weaponList.transform as RectTransform;
        if (listRT == null) { Debug.LogWarning("TempUI: weaponList is not RectTransform"); return; }

        // �����A�C�R���N���A
        for (int i = listRT.childCount - 1; i >= 0; --i)
            Destroy(listRT.GetChild(i).gameObject);

        // �q�X���C�h�p�̎��W�e�[�u��
        var childRTs = new List<RectTransform>();
        var fromXs = new List<float>();
        var toXs = new List<float>();

        // --- �f��A�C�R���ivirtual index = -1�j---
        if (weapons == null || weapons.Count == 0 || to < 0) // �f����̂�
        {
            GameObject go = Instantiate(weaponPrefab, weaponList.transform);
            go.name = "Icon_-1_Fist";
            RectTransform rt = go.transform as RectTransform;
            if (rt != null)
            {
                float sx = IndexToLinear(-1, from) * weaponIconLength; // start x
                float ex = IndexToLinear(-1, to) * weaponIconLength;   // end   x
                rt.anchoredPosition = new Vector2(sx, 0f);

                childRTs.Add(rt);
                fromXs.Add(sx);
                toXs.Add(ex);
            }
        }

        // --- ����A�C�R������ ---
        for (int i = 0; i < weapons.Count; i++)
        {
            GameObject go = Instantiate(weaponPrefab, weaponList.transform);
            go.name = $"Icon_{i}";
            RectTransform rt = go.transform as RectTransform;
            if (rt != null)
            {
                float sx = IndexToLinear(i, from) * weaponIconLength;
                float ex = IndexToLinear(i, to) * weaponIconLength;
                rt.anchoredPosition = new Vector2(sx, 0f);

                childRTs.Add(rt);
                fromXs.Add(sx);
                toXs.Add(ex);
            }

            var img = go.GetComponent<Image>();
            var spr = weapons[i]?.template?.icon;
            if (img != null) img.sprite = spr;
        }

        // �ϋv�\���ifrom �𒆐S�j
        UpdateRightDurabilityByIndex(weapons, from);

        // --- �q�I�u�W�F�N�g���X���C�h�i�e�͓������Ȃ��j---
        if (slideCo != null) StopCoroutine(slideCo);
        slideCo = StartCoroutine(SlideChildrenX(childRTs, fromXs, toXs, 0.2f, () =>
        {
            currentRightIndex = to;
            if (to < 0)
            {
                if (weaponDurableText != null) weaponDurableText.text = "��";
            }
            else
            {
                UpdateRightDurabilityByIndex(weapons, to);
            }
        }));
    }

    // ====== �ϋv�ω��i�E��j======
    private void OnDurabilityChanged(HandType hand, int index, int current, int max)
    {
        // �E��ŁA���� UI ���ǐՒ��̃C���f�b�N�X�̂Ƃ��̂ݍX�V
        if (hand != HandType.Main) return;
        if (index != currentRightIndex) return;
        if (weaponDurableText == null) return;

        // �E��̑������ς���Ă��Ȃ��Ȃ琔�l�����X�V
        weaponDurableText.text = $"{current}/{max}";
    }

    // ====== �j��ʒm�i���O�̂݁j======
    private void OnWeaponDestroyed(int removedIndex, WeaponItem item)
    {
        // �ERemoveAtAndRecover ���� SetHandIndex ���������߁A
        // �E�����ł͉��o�g���K�������Ă��悢�i�_��/SE���j�B
        Debug.Log($"TempUI: Weapon destroyed @index={removedIndex} ({item?.weaponName})");
    }

    // ====== �C���f�b�N�X�������ʒu�ւ̕ϊ� ======
    private int IndexToLinear(int idx, int centerIndex)
    {
        // �EcenterIndex �� 0 �Ƃ��đ��Ή����AX �ʒu = linear * weaponIconLength�B
        int linear = (idx < 0) ? 0 : (idx + 1);
        int center = (centerIndex < 0) ? 0 : (centerIndex + 1);
        return linear - center;
    }

    // ====== �R���[�`���F�X���C�h ======
    private IEnumerator SlideChildrenX(
        List<RectTransform> items,
        List<float> fromXs,
        List<float> toXs,
        float duration,
        System.Action onDone = null)
    {
        // �E�S�ă��[�J��UI���W�ianchoredPosition�j�ŏ����B
        // �ETimeScale=0 �ł����������� unscaledDeltaTime ���g�p�B
        int count = Mathf.Min(items.Count, Mathf.Min(fromXs.Count, toXs.Count));
        if (count <= 0) { onDone?.Invoke(); yield break; }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);

            for (int i = 0; i < count; i++)
            {
                var rt = items[i];
                if (rt == null) continue;

                float x = Mathf.Lerp(fromXs[i], toXs[i], k);
                var ap = rt.anchoredPosition;
                rt.anchoredPosition = new Vector2(x, ap.y);
            }
            yield return null;
        }

        // �I�[�Ō덷���z��
        for (int i = 0; i < count; i++)
        {
            var rt = items[i];
            if (rt == null) continue;
            var ap = rt.anchoredPosition;
            rt.anchoredPosition = new Vector2(toXs[i], ap.y);
        }

        onDone?.Invoke();
    }

    // ====== �E��̑ϋv�e�L�X�g ======
    private void UpdateRightDurabilityByIndex(List<WeaponInstance> weapons, int idx)
    {
        if (weaponDurableText == null) return;

        if (idx >= 0 && idx < (weapons?.Count ?? 0) && weapons[idx] != null)
        {
            var w = weapons[idx];
            weaponDurableText.text = $"{w.currentDurability}/{w.template.maxDurability}";
        }
        else
        {
            // �f��Ȃ�
            weaponDurableText.text = "��";
        }
    }
}
