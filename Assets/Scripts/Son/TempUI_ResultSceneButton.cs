using UnityEngine;
using UnityEngine.SceneManagement;

public class TempUI_ResultSceneButton : MonoBehaviour
{
    public void OnReturnClick()
    {
        GameManager.Instance?.ToTitle();
    }
    public void OnStartClick()
    {
        GameManager.Instance?.StartGame();
    }
    public void OnTestMapClick() 
    {
        SceneManager.LoadScene("Map1_temple");
    }
}
