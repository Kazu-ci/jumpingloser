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
        GameManager.Instance?.ToIntro();
    }
    public void OnContinueClick()
    {
        TempUI ui = Object.FindFirstObjectByType<TempUI>(); // Updated to use FindFirstObjectByType  
        ui.SwitchMenu();
    }

    public void OnRetryClick()
    {
        GameManager.Instance?.ReTry();
    }
    public void OnTestMapClick()
    {
        SceneManager.LoadScene("SampleScene 1");
    }
}
