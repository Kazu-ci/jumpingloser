using System.Transactions;
using UnityEngine;

public class PickableWeapon : MonoBehaviour
{
    public WeaponItem weaponPrefab; // ïêäÌÇÃÉvÉåÉnÉu
    public float rotSpeed;
    public GameObject pickEffect;
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
                if (pickEffect != null)
                {
                    Instantiate(pickEffect, transform.position, Quaternion.identity);
                }
                Destroy(gameObject);
            }
        }
    }
}