using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;


[RequireComponent(typeof(Image))]
public class UIButtonEffect_Unscaled : MonoBehaviour,
    ISelectHandler, IDeselectHandler,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler,
    ISubmitHandler
{
    // === 参照 ===
    public Image targetImage;

    // === 元カラー保持 ===
    public Color originalColor;

    // === 状態フラグ ===
    private bool isSelected = false; // UI上で選択されているか
    private bool isBlinking = false; // 点滅を行うか
    private bool isPressed = false;  // 押下中（マウス）か

    [Header("点滅設定（TimeScale無視）")]
    [Tooltip("点滅時の最小アルファ値")]
    [Range(0f, 1f)] public float minAlpha = 0.01f;
    [Tooltip("点滅速度（Hz）")]
    public float blinkSpeedHz = 1f;

    [Header("押下時の暗さ係数")]
    [Tooltip("RGBに乗算する係数。0=真っ黒, 1=変化なし")]
    [Range(0f, 1f)] public float darkenFactor = 0.6f;

    private void Start()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
            originalColor = targetImage.color;
        }
    }

    private void OnEnable()
    {
        // 念のため初期化
        ResetVisualImmediate();
    }

    private void OnDisable()
    {
        // 無効化時に見た目を戻す
        ResetVisualImmediate();
    }

    private void Update()
    {
        // === 点滅は Time.unscaledTime を使用 ===
        if (isBlinking && !isPressed)
        {
            float t = (Mathf.Sin(Time.unscaledTime * blinkSpeedHz * 2f * Mathf.PI) + 1f) * 0.5f;

            float targetA = Mathf.Lerp(minAlpha, originalColor.a, t);

            Color c = targetImage.color;
            c.a = targetA;
            targetImage.color = c;
        }
    }

    // === 見た目を即座に元へ ===
    private void ResetVisualImmediate()
    {
        isBlinking = false;
        isPressed = false;
        targetImage.color = originalColor;
    }

    // === 選択された ===
    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
        if (!isPressed) isBlinking = true;
    }

    // === 選択解除 ===
    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
        ResetVisualImmediate();
    }

    // === マウスが入った ===
    public void OnPointerEnter(PointerEventData eventData)
    {
        isSelected = true;
        if (!isPressed) isBlinking = true;
    }

    // === マウスが出た ===
    public void OnPointerExit(PointerEventData eventData)
    {
        isSelected = false;
        ResetVisualImmediate();
    }

    // === マウス押下 ===
    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        isBlinking = false; // 押している間は点滅停止

        // RGB を減衰して暗くする。アルファは視認性のため元に戻す
        Color c = targetImage.color;
        c.r = originalColor.r * darkenFactor;
        c.g = originalColor.g * darkenFactor;
        c.b = originalColor.b * darkenFactor;
        c.a = originalColor.a;
        targetImage.color = c;
    }

    // === マウス離上 ===
    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;

        if (isSelected)
        {
            isBlinking = true;

            Color c = targetImage.color;
            c.a = originalColor.a;
            targetImage.color = c;
        }
        else
        {
            ResetVisualImmediate();
        }
    }

    // === Submit ===
    public void OnSubmit(BaseEventData eventData)
    {
        StartCoroutine(FlashDarkOnSubmit_Unscaled());
    }


    private IEnumerator FlashDarkOnSubmit_Unscaled()
    {
        // 暗くする
        Color c = targetImage.color;
        c.r = originalColor.r * darkenFactor;
        c.g = originalColor.g * darkenFactor;
        c.b = originalColor.b * darkenFactor;
        c.a = originalColor.a; // アルファはしっかり表示
        targetImage.color = c;

        yield return new WaitForEndOfFrame();

        if (isSelected)
        {
            isPressed = false;
            isBlinking = true;

            Color c2 = targetImage.color;
            c2.a = originalColor.a;
            targetImage.color = c2;
        }
        else
        {
            ResetVisualImmediate();
        }
    }
}
