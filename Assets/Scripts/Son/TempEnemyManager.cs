using UnityEngine;
using System.Collections.Generic;

public class TempEnemyManager : MonoBehaviour
{
    public List<GameObject> enemies = new List<GameObject>();

    public float Delay = 2f;

    bool isGameClear = false;
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
    }
}
