/*
用來初始化Mobile Mode的陀螺儀以及不斷更新Camera視角
*/
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;

public class MobileModeController : MonoBehaviour
{
    // 非陀螺儀模式下的預設視野值
    private const float _defaultFieldOfView = 60.0f;
    
    // 場景中的主相機
    private Camera _mainCamera;

    // 陀螺儀旋轉的平滑係數
    public float gyroRotationSpeed = 30.0f;
    // 低通濾波因子 (0 ~ 1，值越低濾波越強)
    public float lowPassFilterFactor = 0.1f;

    // 儲存經過濾波後的目標旋轉
    private Quaternion _filteredGyroRotation;

    /// <summary>
    /// 取得本幀是否有觸碰到螢幕（可用於其他互動判斷）
    /// </summary>
    private bool _isScreenTouched
    {
        get
        {
            TouchControl touch = GetFirstTouchIfExists();
            return touch != null && touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began;
        }
    }

    /// <summary>
    /// 初始化：取得主相機，防止螢幕休眠，設定亮度，並啟用陀螺儀
    /// </summary>
    public void Start()
    {
        _mainCamera = Camera.main;
        // 初始濾波旋轉以目前相機角度為準
        _filteredGyroRotation = _mainCamera.transform.localRotation;

        // 防止螢幕休眠並設置亮度為最大
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Screen.brightness = 1.0f;

        // 若設備支援，啟用陀螺儀
        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
        }
    }

    /// <summary>
    /// 每一幀更新相機旋轉，使其依據陀螺儀的輸出進行平滑旋轉
    /// </summary>
    public void Update()
    {
        if (SystemInfo.supportsGyroscope)
        {
            // 取得原始陀螺儀輸出轉換後的旋轉
            Quaternion rawGyroRotation = ConvertRotation(Input.gyro.attitude);

            // 移除 roll 分量以穩定視角 (僅保留 pitch 與 yaw)
            Vector3 rawEuler = rawGyroRotation.eulerAngles;
            rawEuler.z = 0f;
            rawGyroRotation = Quaternion.Euler(rawEuler);

            // 使用低通濾波平滑原始輸出
            _filteredGyroRotation = Quaternion.Slerp(_filteredGyroRotation, rawGyroRotation, lowPassFilterFactor);

            // 再以 Slerp 方式平滑相機旋轉過渡到濾波後的旋轉
            _mainCamera.transform.localRotation = Quaternion.Slerp(
                _mainCamera.transform.localRotation,
                _filteredGyroRotation,
                Time.deltaTime * gyroRotationSpeed);
        }
    }

    /// <summary>
    /// 從當前螢幕觸控設備中取得第一個觸控點（如果存在的話）
    /// </summary>
    /// <returns>如果有觸控則返回第一個觸控點，否則返回 null</returns>
    private static TouchControl GetFirstTouchIfExists()
    {
        Touchscreen touchScreen = Touchscreen.current;
        if (touchScreen == null)
        {
            return null;
        }
        if (!touchScreen.enabled)
        {
            InputSystem.EnableDevice(touchScreen);
        }
        ReadOnlyArray<TouchControl> touches = touchScreen.touches;
        if (touches.Count == 0)
        {
            return null;
        }
        return touches[0];
    }

    /// <summary>
    /// 將陀螺儀返回的四元數轉換成 Unity 坐標系下的旋轉
    /// 注意：此處的轉換公式根據設備初始姿態可能需要調整
    /// </summary>
    /// <param name="q">陀螺儀的四元數</param>
    /// <returns>轉換後的四元數</returns>
    private Quaternion ConvertRotation(Quaternion q)
    {
        // 通常的轉換方法：先將 x 與 y 取反，再乘以一個 90 度繞 x 軸的旋轉修正
        return Quaternion.Euler(90f, 0f, 0f) * new Quaternion(-q.x, -q.y, q.z, q.w);
    }
}
