using UnityEngine;

public class Rapper: MonoBehaviour
{
    public Enemy parentController; // Cube に付けたスクリプト
    public void OnAttackSet()
    {
        parentController.OnAttackSet();
    }

    public void OnAttackEnd()
    {
        parentController.OnAttackEnd();
    }
}
