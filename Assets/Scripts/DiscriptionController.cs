/*
用來控制Discription的顯示
*/
using UnityEngine;
using TMPro;
using System.Linq; // 引入 LINQ 支援 FirstOrDefault

public class DiscriptionController : MonoBehaviour
{
    private GameObject targetImage;
    private TMP_Text targetTitle;
    private TMP_Text targetText;


    [Header("設定")]
    [SerializeField] private string displayTitle = "請輸入顯示標題";
    [SerializeField] private string displayText = "請輸入顯示文字";

    private void Awake()
    {
        Debug.LogWarning("[DiscriptionController] Awake - 開始執行");
        
        // 使用 Resources.FindObjectsOfTypeAll 可以搜尋到 inactive 狀態的 GameObject
        targetImage = Resources.FindObjectsOfTypeAll<GameObject>()
            .FirstOrDefault(go => go.name == "Discription Window");

        targetText = Resources.FindObjectsOfTypeAll<TMP_Text>()
            .FirstOrDefault(txt => txt.name == "Discribe Text");

        targetTitle = Resources.FindObjectsOfTypeAll<TMP_Text>()
            .FirstOrDefault(txt => txt.name == "Discribe Title");

        if (targetImage == null)
        {
            Debug.LogWarning("找不到名稱為 'Discription Window' 的物件");
        }
        if (targetText == null)
        {
            Debug.LogWarning("找不到名稱為 'Discribe Text' 的 TMP_Text 元件");
        }
        if (targetTitle == null)
        {
            Debug.LogWarning("找不到名稱為 'Discribe Title' 的 TMP_Text 元件");
        }
        
        Debug.LogWarning("[DiscriptionController] Awake - 完成");
    }

    public void OnPointerClick()
    {
        Click();
        Click();
    }
    
    private void Click(){
        Debug.Log("OnPointerClick");
        // 鎖定 reticle pointer
        CardboardReticlePointer pointer = FindObjectOfType<CardboardReticlePointer>();
        if (pointer != null)
        {    
            pointer.clickLock = true;
        }
        MobileCardboardReticlePointer mobilePointer = FindObjectOfType<MobileCardboardReticlePointer>();
        if (mobilePointer != null)
        {
            mobilePointer.clickLock = true;
        }
        if (targetImage != null && !targetImage.activeSelf)
        {
            targetImage.SetActive(true);
            if (targetTitle != null)
            {
                targetTitle.text = displayTitle;
            }
            else
            {
                Debug.LogWarning("targetTitle not found");
            }
            if (targetText != null)
            {
                targetText.text = displayText;
            }
            else
            {
                Debug.LogWarning("targetText not found");
            }
        }
        else
        {
            Debug.LogWarning("targetImage not found");
        }
    }
}
