using UnityEngine;

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
}
