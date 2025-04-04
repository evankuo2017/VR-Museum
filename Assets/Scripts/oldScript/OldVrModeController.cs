//-----------------------------------------------------------------------
// <copyright file="VrModeController.cs" company="Google LLC">
// Copyright 2020 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections;
using Google.XR.Cardboard;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.XR;
using UnityEngine.XR.Management;

using InputSystemTouchPhase = UnityEngine.InputSystem.TouchPhase;

public class OldVrModeController : MonoBehaviour
{
    // 非 VR 模式下默认的视野
    private const float _defaultFieldOfView = 60.0f;

    // 场景主摄像机
    private Camera _mainCamera;

    // 用于陀螺仪旋转的平滑系数，可以根据需要调整
    public float gyroRotationSpeed = 30.0f;

    /// <summary>
    /// 判断当前这一帧是否有屏幕触碰
    /// </summary>
    private bool _isScreenTouched
    {
        get
        {
            TouchControl touch = GetFirstTouchIfExists();
            return touch != null && touch.phase.ReadValue() == InputSystemTouchPhase.Began;
        }
    }

    /// <summary>
    /// 判断当前是否为 VR 模式（XR 初始化完毕即认为处于 VR 模式）
    /// </summary>
    private bool _isVrModeEnabled
    {
        get
        {
            return XRGeneralSettings.Instance.Manager.isInitializationComplete;
        }
    }

    /// <summary>
    /// Start() 中初始化主摄像机、设置屏幕常亮、启用陀螺仪等
    /// </summary>
    public void Start()
    {
        _mainCamera = Camera.main;

        // 防止屏幕休眠和设置亮度为最大
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Screen.brightness = 1.0f;

        // 检查设备参数，如果不存在则扫描（Cardboard 相关）
        if (!Api.HasDeviceParams())
        {
            Api.ScanDeviceParams();
        }

        // 即使在非 VR 模式下也启用陀螺仪（需确保设备支持）
        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
        }
    }

    /// <summary>
    /// Update() 中判断当前模式并分别处理
    /// </summary>
    public void Update()
    {
        if (_isVrModeEnabled)
        {
            if (Api.IsCloseButtonPressed)
            {
                ExitVR();
            }
            if (Api.IsGearButtonPressed)
            {
                Api.ScanDeviceParams();
            }
            Api.UpdateScreenParams();
        }
        else
        {
            // 非 VR 模式下，利用陀螺仪更新摄像机旋转
            if (SystemInfo.supportsGyroscope)
            {
                // 使用 Slerp 平滑更新旋转，转换函数用于将陀螺仪的四元数转换为 Unity 坐标系下的旋转
                _mainCamera.transform.localRotation = Quaternion.Slerp(
                    _mainCamera.transform.localRotation,
                    ConvertRotation(Input.gyro.attitude),
                    Time.deltaTime * gyroRotationSpeed);
            }

            // 若需要可在此处继续监听触摸并进入 VR 模式
            if (_isScreenTouched)
            {
                //EnterVR();
            }
        }
    }

    /// <summary>
    /// 获取当前屏幕上第一个触摸点
    /// </summary>
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
    /// 进入 VR 模式：启动 XR 子系统
    /// </summary>
    private void EnterVR()
    {
        StartCoroutine(StartXR());
        if (Api.HasNewDeviceParams())
        {
            Api.ReloadDeviceParams();
        }
    }

    /// <summary>
    /// 退出 VR 模式：关闭 XR 子系统，并恢复摄像机的默认视野等
    /// </summary>
    private void ExitVR()
    {
        StopXR();
    }

    /// <summary>
    /// 启动 XR 子系统的协程
    /// </summary>
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
            Debug.Log("Starting XR...");
            XRGeneralSettings.Instance.Manager.StartSubsystems();
            Debug.Log("XR started.");
        }
    }

    /// <summary>
    /// 关闭并反初始化 XR 子系统，同时恢复摄像机属性
    /// </summary>
    private void StopXR()
    {
        Debug.Log("Stopping XR...");
        XRGeneralSettings.Instance.Manager.StopSubsystems();
        Debug.Log("XR stopped.");

        Debug.Log("Deinitializing XR...");
        XRGeneralSettings.Instance.Manager.DeinitializeLoader();
        Debug.Log("XR deinitialized.");

        _mainCamera.ResetAspect();
        _mainCamera.fieldOfView = _defaultFieldOfView;
    }

    /// <summary>
    /// 将陀螺仪返回的四元数转换为 Unity 坐标系下的旋转
    /// 注意：此转换公式可能需要根据设备的初始姿态及应用需求进行调整
    /// </summary>
    private Quaternion ConvertRotation(Quaternion q)
    {
        // 此处常用的转换方法为：先将陀螺仪姿态取负（对 x、y 取反），再乘以一个 90 度绕 x 轴的旋转修正
        return Quaternion.Euler(90f, 0f, 0f) * new Quaternion(-q.x, -q.y, q.z, q.w);
    }
}