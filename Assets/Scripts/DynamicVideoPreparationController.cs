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
    public Transform playerTransform;
    [Header("載入距離")]
    public float loadDistance = 20f;

    [Header("Loading Bar")]
    public GameObject loadingPanel;
    public Slider progressBar;
    public float minLoadingTime = 1.0f; // loading bar顯示最少秒數

    private List<ArtworkInfo> artworkInfos = new List<ArtworkInfo>();
    private bool loadingDone = false;

    // 參考場景中的 VrModeController，用來觸發 VR 模式
    private VrModeController vrModeController;

    private void Start()
    {
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
        // 尋找 VrModeController
        vrModeController = FindObjectOfType<VrModeController>();

        // 啟動loading bar
        if (loadingPanel != null) loadingPanel.SetActive(true);
        if (progressBar != null) progressBar.value = 0f;
        StartCoroutine(LoadingBar());
    }

    private IEnumerator LoadingBar()
    {
        float elapsed = 0f;
        
        // 第一階段：預先載入所有影片（佔用loading bar的70%時間）
        float preloadTime = minLoadingTime * 0.7f;
        Debug.Log("開始預先載入所有影片...");
        
        while (elapsed < preloadTime)
        {
            elapsed += Time.deltaTime;
            if (progressBar != null)
                progressBar.value = Mathf.Clamp01(elapsed / minLoadingTime);
            yield return null;
        }
        
        // 預先載入所有影片
        foreach (var info in artworkInfos)
        {
            Debug.Log($"[{info.artwork.name}] 預先載入影片");
            info.controller.InitializeVideo();
            yield return new WaitForSeconds(0.1f); // 稍微延遲，避免同時載入太多影片
        }
        
        // 等待所有影片準備完成
        yield return new WaitForSeconds(0.5f);
        
        // 第二階段：卸載所有影片（佔用loading bar的30%時間）
        float unloadTime = minLoadingTime * 0.3f;
        Debug.Log("開始卸載所有影片...");
        
        while (elapsed < minLoadingTime)
        {
            elapsed += Time.deltaTime;
            if (progressBar != null)
                progressBar.value = Mathf.Clamp01(elapsed / minLoadingTime);
            yield return null;
        }
        
        // 卸載所有影片
        foreach (var info in artworkInfos)
        {
            Debug.Log($"[{info.artwork.name}] 卸載影片");
            info.controller.UnloadVideo();
        }
        
        if (loadingPanel != null) loadingPanel.SetActive(false);
        if (progressBar != null) progressBar.gameObject.SetActive(false);
        loadingDone = true;

        Debug.Log("Loading完成，所有影片已預先載入並卸載");

        // loading結束後才通知進入VR
        if (vrModeController != null)
        {
            vrModeController.RequestEnterVR();
        }
        else
        {
            Debug.LogWarning("VrModeController not found in scene.");
        }
    }

    private void Update()
    {
        if (!loadingDone) return;
        if (playerTransform == null) return;
        foreach (var info in artworkInfos)
        {
            float dist = Vector3.Distance(playerTransform.position, info.artwork.transform.position);
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

    private class ArtworkInfo
    {
        public GameObject artwork;
        public VideoPlayerController controller;
    }
}
