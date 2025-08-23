/*
    於遊戲場景中控制每影片的載入、卸載、撥放與停止時機
*/
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;
using UnityEngine.EventSystems; // 引入事件系統命名空間

public class VideoPlayerController : MonoBehaviour
{
    [Header("reference objects")]
    [SerializeField] private RawImage displayImage;// 影片顯示區域
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage previewImage;// 畫作靜態圖

    [Header("影片設定")]
    [SerializeField] private string videoPath; // 影片在Resources資料夾中的路徑，例如："Videos/MyVideo"

    private bool isVideoLoaded = false;
    private bool isVideoPrepared = false;
    private bool isPlaying = false;
    
    // 用於 Debug 訊息的物件名稱（優先使用父物件名稱）
    private string debugObjectName;

    private void Start()
    {
        // 初始化 Debug 用的物件名稱（優先使用父物件名稱）
        debugObjectName = transform.parent != null ? transform.parent.gameObject.name : gameObject.name;
        
        Debug.Log($"[VideoPlayerController] [{debugObjectName}] Start - 開始執行");
        
        InitializeComponents();
        // 如果沒有設定videoPath，嘗試從VideoPlayer的clip獲取
        if (string.IsNullOrEmpty(videoPath) && videoPlayer != null && videoPlayer.clip != null)
        {
            videoPath = videoPlayer.clip.name;
            Debug.Log($"[{debugObjectName}] 自動設定影片路徑: {videoPath}");
        }
        
        Debug.Log($"[VideoPlayerController] [{debugObjectName}] Start - 完成");
    }

    private void InitializeComponents()
    {
        Debug.Log($"[VideoPlayerController] [{debugObjectName}] InitializeComponents - 開始");
        
        if (videoPlayer != null)
        {
            Debug.Log($"[VideoPlayerController] [{debugObjectName}] 設定 VideoPlayer 參數");
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.prepareCompleted += OnVideoPrepared;
        }
        else
        {
            Debug.LogError($"[VideoPlayerController] [{debugObjectName}] VideoPlayer 為 null！");
        }
        
        Debug.Log($"[VideoPlayerController] [{debugObjectName}] InitializeComponents - 完成");
    }

    // 動態初始化影片
    public void InitializeVideo()
    {
        //Debug.Log($"[VideoPlayerController] [{debugObjectName}] InitializeVideo - 開始");
        
        if (isVideoLoaded || videoPlayer == null) 
        {
            //Debug.Log($"[VideoPlayerController] [{debugObjectName}] InitializeVideo 跳過 - isVideoLoaded:{isVideoLoaded}, videoPlayer:{videoPlayer != null}");
            return;
        }
        
        try
        {
            // 如果videoPlayer.clip為null，嘗試重新載入
            if (videoPlayer.clip == null && !string.IsNullOrEmpty(videoPath))
            {
                Debug.Log($"[VideoPlayerController] [{debugObjectName}] 嘗試動態載入影片: {videoPath}");
                
                VideoClip clip = null;
                
                clip = Resources.Load<VideoClip>($"video/{videoPath}");
                
                if (clip != null)
                {
                    videoPlayer.clip = clip;
                    Debug.Log($"[VideoPlayerController] [{debugObjectName}] 成功載入影片: {clip.name}");
                }
                else
                {
                    Debug.LogError($"[VideoPlayerController] [{debugObjectName}] 無法載入影片: {videoPath}");
                    return;
                }
            }
            
            isVideoLoaded = true;
            Debug.Log($"[VideoPlayerController] [{debugObjectName}] 開始 Prepare 影片");
            videoPlayer.Prepare();
            Debug.Log($"[VideoPlayerController] [{debugObjectName}] InitializeVideo - 完成");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[VideoPlayerController] [{debugObjectName}] InitializeVideo 發生錯誤: {e.Message}\n{e.StackTrace}");
        }
    }

    // 動態卸載影片
    public void UnloadVideo()
    {
        //Debug.Log($"[VideoPlayerController] [{debugObjectName}] UnloadVideo - 開始");
        
        if (!isVideoLoaded || videoPlayer == null) 
        {
            //Debug.Log($"[VideoPlayerController] [{debugObjectName}] UnloadVideo 跳過 - isVideoLoaded:{isVideoLoaded}, videoPlayer:{videoPlayer != null}");
            return;
        }
        
        try
        {
            if (videoPlayer.isPlaying) 
            {
                Debug.Log($"[VideoPlayerController] [{debugObjectName}] 停止正在播放的影片");
                videoPlayer.Stop();
            }
            
            string videoName = videoPlayer.clip != null ? videoPlayer.clip.name : "未知影片";
            Debug.Log($"[VideoPlayerController] [{debugObjectName}] 開始卸載影片: {videoName}");
            
            // 清空影片資源
            videoPlayer.clip = null;
            isVideoLoaded = false;
            isVideoPrepared = false;
            isPlaying = false;
            
            // 重新顯示image
            if (previewImage != null) 
            {
                previewImage.gameObject.SetActive(true);
                Debug.Log($"[VideoPlayerController] [{debugObjectName}] 已重新顯示預覽圖片");
            }
            
            Debug.Log($"[VideoPlayerController] [{debugObjectName}] UnloadVideo - 完成");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[VideoPlayerController] [{debugObjectName}] UnloadVideo 發生錯誤: {e.Message}\n{e.StackTrace}");
        }
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        isVideoPrepared = true;
        vp.frame = 0;
        vp.Pause();
        if (displayImage != null) displayImage.enabled = true;
        
        string videoName = vp.clip != null ? vp.clip.name : "未命名影片";
        Debug.Log($"[{debugObjectName}] 影片準備完成: {videoName}");
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        isPlaying = false;
        vp.frame = 0;
        vp.Pause();
        if (displayImage != null) displayImage.enabled = true;
        string videoName = vp.clip != null ? vp.clip.name : "未命名影片";
        Debug.Log($"[{debugObjectName}] 影片播放結束: {videoName}");
    }

    private void ShowAndPlayVideo()
    {
        if (displayImage != null) displayImage.enabled = true;
        if (videoPlayer != null)
        {
            if (videoPlayer.isPlaying)
            {
                videoPlayer.Pause();
                isPlaying = false;
                string videoName = videoPlayer.clip != null ? videoPlayer.clip.name : "未命名影片";
                Debug.Log($"[{debugObjectName}] 暫停播放影片: {videoName}");
            }
            else if (isVideoPrepared)
            {
                videoPlayer.Play();
                isPlaying = true;
                string videoName = videoPlayer.clip != null ? videoPlayer.clip.name : "未命名影片";
                Debug.Log($"[{debugObjectName}] 開始播放影片: {videoName}");
            }
            else
            {
                Debug.Log($"[{debugObjectName}] 影片尚未準備完成，無法播放");
            }
        }
    }

    public void StopAndHide()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Pause();
            videoPlayer.frame = 0;
            isPlaying = false;
            string videoName = videoPlayer.clip != null ? videoPlayer.clip.name : "未命名影片";
            Debug.Log($"[{debugObjectName}] 停止並隱藏影片: {videoName}");
        }
        if (displayImage != null) displayImage.enabled = true;
    }

    // 滑鼠/觸控進入時觸發
    public void OnPointerEnter()
    {
        Debug.Log($"[{debugObjectName}] OnPointerEnter");
    }

    // 滑鼠/觸控點擊時觸發
    public void OnPointerClick()
    {
        Debug.Log($"[{debugObjectName}] OnPointerClick");
        // 點擊時第一步就隱藏預覽圖，避免黑屏
        // Hide preview image immediately on click to avoid black screen
        if (previewImage != null)
        {
            previewImage.gameObject.SetActive(false);
            Debug.Log($"[{debugObjectName}] 點擊時隱藏預覽圖片");
        }
        ShowAndPlayVideo();
    }

    // 滑鼠/觸控離開時觸發
    public void OnPointerExit()
    {
        Debug.Log($"[{debugObjectName}] OnPointerExit");
        StopAndHide();
    }
}