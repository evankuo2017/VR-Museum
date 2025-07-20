/*
    Menu及Rule頁面使用，持續等待直到所有影片都準備好就把loading bar關掉
*/
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class NormalVideoPreparationController : MonoBehaviour
{
    // 多個影片播放元件，可在 Inspector 中指定
    public VideoPlayer[] videoPlayers;
    // Loading Panel（包含進度條）的物件
    public GameObject loadingPanel;
    // 進度條元件
    public Slider progressBar;
    // 最少顯示 Loading 畫面的時間（秒）
    public float minLoadingTime = 1.0f;
    // 影片等待逾時的最大時間（秒）
    public float videoPrepareTimeout = 5.0f;

    // 加入：參考場景中的 VrModeController，用來觸發 VR 模式
    private VrModeController vrModeController;

    private void Start()
    {
        // 若尚未指定，嘗試取得同一物件上的所有 VideoPlayer
        if (videoPlayers == null || videoPlayers.Length == 0)
        {
            videoPlayers = GetComponents<VideoPlayer>();
        }

        // 加入：搜尋場景中的 VrModeController
        vrModeController = FindObjectOfType<VrModeController>();

        // 依序檢查每個 VideoPlayer，若尚未準備好就開始 Prepare
        foreach (VideoPlayer vp in videoPlayers)
        {
            if (vp != null && !vp.isPrepared)
            {
                vp.Prepare();
            }
        }
        StartCoroutine(WaitForAllVideosPrepared());
    }

    IEnumerator WaitForAllVideosPrepared()
    {
        float startTime = Time.time;
        float elapsed = 0f;
        bool allPrepared = false;

        // 持續等待直到所有影片都準備好或逾時
        while (elapsed < videoPrepareTimeout)
        {
            elapsed = Time.time - startTime;
            allPrepared = true;
            foreach (VideoPlayer vp in videoPlayers)
            {
                if (vp != null && !vp.isPrepared)
                {
                    allPrepared = false;
                    break;
                }
            }
            if (allPrepared)
            {
                break;
            }
            // 模擬進度（因為 VideoPlayer 沒有直接的進度屬性）
            if (progressBar != null)
            {
                float progress = Mathf.Clamp01(elapsed / videoPrepareTimeout);
                progressBar.value = progress;
            }
            yield return null;
        }

        // 確保至少顯示了 minLoadingTime 秒的 Loading 畫面
        float totalWait = Time.time - startTime;
        if (totalWait < minLoadingTime)
        {
            yield return new WaitForSeconds(minLoadingTime - totalWait);
        }

        // 準備完成或逾時後，隱藏 Loading 畫面與進度條
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(false);
        }

        // 加入：通知 VrModeController 進入 VR 模式
        if (vrModeController != null)
        {
            vrModeController.RequestEnterVR();
        }
        else
        {
            Debug.LogWarning("VrModeController not found in scene.");
        }
    }
}