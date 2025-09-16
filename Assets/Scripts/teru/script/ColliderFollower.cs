using UnityEngine;

public class ColliderFollower : MonoBehaviour
{
    [SerializeField] private Transform model;        // Animator が動かすモデル
    [SerializeField] private Transform colliderObj;  // 追従させたいコライダーオブジェクト

    private Vector3 initialOffset;
    private Quaternion initialRotationOffset;

    void Start()
    {
        // 初期の位置・回転の差を保持
        initialOffset = colliderObj.position - model.position;
        initialRotationOffset = Quaternion.Inverse(model.rotation) * colliderObj.rotation;
    }

    void LateUpdate()
    {
        // モデルに追従
        colliderObj.position = model.position;
        colliderObj.rotation = model.rotation * initialRotationOffset;
    }
}