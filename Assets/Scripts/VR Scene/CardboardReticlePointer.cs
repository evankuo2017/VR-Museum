/*
用來處理 VR 模式中的游標（Reticle）與物理移動的腳本
*/
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// CardboardReticlePointer 負責更新 VR 中的游標（Reticle）
/// 並透過物理系統移動 Player（前進、倒退），
/// 其中倒退可由 UI 按鈕透過外部腳本修改 isBackwardPressed 來控制。
/// </summary>
public class CardboardReticlePointer : MonoBehaviour
{
    // ------------------------------
    // 1. Reticle（游標）設定參數
    // ------------------------------
    
    // Reticle 的繪製順序（數值越大越前面繪製）
    [Range(-32767, 32767)]
    public int ReticleSortingOrder = 32767;
    // 互動層：指定哪些層的物件可以被游標互動（例如用 Layer 8 來做互動）
    [Tooltip("互動層，例如用 Layer 8 來做互動")]
    public LayerMask reticleInteractionLayerMask = 1 << 8;
    
    // Reticle 的幾何與成長參數
    private const float _RETICLE_MIN_INNER_ANGLE = 0.0f;
    private const float _RETICLE_MIN_OUTER_ANGLE = 0.5f;
    private const float _RETICLE_GROWTH_ANGLE = 1.5f;
    private const float _RETICLE_MIN_DISTANCE = 0.45f;
    private const float _RETICLE_MAX_DISTANCE = 20.0f;
    private const int _RETICLE_SEGMENTS = 20;
    private const float _RETICLE_GROWTH_SPEED = 8.0f;
    
    // 游標內部狀態變數
    private GameObject gazedAtObject = null;      // 當前被游標指向的物件
    private Material reticleMaterial;               // 游標所使用的材質（Shader 參數更新）
    private float reticleInnerAngle;                // 游標內側角度
    private float reticleOuterAngle;                // 游標外側角度
    private float reticleDistanceInMeters;          // 游標距離
    private float reticleInnerDiameter;             // 游標內側直徑（根據角度與距離計算）
    private float reticleOuterDiameter;             // 游標外側直徑
    
    // 點擊動畫相關參數
    private float clickModifier = 1.0f;
    private bool isAnimatingClick = false;
    
    
    // ------------------------------
    // 2. 移動相關參數
    // ------------------------------
    
    [Tooltip("持續按下超過此秒數後開始移動")]
    public float holdThreshold = 1.0f;
    [Tooltip("移動速度（單位/秒）")]
    public float moveSpeed = 5.0f;
    [Tooltip("指定要移動的 player 物件")]
    public Transform player;
    // 雖然此參數在此版本不直接使用，但可保留做後續擴充（例如利用 Raycast 驗證移動方向）
    [Tooltip("移動判斷用的有效射線檢測層（例如 Default 層）")]
    public LayerMask moveLayerMask = 1 << 0;
    
    // 透過 Rigidbody 進行物理移動，所以必須取得 player 的 Rigidbody 參照
    private Rigidbody playerRb;
    
    // ------------------------------
    // 3. 輸入控制參數
    // ------------------------------
    
    // 使用 InputAction 處理前進移動（透過 Touchscreen 或滑鼠左鍵觸發）
    private InputAction forwardAction;
    private bool isForwardPressed = false;
    // 倒退移動改由 UI 按鈕控制（例如 BackwardButton 腳本會修改此變數）
    public bool isBackwardPressed = false;
    
    // 計時用變數，判斷按鈕按下多久後開始移動
    private float forwardHoldTimer = 0f;
    private float backwardHoldTimer = 0f;
    
    
    // ------------------------------
    // 4. Start
    // ------------------------------
    
    private void Start()
    {
        // 建立 forwardAction：監聽觸控或滑鼠左鍵輸入
        forwardAction = new InputAction(type: InputActionType.Button, binding: "<Touchscreen>/press");
        forwardAction.AddBinding("<Mouse>/leftButton");
        forwardAction.performed += ctx => OnForwardPerformed();
        forwardAction.canceled += ctx => OnForwardCanceled();
        forwardAction.Enable();
        
        // 若日後想用鍵盤等其他方式控制倒退，可在此建立 backwardAction（目前用 UI 按鈕控制）

        // 取得此物件的 Renderer，並設定排序順序與材質
        Renderer rendererComponent = GetComponent<Renderer>();
        rendererComponent.sortingOrder = ReticleSortingOrder;
        reticleMaterial = rendererComponent.material;
        
        // 建立游標 Mesh（用於渲染游標效果）
        CreateMesh();
        
        // 取得 player 上的 Rigidbody 以進行物理移動
        if (player != null)
        {
            playerRb = player.GetComponent<Rigidbody>();
            if (playerRb == null)
            {
                Debug.LogError("Player 沒有 Rigidbody 組件，請加入 Rigidbody 以使用物理移動。");
            }
        }
    }
    
    
    // ------------------------------
    // 5. Update - 處理游標（Reticle）更新與射線檢測
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
            // 若偵測到的新物件與上一次不同，則發送 OnPointerEnter/Exit 訊息
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
            // 更新游標參數（例如角度）根據距離與互動狀態
            SetParams(interactiveHit.distance, true);
        }
        else
        {
            // 未偵測到互動物件時，若之前有互動物件，則發送離開訊息
            if (gazedAtObject != null && IsInteractive(gazedAtObject))
            {
                gazedAtObject.SendMessage("OnPointerExit", SendMessageOptions.DontRequireReceiver);
            }
            gazedAtObject = null;
            ResetParams();
        }
        
        // 更新游標外觀（直徑、材質參數等）
        UpdateDiameters();
    }
    
    
    // ------------------------------
    // 6. FixedUpdate - 處理物理移動
    // ------------------------------
    
    private void FixedUpdate()
    {
        if (playerRb == null)
            return;
        
        // 前進移動：當 isForwardPressed 為 true 且未啟用倒退時，累計按下時間，超過閾值後移動玩家
        if (isForwardPressed && !isBackwardPressed)
        {
            forwardHoldTimer += Time.fixedDeltaTime;
            if (forwardHoldTimer >= holdThreshold)
            {
                // 取水平前進方向（忽略 Y 軸）
                Vector3 forward = transform.forward;
                Vector3 moveDirection = new Vector3(forward.x, 0f, forward.z).normalized;
                float moveDistance = moveSpeed * Time.fixedDeltaTime;
                Vector3 newPos = playerRb.position + moveDirection * moveDistance;
                // 利用 Rigidbody.MovePosition 讓物理系統處理碰撞
                playerRb.MovePosition(newPos);
            }
        }
        else
        {
            forwardHoldTimer = 0f;
        }
        
        // 倒退移動：當 isBackwardPressed 為 true 時，累計按下時間，超過閾值後移動玩家倒退
        if (isBackwardPressed)
        {
            backwardHoldTimer += Time.fixedDeltaTime;
            if (backwardHoldTimer >= holdThreshold)
            {
                // 取水平倒退方向（取 forward 的反向）
                Vector3 backwardDirection = -new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
                float moveDistance = moveSpeed * Time.fixedDeltaTime;
                Vector3 newPos = playerRb.position + backwardDirection * moveDistance;
                playerRb.MovePosition(newPos);
            }
        }
        else
        {
            backwardHoldTimer = 0f;
        }
        
        // 重置玩家的速度，避免因碰撞反作用力而累積移動速度
        playerRb.velocity = Vector3.zero;
    }
    
    
    // ------------------------------
    // 7. 輸入事件回呼：前進移動的按下與放開
    // ------------------------------
    
    /// <summary>
    /// 當前進按鈕觸發時呼叫（例如觸控或滑鼠左鍵按下）
    /// </summary>
    private void OnForwardPerformed()
    {
        // 如果有互動物件，執行點擊動畫
        if (!isAnimatingClick && gazedAtObject != null)
        {
            StartCoroutine(ClickAnimationAndSendMessage(gazedAtObject));
        }
        isForwardPressed = true;
        forwardHoldTimer = 0f;
    }
    
    /// <summary>
    /// 當前進按鈕放開時呼叫
    /// </summary>
    private void OnForwardCanceled()
    {
        isForwardPressed = false;
        forwardHoldTimer = 0f;
    }
    
    
    // ------------------------------
    // 8. 游標參數更新與 Mesh 產生
    // ------------------------------
    
    /// <summary>
    /// 根據距離與點擊狀態更新游標直徑與材質參數
    /// </summary>
    private void UpdateDiameters()
    {
        // 限制距離範圍
        reticleDistanceInMeters = Mathf.Clamp(reticleDistanceInMeters, _RETICLE_MIN_DISTANCE, _RETICLE_MAX_DISTANCE);
        
        // 確保角度不小於最小值
        if (reticleInnerAngle < _RETICLE_MIN_INNER_ANGLE)
            reticleInnerAngle = _RETICLE_MIN_INNER_ANGLE;
        if (reticleOuterAngle < _RETICLE_MIN_OUTER_ANGLE)
            reticleOuterAngle = _RETICLE_MIN_OUTER_ANGLE;
        
        // 計算有效角度（乘上點擊動畫的 modifier）
        float effectiveInnerAngle = reticleInnerAngle * clickModifier;
        float effectiveOuterAngle = reticleOuterAngle * clickModifier;
        
        // 將角度轉換成弧度後求半角值
        float inner_half_angle_radians = Mathf.Deg2Rad * effectiveInnerAngle * 0.5f;
        float outer_half_angle_radians = Mathf.Deg2Rad * effectiveOuterAngle * 0.5f;
        
        // 根據切線求直徑
        float inner_diameter = 2.0f * Mathf.Tan(inner_half_angle_radians);
        float outer_diameter = 2.0f * Mathf.Tan(outer_half_angle_radians);
        
        // 平滑過渡更新直徑
        reticleInnerDiameter = Mathf.Lerp(reticleInnerDiameter, inner_diameter,
                                          Time.unscaledDeltaTime * _RETICLE_GROWTH_SPEED);
        reticleOuterDiameter = Mathf.Lerp(reticleOuterDiameter, outer_diameter,
                                          Time.unscaledDeltaTime * _RETICLE_GROWTH_SPEED);
        
        // 更新材質中的 Shader 參數，供游標渲染使用
        reticleMaterial.SetFloat("_InnerDiameter", reticleInnerDiameter * reticleDistanceInMeters);
        reticleMaterial.SetFloat("_OuterDiameter", reticleOuterDiameter * reticleDistanceInMeters);
        reticleMaterial.SetFloat("_DistanceInMeters", reticleDistanceInMeters);
    }
    
    /// <summary>
    /// 當偵測到互動物件時，根據物件距離設定游標的角度參數
    /// </summary>
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
    
    /// <summary>
    /// 當無互動物件時重設游標參數
    /// </summary>
    private void ResetParams()
    {
        reticleDistanceInMeters = _RETICLE_MAX_DISTANCE;
        reticleInnerAngle = _RETICLE_MIN_INNER_ANGLE;
        reticleOuterAngle = _RETICLE_MIN_OUTER_ANGLE;
    }
    
    /// <summary>
    /// 根據設定參數建立游標 Mesh
    /// </summary>
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
        // 產生每個 segment 的外側與內側頂點
        for (int si = 0; si <= segments_count; ++si)
        {
            float angle = (float)si / segments_count * kTwoPi;
            float x = Mathf.Sin(angle);
            float y = Mathf.Cos(angle);
            // 外側頂點（z = 0）
            vertices[vi++] = new Vector3(x, y, 0.0f);
            // 內側頂點（z = 1）
            vertices[vi++] = new Vector3(x, y, 1.0f);
        }
        
        // 根據頂點建立三角形索引
        int indices_count = segments_count * 6;
        int[] indices = new int[indices_count];
        int vert = 0;
        int idx = 0;
        for (int si = 0; si < segments_count; ++si)
        {
            // 第一個三角形
            indices[idx++] = vert + 1;
            indices[idx++] = vert;
            indices[idx++] = vert + 2;
            
            // 第二個三角形
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
    // 9. 互動相關：檢查物件是否在互動層
    // ------------------------------
    
    private bool IsInteractive(GameObject go)
    {
        if (go == null) return false;
        return (reticleInteractionLayerMask.value & (1 << go.layer)) != 0;
    }
    
    
    // ------------------------------
    // 10. 點擊動畫 Coroutine：播放動畫後發送 OnPointerClick 訊息
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
