using UnityEngine;

public class CrosshairRotate : MonoBehaviour
{
    [Header("=== ‰ñ“]‘¬“x ===")]
    public float rotationSpeed = 100f; // 1•b‚ ‚½‚è‚Ì‰ñ“]Šp“x

    void Update()
    {
        // === Z²‚ğ’†S‚É‰ñ“]‚³‚¹‚é ===
        transform.Rotate(Vector3.forward, rotationSpeed * Time.unscaledDeltaTime);
    }
}
