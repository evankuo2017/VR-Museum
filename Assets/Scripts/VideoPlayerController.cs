using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoPlayerController : MonoBehaviour
{
    [Header("reference objects")]
    [SerializeField] private RawImage displayImage;
    [SerializeField] private VideoPlayer videoPlayer;
    
    
    private void Start()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        if (videoPlayer != null)
        {
            // 設置基本屬性
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.loopPointReached += OnVideoFinished;
            
            // 準備視頻
            videoPlayer.Prepare();
            videoPlayer.prepareCompleted += OnVideoPrepared;

        }
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        // 視頻準備完成時，設置第一幀
        vp.frame = 0;
        vp.Pause();
        
        if (displayImage != null)
        {
            displayImage.enabled = true;
        }
        
        // 移除事件監聽
        vp.prepareCompleted -= OnVideoPrepared;
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        vp.frame = 0;
        vp.Pause();
        displayImage.enabled = true;
    }
    
    private void ShowAndPlayVideo()
    {
        if (displayImage != null)
        {
            displayImage.enabled = true;
        }

        if (videoPlayer != null)
        {
            if (videoPlayer.isPlaying)
            {
                videoPlayer.Pause();
            }
            else
            {
                // 如果是暫停狀態，從當前幀繼續播放
                videoPlayer.Play();
            }
        }
    }

    public void StopAndHide()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Pause();  // 使用 Pause 而不是 Stop
            videoPlayer.frame = 0;
        }
        
        if (displayImage != null)
        {
            displayImage.enabled = true;
        }
    }

    public void OnPointerEnter()
    {
        Debug.Log("OnPointerEnter");
    }

    public void OnPointerClick()
    {
        Debug.Log("OnPointerClick");
        ShowAndPlayVideo();
    }

    public void OnPointerExit()
    {
        Debug.Log("OnPointerExit");
        StopAndHide();
    }
}