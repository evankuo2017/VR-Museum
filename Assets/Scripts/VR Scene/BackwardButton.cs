/*
用來控制後退按鈕的行為來給予Cardboard Reticle Pointer腳本判斷移動
*/
using UnityEngine;
using UnityEngine.EventSystems;

public class BackwardButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public CardboardReticlePointer movementScript;

    public void OnPointerDown(PointerEventData eventData)
    {
        movementScript.isBackwardPressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        movementScript.isBackwardPressed = false;
    }
}
