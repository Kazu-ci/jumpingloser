using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class PanShot : MonoBehaviour
{
    private Transform playerVec;
    private GameObject player;
    bool shot = true;
    float now = 0;
    public int shotTime;
    private void Start()
    {
        player = GameObject.Find("PlayerTest");
        playerVec = player.transform;
        transform.LookAt(playerVec);
        Destroy(gameObject,4f);
        //shotTime = Random.Range(30, 60);
    }
    private void Update()
    { 
        if (now > shotTime && shot)
        {
            OnShot();
        }
        now += Time.deltaTime;
    }

    public void OnShot()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.AddForce(transform.forward * 1000);
        shot = false;
    }
    private void OnTriggerEnter(Collider other)
    {
        var comp=other.GetComponent<PlayerMovement>();
        if (comp!=null)
        {
            Destroy(gameObject);
        }
    }


}
