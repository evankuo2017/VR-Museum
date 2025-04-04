/*
用於首頁背景影片的平滑循環播放器。
解決 Unity VideoPlayer 每次 loop 重播時會卡頓或閃爍的問題。
*/

using UnityEngine;
using UnityEngine.Video;

public class HomeVideoLooper : MonoBehaviour
{
    private VideoPlayer videoPlayer;

    // 若啟用，會在 Console 顯示播放事件
    public bool showDebugLogs = false;

    void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();

        // 確保 VideoPlayer 不會自動 Loop，改由自己控制
        videoPlayer.isLooping = false;

        // 等待影片準備好後播放
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.loopPointReached += OnVideoEnd;

        // 預先載入影片
        videoPlayer.Prepare();
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        if (showDebugLogs)
            Debug.Log("影片已準備好，開始播放。");

        vp.Play();
    }

    private void OnVideoEnd(VideoPlayer vp)
    {
        if (showDebugLogs)
            Debug.Log("影片結束，重播中...");

        vp.frame = 0;   // 跳回第一幀
        vp.Play();      // 立即重新播放（不卡頓）
    }
}
