using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static EventBus;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class StateToSceneName
{
    public GameState state;
    public string name;
}
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;

    [Header("UI Fader")]
    public Image fadeImage;
    public float fadeDuration = 1f;

    [Header("State to SceneName")]
    public List<StateToSceneName> states;
    private Dictionary<GameState,string> dicSceneName = new Dictionary<GameState,string>();
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            fadeImage.color = new Color(0, 0, 0, 1);
            StartCoroutine(Fade(0));
        }
        foreach (StateToSceneName state in states)
        {
            if(!dicSceneName.ContainsKey(state.state))dicSceneName.Add(state.state, state.name);
        }
        
    }
    private void OnEnable()
    {
        SystemEvents.OnGameStateChange += HandleGameStateChange;
    }
    private void OnDisable()
    {
        SystemEvents.OnGameStateChange -= HandleGameStateChange;
    }

    private void HandleGameStateChange(GameState state)
    {
        dicSceneName.TryGetValue(state,out string name);
        if (name != null)
        {
            LoadScene(name);
        }
    }
    public void LoadScene(string sceneName)
    {
        StartCoroutine(Transition(sceneName));
    }

    IEnumerator Transition(string sceneName)
    {
        yield return StartCoroutine(Fade(1));
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone) yield return null;
        yield return StartCoroutine(Fade(0f));

        SystemEvents.OnSceneLoadComplete?.Invoke();
    }

    IEnumerator Fade(float targetAlpha)
    {
        if (fadeImage == null)
            yield break;

        fadeImage.gameObject.SetActive(true);
        float startAlpha = fadeImage.color.a;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        fadeImage.color = new Color(0, 0, 0, targetAlpha);

        if (targetAlpha == 0)
            fadeImage.raycastTarget = false;
    }
}
