using UnityEngine;
using System.Collections;

public class TempGameClear : MonoBehaviour
{
    public float delayTime = 41f;
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
        GameManager.Instance?.ToTitle();
    }
}
