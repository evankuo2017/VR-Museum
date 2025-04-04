/*
用來處理 Mobile 模式中的游標（Reticle）與透過 UI 搖桿控制玩家移動的腳本
*/
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;
using System.Collections;
using UnityEngine.UI; // 為了使用 RectTransformUtility

public class MobileCardboardReticlePointer : MonoBehaviour
{
    // ------------------------------
    // 1. Reticle（游標）設定參數
    // ------------------------------
    
    [Range(-32767, 32767)]
    public int ReticleSortingOrder = 32767;
    [Tooltip("互動層，例如用 Layer 8 來做互動")]
    public LayerMask reticleInteractionLayerMask = 1 << 8;
    
    private const float _RETICLE_MIN_INNER_ANGLE = 0.0f;
    private const float _RETICLE_MIN_OUTER_ANGLE = 0.5f;
    private const float _RETICLE_GROWTH_ANGLE = 1.5f;
    private const float _RETICLE_MIN_DISTANCE = 0.45f;
    private const float _RETICLE_MAX_DISTANCE = 20.0f;
    private const int _RETICLE_SEGMENTS = 20;
    private const float _RETICLE_GROWTH_SPEED = 8.0f;
    
    private GameObject gazedAtObject = null;
    private Material reticleMaterial;
    private float reticleInnerAngle;
    private float reticleOuterAngle;
    private float reticleDistanceInMeters;
    private float reticleInnerDiameter;
    private float reticleOuterDiameter;
    
    // 點擊動畫參數
    private float clickModifier = 1.0f;
    private bool isAnimatingClick = false;
    
    // ------------------------------
    // 2. 搖桿移動相關參數
    // ------------------------------
    
    [Tooltip("玩家移動速度（單位/秒）")]
    public float joystickMoveSpeed = 5.0f;
    
    [Tooltip("UI 搖桿元件（Fixed Joystick 預制件）")]
    public FixedJoystick fixedJoystick;
    
    // ------------------------------
    // 3. 螢幕點擊判斷（回復原點擊邏輯）
    // ------------------------------
    
    private bool _isScreenTouched
    {
        get
        {
            Touchscreen touchScreen = Touchscreen.current;
            if (touchScreen == null)
                return false;
            if (!touchScreen.enabled)
                InputSystem.EnableDevice(touchScreen);
            // 若有觸控點且第一個觸控為 Began 狀態
            if (touchScreen.touches.Count > 0)
            {
                return touchScreen.touches[0].phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began;
            }
            return false;
        }
    }
    
    // ------------------------------
    // 4. Start
    // ------------------------------
    
    private void Start()
    {
        // 取得游標 Renderer，設定排序與材質
        Renderer rendererComponent = GetComponent<Renderer>();
        rendererComponent.sortingOrder = ReticleSortingOrder;
        reticleMaterial = rendererComponent.material;
        
        // 建立游標 Mesh
        CreateMesh();
    }
    
    // ------------------------------
    // 5. Update - 處理游標更新與射線檢測、點擊邏輯
    // ------------------------------
    
    private void Update()
    {
        // Raycast 從當前位置沿 forward 方向偵測互動物件
        RaycastHit interactiveHit;
        bool interactiveHitValid = Physics.Raycast(transform.position, transform.forward,
                                                   out interactiveHit, _RETICLE_MAX_DISTANCE,
                                                   reticleInteractionLayerMask);
        if (interactiveHitValid)
        {
            if (gazedAtObject != interactiveHit.transform.gameObject)
            {
                if (gazedAtObject != null && IsInteractive(gazedAtObject))
                {
                    gazedAtObject.SendMessage("OnPointerExit", SendMessageOptions.DontRequireReceiver);
                }
                gazedAtObject = interactiveHit.transform.gameObject;
                if (IsInteractive(gazedAtObject))
                {
                    gazedAtObject.SendMessage("OnPointerEnter", SendMessageOptions.DontRequireReceiver);
                }
            }
            SetParams(interactiveHit.distance, true);
        }
        else
        {
            if (gazedAtObject != null && IsInteractive(gazedAtObject))
            {
                gazedAtObject.SendMessage("OnPointerExit", SendMessageOptions.DontRequireReceiver);
            }
            gazedAtObject = null;
            ResetParams();
        }
        
        UpdateDiameters();
        
        // 若觸控發生且有互動物件，且觸控點不在搖桿區域內，才執行點擊動畫
        if (_isScreenTouched && gazedAtObject != null && !isAnimatingClick && !IsTouchOverJoystick())
        {
            StartCoroutine(ClickAnimationAndSendMessage(gazedAtObject));
        }
    }
    
    // ------------------------------
    // 6. FixedUpdate - 使用 UI 搖桿控制移動（方向根據 Camera 面向）
    // ------------------------------
    
    private void FixedUpdate()
    {
        if (fixedJoystick == null)
            return;

        // 從 FixedJoystick 讀取輸入 (範圍 -1 ~ 1)
        Vector2 input = new Vector2(fixedJoystick.Horizontal, fixedJoystick.Vertical);
        if (input.sqrMagnitude > 0.01f)
        {
            // 將 UI 搖桿的 2D 輸入轉換為 3D 向量 (忽略 Y 軸)
            Vector3 inputDirection = new Vector3(input.x, 0f, input.y);
            // 使用 Camera 的旋轉方向來決定移動方向
            Vector3 moveDirection = Camera.main.transform.rotation * inputDirection;
            // 強制移動方向保持在 XZ 平面上
            moveDirection.y = 0f;
            moveDirection.Normalize();

            // 直接更新 Camera 的位置
            Camera.main.transform.position += moveDirection * joystickMoveSpeed * Time.fixedDeltaTime;
        }
    }
    
    // ------------------------------
    // 7. 游標參數更新與 Mesh 產生
    // ------------------------------
    
    private void UpdateDiameters()
    {
        reticleDistanceInMeters = Mathf.Clamp(reticleDistanceInMeters, _RETICLE_MIN_DISTANCE, _RETICLE_MAX_DISTANCE);
        if (reticleInnerAngle < _RETICLE_MIN_INNER_ANGLE)
            reticleInnerAngle = _RETICLE_MIN_INNER_ANGLE;
        if (reticleOuterAngle < _RETICLE_MIN_OUTER_ANGLE)
            reticleOuterAngle = _RETICLE_MIN_OUTER_ANGLE;
        
        float effectiveInnerAngle = reticleInnerAngle * clickModifier;
        float effectiveOuterAngle = reticleOuterAngle * clickModifier;
        
        float inner_half_angle_radians = Mathf.Deg2Rad * effectiveInnerAngle * 0.5f;
        float outer_half_angle_radians = Mathf.Deg2Rad * effectiveOuterAngle * 0.5f;
        
        float inner_diameter = 2.0f * Mathf.Tan(inner_half_angle_radians);
        float outer_diameter = 2.0f * Mathf.Tan(outer_half_angle_radians);
        
        reticleInnerDiameter = Mathf.Lerp(reticleInnerDiameter, inner_diameter,
                                          Time.unscaledDeltaTime * _RETICLE_GROWTH_SPEED);
        reticleOuterDiameter = Mathf.Lerp(reticleOuterDiameter, outer_diameter,
                                          Time.unscaledDeltaTime * _RETICLE_GROWTH_SPEED);
        
        reticleMaterial.SetFloat("_InnerDiameter", reticleInnerDiameter * reticleDistanceInMeters);
        reticleMaterial.SetFloat("_OuterDiameter", reticleOuterDiameter * reticleDistanceInMeters);
        reticleMaterial.SetFloat("_DistanceInMeters", reticleDistanceInMeters);
    }
    
    private void SetParams(float distance, bool interactive)
    {
        reticleDistanceInMeters = Mathf.Clamp(distance, _RETICLE_MIN_DISTANCE, _RETICLE_MAX_DISTANCE);
        if (interactive)
        {
            reticleInnerAngle = _RETICLE_MIN_INNER_ANGLE + _RETICLE_GROWTH_ANGLE;
            reticleOuterAngle = _RETICLE_MIN_OUTER_ANGLE + _RETICLE_GROWTH_ANGLE;
        }
        else
        {
            reticleInnerAngle = _RETICLE_MIN_INNER_ANGLE;
            reticleOuterAngle = _RETICLE_MIN_OUTER_ANGLE;
        }
    }
    
    private void ResetParams()
    {
        reticleDistanceInMeters = _RETICLE_MAX_DISTANCE;
        reticleInnerAngle = _RETICLE_MIN_INNER_ANGLE;
        reticleOuterAngle = _RETICLE_MIN_OUTER_ANGLE;
    }
    
    private void CreateMesh()
    {
        Mesh mesh = new Mesh();
        MeshFilter mf = gameObject.AddComponent<MeshFilter>();
        mf.mesh = mesh;
        
        int segments_count = _RETICLE_SEGMENTS;
        int vertex_count = (segments_count + 1) * 2;
        Vector3[] vertices = new Vector3[vertex_count];
        
        const float kTwoPi = Mathf.PI * 2.0f;
        int vi = 0;
        for (int si = 0; si <= segments_count; ++si)
        {
            float angle = (float)si / segments_count * kTwoPi;
            float x = Mathf.Sin(angle);
            float y = Mathf.Cos(angle);
            vertices[vi++] = new Vector3(x, y, 0.0f);
            vertices[vi++] = new Vector3(x, y, 1.0f);
        }
        
        int indices_count = segments_count * 6;
        int[] indices = new int[indices_count];
        int vert = 0;
        int idx = 0;
        for (int si = 0; si < segments_count; ++si)
        {
            indices[idx++] = vert + 1;
            indices[idx++] = vert;
            indices[idx++] = vert + 2;
            
            indices[idx++] = vert + 1;
            indices[idx++] = vert + 2;
            indices[idx++] = vert + 3;
            vert += 2;
        }
        
        mesh.vertices = vertices;
        mesh.triangles = indices;
        mesh.RecalculateBounds();
    }
    
    // ------------------------------
    // 8. 互動相關檢查
    // ------------------------------
    
    private bool IsInteractive(GameObject go)
    {
        if (go == null) return false;
        return (reticleInteractionLayerMask.value & (1 << go.layer)) != 0;
    }
    
    // ------------------------------
    // 9. 點擊動畫 Coroutine：播放動畫後發送 OnPointerClick 訊息
    // ------------------------------
    
    private IEnumerator ClickAnimationAndSendMessage(GameObject target)
    {
        isAnimatingClick = true;
        float duration = 0.3f;
        float halfDuration = duration / 2f;
        float elapsed = 0f;
        float startModifier = 1.0f;
        float targetModifier = 0.5f;
        
        // 縮小動畫
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            clickModifier = Mathf.Lerp(startModifier, targetModifier, elapsed / halfDuration);
            yield return null;
        }
        clickModifier = targetModifier;
        
        elapsed = 0f;
        // 恢復動畫
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            clickModifier = Mathf.Lerp(targetModifier, startModifier, elapsed / halfDuration);
            yield return null;
        }
        clickModifier = startModifier;
        
        target.SendMessage("OnPointerClick", null, SendMessageOptions.DontRequireReceiver);
        isAnimatingClick = false;
    }

    // ------------------------------
    // 10. 判斷觸控是否落在搖桿上
    // ------------------------------
    
    private bool IsTouchOverJoystick()
    {
        if (fixedJoystick == null)
            return false;

        Touchscreen touchScreen = Touchscreen.current;
        if (touchScreen == null || touchScreen.touches.Count == 0)
            return false;

        // 取得第一個觸控點的位置
        Vector2 touchPos = touchScreen.touches[0].position.ReadValue();
        RectTransform joystickRect = fixedJoystick.GetComponent<RectTransform>();

        // 若搖桿使用的是 Screen Space - Overlay 或 Camera 模式，此處的第三個參數可傳 null
        return RectTransformUtility.RectangleContainsScreenPoint(joystickRect, touchPos, null);
    }
}
