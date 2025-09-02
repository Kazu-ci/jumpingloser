using Unity.VisualScripting;
using UnityEngine;

public class pickEffect : MonoBehaviour
{
    public float lifeTime = 1f;
    public AudioClip pickSound;

    private AudioSource audioSource;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
        if (pickSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = pickSound;
            audioSource.Play();
        }
    }
}
