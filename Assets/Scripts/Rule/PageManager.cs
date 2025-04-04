/*
用來處理兩個Rule場景的畫面切換與播放影片
*/

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class PageManager : MonoBehaviour
{
    // 所有圖片頁面，請依順序在 Inspector 中指定（例如：第一頁、第二頁⋯⋯最後一頁）
    public GameObject[] pages;

    // 「上一頁」、「下一頁」及回首頁的按鈕（這些按鈕通常是獨立於各頁面之外的 UI 元件）
    public Button previousButton;
    public Button nextButton;
    public Button homeButton;
    
    // 「跳過」按鈕：僅在影片播放期間顯示，供使用者跳過影片
    public Button skipButton;

    // 影片播放相關的組件
    public VideoPlayer videoPlayer;  // 用於控制影片播放的 VideoPlayer 元件
    public GameObject videoObject;   // 包含 VideoPlayer 的 GameObject（例如 RawImage 或 3D 物件）

    // 記錄目前顯示的頁面索引，初始為 0 (第一頁)
    private int currentPageIndex = 0;

    void Start()
    {
        // 初始化：顯示第一頁並根據頁面位置更新按鈕狀態
        UpdatePages();
        UpdateButtons();

        // 初始隱藏「跳過」按鈕
        skipButton.gameObject.SetActive(false);

        // 當影片播放結束時，自動呼叫 OnVideoFinished 方法
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoFinished;
        }
    }

    /// <summary>
    /// 點擊「下一頁」按鈕時呼叫，若未到最後一頁則切換到下一頁
    /// </summary>
    public void ShowNextPage()
    {
        if (currentPageIndex < pages.Length - 1)
        {
            currentPageIndex++;
            UpdatePages();
            UpdateButtons();
        }
    }

    /// <summary>
    /// 點擊「上一頁」按鈕時呼叫，若未到第一頁則切換到上一頁
    /// </summary>
    public void ShowPreviousPage()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            UpdatePages();
            UpdateButtons();
        }
    }

    /// <summary>
    /// 根據 currentPageIndex 更新各頁面的顯示狀態
    /// 只有索引與 currentPageIndex 相符的頁面會被設為 active，其它頁面則隱藏
    /// </summary>
    void UpdatePages()
    {
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(i == currentPageIndex);
        }
    }

    /// <summary>
    /// 根據當前頁面位置更新「上一頁」與「下一頁」按鈕的顯示狀態
    /// 當處於第一頁時，隱藏「上一頁」按鈕；處於最後一頁時，隱藏「下一頁」按鈕
    /// </summary>
    void UpdateButtons()
    {
        previousButton.gameObject.SetActive(currentPageIndex != 0);
        nextButton.gameObject.SetActive(currentPageIndex != pages.Length - 1);
    }

    /// <summary>
    /// 停止影片播放並重置至第一幀
    /// </summary>
    void ResetVideo()
    {
        videoPlayer.frame = 0;
        videoPlayer.Pause();
    }

    /// <summary>
    /// 點擊影片播放按鈕時呼叫的方法
    /// 此方法會隱藏目前的頁面與部份按鈕，重置影片到第一幀，
    /// 啟用影片撥放，並啟動「跳過」按鈕
    /// </summary>
    public void PlayVideo()
    {
        // 重置影片，確保每次播放都從第一幀開始
        ResetVideo();

        // 隱藏目前頁面（該頁面內包含影片播放按鈕，因此會一起隱藏）
        pages[currentPageIndex].SetActive(false);

        // 隱藏獨立於頁面的按鈕：上一頁與回首頁
        previousButton.gameObject.SetActive(false);
        homeButton.gameObject.SetActive(false);

        // 啟用「跳過」按鈕，僅在影片播放期間顯示
        skipButton.gameObject.SetActive(true);

        // 開始撥放影片
        videoPlayer.Play();
    }

    /// <summary>
    /// 當影片播放完畢時，由 VideoPlayer 的事件觸發呼叫
    /// 此方法將呼叫 EndVideo() 來處理播放結束後的共同行為
    /// </summary>
    /// <param name="vp">觸發事件的 VideoPlayer 參考</param>
    void OnVideoFinished(VideoPlayer vp)
    {
        EndVideo();
    }

    /// <summary>
    /// 處理影片播放結束或被跳過後的共同行為：
    /// 1. 重置影片到第一幀並暫停
    /// 2. 用最後一張圖片覆蓋影片畫面
    /// 3. 恢復回首頁按鈕與上下頁按鈕的顯示
    /// 4. 隱藏「跳過」按鈕
    /// </summary>
    void EndVideo()
    {
        // 重置影片並暫停撥放
        ResetVideo();

        // 讓最後一張圖片（即當前頁面）重新顯示，覆蓋影片畫面
        pages[currentPageIndex].SetActive(true);

        // 恢復回首頁按鈕的顯示
        homeButton.gameObject.SetActive(true);

        // 根據目前頁面狀態更新上一頁與下一頁按鈕
        UpdateButtons();

        // 隱藏「跳過」按鈕，因為影片已不在撥放中
        skipButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// 點擊「跳過」按鈕時呼叫的方法
    /// 此方法與影片播放完畢後的處理一致，
    /// 因此直接調用 EndVideo() 來完成重置、覆蓋影片及恢復 UI
    /// </summary>
    public void SkipVideo()
    {
        EndVideo();
    }
}
