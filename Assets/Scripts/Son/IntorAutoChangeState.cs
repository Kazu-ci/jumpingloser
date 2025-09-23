using UnityEngine;
using System.Collections;

public class IntorAutoChangeState : MonoBehaviour
{
    public float delayTime = 30f;
    void Start()
    {
        StartCoroutine(DelayCall());
    }

    IEnumerator DelayCall()
    {
        yield return new WaitForSeconds(delayTime);
        MyMethod();
    }

    void MyMethod()
    {
        GameManager.Instance?.StartGame();
    }
}
