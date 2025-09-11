using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using static EventBus;
using TMPro;
using System.Collections;

public class TempUI : MonoBehaviour
{
    [Header("右手UI（サンプル）")]
    public TextMeshProUGUI weaponDurableText; // 現在右手装備の耐久だけ表示
    public float weaponIconLength = 100f;     // 隣のアイコンまでのローカルX距離
    public GameObject weaponPrefab;           // 1個の武器アイコンUIプレハブ（Imageを想定）
    public GameObject weaponList;             // 右下の空オブジェクト（全アイコンの親）

    public GameObject dashEnableIcon;       // ダッシュ可能アイコン
    public GameObject dashDisableIcon;

    public GameObject AimIcon;
    private GameObject lockTarget = null;

    private Coroutine slideCo;

    // 右手現在インデックス
    private int currentRightIndex = -1;
    // 直近の所持武器リスト
    private List<WeaponInstance> lastWeaponsRef;

    private void OnEnable()
    {
        UIEvents.OnRightWeaponSwitch += ChangeRightWeapon;
        UIEvents.OnDurabilityChanged += OnDurabilityChanged;   // 手元の耐久UI反映
        UIEvents.OnWeaponDestroyed += OnWeaponDestroyed;     // ログのみ

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
    // ロックオンアイコン表示
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
    // ====== 初期表示（素手のみ）======
    private void TryRenderFistOnly()
    {
        if (weaponList == null || weaponPrefab == null) return;

        var listRT = weaponList.transform as RectTransform;
        if (listRT == null) return;

        // 既存アイコンクリア（安全のため）
        for (int i = listRT.childCount - 1; i >= 0; --i)
            Destroy(listRT.GetChild(i).gameObject);

        // 素手アイコン（index = -1）
        var go = Instantiate(weaponPrefab, weaponList.transform);
        go.name = "Icon_-1_Fist";
        var rt = go.transform as RectTransform;
        if (rt != null)
        {
            // 中心を -1（素手）に合わせる。X=0 が中央。
            rt.anchoredPosition = new Vector2(0f, 0f);
        }

        // 耐久は「∞」
        if (weaponDurableText != null)
            weaponDurableText.text = "∞";

        // 現在右手インデックス = 素手
        currentRightIndex = -1;
    }

    // ====== 切替ハンドラ（右手サンプル）======
    private void ChangeRightWeapon(List<WeaponInstance> weapons, int from, int to)
    {
        // --- 参照ガード ---
        if (weaponList == null) { Debug.LogWarning("TempUI: No UI weaponList"); return; }
        if (weaponPrefab == null) { Debug.LogWarning("TempUI: No UI weaponPrefab"); return; }

        lastWeaponsRef = weapons; // 参照保持

        RectTransform listRT = weaponList.transform as RectTransform;
        if (listRT == null) { Debug.LogWarning("TempUI: weaponList is not RectTransform"); return; }

        // 既存アイコンクリア
        for (int i = listRT.childCount - 1; i >= 0; --i)
            Destroy(listRT.GetChild(i).gameObject);

        // 子スライド用の収集テーブル
        var childRTs = new List<RectTransform>();
        var fromXs = new List<float>();
        var toXs = new List<float>();

        // --- 素手アイコン（virtual index = -1）---
        if (weapons == null || weapons.Count == 0 || to < 0) // 素手や空のみ
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

        // --- 武器アイコン生成 ---
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

        // 耐久表示（from を中心）
        UpdateRightDurabilityByIndex(weapons, from);

        // --- 子オブジェクトをスライド（親は動かさない）---
        if (slideCo != null) StopCoroutine(slideCo);
        slideCo = StartCoroutine(SlideChildrenX(childRTs, fromXs, toXs, 0.2f, () =>
        {
            currentRightIndex = to;
            if (to < 0)
            {
                if (weaponDurableText != null) weaponDurableText.text = "∞";
            }
            else
            {
                UpdateRightDurabilityByIndex(weapons, to);
            }
        }));
    }

    // ====== 耐久変化（右手）======
    private void OnDurabilityChanged(HandType hand, int index, int current, int max)
    {
        // 右手で、かつ UI が追跡中のインデックスのときのみ更新
        if (hand != HandType.Main) return;
        if (index != currentRightIndex) return;
        if (weaponDurableText == null) return;

        // 右手の装備が変わっていないなら数値だけ更新
        weaponDurableText.text = $"{current}/{max}";
    }

    // ====== 破壊通知（ログのみ）======
    private void OnWeaponDestroyed(int removedIndex, WeaponItem item)
    {
        // ・RemoveAtAndRecover 内で SetHandIndex が動くため、
        // ・ここでは演出トリガ等を入れてもよい（点滅/SE等）。
        Debug.Log($"TempUI: Weapon destroyed @index={removedIndex} ({item?.weaponName})");
    }

    // ====== インデックス→直線位置への変換 ======
    private int IndexToLinear(int idx, int centerIndex)
    {
        // ・centerIndex を 0 として相対化し、X 位置 = linear * weaponIconLength。
        int linear = (idx < 0) ? 0 : (idx + 1);
        int center = (centerIndex < 0) ? 0 : (centerIndex + 1);
        return linear - center;
    }

    // ====== コルーチン：スライド ======
    private IEnumerator SlideChildrenX(
        List<RectTransform> items,
        List<float> fromXs,
        List<float> toXs,
        float duration,
        System.Action onDone = null)
    {
        // ・全てローカルUI座標（anchoredPosition）で処理。
        // ・TimeScale=0 でも動かすため unscaledDeltaTime を使用。
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

        // 終端で誤差を吸収
        for (int i = 0; i < count; i++)
        {
            var rt = items[i];
            if (rt == null) continue;
            var ap = rt.anchoredPosition;
            rt.anchoredPosition = new Vector2(toXs[i], ap.y);
        }

        onDone?.Invoke();
    }

    // ====== 右手の耐久テキスト ======
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
            // 素手など
            weaponDurableText.text = "∞";
        }
    }
}
