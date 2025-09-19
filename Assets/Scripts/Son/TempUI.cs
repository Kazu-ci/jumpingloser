using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using static EventBus;
using TMPro;
using System.Collections;

public class TempUI : MonoBehaviour
{
    [Header("右手UI（サンプル）")]
    // ※ 旧: public TextMeshProUGUI weaponDurableText; // テキスト表示は廃止
    public float weaponIconLength = 100f;     // 隣のアイコンまでのローカルX距離
    public GameObject weaponPrefab;           // 1個の武器アイコンUIプレハブ（Imageを想定）
    public GameObject weaponList;             // 右下の空オブジェクト（全アイコンの親）

    public Sprite weaponDurMaxImg;            // 耐久MAX時のスプライト
    public Sprite weaponDurCurImg;            // 耐久通常時のスプライト

    [Header("ダッシュUI")]
    public GameObject dashEnableIcon;       // ダッシュ可能アイコン
    public GameObject dashDisableIcon;

    [Header("HPUI")]
    public GameObject HPUIFront;       // HPUI前景
    public float HPUILength = 500f;    // HPUIの長さ

    [Header("ロックオンUI")]
    public GameObject AimIcon;
    private Transform lockTarget = null;

    [Header("Menu UI")]
    public GameObject mapObj;
    public GameObject tutorial;
    private bool isMenuOpen = false;

    public InputSystem_Actions inputActions;

    private Coroutine slideCo;

    // 右手現在インデックス（中央位置の計算にのみ使用）
    private int currentRightIndex = -1;

    // 直近の所持武器リスト参照
    private List<WeaponInstance> lastWeaponsRef;

    // 生成した武器アイコンの参照（index -> アイコン）
    // ・素手(-1)は保持しない（耐久表示が不要のため）
    private readonly Dictionary<int, TempUI_WeaponIcon> rightIconByIndex = new Dictionary<int, TempUI_WeaponIcon>(16);

    private void OnEnable()
    {
        UIEvents.OnRightWeaponSwitch += ChangeRightWeapon;
        UIEvents.OnDurabilityChanged += OnDurabilityChanged;   // 個別武器の耐久UI反映
        //UIEvents.OnWeaponDestroyed += OnWeaponDestroyed;     // 必要なら演出トリガ

        UIEvents.OnAimPointChanged += SwitchLockIcon;
        UIEvents.OnDashUIChange += SwitchDashIcon;
        UIEvents.OnPlayerHpChange += SetHpBar;

        UIEvents.OnAttackHoldProgress += OnAttacHolding;
        UIEvents.OnAttackHoldUI += OnAttackHoldChange;
        UIEvents.OnAttackHoldDenied += OnAttackHoldDenied;


        if (inputActions == null)
        {
            inputActions = new InputSystem_Actions();
            inputActions.UI.Enable();
            inputActions.UI.Menu.performed += ctx => SwitchMenu();
        }

        if (AimIcon != null)
            AimIcon.SetActive(false);
        ClearRightIcons();
    }

    private void OnDisable()
    {
        UIEvents.OnRightWeaponSwitch -= ChangeRightWeapon;
        UIEvents.OnDurabilityChanged -= OnDurabilityChanged;
        //UIEvents.OnWeaponDestroyed -= OnWeaponDestroyed;
        UIEvents.OnAimPointChanged -= SwitchLockIcon;
        UIEvents.OnDashUIChange -= SwitchDashIcon;

        UIEvents.OnPlayerHpChange -= SetHpBar;
        UIEvents.OnAttackHoldProgress -= OnAttacHolding;
        UIEvents.OnAttackHoldUI -= OnAttackHoldChange;
        UIEvents.OnAttackHoldDenied -= OnAttackHoldDenied;

        if (inputActions != null)
        {
            inputActions.UI.Menu.performed -= ctx => SwitchMenu();
            inputActions.UI.Disable();
            inputActions = null;
        }
        if (slideCo != null) StopCoroutine(slideCo);
        if (isMenuOpen)
        {
            SystemEvents.OnGameResume?.Invoke();
            isMenuOpen = false;
        }
        
    }

    private void Update()
    {
        // --- ロックオンアイコンのスクリーン追従 ---
        if (lockTarget != null && AimIcon != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(lockTarget.position + Vector3.up * -0f);
            AimIcon.transform.position = screenPos;
        }
    }

    private void SwitchMenu()
    {
        if (!isMenuOpen)
        {
            if (mapObj != null) mapObj.SetActive(true);
            if (tutorial != null) tutorial.SetActive(true);
            isMenuOpen = true;
            SystemEvents.OnGamePause?.Invoke();
        }
        else
        {
            if (mapObj != null) mapObj.SetActive(false);
            if (tutorial != null) tutorial.SetActive(false);
            isMenuOpen = false;
            SystemEvents.OnGameResume?.Invoke();
        }
    }

    // ===== ロックオンアイコン表示 =====
    private void SwitchLockIcon(Transform target)
    {
        lockTarget = target;
        if (AimIcon != null)
            AimIcon.SetActive(lockTarget != null);
    }

    private void SwitchDashIcon(bool enable)
    {
        if (dashEnableIcon != null) dashEnableIcon.SetActive(enable);
        if (dashDisableIcon != null) dashDisableIcon.SetActive(!enable);
    }

    private void SetHpBar(int amount, int max)
    {
        if (HPUIFront != null)
        {
            var rt = HPUIFront.transform as RectTransform;
            if (rt != null)
            {
                float len = HPUILength * ((float)amount / (float)max);
                rt.sizeDelta = new Vector2(len, rt.sizeDelta.y);
            }
        }
    }
    private void OnAttacHolding(float amount)
    {
        var weapon = rightIconByIndex.TryGetValue(currentRightIndex, out var icon) ? icon : null;
        if(weapon == null || weapon.skillImgObj == null) return;
        weapon.skillImgObj.enabled = true;
        weapon.skillImgObj.fillAmount = amount;
    }
        
    private void OnAttackHoldChange(bool isHolding)
    {
        var weapon = rightIconByIndex.TryGetValue(currentRightIndex, out var icon) ? icon : null;
        if (weapon == null || weapon.skillImgObj == null) return;
        weapon.skillImgObj.enabled = isHolding;
        if (!isHolding)
            weapon.skillImgObj.fillAmount = 0f;
    }
    private void OnAttackHoldDenied()
    {
        var weapon = rightIconByIndex.TryGetValue(currentRightIndex, out var icon) ? icon : null;
        if (weapon == null || weapon.skillImgObj == null) return;
        Color ori = weapon.skillImgObj.color;
        weapon.skillImgObj.color = Color.red;
        StartCoroutine(ResetColor(weapon.skillImgObj, ori, 0.2f));
    }

    private IEnumerator ResetColor(Image img, Color to, float duration)
    {
        if (img == null) yield break;
        Color from = img.color;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            img.color = Color.Lerp(from, to, k);
            yield return null;
        }
        img.color = to;
        img.enabled = false;
    }
    // ===== 初期表示（素手のみ）=====
    private void TryRenderFistOnly()
    {
        if (weaponList == null || weaponPrefab == null) return;

        var listRT = weaponList.transform as RectTransform;
        if (listRT == null) return;

        // 既存アイコンクリア
        for (int i = listRT.childCount - 1; i >= 0; --i)
            Destroy(listRT.GetChild(i).gameObject);
        rightIconByIndex.Clear();

        // 素手アイコン（index = -1）
        var go = Instantiate(weaponPrefab, weaponList.transform);
        go.name = "Icon_-1_Fist";
        var rt = go.transform as RectTransform;
        if (rt != null)
        {
            // 中心を -1（素手）に合わせる。X=0 が中央。
            rt.anchoredPosition = new Vector2(0f, 0f);
        }

        // 素手は耐久表示なし：TempUI_WeaponIcon が付いているならゲージを非表示
        var fistIcon = go.GetComponent<TempUI_WeaponIcon>();
        if (fistIcon != null && fistIcon.durImgObj != null)
        {
            fistIcon.durImgObj.fillAmount = 0f;
            fistIcon.durImgObj.enabled = false;
        }
        if (fistIcon != null && fistIcon.skillImgObj != null)
        {
            fistIcon.skillImgObj.fillAmount = 0f;
        }

        // 現在右手インデックス = 素手
        currentRightIndex = -1;
    }

    // ===== 切替ハンドラ（右手）=====
    private void ChangeRightWeapon(List<WeaponInstance> weapons, int from, int to)
    {
        // --- 参照ガード ---
        if (weaponList == null) { Debug.LogWarning("TempUI: No UI weaponList"); return; }
        if (weaponPrefab == null) { Debug.LogWarning("TempUI: No UI weaponPrefab"); return; }

        lastWeaponsRef = weapons; // 参照保持

        RectTransform listRT = weaponList.transform as RectTransform;
        if (listRT == null) { Debug.LogWarning("TempUI: weaponList is not RectTransform"); return; }

        // 既存アイコン全削除
        for (int i = listRT.childCount - 1; i >= 0; --i)
            Destroy(listRT.GetChild(i).gameObject);
        rightIconByIndex.Clear();

        // 空の場合は何も出さず終了
        if (weapons == null || weapons.Count == 0)
        {
            currentRightIndex = -1;
            return;
        }

        // スライド用収集
        var childRTs = new List<RectTransform>();
        var fromXs = new List<float>();
        var toXs = new List<float>();

        // --- 武器アイコン群（index: 0..Count-1）---
        for (int i = 0; i < weapons.Count; i++)
        {
            GameObject go = Instantiate(weaponPrefab, weaponList.transform);
            go.name = $"Icon_{i}";
            RectTransform rt = go.transform as RectTransform;
            if (rt != null)
            {
                // ★ 中央は center = from / to をそのまま使用する（素手オフセット無し）
                float sx = IndexToLinear(i, from) * weaponIconLength;
                float ex = IndexToLinear(i, to) * weaponIconLength;
                rt.anchoredPosition = new Vector2(sx, 0f);

                childRTs.Add(rt);
                fromXs.Add(sx);
                toXs.Add(ex);
            }

            // メインのアイコン画像
            var img = go.GetComponent<Image>();
            var spr = weapons[i]?.template?.icon;
            if (img != null) img.sprite = spr;

            // 耐久/スキルUIの初期化
            var weaponIcon = go.GetComponent<TempUI_WeaponIcon>();
            if (weaponIcon != null)
            {
                // --- 耐久バー初期化 ---
                if (weaponIcon.durImgObj != null)
                {
                    int cur = Mathf.Max(0, weapons[i].currentDurability);
                    int max = Mathf.Max(1, weapons[i].template.maxDurability);
                    float ratio = (float)cur / (float)max;

                    weaponIcon.durImgObj.fillAmount = ratio;
                    weaponIcon.durImgObj.enabled = true;
                    weaponIcon.durImgObj.sprite = (cur >= max) ? weaponDurMaxImg : weaponDurCurImg;
                }

                // --- スキルゲージ初期化（0%） ---
                if (weaponIcon.skillImgObj != null)
                    weaponIcon.skillImgObj.fillAmount = 0f;

                // インデックス→アイコン参照
                rightIconByIndex[i] = weaponIcon;
            }
        }

        // --- 子オブジェクトをスライド（親は固定）---
        if (slideCo != null) StopCoroutine(slideCo);
        slideCo = StartCoroutine(SlideChildrenX(childRTs, fromXs, toXs, 0.2f, () =>
        {
            currentRightIndex = to;
            RefreshAllRightDurability(); // 念のため同期
        }));
    }

    // ===== 個別耐久変化（右手）=====
    private void OnDurabilityChanged(HandType hand, int index, int current, int max)
    {
        // ・右手のみ反映
        if (hand != HandType.Main) return;

        // ・生成済みの該当インデックスのアイコンがあれば更新
        if (rightIconByIndex.TryGetValue(index, out var icon) && icon != null && icon.durImgObj != null)
        {
            // --- fillAmount を耐久割合に更新 ---
            int safeMax = Mathf.Max(1, max);
            int safeCur = Mathf.Clamp(current, 0, safeMax);
            float ratio = (float)safeCur / (float)safeMax;

            icon.durImgObj.fillAmount = ratio;
            icon.durImgObj.enabled = true;
            icon.durImgObj.sprite = (safeCur >= safeMax) ? weaponDurMaxImg : weaponDurCurImg;
        }
        // ・見つからない場合は全体更新でフォールバック（例えばスロット構成変化直後）
        else
        {
            RefreshAllRightDurability();
        }
    }

    // ===== 破壊通知（必要なら演出用）=====
    private void OnWeaponDestroyed(int removedIndex, WeaponItem item)
    {
        // ・ここでは演出トリガ等を入れてもよい（点滅/SE等）
        Debug.Log($"TempUI: Weapon destroyed @index={removedIndex} ({item?.weaponName})");
    }

    // ===== インデックス→直線位置への変換 =====
    private int IndexToLinear(int idx, int centerIndex)
    {
        // 例：centerIndex=2 の場合、idx=2 が 0（中央）、idx=1 が -1、idx=3 が +1
        return idx - centerIndex;
    }

    // ===== コルーチン：スライド =====
    private IEnumerator SlideChildrenX(
        List<RectTransform> items,
        List<float> fromXs,
        List<float> toXs,
        float duration,
        System.Action onDone = null)
    {
        int count = Mathf.Min(items.Count, Mathf.Min(fromXs.Count, toXs.Count));
        if (count <= 0) { onDone?.Invoke(); yield break; }

        float t = 0f;
        while (t < duration)
        {
            // --- Time.timeScale=0 でも動作させるため unscaledDeltaTime を使用 ---
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

        // --- 終端で位置誤差を吸収 ---
        for (int i = 0; i < count; i++)
        {
            var rt = items[i];
            if (rt == null) continue;
            var ap = rt.anchoredPosition;
            rt.anchoredPosition = new Vector2(toXs[i], ap.y);
        }

        onDone?.Invoke();
    }

    // ===== 全アイコンの耐久を所持データから再同期 =====
    private void RefreshAllRightDurability()
    {
        // ・所持リストが無ければ何もしない
        if (lastWeaponsRef == null) return;

        for (int i = 0; i < lastWeaponsRef.Count; i++)
        {
            if (!rightIconByIndex.TryGetValue(i, out var icon) || icon == null || icon.durImgObj == null)
                continue;

            int cur = Mathf.Max(0, lastWeaponsRef[i].currentDurability);
            int max = Mathf.Max(1, lastWeaponsRef[i].template.maxDurability);
            float ratio = (float)cur / (float)max;

            icon.durImgObj.fillAmount = ratio;
            icon.durImgObj.enabled = true;
            icon.durImgObj.sprite = (cur >= max) ? weaponDurMaxImg : weaponDurCurImg;
        }
    }
    private void ClearRightIcons()
    {
        if (weaponList == null) return;

        // 子を全破棄
        var listRT = weaponList.transform as RectTransform;
        if (listRT == null) return;

        for (int i = listRT.childCount - 1; i >= 0; --i)
            Destroy(listRT.GetChild(i).gameObject);

        rightIconByIndex.Clear();

        // 現在インデックス
        currentRightIndex = -1;
    }
}
