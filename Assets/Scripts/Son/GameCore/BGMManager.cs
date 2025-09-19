using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// GameState と BGM の対応関係
/// </summary>
[System.Serializable]
public struct StateToBGM
{
    public GameState state;
    public string bgmName;
}

/// <summary>
/// 音声情報（名前、AudioClip、音量）
/// </summary>
[System.Serializable]
public struct soundAudio
{
    public string name;
    public AudioClip audioClip;
    public float volume;
}

public class BGMManager : MonoBehaviour
{
    public static BGMManager instance;

    [Header("BGM定義リスト")]
    public List<soundAudio> soundList = new List<soundAudio>();

    [Header("BGM再生用AudioSource")]
    public AudioSource audioSource;

    [Header("GameStateごとのBGMマッピング")]
    public List<StateToBGM> bgmMappings = new List<StateToBGM>();

    private Dictionary<GameState, string> stateToBgmMap = new Dictionary<GameState, string>();

    private string currentPlayingName = null;
    private GameState nextState = GameState.Startup; // 次のGameState
    public float fadeDuration = 1f; // フェード時間

    /// <summary>
    /// シングルトンの初期化
    /// </summary>
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // GameStateとBGMのマッピングを辞書に登録
        foreach (var mapping in bgmMappings)
        {
            if (!stateToBgmMap.ContainsKey(mapping.state))
                stateToBgmMap.Add(mapping.state, mapping.bgmName);
        }
    }

    /// <summary>
    /// AudioSourceの初期化
    /// </summary>
    private void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    /// <summary>
    /// イベント購読
    /// </summary>
    private void OnEnable()
    {
        EventBus.SystemEvents.OnGameStateChange += PrepareToEnterState;
    }

    /// <summary>
    /// イベント解除
    /// </summary>
    private void OnDisable()
    {
        EventBus.SystemEvents.OnGameStateChange -= PrepareToEnterState;
    }

    /// <summary>
    /// 名前からサウンド情報を取得
    /// </summary>
    public soundAudio GetClipByName(string name)
    {
        foreach (var s in soundList)
        {
            if (s.name == name)
            {
                return s;
            }
        }

        soundAudio nu = new soundAudio();
        return nu;
    }

    /// <summary>
    /// GameStateに対応するBGM名を取得
    /// </summary>
    private string GetBGMNameForState(GameState state)
    {
        return stateToBgmMap.TryGetValue(state, out string bgmName) ? bgmName : null;
    }

    /// <summary>
    /// シーン切替前に呼ばれる関数。次の状態を元にBGMを変更する必要があるか確認。
    /// </summary>
    private void PrepareToEnterState(GameState nextState)
    {
        this.nextState = nextState;
        string nextBgmName = GetBGMNameForState(nextState);

        if (nextBgmName == null || nextBgmName == currentPlayingName)
            return;

        StartCoroutine(FadeOutThenPlay(nextBgmName));
    }

    /// <summary>
    /// フェードアウトしてから新しいBGMを再生
    /// </summary>
    private IEnumerator FadeOutThenPlay(string nextBgmName)
    {
        float startVolume = audioSource.volume;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, time / fadeDuration);
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = startVolume;
        currentPlayingName = null;

        PlayBGMForState(); // フェード後に新しいBGMを再生
    }

    /// <summary>
    /// 次のGameStateに対応するBGMを再生
    /// </summary>
    public void PlayBGMForState()
    {
        string bgmName = GetBGMNameForState(nextState);

        if (bgmName == null)
            return;

        if (currentPlayingName == bgmName)
            return;

        soundAudio s = GetClipByName(bgmName);
        if (s.audioClip == null)
            return;

        audioSource.clip = s.audioClip;
        audioSource.volume = 0f;
        audioSource.loop = true;
        audioSource.Play();
        currentPlayingName = bgmName;
        StartCoroutine(FadeIn(s.volume));
    }

    /// <summary>
    /// フェードイン処理
    /// </summary>
    private IEnumerator FadeIn(float targetVolume)
    {
        float time = 0f;
        float duration = fadeDuration / 2;
        while (time < duration)
        {
            time += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, targetVolume, time / duration);
            yield return null;
        }
        audioSource.volume = targetVolume;
    }
}
