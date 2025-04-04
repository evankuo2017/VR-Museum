using UnityEngine;

public enum GameMode
{
    VRMode,
    MobileMode
}

public class GameModeManager : MonoBehaviour
{
    public static GameModeManager Instance { get; private set; }

    // 目前模式，會由首頁設定後持續存在
    public GameMode CurrentMode = GameMode.MobileMode;

    private void Awake()
    {
        // 如果已經有一個 GameModeManager 實例，就銷毀它
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }
        
        // 將當前這個設定為新的單例實例
        Instance = this;
        DontDestroyOnLoad(gameObject);  // 跨場景保留 GameModeManager
    }

    // 以下可提供 UI 按鈕直接呼叫的方法
    public void SetToVRMode()
    {
        CurrentMode = GameMode.VRMode;
    }

    public void SetToMobileMode()
    {
        CurrentMode = GameMode.MobileMode;
    }
}
