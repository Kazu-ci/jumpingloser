using UnityEngine;
using System.Collections.Generic;

public class TempEnemyManager : MonoBehaviour
{
    public List<GameObject> enemies = new List<GameObject>();

    public float Delay = 2f;

    bool isGameClear = false;

    public float startDelay = 1f;
    private bool isCamPerformed = false;
    public float rotSpeed = 72f;
    public float camDuration = 5f;
    public Transform camTarget;
    private void Update()
    {
        enemies.RemoveAll(e => e == null);
        if (enemies.Count == 0 && !isGameClear)
        {
            Delay -= Time.deltaTime;
            if (Delay <= 0f)
            {
                GameManager.Instance?.GameClear();
                isGameClear = true;
            }
        }

        if(startDelay > 0) startDelay -= Time.deltaTime;
        if (startDelay <= 0 && !isCamPerformed)
        {
            if (camTarget != null) 
            {

                EventBus.PlayerEvents.ChangeCameraTarget?.Invoke(camTarget, camDuration, rotSpeed);
                isCamPerformed = true;
            }
        }

    }
}
