using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
public class ClearMoviePlayer : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public string nextSceneName = "TestTitleScene"; // �����ɖ߂�V�[��

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
        // ���悪�I�������^�C�g����
        SceneManager.LoadScene(nextSceneName);
    }
}
