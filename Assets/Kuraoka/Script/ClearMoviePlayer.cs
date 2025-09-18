using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
public class ClearMoviePlayer : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public string nextSceneName = "TestTitleScene"; // 動画後に戻るシーン

    void Start()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnMovieFinished;
            videoPlayer.Play();
        }
    }

    void OnMovieFinished(VideoPlayer vp)
    {
        // 動画が終わったらタイトルへ
        SceneManager.LoadScene(nextSceneName);
    }
}
