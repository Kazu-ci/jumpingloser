using UnityEngine;
using static EventBus;
using System.Collections.Generic;

public class TempUI_Loading : MonoBehaviour
{
    public static TempUI_Loading Instance { get; private set; }
    public GameObject loadingUI;
    public List<GameState> loadingStates= new List<GameState>( );

    public List<GameObject> Loadings = new List<GameObject>( );
    private void Awake()
    {
        // ÉVÉìÉOÉãÉgÉìï€èÿ
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    private void OnEnable()
    {
        SystemEvents.OnGameStateChange += HandlePreLoading;
        SystemEvents.OnSceneLoadComplete += HandleLoadingComplete;
    }
    private void OnDisable()
    {
        SystemEvents.OnGameStateChange -= HandlePreLoading;
        SystemEvents.OnSceneLoadComplete -= HandleLoadingComplete;
    }

    private void HandlePreLoading(GameState state)
    {
        if(loadingUI == null) { return; }
        if (loadingStates.Contains(state))
        {
            loadingUI.SetActive(true);
        }
        else
        {
            loadingUI.SetActive(false);
        }
    }
    private void HandleLoadingComplete()
    {
        if (loadingUI == null) { return; }
        loadingUI.SetActive(false);
    }
}
