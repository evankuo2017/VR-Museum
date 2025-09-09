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
    public Transform playerTransform;      // 玩家
    [Header("載入距離")]
    public float loadDistance = 40f;

    //[Header("視角閾值 (0-180)")]
    //[Range(0, 180)]
    //public float viewAngleThreshold = 120f; // 只有在此角度範圍內才載入影片

    [Header("Loading Bar")]
    public GameObject loadingPanel;
    public Slider progressBar;
    public float minLoadingTime = 1.0f; // loading bar顯示最少秒數

    private List<ArtworkInfo> artworkInfos = new List<ArtworkInfo>();
    private bool loadingDone = false;
    private Transform currentPlayerTransform; // 當前使用的玩家Transform

    private void Start()
    {
        Debug.Log("[DynamicVideoPreparationController] Start - 開始執行");
        
        // 準備畫作資訊
        Debug.Log($"[DynamicVideoPreparationController] 開始準備 {artworks.Count} 個畫作資訊");
        foreach (var artwork in artworks)
        {
            if (artwork == null) continue;
            VideoPlayerController vpc = artwork.GetComponentInChildren<VideoPlayerController>(true);
            if (vpc == null)
            {
                Debug.Log($"[DynamicVideoPreparationController] {artwork.name} 下找不到 VideoPlayerController");
                continue;
            }
            
            artworkInfos.Add(new ArtworkInfo
            {
                artwork = artwork,
                controller = vpc
            });
            Debug.Log($"[DynamicVideoPreparationController] 成功添加畫作: {artwork.name}");
        }
        
        Debug.Log($"[DynamicVideoPreparationController] 總共準備了 {artworkInfos.Count} 個畫作");
        
        // 顯示 loading bar，僅作為過場效果，不做任何影片載入
        if (loadingPanel != null) loadingPanel.SetActive(true);
        if (progressBar != null) progressBar.value = 0f;
        Debug.Log("[DynamicVideoPreparationController] 開始 Loading Bar 效果");
        StartCoroutine(LoadingBarOnlyEffect());
        
        Debug.Log("[DynamicVideoPreparationController] Start - 完成");
    }

    // 只顯示 loading bar 最少秒數，不做任何影片載入/卸載
    private IEnumerator LoadingBarOnlyEffect()
    {
        Debug.Log("[DynamicVideoPreparationController] LoadingBarOnlyEffect - 開始");
        
        float elapsed = 0f;
        while (elapsed < minLoadingTime)
        {
            elapsed += Time.deltaTime;
            if (progressBar != null)
                progressBar.value = Mathf.Clamp01(elapsed / minLoadingTime);
            yield return null;
        }
        
        Debug.Log("[DynamicVideoPreparationController] Loading Bar 效果完成，隱藏面板");
        if (loadingPanel != null) loadingPanel.SetActive(false);
        if (progressBar != null) progressBar.gameObject.SetActive(false);
        loadingDone = true;
        
        Debug.Log("[DynamicVideoPreparationController] LoadingBarOnlyEffect - 完成，開始動態影片管理");
    }

    private void Update()
    {
        if (!loadingDone) return;
        
        // 減少 Update 中的 log 頻率，只在第一次執行時記錄
        if (Time.frameCount % 300 == 0) // 每 300 幀記錄一次狀態
        {
            Debug.Log($"[DynamicVideoPreparationController] Update 狀態檢查 - 當前玩家位置: {(playerTransform != null ? playerTransform.position.ToString() : "null")}");
        }
        
        try
        {
            if (playerTransform == null)
            {
                if (Time.frameCount % 60 == 0) // 每秒記錄一次錯誤
                {
                    Debug.LogError("[DynamicVideoPreparationController] playerTransform 為 null，無法計算距離");
                }
                return;
            }
            
            foreach (var info in artworkInfos)
            {
                if (info?.artwork == null || info?.controller == null) continue;
                
                float dist = Vector3.Distance(playerTransform.position, info.artwork.transform.position);
                
                if (dist < loadDistance)
                {
                    // 只在第一次載入時記錄
                    if (Time.frameCount % 300 == 0)
                    {
                        //Debug.Log($"[DynamicVideoPreparationController] {info.artwork.name} 在載入範圍內 (距離: {dist:F1})，初始化影片");
                    }
                    info.controller.InitializeVideo();
                }
                else
                {
                    // 只在第一次卸載時記錄
                    if (Time.frameCount % 300 == 0)
                    {
                        //Debug.Log($"[DynamicVideoPreparationController] {info.artwork.name} 超出載入範圍 (距離: {dist:F1})，卸載影片");
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

    

    private class ArtworkInfo
    {
        public GameObject artwork;
        public VideoPlayerController controller;
    }
}
