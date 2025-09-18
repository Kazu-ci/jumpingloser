using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class ReturnToGame : MonoBehaviour
{
    [Header("点滅させるUI")]
    public GameObject blinkingUI; // Canvas の子オブジェクトなど

    [Header("点滅設定")]
    [Tooltip("1回の点滅にかかる時間（秒）")]
    public float fadeDuration = 0.5f;

    [Header("ステージ情報")]
    public string stageSceneName = "NormalStage1_Forest"; // ステージ1
    public string bossSceneName = "SampleScene 1";        // ボス戦
    public bool isBossStage = false;

    private CanvasGroup canvasGroup;

    private void Start()
    {
        if (blinkingUI != null)
        {
            // CanvasGroup を追加して透明度制御
            canvasGroup = blinkingUI.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = blinkingUI.AddComponent<CanvasGroup>();

            StartCoroutine(FadeBlinkUI());
        }
    }

    private IEnumerator FadeBlinkUI()
    {
        while (true)
        {
            // フェードアウト
            yield return StartCoroutine(Fade(1f, 0f, fadeDuration));
            // フェードイン
            yield return StartCoroutine(Fade(0f, 1f, fadeDuration));
        }
    }

    private IEnumerator Fade(float start, float end, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(start, end, elapsed / duration);
            canvasGroup.alpha = alpha;
            yield return null;
        }
        canvasGroup.alpha = end;
    }

    // ボタンから呼ぶ
    public void OnClickReturnToGame()
    {
        string targetScene = isBossStage ? bossSceneName : stageSceneName;

        // シーンロード
        SceneManager.LoadScene(targetScene);

        // GameManager 経由でステート遷移
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame(); // Preloading → Playing へ
        }
    }
}
