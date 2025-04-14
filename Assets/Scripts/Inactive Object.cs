/*
用來讓描述頁能在點擊後關閉，關閉時可能發生觸發銀幕點擊導致畫作互動或打開別的描述頁，因此要好好控制reticle pointer的lock狀態
*/
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class ImageTouchDeactivate : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        // 啟動 Coroutine 延遲處理解除鎖定以及關閉物件
        StartCoroutine(DelayedUnlockAndDeactivate(0.1f));
    }

    private IEnumerator DelayedUnlockAndDeactivate(float delay)
    {
        // 先等待一段時間，讓後續程式能在點擊後處理
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
        
        // 等待解除鎖定後再停用自己
        gameObject.SetActive(false);
    }
}


