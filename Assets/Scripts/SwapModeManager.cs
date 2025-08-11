/*
用於啟動與關閉不同模式該在Game Scene使用的物件
*/
using UnityEngine;
using UnityEngine.UI;  // 用於 Button 型別
using UnityEngine.SceneManagement; // 用於場景遍歷

public class SwapModeManager : MonoBehaviour
{
    [Header("同一物件上的模式初始化腳本")]
    public VrModeController vrController;          // 請在 Inspector 指定 VR 模式專用腳本
    public MobileModeController mobileController;  // 請在 Inspector 指定 Mobile 模式專用腳本

    [Header("同一物件上的模式互動腳本")]
    public CardboardReticlePointer vrReticlePointer;          // 請在 Inspector 指定 VR 模式專用腳本
    public MobileCardboardReticlePointer mobileReticlePointer; // 請在 Inspector 指定 Mobile 模式專用腳本

    [Header("VR模式才會用的UI")]
    public Button reverse;
    public Image mask;

    [Header("Mobile模式才會用的UI")]
    public Button BackToMenu;
    
    // 將 Joystick 改為 GameObject 以整個物件做啟用/停用
    public GameObject fixedJoystickObject;

    [Header("Player 物件（僅在對應模式啟用）")]
    public GameObject vrPlayerRoot;      // VR 模式的 Player/XR Rig 物件（只在 VR 開啟）
    public GameObject mobilePlayerRoot;  // Mobile 模式的 Player 物件（只在 Mobile 開啟）

    [Header("EventSystem（僅 Mobile 模式啟用）")]
    public GameObject mobileEventSystem; // 僅在 Mobile 模式下啟用的 EventSystem（避免與 XR 的 EventSystem 衝突）

    // 一定要用Awake!因為要趕在上述這些物件的腳本執行start前把他們disable
    private void Awake()
    {
        Debug.LogWarning("Awake");
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
                if (vrController != null) vrController.enabled = true;
                if (vrReticlePointer != null) vrReticlePointer.enabled = true;
                if (reverse != null) reverse.gameObject.SetActive(true);
                if (mask != null) mask.gameObject.SetActive(true);
                // 啟用 VR 專用 Player，關閉 Mobile 專用 Player
                if (vrPlayerRoot != null) vrPlayerRoot.SetActive(true);
                if (mobilePlayerRoot != null) mobilePlayerRoot.SetActive(false);
                // 關閉只在 Mobile 模式啟用的 EventSystem
                if (mobileEventSystem != null) mobileEventSystem.SetActive(false);

                if (mobileController != null) mobileController.enabled = false;
                if (mobileReticlePointer != null) mobileReticlePointer.enabled = false;
                if (BackToMenu != null) BackToMenu.gameObject.SetActive(false);
                if (fixedJoystickObject != null) fixedJoystickObject.SetActive(false);

                // VR模式下，將場景中所有名稱為"Description"的物件與其子物件設置為 UI layer
                SetDescriptionsLayer(uiLayer);
            }
            else if (GameModeManager.Instance.CurrentMode == GameMode.MobileMode)
            {
                if (vrController != null) vrController.enabled = false;
                if (vrReticlePointer != null) vrReticlePointer.enabled = false;
                if (reverse != null) reverse.gameObject.SetActive(false);
                if (mask != null) mask.gameObject.SetActive(false);

                if (mobileController != null) mobileController.enabled = true;
                if (mobileReticlePointer != null) mobileReticlePointer.enabled = true;
                if (BackToMenu != null) BackToMenu.gameObject.SetActive(true);
                if (fixedJoystickObject != null) fixedJoystickObject.SetActive(true);
                // 啟用 Mobile 專用 Player，關閉 VR 專用 Player
                if (vrPlayerRoot != null) vrPlayerRoot.SetActive(false);
                if (mobilePlayerRoot != null) mobilePlayerRoot.SetActive(true);
                // 啟用只在 Mobile 模式啟用的 EventSystem
                if (mobileEventSystem != null) mobileEventSystem.SetActive(true);

                // Mobile模式下，將場景中所有名稱為"Description"的物件與其子物件設置為 Inactive layer
                SetDescriptionsLayer(interactiveLayer);
            }
        }
        else
        {
            Debug.LogWarning("找不到 GameModeManager，請確認首頁有正確建立並保留此單例。");
        }
    }

    /// <summary>
    /// 遍歷當前場景所有物件，將名稱為"Description"的物件與其子物件設置為指定 layer
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