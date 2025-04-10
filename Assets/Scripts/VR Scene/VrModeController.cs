/*
用來初始化 XR 服務，並控制 VR 模式的啟用與退出
 */
using System.Collections;
using Google.XR.Cardboard;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.SceneManagement;

public class VrModeController : MonoBehaviour
{
    // 非 VR 模式下默认的视野
    private const float _defaultFieldOfView = 60.0f;
    
    // 場景主相機
    private Camera _mainCamera;

    // 加入：等待外部觸發的旗標
    private bool _vrEnterRequested = false;
    private bool _closeButtonHandled = false;
    private bool _gearButtonHandled = false;

    public void Start()
    {
        _mainCamera = Camera.main;
        // 防止螢幕休眠與調整亮度
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Screen.brightness = 1.0f;

        // 檢查設備參數，如無則掃描（Cardboard 相關）
        if (!Api.HasDeviceParams())
        {
            Api.ScanDeviceParams();
        }
    }

    public void Update()
    {
        if (_isVrModeEnabled)
        {
            HandleVrButtons();
            Api.UpdateScreenParams(); // Cardboard SDK 要求每幀更新
        }
        else
        {
            if (_vrEnterRequested)
            {
                _vrEnterRequested = false;
                EnterVR();
            }
        }
    }

    private void HandleVrButtons()
    {
        // 改善退出 VR 的按鈕靈敏度：當按下並釋放一次即觸發
        if (Api.IsCloseButtonPressed)
        {
            if (!_closeButtonHandled)
            {
                _closeButtonHandled = true;
            }
        }
        else
        {
            if (_closeButtonHandled)
            {
                _closeButtonHandled = false;
                ExitVR(); // 放開按鈕時觸發 ExitVR
            }
        }

        // 處理齒輪按鈕，只觸發一次
        if (Api.IsGearButtonPressed)
        {
            if (!_gearButtonHandled)
            {
                _gearButtonHandled = true;
                Api.ScanDeviceParams();
            }
        }
        else
        {
            _gearButtonHandled = false;
        }
    }

    // 判斷 XR 是否已初始化完成（代表 VR 模式是否啟用）
    private bool _isVrModeEnabled
    {
        get { return XRGeneralSettings.Instance.Manager.isInitializationComplete; }
    }

    private void EnterVR()
    {
        StartCoroutine(StartXR());
        if (Api.HasNewDeviceParams())
        {
            Api.ReloadDeviceParams();
        }
    }

    // 啟動 XR 服務的協程
    private IEnumerator StartXR()
    {
        Debug.Log("Initializing XR...");
        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

        if (XRGeneralSettings.Instance.Manager.activeLoader == null)
        {
            Debug.LogError("Initializing XR Failed.");
        }
        else
        {
            Debug.Log("XR initialized.");
            yield return null; // 等一幀，防止畫面錯亂
            XRGeneralSettings.Instance.Manager.StartSubsystems();
            Debug.Log("XR started.");
        }
    }

    // 退出 VR 模式：停止 XR 子系統、解除初始化 XR Loader，並載入 Menu 場景
    private void ExitVR()
    {
        Debug.Log("Stopping XR...");
        XRGeneralSettings.Instance.Manager.StopSubsystems();
        Debug.Log("XR stopped.");

        Debug.Log("Deinitializing XR...");
        XRGeneralSettings.Instance.Manager.DeinitializeLoader();
        Debug.Log("XR deinitialized.");

        // 重置相機參數
        if (_mainCamera != null)
        {
            _mainCamera.ResetAspect();
            _mainCamera.fieldOfView = _defaultFieldOfView;
        }

        // 載入場景
        SceneManager.LoadScene("Menu");
    }

    // 加入：讓外部（如影片播放完畢）觸發進入 VR 模式
    public void RequestEnterVR()
    {
        _vrEnterRequested = true;
    }
}
