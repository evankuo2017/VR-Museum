
/*
    於遊戲場景中控制每影片的載入、卸載、撥放與停止時機
    Added first frame overlay to prevent gray flash
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
    private Texture2D firstFrameTexture; // Store first frame to prevent gray flash
    private bool playOnPrepared = false; // Flag to indicate if we should play when prepared
    private void Start()
    {
        InitializeComponents();
        // 如果沒有設定videoPath，嘗試從VideoPlayer的clip獲取
        if (string.IsNullOrEmpty(videoPath) && videoPlayer != null && videoPlayer.clip != null)
        {
            videoPath = videoPlayer.clip.name;
            Debug.Log($"[{gameObject.name}] 自動設定影片路徑: {videoPath}");
        }
    }

    private void InitializeComponents()
    {
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.prepareCompleted += OnVideoPrepared;
        }
    }

    // 動態初始化影片
    public void InitializeVideo()
    {
        if (isVideoLoaded || videoPlayer == null) return;
        
        // 如果videoPlayer.clip為null，嘗試重新載入
        if (videoPlayer.clip == null && !string.IsNullOrEmpty(videoPath))
        {
            Debug.Log($"[{gameObject.name}] 嘗試動態載入影片: {videoPath}");
            
            VideoClip clip = null;
            
            clip = Resources.Load<VideoClip>($"video/{videoPath}");
            
            if (clip != null)
            {
                videoPlayer.clip = clip;
                Debug.Log($"[{gameObject.name}] 成功載入影片: {clip.name}");
            }
            else
            {
                Debug.LogError($"[{gameObject.name}] 無法載入影片: {videoPath}");
                return;
            }
        }
        isVideoLoaded = true;
        videoPlayer.Prepare();
    }

    // 動態卸載影片
    public void UnloadVideo()
    {
        if (!isVideoLoaded || videoPlayer == null) return;
        
        if (videoPlayer.isPlaying) videoPlayer.Stop();
        
        Debug.Log($"[{gameObject.name}] 開始卸載影片: {videoPlayer.clip.name}");
        
        // Clean up first frame texture
        if (firstFrameTexture != null)
        {
            Destroy(firstFrameTexture);
            firstFrameTexture = null;
        }
        
        // 清空影片資源
        videoPlayer.clip = null;
        isVideoLoaded = false;
        isVideoPrepared = false;
        isPlaying = false;
        
        // 重新顯示image
        if (previewImage != null) 
        {
            previewImage.gameObject.SetActive(true);
            Debug.Log($"[{gameObject.name}] 已重新顯示預覽圖片");
        }
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        isVideoPrepared = true;
        
        // Capture first frame to use as overlay and prevent gray flash
        StartCoroutine(CaptureFirstFrame(vp));
        
        string videoName = vp.clip != null ? vp.clip.name : "未命名影片";
        Debug.Log($"[{gameObject.name}] 影片準備完成: {videoName}");
        // 如果有等待播放，準備好後自動播放
        if (playOnPrepared)
        {
            playOnPrepared = false;
            // ShowAndPlayVideo();
            StartCoroutine(PlayAfterFirstFrame(vp));
        }
        else
        {
            // 只抓第一幀，不播放
            StartCoroutine(CaptureFirstFrame(vp));
        }
    }

    // 新增協程
    private IEnumerator PlayAfterFirstFrame(VideoPlayer vp)
    {
        yield return StartCoroutine(CaptureFirstFrame(vp));
        // 設定 displayImage 為影片畫面
        if (displayImage != null)
        {
            displayImage.texture = vp.texture;
            displayImage.enabled = true;
        }
        vp.Play();
        isPlaying = true;
        string videoName = vp.clip != null ? vp.clip.name : "未命名影片";
        Debug.Log($"[{gameObject.name}] 準備完成後自動播放影片: {videoName}");
    }

    private IEnumerator CaptureFirstFrame(VideoPlayer vp)
    {
        // Set video to first frame and play briefly to capture it
        vp.time = 0;
        vp.Play();
        
        // Wait a couple of frames to ensure the video texture is updated
        yield return null;
        yield return null;
        
        // Capture the first frame
        if (vp.texture != null)
        {
            RenderTexture currentRT = RenderTexture.active;
            RenderTexture tempRT = RenderTexture.GetTemporary(vp.texture.width, vp.texture.height, 0, RenderTextureFormat.ARGB32);
            
            Graphics.Blit(vp.texture, tempRT);
            RenderTexture.active = tempRT;
            
            firstFrameTexture = new Texture2D(tempRT.width, tempRT.height, TextureFormat.RGB24, false);
            firstFrameTexture.ReadPixels(new Rect(0, 0, tempRT.width, tempRT.height), 0, 0);
            firstFrameTexture.Apply();
            
            RenderTexture.active = currentRT;
            RenderTexture.ReleaseTemporary(tempRT);
            
            Debug.Log($"[{gameObject.name}] 已擷取第一幀作為覆蓋圖");
        }
        
        // Pause video and reset to first frame
        vp.Pause();
        vp.time = 0;
        
        // Set the first frame texture to display image to prevent gray flash
        if (displayImage != null && firstFrameTexture != null)
        {
            displayImage.texture = firstFrameTexture;
            displayImage.enabled = true;
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        isPlaying = false;
        vp.time = 0;
        vp.Pause();
        
        // Show first frame again instead of gray screen
        if (displayImage != null && firstFrameTexture != null)
        {
            displayImage.texture = firstFrameTexture;
            displayImage.enabled = true;
        }
        
        string videoName = vp.clip != null ? vp.clip.name : "未命名影片";
        Debug.Log($"[{gameObject.name}] 影片播放結束: {videoName}");
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
                
                // Show first frame when paused
                // if (displayImage != null && firstFrameTexture != null)
                // {
                //     displayImage.texture = firstFrameTexture;
                // }
                
                string videoName = videoPlayer.clip != null ? videoPlayer.clip.name : "未命名影片";
                Debug.Log($"[{gameObject.name}] 暫停播放影片: {videoName}");
            }
            else if (isVideoPrepared)
            {
                // Switch to video texture when playing
                if (displayImage != null)
                {
                    displayImage.texture = videoPlayer.texture;
                }
                
                videoPlayer.Play();
                isPlaying = true;
                string videoName = videoPlayer.clip != null ? videoPlayer.clip.name : "未命名影片";
                Debug.Log($"[{gameObject.name}] 開始播放影片: {videoName}");
            }
            else
            {
                Debug.Log($"[{gameObject.name}] 影片尚未準備完成，無法播放");
                playOnPrepared = true;
                videoPlayer.Prepare();
            }
        }
    }

    public void StopAndHide()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Pause();
            videoPlayer.time = 0;
            isPlaying = false;
            
            // Show first frame when stopped
            if (displayImage != null && firstFrameTexture != null)
            {
                displayImage.texture = firstFrameTexture;
            }
            
            string videoName = videoPlayer.clip != null ? videoPlayer.clip.name : "未命名影片";
            Debug.Log($"[{gameObject.name}] 停止並隱藏影片: {videoName}");
        }
        if (displayImage != null) displayImage.enabled = true;
    }

    // 滑鼠/觸控進入時觸發
    public void OnPointerEnter()
    {
        Debug.Log($"[{gameObject.name}] OnPointerEnter");
    }

    // 滑鼠/觸控點擊時觸發
    public void OnPointerClick()
    {
        Debug.Log($"[{gameObject.name}] OnPointerClick");
        // 點擊時第一步就隱藏預覽圖，避免黑屏
        // Hide preview image immediately on click to avoid black screen
        if (previewImage != null)
        {
            previewImage.gameObject.SetActive(false);
            Debug.Log($"[{gameObject.name}] 點擊時隱藏預覽圖片");
        }
        ShowAndPlayVideo();
    }

    // 滑鼠/觸控離開時觸發
    public void OnPointerExit()
    {
        Debug.Log($"[{gameObject.name}] OnPointerExit");
        StopAndHide();
    }

    private void OnDestroy()
    {
        // Clean up first frame texture when object is destroyed
        if (firstFrameTexture != null)
        {
            Destroy(firstFrameTexture);
            firstFrameTexture = null;
        }
    }
}
