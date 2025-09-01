using UnityEngine;
using System.Collections.Generic;

public class TempEnemyManager : MonoBehaviour
{
    public List<GameObject> enemies = new List<GameObject>();
    private void Update()
    {
        enemies.RemoveAll(e => e == null);
        if (enemies.Count == 0)
        {
            GameManager.Instance?.GameOver();
        }
    }
}
