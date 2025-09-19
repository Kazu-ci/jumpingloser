using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class GoToClearScene : MonoBehaviour
{
    [Header("ボスオブジェクト")]
    public GameObject bossObject; // BossEnemy がアタッチされているオブジェクト

    [Header("遷移設定")]
    public string clearSceneName = "GameClearScene";

    [Header("フェード用UI")]
    public Image fadeImage;
    public float fadeDuration = 1f;

    private bool triggered = false;

    void Update()
    {
        if (!triggered && bossObject == null) // ボスが消えたら
        {
            triggered = true;
            StartCoroutine(FadeAndLoad());
        }
    }

    IEnumerator FadeAndLoad()
    {
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            Color c = fadeImage.color;
            c.a = 0;
            fadeImage.color = c;

            float time = 0f;
            while (time < fadeDuration)
            {
                time += Time.deltaTime;
                c.a = Mathf.Lerp(0, 1, time / fadeDuration);
                fadeImage.color = c;
                yield return null;
            }
        }

        SceneManager.LoadScene(clearSceneName);
    }
}
