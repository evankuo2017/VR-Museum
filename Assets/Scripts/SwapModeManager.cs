using UnityEngine;
using UnityEngine.UI;  // 用於 Button 型別

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

    [Header("Mobile模式才會用的UI")]
    public Button BackToMenu;
    
    // 將 Joystick 改為 GameObject 以整個物件做啟用/停用
    public GameObject fixedJoystickObject;

    // 一定要用Awake!因為要趕在上述這些物件的腳本執行start前把他們disable
    private void Awake()
    {
        Debug.LogWarning("Awake");
        // 確保 GameModeManager 存在
        if (GameModeManager.Instance != null)
        {
            if (GameModeManager.Instance.CurrentMode == GameMode.VRMode)
            {
                if (vrController != null) vrController.enabled = true;
                if (vrReticlePointer != null) vrReticlePointer.enabled = true;
                if (reverse != null) reverse.gameObject.SetActive(true);

                if (mobileController != null) mobileController.enabled = false;
                if (mobileReticlePointer != null) mobileReticlePointer.enabled = false;
                if (BackToMenu != null) BackToMenu.gameObject.SetActive(false);
                if (fixedJoystickObject != null) fixedJoystickObject.SetActive(false);
            }
            else if (GameModeManager.Instance.CurrentMode == GameMode.MobileMode)
            {
                if (vrController != null) vrController.enabled = false;
                if (vrReticlePointer != null) vrReticlePointer.enabled = false;
                if (reverse != null) reverse.gameObject.SetActive(false);

                if (mobileController != null) mobileController.enabled = true;
                if (mobileReticlePointer != null) mobileReticlePointer.enabled = true;
                if (BackToMenu != null) BackToMenu.gameObject.SetActive(true);
                if (fixedJoystickObject != null) fixedJoystickObject.SetActive(true);
            }
        }
        else
        {
            Debug.LogWarning("找不到 GameModeManager，請確認首頁有正確建立並保留此單例。");
        }
    }
}
