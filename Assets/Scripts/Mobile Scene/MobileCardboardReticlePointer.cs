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
    // 2. 搖桿與移動相關參數
    // ------------------------------
    
    [Tooltip("玩家移動速度（單位/秒）")]
    public float joystickMoveSpeed = 5.0f;
    
    [Tooltip("UI 搖桿元件（Fixed Joystick 預制件）")]
    public FixedJoystick fixedJoystick;
    
    [Tooltip("指定要移動的 player 物件（必須包含 Rigidbody）")]
    public Transform player;
    
    private Rigidbody playerRb;
    
    // ------------------------------
    // 3. 使用 InputAction 處理觸控輸入
    // ------------------------------
    
    private InputAction touchAction;

    private void OnEnable()
    {
        // 建立一個 PassThrough 型態的 InputAction，綁定所有觸控按壓事件
        touchAction = new InputAction(type: InputActionType.PassThrough, binding: "<Touchscreen>/touch*/press");
        touchAction.performed += OnTouchPerformed;
        touchAction.Enable();
    }

    private void OnDisable()
    {
        touchAction.performed -= OnTouchPerformed;
        touchAction.Disable();
    }
    
    // 觸控事件回調：每當有觸控按壓時執行
    private void OnTouchPerformed(InputAction.CallbackContext context)
    {
        // 檢查是否有互動物件，以及是否正在播放點擊動畫
        if (gazedAtObject == null || isAnimatingClick)
            return;

        // 檢查觸控點是否位於搖桿上（若在搖桿上則忽略此點擊）
        Touchscreen touchScreen = Touchscreen.current;
        if (touchScreen != null && touchScreen.touches.Count > 0)
        {
            // 此處取得第一個觸控點的位置作為參考
            Vector2 touchPos = touchScreen.touches[0].position.ReadValue();
            if (IsTouchOverJoystick(touchPos))
                return;
        }
        
        // 若檢查通過則觸發點擊動畫與 OnPointerClick 訊息
        StartCoroutine(ClickAnimationAndSendMessage(gazedAtObject));
    }
    
    // 依照傳入的螢幕座標檢查是否落在搖桿區域
    private bool IsTouchOverJoystick(Vector2 position)
    {
        if (fixedJoystick == null)
            return false;
        RectTransform joystickRect = fixedJoystick.GetComponent<RectTransform>();
        return RectTransformUtility.RectangleContainsScreenPoint(joystickRect, position, null);
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

        // 取得 player 的 Rigidbody（請確保 player 已指定且包含 Rigidbody）
        if (player != null)
        {
            playerRb = player.GetComponent<Rigidbody>();
            if (playerRb == null)
            {
                Debug.LogError("Player 沒有 Rigidbody 組件，請加入 Rigidbody 以使用物理移動。");
            }
        }
        else
        {
            Debug.LogError("Player 未指定！");
        }
    }
    
    // ------------------------------
    // 5. Update - 處理游標更新與射線檢測
    // ------------------------------
    
    private void Update()
    {
        // 使用 Raycast 從當前位置沿著 forward 方向偵測互動物件
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
    }
    
    // ------------------------------
    // 6. FixedUpdate - 使用 UI 搖桿控制玩家移動（考慮碰撞，利用 Rigidbody）
    // ------------------------------
    
    private void FixedUpdate()
    {
        // 若無指定搖桿或玩家，則不移動
        if (fixedJoystick == null || playerRb == null)
            return;

        // 從 FixedJoystick 讀取輸入 (範圍 -1 ~ 1)
        Vector2 input = new Vector2(fixedJoystick.Horizontal, fixedJoystick.Vertical);
        if (input.sqrMagnitude > 0.01f)
        {
            // 將 UI 搖桿的 2D 輸入轉換為 3D 向量（忽略 Y 軸）
            Vector3 inputDirection = new Vector3(input.x, 0f, input.y);
            // 使用 Camera 的旋轉方向來決定移動方向
            Vector3 moveDirection = Camera.main.transform.rotation * inputDirection;
            // 保持移動方向在 XZ 平面
            moveDirection.y = 0f;
            moveDirection.Normalize();

            // 計算移動位移
            float moveDistance = joystickMoveSpeed * Time.fixedDeltaTime;
            Vector3 newPos = playerRb.position + moveDirection * moveDistance;
            // 使用 Rigidbody.MovePosition 考慮碰撞
            playerRb.MovePosition(newPos);
        }
        else
        {
            // 可選：若無輸入，清空速度以防物理干擾
            playerRb.velocity = Vector3.zero;
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
        
        // 播放縮小動畫
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            clickModifier = Mathf.Lerp(startModifier, targetModifier, elapsed / halfDuration);
            yield return null;
        }
        clickModifier = targetModifier;
        
        elapsed = 0f;
        // 播放恢復動畫
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            clickModifier = Mathf.Lerp(targetModifier, startModifier, elapsed / halfDuration);
            yield return null;
        }
        clickModifier = startModifier;
        
        // 發送點擊事件到目標物件
        target.SendMessage("OnPointerClick", null, SendMessageOptions.DontRequireReceiver);
        isAnimatingClick = false;
    }
}
