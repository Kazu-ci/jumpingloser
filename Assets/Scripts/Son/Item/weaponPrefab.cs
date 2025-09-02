using UnityEngine;
using DG.Tweening;

public class weaponPrefab : MonoBehaviour
{
    public GameObject switchEffect;
    private Vector3 oriScale;
    public float resizeDuration = 0.5f;

    private void Start()
    {
        if (switchEffect != null)
        {
            var eff = Instantiate(switchEffect, transform.position, Quaternion.identity);
            eff.transform.SetParent(transform.parent);
        }
        oriScale = transform.localScale;
        transform.localScale = Vector3.zero;

        transform.DOScale(oriScale, resizeDuration)
                 .SetEase(Ease.OutBack);
    }
}

