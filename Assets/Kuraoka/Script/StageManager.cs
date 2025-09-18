using UnityEngine;

public class StageManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private string stageSceneName;
    [SerializeField] private bool isBossStage;

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetStageInfo(stageSceneName, isBossStage);
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
