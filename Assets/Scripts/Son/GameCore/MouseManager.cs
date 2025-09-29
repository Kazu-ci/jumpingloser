using UnityEngine;
using UnityEngine.InputSystem;

public class MouseManager : MonoBehaviour
{
    [Header("=== マウス管理 ===")]
    [Tooltip("マウスを表示する移動量の閾値")]
    public float moveThreshold = 10f;

    [Tooltip("非表示になるまでの秒数")]
    public float hideDelay = 3f;

    private float idleTimer = 0f;
    private bool isVisible = true;

    private GameState gameState = GameState.Startup;

    public static MouseManager Instance { get; private set; }

    private void Awake()
    {
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
        EventBus.SystemEvents.OnGameStateChange += CheckState;
    }
    private void OnDisable()
    {
        EventBus.SystemEvents.OnGameStateChange -= CheckState;
    }

    void Update()
    {
        if (Mouse.current == null || gameState == GameState.Playing) return;

        // マウス移動量を取得
        Vector2 delta = Mouse.current.delta.ReadValue();

        // 一定以上動いたら表示に切り替え
        if (delta.sqrMagnitude > moveThreshold * moveThreshold)
        {
            ShowMouse();
            idleTimer = 0f;
        }
        else
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= hideDelay)
            {
                HideMouse();
            }
        }
    }

    private void CheckState(GameState state)
    {
        gameState = state;
        if (gameState == GameState.Playing)
        {
            HideMouse();
        }
    }
    private void ShowMouse()
    {
        if (!isVisible)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None; // ロック解除
            isVisible = true;
        }
    }

    private void HideMouse()
    {
        if (isVisible)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked; // 中央に固定
            isVisible = false;
        }
    }
}