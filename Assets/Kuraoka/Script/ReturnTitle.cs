using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class ReturnTitle : MonoBehaviour
{
    private bool _requestedReturn = false; // ボタンでタイトルへ戻すフラグ

    private void Start()
    {
        // 起動時に Result ステートに遷移
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
        else
        {
            StartCoroutine(WaitForGameManagerAndGameOver());
        }
    }

    private IEnumerator WaitForGameManagerAndGameOver()
    {
        while (GameManager.Instance == null)
            yield return null;

        GameManager.Instance.GameOver();
    }

    // ボタンから呼ぶ
    public void OnClickReturn()
    {
        if (GameManager.Instance == null) return;

        _requestedReturn = true;       // タイトルに戻すフラグ
        GameManager.Instance.ToTitle(); // 状態遷移を実行
        StartCoroutine(WaitForTitleState());
    }

    // Title 状態になるまで待機
    private IEnumerator WaitForTitleState()
    {
        while (GameManager.Instance != null &&
               GameManager.Instance.CurrentState != GameState.Title)
        {
            yield return null;
        }

        if (_requestedReturn)
        {
            SceneManager.LoadScene("TestTitleScene");
            _requestedReturn = false;
        }
    }
}