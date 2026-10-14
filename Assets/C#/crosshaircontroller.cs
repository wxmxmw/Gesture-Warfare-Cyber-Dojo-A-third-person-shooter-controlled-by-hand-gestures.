
using System.Diagnostics;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 简单的准星控制器，根据食指指尖位置和方向动态定位准星
/// </summary>
public class CrosshairController : MonoBehaviour
{
    [Header("准星设置")]
    public Image crosshairImage; // 准星UI图像
    public float offsetDistance = 50f; // 准星距离食指指尖的偏移距离（像素）
    Vector3 crosshairWorldPos;
    [Header("纵深感设置")]
    public float baseScale = 1f; // 基础缩放大小
    public float depthScaleFactor = 0.001f; // 纵深缩放系数，值越大纵深感越强
    public float distanceMultiplier = 3f; // 距离倍增系数，控制准星距离手的远近
    
    [Header("显示控制")]
    public bool showCrosshair = true; // 是否显示准星
    public bool showDebugInfo = true; // 是否显示调试信息

    [Header("准心摇晃效果设置")]
    public float shakeIntensity = 10f; // 摇晃强度（像素）
    public float shakeDuration = 0.2f; // 摇晃持续时间（秒）
    public float shakeFrequency = 20f; // 摇晃频率（每秒次数）
    public bool enableShakeEffect = true; // 是否启用摇晃效果

    [Header("击中效果设置")]
    public GameObject crosshairEffect; // 击中效果物体
    public bool enableHitEffect = true; // 是否启用击中效果

    public GameObject mirrorCrosshair; // 对应敌人生成平面上的准心 
    private crosshairTarget mirrorTarget; // 镜像准心的目标组件

    private RectTransform crosshairRect;
    private Canvas parentCanvas;
    
    // 镜像准心碰撞体相关
    private CircleCollider2D mirrorCollider;
    private float originalColliderRadius = 50f; // 原始碰撞体半径
    
    // crosshairRange子物体相关
    private GameObject crosshairRange; // 碰撞体范围显示子物体
    private SpriteRenderer crosshairRangeRenderer; // 范围显示精灵渲染器
    private SpriteRenderer[] mirrorRenderers;
    private float originalRangeSize = 100f; // 原始范围大小（100*100）
    private float rotationSpeed = 90f; // 旋转速度（度/秒）

    // 准心摇晃相关变量
    private Vector3 originalCrosshairPosition; // 原始准心位置
    private bool isShaking = false; // 是否正在摇晃
    private Coroutine shakeCoroutine; // 摇晃协程引用

    // 准心迟缓移动相关变量
    [Header("迟缓移动设置")]
    public bool isLagModeEnabled = false; // 是否启用迟缓模式
    public float lagMoveSpeed = 0.3f; // 迟缓移动速度 (0-1, 越小越慢)
    private Vector2 currentLagPosition; // 当前迟缓位置
    private Vector2 targetLagPosition; // 目标迟缓位置
    private bool lagPositionInitialized = false; // 迟缓位置是否已初始化

    // 单例实例
    public static CrosshairController Instance { get; private set; }

    void Awake()
    {
        // 单例模式实现
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // 获取父Canvas
        parentCanvas = GetComponentInParent<Canvas>();

        if (parentCanvas == null)
        {
            parentCanvas = FindObjectOfType<Canvas>();
        }

        if (crosshairImage != null)
        {
            crosshairRect = crosshairImage.GetComponent<RectTransform>();
            crosshairImage.enabled = false;
            crosshairImage.raycastTarget = false;
        }
        else if (parentCanvas != null)
        {
            var anchorObj = new GameObject("CrosshairAnchor");
            anchorObj.transform.SetParent(parentCanvas.transform, false);
            crosshairRect = anchorObj.AddComponent<RectTransform>();
            crosshairRect.anchorMin = new Vector2(0.5f, 0.5f);
            crosshairRect.anchorMax = new Vector2(0.5f, 0.5f);
            crosshairRect.pivot = new Vector2(0.5f, 0.5f);
        }
        
        // 初始化击中效果物体状态
        if (crosshairEffect != null)
        {
            crosshairEffect.SetActive(false);
        }
        else if (enableHitEffect)
        {
            //Debug.LogWarning("[CrosshairController] crosshairEffect 物体未设置！");
        }
        
        // 获取镜像准心的目标组件和碰撞体
        if (mirrorCrosshair != null)
        {
            mirrorRenderers = mirrorCrosshair.GetComponentsInChildren<SpriteRenderer>(true);
            mirrorTarget = mirrorCrosshair.GetComponent<crosshairTarget>();
            mirrorCollider = mirrorCrosshair.GetComponent<CircleCollider2D>();
            
            // 获取crosshairRange子物体
            crosshairRange = mirrorCrosshair.transform.Find("crosshairRange")?.gameObject;
            if (crosshairRange != null)
            {
                crosshairRangeRenderer = crosshairRange.GetComponent<SpriteRenderer>();
                if (crosshairRangeRenderer == null)
                {
                    UnityEngine.Debug.LogWarning("crosshairRange子物体上没有找到SpriteRenderer组件！");
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning("未找到crosshairRange子物体！");
            }
            
            if (mirrorTarget == null)
            {
                UnityEngine.Debug.LogWarning("镜像准心对象上没有找到crosshairTarget组件！");
            }
            
            if (mirrorCollider == null)
            {
                UnityEngine.Debug.LogWarning("镜像准心对象上没有找到CircleCollider2D组件！");
            }
            else
            {
                // 记录原始半径
                originalColliderRadius = mirrorCollider.radius;
            }
        }
        else
        {
            UnityEngine.Debug.LogError("镜像准心对象未设置！");
        }
    }
    
    void Update()
    {
        if (InputManager_Hand.Instance == null)
        {
            return;
        }

        // 直接从InputManager_Hand获取目标手索引
        int targetHandIndex = InputManager_Hand.Instance.TargetHandIndex;

        if (targetHandIndex != 0)
        {
            // 有手部数据时，更新准星位置
            UpdateCrosshairPosition(targetHandIndex);
        }

        // 持续旋转crosshairRange子物体
        RotateCrosshairRange();

        // 测试方法
        if (Input.GetKeyDown(KeyCode.Mouse0)) 
        {
            Shoot();
        }
    }

    /// <summary>
    /// 触发射击
    /// </summary>
    public void Shoot()
    {
        // 如果镜像准心目标组件存在，激活其碰撞体
        if (mirrorTarget != null)
        {
            mirrorTarget.ActivateCollider();
            //UnityEngine.Debug.Log("射击触发，激活镜像准心碰撞体！");
        }

        // 触发射击摇晃效果
        if (enableShakeEffect)
        {
            StartShakeEffect();
        }
    }

    /// <summary>
    /// 开始准心摇晃效果
    /// </summary>
    public void StartShakeEffect()
    {
        if (crosshairRect == null)
        {
            return;
        }

        // 如果已经在摇晃，先停止之前的摇晃
        if (isShaking && shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }

        // 记录当前准心位置作为原始位置
        originalCrosshairPosition = crosshairRect.anchoredPosition;
        
        // 开始摇晃协程
        shakeCoroutine = StartCoroutine(ShakeEffectCoroutine());
    }

    /// <summary>
    /// 准心摇晃效果协程
    /// </summary>
    private IEnumerator ShakeEffectCoroutine()
    {
        isShaking = true;
        
        // 显示击中效果物体
        if (enableHitEffect && crosshairEffect != null)
        {
            crosshairEffect.SetActive(true);
            if (showDebugInfo)
            {
                //Debug.Log("[CrosshairController] 击中效果物体已激活");
            }
        }
        
        float elapsedTime = 0f;

        while (elapsedTime < shakeDuration)
        {
            // 计算摇晃偏移
            float shakeX = Random.Range(-shakeIntensity, shakeIntensity);
            float shakeY = Random.Range(-shakeIntensity, shakeIntensity);
            
            // 应用摇晃偏移到准心位置
            crosshairRect.anchoredPosition = originalCrosshairPosition + new Vector3(shakeX, shakeY, 0f);
            
            // 更新镜像准心位置（如果有的话）
            if (mirrorCrosshair != null)
            {
                UpdateMirrorPosition();
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 恢复原始位置
        crosshairRect.anchoredPosition = originalCrosshairPosition;
        if (mirrorCrosshair != null)
        {
            UpdateMirrorPosition();
        }

        // 隐藏击中效果物体
        if (enableHitEffect && crosshairEffect != null)
        {
            crosshairEffect.SetActive(false);
            if (showDebugInfo)
            {
                //Debug.Log("[CrosshairController] 击中效果物体已隐藏");
            }
        }

        isShaking = false;
        shakeCoroutine = null;
    }
    
    /// <summary>
    /// 更新准星位置
    /// </summary>
    void UpdateCrosshairPosition(int targetHandIndex)
    {
        if (crosshairRect == null || parentCanvas == null || Camera.main == null || InputManager_Hand.Instance == null)
        {
            return;
        }

        // 如果正在摇晃，不更新准心位置
        if (isShaking) return;

        // 获取食指指尖的世界坐标
        Vector3 indexTipWorldPos = InputManager_Hand.Instance.GetHandNodePosition(targetHandIndex, (int)HandNodeIndex.iftip);
        
        // 获取食指指向方向
        Vector3 indexDirection = InputManager_Hand.Instance.GetIndexFingerDirection(targetHandIndex);
        
        // 如果方向无效，不更新位置
        if (indexDirection == Vector3.zero)
            return;
            
        // 先计算基础准星世界坐标
        Vector3 baseCrosshairWorldPos = indexTipWorldPos + indexDirection * offsetDistance;
        
        // 将基础坐标转换为屏幕坐标以获取y值
        Vector2 baseScreenPos = Camera.main.WorldToScreenPoint(baseCrosshairWorldPos);
        
        // 根据屏幕y坐标绝对值计算额外距离偏移
        float yDistance = Mathf.Abs(baseScreenPos.y);
        float extraDistance = yDistance * distanceMultiplier;
        
        // 根据y轴距离调整准星大小
        float scale = baseScale / (1f + yDistance * depthScaleFactor);
        crosshairRect.localScale = Vector3.one * scale;

        // 计算最终准星世界坐标：基础位置 + 额外距离偏移
        crosshairWorldPos = indexTipWorldPos + indexDirection * (offsetDistance + extraDistance);
        
        // 将最终世界坐标转换为屏幕坐标
        Vector2 screenPos = Camera.main.WorldToScreenPoint(crosshairWorldPos);
        
        // 将屏幕坐标转换为Canvas坐标
        Vector2 targetCanvasPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.GetComponent<RectTransform>(),
            screenPos,
            parentCanvas.worldCamera,
            out targetCanvasPos
        );

        // 根据是否启用迟缓模式来设置准星位置
        Vector2 finalCanvasPos;
        if (isLagModeEnabled)
        {
            finalCanvasPos = UpdateLagPosition(targetCanvasPos);
        }
        else
        {
            finalCanvasPos = targetCanvasPos;
            // 重置迟缓位置初始化标志
            lagPositionInitialized = false;
        }
        
        // 设置准星位置
        crosshairRect.anchoredPosition = finalCanvasPos;
        
        // 更新镜像碰撞体的位置和大小
        UpdateMirrorPosition();
        UpdateMirrorColliderRadius(scale);
        UpdateCrosshairRangeSize(scale);
    }

    /// <summary>
    /// 更新迟缓位置
    /// </summary>
    private Vector2 UpdateLagPosition(Vector2 targetPosition)
    {
        if (crosshairRect == null)
        {
            return targetPosition;
        }

        // 初始化迟缓位置
        if (!lagPositionInitialized)
        {
            currentLagPosition = crosshairRect.anchoredPosition;
            targetLagPosition = targetPosition;
            lagPositionInitialized = true;
            return currentLagPosition;
        }

        // 更新目标位置
        targetLagPosition = targetPosition;

        // 使用Lerp进行线性插值，实现迟缓移动
        currentLagPosition = Vector2.Lerp(currentLagPosition, targetLagPosition, lagMoveSpeed * Time.deltaTime * 5f);

        return currentLagPosition;
    }
    
    /// <summary>
    /// 设置准星显示状态
    /// </summary>
    /// <param name="show">是否显示</param>
    public void SetCrosshairVisible(bool show)
    {
        showCrosshair = show;
        if (mirrorRenderers != null)
        {
            foreach (var renderer in mirrorRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = show;
                }
            }
        }
    }
    
    /// <summary>
    /// 设置偏移距离
    /// </summary>
    /// <param name="distance">偏移距离（像素）</param>
    public void SetOffsetDistance(float distance)
    {
        offsetDistance = distance;
    }

    /// <summary>
    /// 设置摇晃效果参数
    /// </summary>
    /// <param name="intensity">摇晃强度</param>
    /// <param name="duration">摇晃持续时间</param>
    /// <param name="frequency">摇晃频率</param>
    public void SetShakeParameters(float intensity, float duration, float frequency)
    {
        shakeIntensity = intensity;
        shakeDuration = duration;
        shakeFrequency = frequency;
    }

    /// <summary>
    /// 设置准心迟缓模式
    /// </summary>
    /// <param name="enabled">是否启用迟缓模式</param>
    /// <param name="speed">迟缓移动速度 (0-1, 越小越慢)</param>
    public void SetLagMode(bool enabled, float speed)
    {
        isLagModeEnabled = enabled;
        lagMoveSpeed = Mathf.Clamp01(speed);
        
        if (showDebugInfo)
        {
            UnityEngine.Debug.Log($"[CrosshairController] 迟缓模式: {(enabled ? "启用" : "禁用")}, 速度: {lagMoveSpeed:F2}");
        }
        
        // 如果禁用迟缓模式，重置相关状态
        if (!enabled)
        {
            lagPositionInitialized = false;
        }
    }

    /// <summary>
    /// 启用或禁用摇晃效果
    /// </summary>
    /// <param name="enable">是否启用</param>
    public void SetShakeEffectEnabled(bool enable)
    {
        enableShakeEffect = enable;
    }

    /// <summary>
    /// 启用或禁用击中效果
    /// </summary>
    /// <param name="enable">是否启用</param>
    public void SetHitEffectEnabled(bool enable)
    {
        enableHitEffect = enable;
    }

    /// <summary>
    /// 设置击中效果物体
    /// </summary>
    /// <param name="effectObject">击中效果物体</param>
    public void SetCrosshairEffectObject(GameObject effectObject)
    {
        crosshairEffect = effectObject;
        if (crosshairEffect != null)
        {
            crosshairEffect.SetActive(false);
        }
    }

    /// <summary>
    /// 手动触发击中效果
    /// </summary>
    public void TriggerHitEffect()
    {
        if (enableHitEffect && crosshairEffect != null)
        {
            crosshairEffect.SetActive(true);
            if (showDebugInfo)
            {
                //Debug.Log("[CrosshairController] 手动触发击中效果");
            }
        }
    }

    /// <summary>
    /// 手动隐藏击中效果
    /// </summary>
    public void HideHitEffect()
    {
        if (crosshairEffect != null)
        {
            crosshairEffect.SetActive(false);
            if (showDebugInfo)
            {
                //Debug.Log("[CrosshairController] 手动隐藏击中效果");
            }
        }
    }

    public void UpdateMirrorPosition() 
    {
        if (mirrorCrosshair == null || parentCanvas == null || crosshairRect == null)
        {
            return;
        }

        // 获取Canvas的RectTransform
        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
        
        // 获取Canvas的尺寸
        Vector2 canvasSize = canvasRect.sizeDelta;
        
        // 计算Canvas的中心点偏移
        float canvasCenterX = canvasSize.x * 0.5f;
        float canvasCenterY = canvasSize.y * 0.5f;
        
        // 使用Canvas尺寸动态计算镜像准心位置
        // 原来的硬编码数值433.5f和290.5f现在用Canvas尺寸替代
        mirrorCrosshair.transform.localPosition = new Vector3(
            canvasCenterX - crosshairRect.localPosition.x, 
            canvasCenterY + crosshairRect.localPosition.y, 
            0
        );
    }
    
    /// <summary>
    /// 更新镜像准心碰撞体的半径
    /// </summary>
    /// <param name="scale">准星的缩放值</param>
    public void UpdateMirrorColliderRadius(float scale)
    {
        if (mirrorCollider != null)
        {
            // 根据准星缩放调整碰撞体半径：原半径 * 缩放值
            mirrorCollider.radius = originalColliderRadius * scale;
        }
    }
    
    /// <summary>
    /// 更新crosshairRange子物体的大小
    /// </summary>
    /// <param name="scale">准星的缩放值</param>
    public void UpdateCrosshairRangeSize(float scale)
    {
        if (crosshairRangeRenderer != null)
        {
            // 计算新的尺寸：原始尺寸 * 缩放值
            float newSize = originalRangeSize * scale;
            
            // 设置SpriteRenderer的size（注意：这里设置的是直径，所以直接使用newSize）
            crosshairRangeRenderer.size = new Vector2(newSize, newSize);
        }
    }
    
    /// <summary>
    /// 持续旋转crosshairRange子物体
    /// </summary>
    public void RotateCrosshairRange()
    {
        if (crosshairRange != null)
        {
            // 顺时针旋转
            crosshairRange.transform.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
        }
    }

    public Vector3 GetCrosshairWorldPosition() {
        return mirrorCrosshair.transform.localPosition;
    }
}
