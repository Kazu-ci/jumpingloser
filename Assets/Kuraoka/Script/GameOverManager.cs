using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using static EventBus;

public class GameOverManager : MonoBehaviour
{
    [Header("フェード用 UI CanvasGroup")]
    public CanvasGroup fadeCanvasGroup;

    [Header("フェード時間")]
    public float fadeDuration = 1.5f;

    [Header("遷移先シーン名")]
    public string nextSceneName = "GameOverScene";

    private bool isDead = false;

    private void OnEnable()
    {
        UIEvents.OnPlayerHpChange += HandleHpChange;
    }

    private void OnDisable()
    {
        UIEvents.OnPlayerHpChange -= HandleHpChange;
    }

    private void HandleHpChange(int current, int max)
    {
        if (isDead) return;

        if (current <= 0)
        {
            isDead = true;
            StartCoroutine(FadeOutAndLoadScene());
        }
    }

    private IEnumerator FadeOutAndLoadScene()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.gameObject.SetActive(true);

            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Clamp01(timer / fadeDuration);
                yield return null;
            }

            fadeCanvasGroup.alpha = 1f;
        }

        // シーン遷移
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}