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
        // 根據當前模式設定使用的玩家Transform
        UpdateCurrentPlayer();
        
        // 準備畫作資訊
        foreach (var artwork in artworks)
        {
            if (artwork == null) continue;
            VideoPlayerController vpc = artwork.GetComponentInChildren<VideoPlayerController>(true);
            if (vpc == null)
            {
                Debug.LogWarning($"{artwork.name} 下找不到 VideoPlayerController");
                continue;
            }
            
            artworkInfos.Add(new ArtworkInfo
            {
                artwork = artwork,
                controller = vpc
            });
        }
        // 顯示 loading bar，僅作為過場效果，不做任何影片載入
        if (loadingPanel != null) loadingPanel.SetActive(true);
        if (progressBar != null) progressBar.value = 0f;
        StartCoroutine(LoadingBarOnlyEffect());
    }

    // 只顯示 loading bar 最少秒數，不做任何影片載入/卸載
    private IEnumerator LoadingBarOnlyEffect()
    {
        float elapsed = 0f;
        while (elapsed < minLoadingTime)
        {
            elapsed += Time.deltaTime;
            if (progressBar != null)
                progressBar.value = Mathf.Clamp01(elapsed / minLoadingTime);
            yield return null;
        }
        if (loadingPanel != null) loadingPanel.SetActive(false);
        if (progressBar != null) progressBar.gameObject.SetActive(false);
        loadingDone = true;
    }

    private void Update()
    {
        if (!loadingDone) return;
        try
        {
            foreach (var info in artworkInfos)
            {
                if (info?.artwork == null || info?.controller == null) continue;
                
                float dist = Vector3.Distance(currentPlayerTransform.position, info.artwork.transform.position);
                if (dist < loadDistance)
                {
                    info.controller.InitializeVideo();
                }
                else
                {
                    info.controller.UnloadVideo();
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DynamicVideoPreparationController] Update方法發生錯誤: {e.Message}");
        }
    }

    /// <summary>
    /// 根據當前遊戲模式更新使用的玩家Transform
    /// </summary>
    private void UpdateCurrentPlayer()
    {
        try
        {
            if (GameModeManager.Instance != null)
            {
                if (GameModeManager.Instance.CurrentMode == GameMode.VRMode)
                {
                    currentPlayerTransform = vrPlayerTransform;
                    if (vrPlayerTransform == null)
                        Debug.LogWarning("[DynamicVideoPreparationController] VR模式下但vrPlayerTransform未設定！");
                }
                else if (GameModeManager.Instance.CurrentMode == GameMode.MobileMode)
                {
                    currentPlayerTransform = testPlayerTransform;
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
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DynamicVideoPreparationController] UpdateCurrentPlayer發生錯誤: {e.Message}");
            // 嘗試使用任何可用的玩家作為備用
            currentPlayerTransform = vrPlayerTransform ?? testPlayerTransform;
        }
    }

    private class ArtworkInfo
    {
        public GameObject artwork;
        public VideoPlayerController controller;
    }
}
