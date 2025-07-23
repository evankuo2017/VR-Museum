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
        // loading bar 結束後才通知進入VR
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
