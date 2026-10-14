using UnityEngine;
using System.Collections;

/// <summary>
/// 射线效果控制器 - 处理射击手势触发的射线动画
/// 优化设计：使用对象池和简单的LineRenderer实现高效射线效果
/// </summary>
public class RaycastEffectController : MonoBehaviour
{
    [Header("射线效果设置")]
    public float rayDuration = 0.2f; // 射线显示持续时间
    public float rayWidth = 0.02f; // 射线宽度
    public Color rayColor = Color.red; // 射线颜色
    
    [Header("性能优化设置")]
    public int maxRayCount = 3; // 最大同时显示的射线数量
    
    [Header("第三人称射线起点设置")]
    public float rayStartOffsetX = -100f; // 射线起点X轴偏移距离（由于镜像 左右反向 因此设为负值）
    public float rayStartOffsetY = -10f; // 射线起点Y轴偏移距离
    
    // 射线对象池
    private LineRenderer[] rayPool;
    private Coroutine[] rayCoroutines;
    private int currentRayIndex = 0;
    
    // 单例实例
    public static RaycastEffectController Instance { get; private set; }
    
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
        
        InitializeRayPool();
    }
    
    void Start()
    {
        // 射击功能现在由GestureFunctionController统一管理，不再直接订阅手势事件
        // 保留Start方法以备将来扩展使用
    }
    
    void OnDestroy()
    {
        // 清理资源（如果需要的话）
    }
    
    /// <summary>
    /// 初始化射线对象池
    /// </summary>
    void InitializeRayPool()
    {
        rayPool = new LineRenderer[maxRayCount];
        rayCoroutines = new Coroutine[maxRayCount];
        
        for (int i = 0; i < maxRayCount; i++)
        {
            // 创建射线对象
            GameObject rayObj = new GameObject($"Ray_{i}");
            rayObj.transform.SetParent(transform);
            
            LineRenderer lineRenderer = rayObj.AddComponent<LineRenderer>();
            
            // 配置LineRenderer
            lineRenderer.material = CreateRayMaterial();
            lineRenderer.startWidth = rayWidth;
            lineRenderer.endWidth = rayWidth;
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;
            lineRenderer.enabled = false;
            
            rayPool[i] = lineRenderer;
        }
    }
    
    /// <summary>
    /// 创建射线材质
    /// </summary>
    Material CreateRayMaterial()
    {
        // 使用Unity内置的Unlit/Color着色器创建简单材质
        Material rayMaterial = new Material(Shader.Find("Unlit/Color"));
        rayMaterial.color = rayColor;
        return rayMaterial;
    }
    
    /// <summary>
    /// 触发射击射线效果
    /// </summary>
    public void TriggerShootingRay()
    {
        // 获取当前活跃的手（优先左手）
        int activeHandIndex = InputManager_Hand.Instance.TargetHandIndex;
        if (activeHandIndex == 0) return; // 没有活跃的手
        
        // 获取玩家位置作为射线起点基础位置
        Vector3 playerPos = InputManager_Hand.Instance.GetPlayerPosition();
        
        // 根据targetHandIndex调整射线起点位置
        Vector3 rayStartPos = playerPos;
        if (activeHandIndex == 1) // 左手
        {
            // 左上移：X坐标减少，Y坐标增加
            rayStartPos.x -= rayStartOffsetX;
            rayStartPos.y += rayStartOffsetY;
        }
        else if (activeHandIndex == 2) // 右手
        {
            // 右上移：X坐标增加，Y坐标增加
            rayStartPos.x += rayStartOffsetX;
            rayStartPos.y += rayStartOffsetY;
        }
        
        // 获取准星位置
        Vector3 crosshairPos = CrosshairController.Instance.GetCrosshairWorldPosition();
        
        // 如果无法获取有效位置，返回
        if (playerPos==Vector3.zero||crosshairPos == Vector3.zero)
        {
            Debug.LogWarning("[RaycastEffectController] 无法获取有效的射线起点或终点位置位置");
            return;
        }
        
        // 2D游戏：确保Z坐标为0
        rayStartPos.z = 0f;
        crosshairPos.z = 0f;
        
        // 创建射线效果
        CreateRayEffect(rayStartPos, crosshairPos);
    }
    

    
    /// <summary>
    /// 获取准星的世界坐标位置
    /// </summary>
    Vector3 GetCrosshairWorldPosition()
    {
        // 查找CrosshairController组件
        CrosshairController crosshairController = FindObjectOfType<CrosshairController>();
        if (crosshairController == null || crosshairController.crosshairImage == null)
        {
            Debug.LogWarning("[RaycastEffectController] 未找到CrosshairController或准星图像");
            return Vector3.zero;
        }
        
        // 获取准星的RectTransform和Canvas
        RectTransform crosshairRect = crosshairController.crosshairImage.GetComponent<RectTransform>();
        Canvas parentCanvas = crosshairController.GetComponentInParent<Canvas>();
        
        if (parentCanvas == null)
        {
            Debug.LogWarning("[RaycastEffectController] 未找到准星的父Canvas");
            return Vector3.zero;
        }
        
        // 将UI坐标转换为世界坐标
        Vector3[] worldCorners = new Vector3[4];
        crosshairRect.GetWorldCorners(worldCorners);
        
        // 获取准星中心的世界坐标
        Vector3 worldPos = (worldCorners[0] + worldCorners[2]) / 2f;
        worldPos.z = 0f; // 2D游戏确保Z坐标为0
        
        return worldPos;
    }
    
    /// <summary>
    /// 创建射线效果
    /// </summary>
    void CreateRayEffect(Vector3 startPos, Vector3 endPos)
    {
        // 从对象池获取可用的射线对象
        LineRenderer rayLine = GetAvailableRay();
        if (rayLine == null) return;
        
        // 设置射线位置
        rayLine.SetPosition(0, startPos);
        rayLine.SetPosition(1, endPos);
        
        // 启用射线显示
        rayLine.enabled = true;
        
        // 启动射线动画协程
        int rayIndex = System.Array.IndexOf(rayPool, rayLine);
        if (rayCoroutines[rayIndex] != null)
        {
            StopCoroutine(rayCoroutines[rayIndex]);
        }
        rayCoroutines[rayIndex] = StartCoroutine(AnimateRay(rayLine, rayIndex));
        //Debug.Log($"[RaycastEffectController] 射线效果触发：从 {startPos} 到 {endPos}");
    }
    
    /// <summary>
    /// 从对象池获取可用的射线对象
    /// </summary>
    LineRenderer GetAvailableRay()
    {
        // 循环使用射线对象池
        LineRenderer ray = rayPool[currentRayIndex];
        currentRayIndex = (currentRayIndex + 1) % maxRayCount;
        return ray;
    }
    
    /// <summary>
    /// 射线动画协程 - 简化版本
    /// </summary>
    IEnumerator AnimateRay(LineRenderer rayLine, int rayIndex)
    {
        // 简化：直接显示射线持续指定时间
        yield return new WaitForSeconds(rayDuration);
        
        // 隐藏射线
        rayLine.enabled = false;
        rayCoroutines[rayIndex] = null;
    }
}