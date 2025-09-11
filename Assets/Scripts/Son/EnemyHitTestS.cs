using UnityEngine;

public class EnemyHitTestS : MonoBehaviour
{
    public Collider hitCollider;
    public MeshRenderer meshRenderer;
    public float loopTime = 2f;
    public float hitTime = 0.2f;
    private bool isHitted = false;
    private bool isActive = false;
    private float timer = 0f;

    private void Start()
    {
        if (hitCollider == null)
        {
            hitCollider = GetComponent<Collider>();
        }
        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }
        hitCollider.enabled = false;
        meshRenderer.enabled = false;
    }
    private void Update()
    {
        timer += Time.deltaTime;
        if(timer >= loopTime && isActive == false)
        {
            timer = 0f;
            isHitted = false;
            isActive = true;
            hitCollider.enabled = true;
            meshRenderer.enabled = true;
        }
        if(isActive && timer >= hitTime)
        {
            isActive = false;
            hitCollider.enabled = false;
            meshRenderer.enabled = false;
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if(isHitted) { return; }
        if (other.CompareTag("Player") && isHitted == false)
        {
            DamageData damageData = new DamageData(10);
            other.GetComponent<PlayerMovement>().TakeDamage(damageData);
            isHitted = true;
        }
    }
}


