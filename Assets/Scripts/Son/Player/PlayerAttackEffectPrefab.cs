using UnityEngine;
using System.Collections.Generic;

public class PlayerAttackEffectPrefab : MonoBehaviour
{
    public float lifeTime = 2f;
    private Transform playerTransform;
    public List<GameObject> effectPrefabs = new List<GameObject>();
    public List<float> effectTimeList = new List<float>();
    private List<bool> effectList = new List<bool>();
    public AudioSource audioSource;
    public List<AudioClip> audioClips = new List<AudioClip>();
    public List<float> audioTimeList = new List<float>();
    private List<bool> audioList = new List<bool>();

    private float timer = 0f;

    private void Start()
    {
        playerTransform = EventBus.PlayerEvents.GetPlayerObject().transform;
        Destroy(gameObject, lifeTime);
        for (int i = 0; i < effectPrefabs.Count; i++)
        {
            effectList.Add(false);
        }
        for (int i = 0; i < audioClips.Count; i++)
        {
            audioList.Add(false);
        }
    }
    private void Update()
    {
        timer += Time.deltaTime;
        if (playerTransform != null)
        {
            transform.position = playerTransform.position;
            transform.rotation = playerTransform.rotation;
        }
        for (int i = 0; i < effectPrefabs.Count; i++)
        {
            if (timer >= effectTimeList[i] && !effectList[i])
            {
                Instantiate(effectPrefabs[i], transform.position, transform.rotation);
                effectList[i] = true;
            }
        }
        if (audioSource != null)
        {
            for (int i = 0; i < audioClips.Count; i++)
            {
                if (timer >= audioTimeList[i] && !audioList[i])
                {
                    audioSource.PlayOneShot(audioClips[i]);
                    audioList[i] = true;
                }
            }
        }
    }
}
