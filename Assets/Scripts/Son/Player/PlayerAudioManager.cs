using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


public enum PlayerAudioPart
{
    None,
    LHand,
    RHand,
    Body,
    Mouth,
    feet
}
[System.Serializable]
public class AudioPart
{
    public PlayerAudioPart part;
    public AudioSource source;
}
public class PlayerAudioManager : MonoBehaviour
{

    [SerializeField]
    private List<AudioPart> audioList = new List<AudioPart>();
    private readonly Dictionary<PlayerAudioPart, AudioSource> audioDictionary = new Dictionary<PlayerAudioPart, AudioSource>();
    private void Start()
    {
        foreach (var part in audioList)
        {
            if (part == null || part.source == null)
            {
                continue;
            }
            if (part.part == PlayerAudioPart.None)
            {
                continue;
            }
            if (audioDictionary.ContainsKey(part.part))
            {
                continue;
            }

            audioDictionary.Add(part.part, part.source);
            
        }

    }

    private void OnEnable()
    {
        EventBus.PlayerEvents.PlayClipByPart += PlayClipOnAudioPart;
    }
    private void OnDisable()
    {
        EventBus.PlayerEvents.PlayClipByPart -= PlayClipOnAudioPart;
    }

    private bool PlayClipOnAudioPart(PlayerAudioPart part, AudioClip clip,float volume = 1.0f,float speed = 1.0f,float delay = 0f)
    {
        if (clip == null) return false;

        if (!audioDictionary.TryGetValue(part, out var source) || source == null)
        {
            return false;
        }
        source.pitch = Mathf.Clamp(speed, -3f, 3f);

        if (source.isPlaying) source.Stop();
        source.clip = clip;
        source.volume = Mathf.Clamp01(volume);
        if (delay > 0f)
        {
            double startTime = AudioSettings.dspTime + delay;
            source.PlayScheduled(startTime);
        }
        else
        {
            source.Play();
        }
        return true;
    }
    public bool StopClipOnAudioPart(PlayerAudioPart part)
    {
        if (!audioDictionary.TryGetValue(part, out var source) || source == null)
        {
            return false;
        }
        if (source.isPlaying) source.Stop();
        return true;
    }
}
