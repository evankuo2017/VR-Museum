/*
GameModeManager負責管理遊戲模式掛在GameModeManager上，跟他一起持續存在。
*/
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
        Debug.LogWarning($"[GameModeManager] Awake - 開始執行，CurrentMode: {CurrentMode}");
        
        // 如果已經有一個 GameModeManager 實例，就銷毀它
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameModeManager] 發現重複實例，銷毀舊實例");
            Destroy(Instance.gameObject);
        }
        
        // 將當前這個設定為新的單例實例
        Instance = this;
        DontDestroyOnLoad(gameObject);  // 跨場景保留 GameModeManager
        
        Debug.LogWarning($"[GameModeManager] Awake - 完成，單例已設定，CurrentMode: {CurrentMode}");
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
