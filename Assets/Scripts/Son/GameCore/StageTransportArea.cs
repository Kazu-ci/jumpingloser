using UnityEngine;

public class StageTransportArea : MonoBehaviour
{
    bool triggered = false;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance?.EnterStage2();
        }
    }
}
