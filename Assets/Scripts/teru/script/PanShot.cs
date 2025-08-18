using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class PanShot : MonoBehaviour
{
    private Transform playerVec;
    private GameObject player;
    bool shot = true;
    public bool orientation;
    int now = 0;
    public int shotTime;
    private void Start()
    {
        player = GameObject.Find("PlayerTest");
        playerVec = player.transform;
        //shotTime = Random.Range(30, 60);
    }
    private void Update()
    {
        if (orientation)
        {
            transform.LookAt(playerVec);
        }
        if (now > shotTime && shot)
        {
            OnShot();
        }
        now++;
    }

    public void OnShot()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.AddForce(transform.forward * 1000);
        shot = false;
    }


}
