using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using static EventBus;
public class ClearMoviePlayer : MonoBehaviour
{
    [Header("ボスオブジェクトをここにアサイン")]
    public GameObject boss;

    private bool triggered = false;

    void Update()
    {
        // まだ遷移していない && ボスが null または非アクティブなら
        if (!triggered && (boss == null || !boss.activeInHierarchy))
        {
            triggered = true;
            // GameManager経由でTitleへ戻る
            GameManager.Instance?.ToTitle();
        }
    }
}
