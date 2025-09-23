using UnityEngine;

public class CutInput : MonoBehaviour
{
    public void OnIntroToContinueClick()
    {
        GameManager.Instance?.StartGame();
    }
    public void OnSkipClick()
    {
        GameManager.Instance?.ToTitle();
    }
}
