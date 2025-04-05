/*
用來初始化手機模式與用陀螺儀控制視角變化
*/
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;

public class MobileModeController : MonoBehaviour
{
    // 預設視野值（未來可用於調整 FOV）
    private const float _defaultFieldOfView = 60.0f;
    
    // 主相機
    private Camera _mainCamera;

    // 旋轉速度參數（調整此值可改變更新速度）
    public float rotationSpeed = 30.0f;

    // 儲存平滑後的旋轉
    private Quaternion _filteredGyroRotation;

    // 噪音閾值：角度差低於此值視為噪音，不做更新（單位：度）
    public float noiseThreshold = 0.5f;

    public void Start()
    {
        _mainCamera = Camera.main;
        
        // 如果設備支援陀螺儀，先用一次讀數初始化，避免開始時大幅偏差
        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
            Quaternion initialGyro = ConvertRotation(Input.gyro.attitude);
            Vector3 initialEuler = initialGyro.eulerAngles;
            initialEuler.z = 0f;
            _filteredGyroRotation = Quaternion.Euler(initialEuler);
        }
        else
        {
            _filteredGyroRotation = _mainCamera.transform.localRotation;
        }

        // 防止螢幕休眠與調整亮度
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Screen.brightness = 1.0f;
    }

    public void Update()
    {
        if (SystemInfo.supportsGyroscope)
        {
            // 取得最新陀螺儀讀數並進行座標修正
            Quaternion rawGyroRotation = ConvertRotation(Input.gyro.attitude);
            Vector3 rawEuler = rawGyroRotation.eulerAngles;
            rawEuler.z = 0f;  // 移除 roll 分量，穩定視角
            rawGyroRotation = Quaternion.Euler(rawEuler);

            // 計算目前與最新讀數之間的角度差（單位：度）
            float angleDiff = Quaternion.Angle(_filteredGyroRotation, rawGyroRotation);

            // 如果角度差小於噪音閾值，認為是噪音，保持原有旋轉，不更新
            if (angleDiff < noiseThreshold)
            {
                // 不更新 _filteredGyroRotation，保持穩定
                _mainCamera.transform.localRotation = _filteredGyroRotation;
                return;
            }

            // 當角差越大，更新速度應該更快
            // 這裡以 20° 為門檻，並設定一個基礎更新比例（baseline）
            float baseline = 0.3f;  // 最小更新比例
            float adaptiveFactor = baseline + (1 - baseline) * Mathf.Clamp01(angleDiff / 20f);

            // 使用指數平滑公式，並乘上自適應係數
            float smoothingFactor = 1 - Mathf.Exp(-rotationSpeed * Time.deltaTime * adaptiveFactor);
            _filteredGyroRotation = Quaternion.Slerp(_filteredGyroRotation, rawGyroRotation, smoothingFactor);

            // 將結果應用到主相機
            _mainCamera.transform.localRotation = _filteredGyroRotation;
        }
    }

    private Quaternion ConvertRotation(Quaternion q)
    {
        // 常見的轉換：先將 x 與 y 取反，再乘以 90° 繞 x 軸的修正
        return Quaternion.Euler(90f, 0f, 0f) * new Quaternion(-q.x, -q.y, q.z, q.w);
    }
}

