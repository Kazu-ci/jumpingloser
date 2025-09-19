using UnityEngine;
using System.Collections;

public class TempGameClear : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(DelayCall());
    }

    IEnumerator DelayCall()
    {
        yield return new WaitForSeconds(45f);
        MyMethod();
    }

    void MyMethod()
    {
        GameManager.Instance?.ToTitle();
    }
}
