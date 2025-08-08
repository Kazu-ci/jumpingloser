using System.Transactions;
using UnityEngine;

public class PickableWeapon : MonoBehaviour
{
    public WeaponItem weaponPrefab; // •Ší‚ÌƒvƒŒƒnƒu
    public float rotSpeed;
    private void Update()
    {
        transform.Rotate(Vector3.up, rotSpeed * Time.deltaTime, Space.World);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerMovement player = other.GetComponent<PlayerMovement>();
            if (player != null)
            {
                player.PickUpWeapon(weaponPrefab);
                Destroy(gameObject);
            }
        }
    }
}