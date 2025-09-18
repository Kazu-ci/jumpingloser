 using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class ReturnTitle : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void OnReturnClick()
    {
        GameManager.Instance?.ToTitle();
    }

}
