using UnityEngine;

public class weaponPrefab : MonoBehaviour
{
    public GameObject switchEffect;

    private void Start()
    {
        if (switchEffect != null)
        {
            var eff = Instantiate(switchEffect, transform.position, Quaternion.identity);
            eff.transform.SetParent(transform);
        }
    }
}

