/*
    於遊戲場景的Painting中，控制影片預載入、根據玩家位置呼叫VideoPlayerController載入或卸載影片
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DynamicVideoPreparationController : MonoBehaviour
{
    [Header("畫作物件（請手動掛載）")]
    public List<GameObject> artworks;
    [Header("玩家Transform")]
    public Transform vrPlayerTransform;      // VR模式玩家
    public Transform testPlayerTransform;    // 測試模式玩家
    [Header("載入距離")]
    public float loadDistance = 20f;

    [Header("Loading Bar")]
    public GameObject loadingPanel;
    public Slider progressBar;
    public float minLoadingTime = 1.0f; // loading bar顯示最少秒數

    private List<ArtworkInfo> artworkInfos = new List<ArtworkInfo>();
    private bool loadingDone = false;
    private Transform currentPlayerTransform; // 當前使用的玩家Transform

    private void Start()
    {
        Debug.LogWarning("[DynamicVideoPreparationController] Start - 開始執行");
        
        // 根據當前模式設定使用的玩家Transform
        Debug.LogWarning("[DynamicVideoPreparationController] 開始更新當前玩家");
        UpdateCurrentPlayer();
        
        // 準備畫作資訊
        Debug.LogWarning($"[DynamicVideoPreparationController] 開始準備 {artworks.Count} 個畫作資訊");
        foreach (var artwork in artworks)
        {
            if (artwork == null) continue;
            VideoPlayerController vpc = artwork.GetComponentInChildren<VideoPlayerController>(true);
            if (vpc == null)
            {
                Debug.LogWarning($"[DynamicVideoPreparationController] {artwork.name} 下找不到 VideoPlayerController");
                continue;
            }
            
            artworkInfos.Add(new ArtworkInfo
            {
                artwork = artwork,
                controller = vpc
            });
            Debug.LogWarning($"[DynamicVideoPreparationController] 成功添加畫作: {artwork.name}");
        }
        
        Debug.LogWarning($"[DynamicVideoPreparationController] 總共準備了 {artworkInfos.Count} 個畫作");
        
        // 顯示 loading bar，僅作為過場效果，不做任何影片載入
        if (loadingPanel != null) loadingPanel.SetActive(true);
        if (progressBar != null) progressBar.value = 0f;
        Debug.LogWarning("[DynamicVideoPreparationController] 開始 Loading Bar 效果");
        StartCoroutine(LoadingBarOnlyEffect());
        
        Debug.LogWarning("[DynamicVideoPreparationController] Start - 完成");
    }

    // 只顯示 loading bar 最少秒數，不做任何影片載入/卸載
    private IEnumerator LoadingBarOnlyEffect()
    {
        Debug.LogWarning("[DynamicVideoPreparationController] LoadingBarOnlyEffect - 開始");
        
        float elapsed = 0f;
        while (elapsed < minLoadingTime)
        {
            elapsed += Time.deltaTime;
            if (progressBar != null)
                progressBar.value = Mathf.Clamp01(elapsed / minLoadingTime);
            yield return null;
        }
        
        Debug.LogWarning("[DynamicVideoPreparationController] Loading Bar 效果完成，隱藏面板");
        if (loadingPanel != null) loadingPanel.SetActive(false);
        if (progressBar != null) progressBar.gameObject.SetActive(false);
        loadingDone = true;
        
        Debug.LogWarning("[DynamicVideoPreparationController] LoadingBarOnlyEffect - 完成，開始動態影片管理");
    }

    private void Update()
    {
        if (!loadingDone) return;
        
        // 減少 Update 中的 log 頻率，只在第一次執行時記錄
        if (Time.frameCount % 300 == 0) // 每 300 幀記錄一次狀態
        {
            Debug.LogWarning($"[DynamicVideoPreparationController] Update 狀態檢查 - 當前玩家位置: {(currentPlayerTransform != null ? currentPlayerTransform.position.ToString() : "null")}");
        }
        
        try
        {
            if (currentPlayerTransform == null)
            {
                if (Time.frameCount % 60 == 0) // 每秒記錄一次錯誤
                {
                    Debug.LogError("[DynamicVideoPreparationController] currentPlayerTransform 為 null，無法計算距離");
                }
                return;
            }
            
            foreach (var info in artworkInfos)
            {
                if (info?.artwork == null || info?.controller == null) continue;
                
                float dist = Vector3.Distance(currentPlayerTransform.position, info.artwork.transform.position);
                
                if (dist < loadDistance)
                {
                    // 只在第一次載入時記錄
                    if (Time.frameCount % 300 == 0)
                    {
                        Debug.LogWarning($"[DynamicVideoPreparationController] {info.artwork.name} 在載入範圍內 (距離: {dist:F1})，初始化影片");
                    }
                    info.controller.InitializeVideo();
                }
                else
                {
                    // 只在第一次卸載時記錄
                    if (Time.frameCount % 300 == 0)
                    {
                        Debug.LogWarning($"[DynamicVideoPreparationController] {info.artwork.name} 超出載入範圍 (距離: {dist:F1})，卸載影片");
                    }
                    info.controller.UnloadVideo();
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DynamicVideoPreparationController] Update方法發生錯誤: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// 根據當前遊戲模式更新使用的玩家Transform
    /// </summary>
    private void UpdateCurrentPlayer()
    {
        Debug.LogWarning("[DynamicVideoPreparationController] UpdateCurrentPlayer - 開始");
        
        try
        {
            if (GameModeManager.Instance != null)
            {
                Debug.LogWarning($"[DynamicVideoPreparationController] GameModeManager 找到，當前模式: {GameModeManager.Instance.CurrentMode}");
                
                if (GameModeManager.Instance.CurrentMode == GameMode.VRMode)
                {
                    currentPlayerTransform = vrPlayerTransform;
                    Debug.LogWarning($"[DynamicVideoPreparationController] 設定為 VR 模式，vrPlayerTransform: {(vrPlayerTransform != null ? vrPlayerTransform.name : "null")}");
                    if (vrPlayerTransform == null)
                        Debug.LogWarning("[DynamicVideoPreparationController] VR模式下但vrPlayerTransform未設定！");
                }
                else if (GameModeManager.Instance.CurrentMode == GameMode.MobileMode)
                {
                    currentPlayerTransform = testPlayerTransform;
                    Debug.LogWarning($"[DynamicVideoPreparationController] 設定為 Mobile 模式，testPlayerTransform: {(testPlayerTransform != null ? testPlayerTransform.name : "null")}");
                    if (testPlayerTransform == null)
                        Debug.LogWarning("[DynamicVideoPreparationController] 測試模式下但testPlayerTransform未設定！");
                }
                else
                {
                    Debug.LogWarning("[DynamicVideoPreparationController] 未知的遊戲模式，使用VR玩家作為預設");
                    currentPlayerTransform = vrPlayerTransform;
                }
            }
            else
            {
                Debug.LogWarning("[DynamicVideoPreparationController] 找不到GameModeManager，使用VR玩家作為預設");
                currentPlayerTransform = vrPlayerTransform;
            }
            
            // 最終檢查：如果兩個玩家都沒設定，給出明確警告
            if (currentPlayerTransform == null)
            {
                Debug.LogError("[DynamicVideoPreparationController] 當前選定的玩家Transform為空！影片載入功能將無法正常運作。");
            }
            else
            {
                Debug.LogWarning($"[DynamicVideoPreparationController] 成功設定當前玩家: {currentPlayerTransform.name}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DynamicVideoPreparationController] UpdateCurrentPlayer發生錯誤: {e.Message}\n{e.StackTrace}");
            // 嘗試使用任何可用的玩家作為備用
            currentPlayerTransform = vrPlayerTransform ?? testPlayerTransform;
            Debug.LogWarning($"[DynamicVideoPreparationController] 使用備用玩家: {(currentPlayerTransform != null ? currentPlayerTransform.name : "null")}");
        }
        
        Debug.LogWarning("[DynamicVideoPreparationController] UpdateCurrentPlayer - 完成");
    }

    private class ArtworkInfo
    {
        public GameObject artwork;
        public VideoPlayerController controller;
    }
}
