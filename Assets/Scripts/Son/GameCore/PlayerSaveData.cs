using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerSaveData
{
    public int dataVersion = 1;

    public int maxHp = 50;
    public int currentHp = 50;

    public List<WeaponInstance> inventory = new List<WeaponInstance>();

    public int mainIndex = -1;

    public double elapsedGameTimeSec = 0.0;
    public int skillUseCount = 0;
    public int enemyDefeatCount = 0;

    public PlayerSaveData CloneShallow()
    {
        var c = new PlayerSaveData();
        c.dataVersion = dataVersion;
        c.maxHp = maxHp;
        c.currentHp = currentHp;
        c.mainIndex = mainIndex;
        c.elapsedGameTimeSec = elapsedGameTimeSec;
        c.skillUseCount = skillUseCount;
        c.enemyDefeatCount = enemyDefeatCount;
        c.inventory = new List<WeaponInstance>(inventory.Count);
        for (int i = 0; i < inventory.Count; ++i)
        {
            var w = inventory[i];
            c.inventory.Add(w != null ? w.Clone() : null);
        }
        return c;
    }
}
