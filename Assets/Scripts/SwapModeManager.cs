/*
用於啟動與關閉不同模式該在Game Scene使用的物件以及Description功能
*/
using UnityEngine;
using UnityEngine.UI;  // 用於 Button 型別
using UnityEngine.SceneManagement; // 用於場景遍歷

public class SwapModeManager : MonoBehaviour
{
    [Header("測試模式才會用的腳本")]
    public MobileModeController mobileController;  // 請在 Inspector 指定 Mobile 模式專用腳本
    public MobileCardboardReticlePointer mobileReticlePointer; // 請在 Inspector 指定 Mobile 模式專用腳本
    [Header("測試模式才會用的UI")]
    // 測試用的 Canvas 物件，包含按鈕和搖桿
    public GameObject testCanvas;

    [Header("Player 物件（僅在對應模式啟用）")]
    public GameObject vrPlayerRoot;      // VR 模式的 Player/XR Rig 物件（只在 VR 開啟）
    public GameObject testPlayerRoot;  // 測試模式的 Player 物件

    [Header("EventSystem（僅 Mobile 模式啟用）")]
    public GameObject testEventSystem; // 僅在測試模式下啟用的 EventSystem（避免與 XR 的 EventSystem 衝突）

    // 一定要用Awake!因為要趕在上述這些物件的腳本執行start前把他們disable
    private void Awake()
    {
        Debug.LogWarning("SwapModeManager Awake");
        // 確保 GameModeManager 存在
        if (GameModeManager.Instance != null)
        {
            // 設定各 Description 物件的 layer
            int interactiveLayer = LayerMask.NameToLayer("interactive");
            int uiLayer = LayerMask.NameToLayer("UI");
            if(interactiveLayer == -1 || uiLayer == -1)
                Debug.LogWarning("interactive or UI layer not found");

            if (GameModeManager.Instance.CurrentMode == GameMode.VRMode)
            {
                // 啟用 VR 專用 Player，關閉測試專用 Player
                if (vrPlayerRoot != null) vrPlayerRoot.SetActive(true);
                if (testPlayerRoot != null) testPlayerRoot.SetActive(false);
                
                if (mobileController != null) mobileController.enabled = false;
                if (mobileReticlePointer != null) mobileReticlePointer.enabled = false;
                if (testCanvas != null) testCanvas.SetActive(false);

                // 關閉只在測試模式啟用的 EventSystem
                if (testEventSystem != null) testEventSystem.SetActive(false);

                // VR模式下，將場景中所有名稱為"Description"的物件與其子物件設置為 interactive layer
                //SetDescriptionsLayer(uiLayer);
                SetDescriptionsLayer(interactiveLayer);
            }
            else if (GameModeManager.Instance.CurrentMode == GameMode.MobileMode)
            {
                // 啟用測試專用 Player，關閉 VR 專用 Player
                if (vrPlayerRoot != null) vrPlayerRoot.SetActive(false);
                if (testPlayerRoot != null) testPlayerRoot.SetActive(true);

                if (mobileController != null) mobileController.enabled = true;
                if (mobileReticlePointer != null) mobileReticlePointer.enabled = true;
                if (testCanvas != null) testCanvas.SetActive(true);
                
                // 啟用只在測試模式啟用的 EventSystem
                if (testEventSystem != null) testEventSystem.SetActive(true);

                // 測試模式下，將場景中所有名稱為"Description"的物件與其子物件設置為 Inactive layer
                SetDescriptionsLayer(interactiveLayer);
            }
        }
        else
        {
            Debug.LogWarning("找不到 GameModeManager，請確認首頁有正確建立並保留此單例。");
        }
    }

    /// <summary>
    /// 遍歷當前場景所有物件，將名稱為"Description"的物件與其子物件設置為指定 layer(用以開關Description功能)
    /// </summary>
    /// <param name="layer">目標 layer 索引</param>
    private void SetDescriptionsLayer(int layer)
    {
        // 遍歷場景根物件
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject root in rootObjects)
        {
            ApplyLayerRecursively(root, layer);
        }
    }

    /// <summary>
    /// 遞迴應用 layer 至目標和所有子物件
    /// </summary>
    /// <param name="obj">目標物件</param>
    /// <param name="layer">目標 layer 索引</param>
    private void ApplyLayerRecursively(GameObject obj, int layer)
    {
        if (obj.name == "Description")
        {
            SetLayerRecursively(obj, layer);
        }
        else
        {
            // 若非 Description 自身，也要繼續搜尋其子物件中是否有符合名稱的物件
            foreach (Transform child in obj.transform)
            {
                ApplyLayerRecursively(child.gameObject, layer);
            }
        }
    }

    /// <summary>
    /// 將物件與其所有子物件設置為同一 layer
    /// </summary>
    /// <param name="obj">目標物件</param>
    /// <param name="layer">目標 layer 索引</param>
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}