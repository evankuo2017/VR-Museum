/*
用來初始化 XR 服務，並控制 VR 模式的啟用與退出
*/
using System.Collections;
using Google.XR.Cardboard;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Linq; // 引入 LINQ 支援 FirstOrDefault

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

    // 加入：避免重複觸發退出
    private bool _isExiting = false;

    // canvas Loading遮罩
    public Image mask;

    // Discription Window 物件
    private GameObject targetImage;
    private TMP_Text targetTitle;
    private TMP_Text targetText;

    public void Start()
    {
        _mainCamera = Camera.main;
        // 防止螢幕休眠與調整亮度
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        //Screen.brightness = 1.0f;

        // 檢查設備參數，如無則掃描（Cardboard 相關）
        if (!Api.HasDeviceParams())
        {
            Api.ScanDeviceParams();
        }

        targetImage = Resources.FindObjectsOfTypeAll<GameObject>()
            .FirstOrDefault(go => go.name == "Discription Window");

        targetText = Resources.FindObjectsOfTypeAll<TMP_Text>()
            .FirstOrDefault(txt => txt.name == "Discribe Text");

        targetTitle = Resources.FindObjectsOfTypeAll<TMP_Text>()
            .FirstOrDefault(txt => txt.name == "Discribe Title");
        // 設定文字的Width
        targetText.rectTransform.sizeDelta = new Vector2(800, targetText.rectTransform.sizeDelta.y);
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
        // 改善退出 VR 的按鈕靈敏度：按下當下即觸發，避免錯過觸發時機
        if (Api.IsCloseButtonPressed && !_closeButtonHandled && !_isExiting)
        {
            Debug.Log("Close button pressed → ExitVR triggered");
            _closeButtonHandled = true;
            ExitVR(); // 按下按鈕時立即觸發 ExitVR
        }
        else if (!Api.IsCloseButtonPressed)
        {
            _closeButtonHandled = false;
        }

        // 處理齒輪按鈕，只觸發一次
        if (Api.IsGearButtonPressed)
        {
            if (!_gearButtonHandled)
            {
                Debug.Log("Gear button pressed → ScanDeviceParams");
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
        Debug.Log("Request to Enter VR");
        StartCoroutine(StartXR());
        if (Api.HasNewDeviceParams())
        {
            Api.ReloadDeviceParams();
        }
        if (mask != null) mask.gameObject.SetActive(false);
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
        if (_isExiting) return;
        _isExiting = true;

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
        Debug.Log("External trigger: RequestEnterVR");
        _vrEnterRequested = true;
    }
}
