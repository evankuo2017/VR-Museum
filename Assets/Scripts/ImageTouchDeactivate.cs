/*
放在Discription Window上用來讓描述頁能在點擊後關閉，關閉時可能發生觸發銀幕點擊導致畫作互動或打開別的描述頁，因此要好好控制reticle pointer的lock狀態
*/
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems; // UI 事件（PointerClick/Submit）
using System.Collections;

public class ImageTouchDeactivate : MonoBehaviour, IPointerClickHandler, ISubmitHandler
{
    private InputAction tapAction;
    private bool tapped = false;

    [Tooltip("點擊後多少秒關閉物件")]
    public float delayBeforeDeactivate = 0.1f;

    private void OnEnable()
    {
        // 初始化並啟用點擊輸入
        tapAction = new InputAction(type: InputActionType.Button, binding: "<Touchscreen>/press");
        tapAction.AddBinding("<Mouse>/leftButton");
        tapAction.performed += ctx => tapped = true;
        tapAction.Enable();
    }

    private void OnDisable()
    {
        tapAction.Disable();
    }

    private void Update()
    {
        if (tapped)
        {
            tapped = false;
            StartCoroutine(DelayedUnlockAndDeactivate(delayBeforeDeactivate));
        }
    }

    private IEnumerator DelayedUnlockAndDeactivate(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 解除 Reticle Pointer 的鎖定
        CardboardReticlePointer pointer = FindObjectOfType<CardboardReticlePointer>();
        if (pointer != null)
        {
            pointer.clickLock = false;
        }
        else
        {
            Debug.LogWarning("找不到 CardboardReticlePointer 物件");
        }

        MobileCardboardReticlePointer mobilePointer = FindObjectOfType<MobileCardboardReticlePointer>();
        if (mobilePointer != null)
        {
            mobilePointer.clickLock = false;
        }
        else
        {
            Debug.LogWarning("找不到 MobileCardboardReticlePointer 物件");
        }

        // 關閉這個 GameObject
        gameObject.SetActive(false);
    }

    public void Deactivate()
    {
        Debug.Log("Deactivate觸發");
        gameObject.SetActive(false);
            
    }

    // 讓 XR 控制器的 UI Select/Click 可觸發關閉
    public void OnPointerClick(PointerEventData eventData)
    {
        StartCoroutine(DelayedUnlockAndDeactivate(delayBeforeDeactivate));
    }

    // 有些裝置會以 Submit 送出（例如按 Trigger/PrimaryButton）
    public void OnSubmit(BaseEventData eventData)
    {
        StartCoroutine(DelayedUnlockAndDeactivate(delayBeforeDeactivate));
    }
}
