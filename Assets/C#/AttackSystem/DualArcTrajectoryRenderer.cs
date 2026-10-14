using UnityEngine;
using System.Collections;

/// <summary>
/// 双弧曳光弹渲染器
/// 渲染两条对称的弧线曳光弹从敌人位置飞向玩家位置
/// </summary>
public class DualArcTrajectoryRenderer : MonoBehaviour
{
    [Header("弹道设置")]
    [SerializeField] private float trajectoryWidth = 0.15f;         // 弹道宽度
    [SerializeField] private Color trajectoryColor = Color.blue;    // 弹道颜色
    [SerializeField] private float curvature = 0.5f;                // 弧线弯曲程度 (0-1)
    [SerializeField] private float arcOffset = 1.5f;                // 两条弧线的偏移距离
    [SerializeField] private int segments = 30;                     // 弧线分段数量
    
    [Header("动画设置")]
    [SerializeField] private float animationSpeed = 2f;             // 曳光弹动画速度
    [SerializeField] private bool useGradient = true;               // 是否使用颜色渐变
    [SerializeField] private Gradient colorGradient;                // 颜色渐变
    
    [Header("视觉效果")]
    [SerializeField] private Material trajectoryMaterial;           // 弹道材质
    [SerializeField] private AnimationCurve trailFadeCurve = AnimationCurve.Linear(0, 1, 1, 0); // 拖尾淡出曲线
    
    [Header("调试设置")]
    [SerializeField] private bool enableDebugLogs = false;
    [SerializeField] private bool showDebugGizmos = false;
    
    // 私有变量
    private LineRenderer lineRenderer1;     // 第一条弧线渲染器
    private LineRenderer lineRenderer2;     // 第二条弧线渲染器
    private Vector3 startPosition;          // 起始位置（敌人位置）
    private Vector3 endPosition;            // 结束位置（玩家位置）
    private Vector3[] arc1Points;           // 第一条弧线的所有点
    private Vector3[] arc2Points;           // 第二条弧线的所有点
    private Coroutine animationCoroutine;   // 动画协程
    private bool isTrajectoryActive = false;
    private float currentProgress = 0f;     // 当前动画进度 (0-1)
    
    #region Unity生命周期
    
    private void Awake()
    {
        InitializeLineRenderers();
        InitializeGradient();
    }
    
    private void Update()
    {
        if (isTrajectoryActive)
        {
            // 可以在这里添加额外的视觉效果更新
        }
    }
    
    #endregion
    
    #region 初始化
    
    /// <summary>
    /// 初始化LineRenderer组件
    /// </summary>
    private void InitializeLineRenderers()
    {
        // 创建第一条弧线的LineRenderer
        GameObject arc1Obj = new GameObject("Arc1");
        arc1Obj.transform.SetParent(transform);
        lineRenderer1 = arc1Obj.AddComponent<LineRenderer>();
        ConfigureLineRenderer(lineRenderer1);
        
        // 创建第二条弧线的LineRenderer
        GameObject arc2Obj = new GameObject("Arc2");
        arc2Obj.transform.SetParent(transform);
        lineRenderer2 = arc2Obj.AddComponent<LineRenderer>();
        ConfigureLineRenderer(lineRenderer2);
        
        if (enableDebugLogs)
        {
            Debug.Log("DualArcTrajectoryRenderer: LineRenderer已初始化");
        }
    }
    
    /// <summary>
    /// 配置单个LineRenderer
    /// </summary>
    private void ConfigureLineRenderer(LineRenderer lr)
    {
        lr.startWidth = trajectoryWidth;
        lr.endWidth = trajectoryWidth * 0.3f; // 末端稍细
        lr.positionCount = 0;
        lr.useWorldSpace = true;
        lr.sortingOrder = 10;
        
        // 设置材质
        if (trajectoryMaterial != null)
        {
            lr.material = trajectoryMaterial;
        }
        else
        {
            // 使用默认材质
            Material defaultMat = new Material(Shader.Find("Sprites/Default"));
            defaultMat.color = trajectoryColor;
            lr.material = defaultMat;
        }
        
        // 设置颜色
        if (useGradient && colorGradient != null)
        {
            lr.colorGradient = colorGradient;
        }
        else
        {
            // 使用纯色
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(trajectoryColor, 0.0f), 
                    new GradientColorKey(trajectoryColor, 1.0f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(1f, 0.0f), 
                    new GradientAlphaKey(0.5f, 1.0f) 
                }
            );
            lr.colorGradient = gradient;
        }
        
        // 设置阴影
        lr.receiveShadows = false;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        
        // 默认禁用
        lr.enabled = false;
    }
    
    /// <summary>
    /// 初始化默认渐变
    /// </summary>
    private void InitializeGradient()
    {
        if (colorGradient == null)
        {
            colorGradient = new Gradient();
            colorGradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(Color.blue, 0.0f), 
                    new GradientColorKey(Color.cyan, 0.5f),
                    new GradientColorKey(Color.blue, 1.0f)
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(1f, 0.0f), 
                    new GradientAlphaKey(0.8f, 0.5f),
                    new GradientAlphaKey(0.3f, 1.0f) 
                }
            );
        }
    }
    
    #endregion
    
    #region 公共接口
    
    /// <summary>
    /// 创建双弧曳光弹
    /// </summary>
    /// <param name="start">起始位置（敌人位置）</param>
    /// <param name="end">结束位置（玩家位置）</param>
    /// <param name="config">弹道配置（可选）</param>
    public void CreateDualArc(Vector3 start, Vector3 end, TrajectoryEffectConfig config = null)
    {
        startPosition = start;
        endPosition = end;
        
        // 应用配置
        if (config != null)
        {
            ApplyConfig(config);
        }
        
        // 停止之前的动画
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        
        // 计算弧线路径
        CalculateArcPaths();
        
        // 启用LineRenderer
        lineRenderer1.enabled = true;
        lineRenderer2.enabled = true;
        
        // 开始动画
        animationCoroutine = StartCoroutine(AnimationCoroutine());
        
        isTrajectoryActive = true;
        
        if (enableDebugLogs)
        {
            Debug.Log($"DualArcTrajectoryRenderer: 创建双弧曳光弹 {start} -> {end}");
            Debug.Log($"DualArcTrajectoryRenderer: 弧线弯曲程度: {curvature}, 偏移距离: {arcOffset}");
        }
    }
    
    /// <summary>
    /// 隐藏弹道
    /// </summary>
    /// <param name="immediate">是否立即隐藏</param>
    public void HideTrajectory(bool immediate = false)
    {
        if (immediate)
        {
            // 立即隐藏
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }
            
            lineRenderer1.enabled = false;
            lineRenderer2.enabled = false;
            isTrajectoryActive = false;
            currentProgress = 0f;
        }
        else
        {
            // 淡出隐藏
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }
            animationCoroutine = StartCoroutine(FadeOutCoroutine());
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"DualArcTrajectoryRenderer: 隐藏弹道 (immediate={immediate})");
        }
    }
    
    #endregion
    
    #region 配置应用
    
    /// <summary>
    /// 应用弹道配置
    /// </summary>
    private void ApplyConfig(TrajectoryEffectConfig config)
    {
        trajectoryWidth = config.dualArcWidth;
        trajectoryColor = config.dualArcColor;
        curvature = Mathf.Clamp01(config.dualArcCurvature);
        arcOffset = config.dualArcOffset;
        segments = Mathf.Max(10, config.dualArcSegments);
        animationSpeed = config.dualArcAnimationSpeed;
        trajectoryMaterial = config.dualArcMaterial;
        useGradient = config.dualArcUseGradient;
        
        if (config.dualArcColorGradient != null)
        {
            colorGradient = config.dualArcColorGradient;
        }
        
        // 更新LineRenderer设置
        UpdateLineRendererSettings();
    }
    
    /// <summary>
    /// 更新LineRenderer设置
    /// </summary>
    private void UpdateLineRendererSettings()
    {
        if (lineRenderer1 != null)
        {
            lineRenderer1.startWidth = trajectoryWidth;
            lineRenderer1.endWidth = trajectoryWidth * 0.3f;
            
            if (trajectoryMaterial != null)
            {
                lineRenderer1.material = trajectoryMaterial;
            }
            
            if (useGradient && colorGradient != null)
            {
                lineRenderer1.colorGradient = colorGradient;
            }
        }
        
        if (lineRenderer2 != null)
        {
            lineRenderer2.startWidth = trajectoryWidth;
            lineRenderer2.endWidth = trajectoryWidth * 0.3f;
            
            if (trajectoryMaterial != null)
            {
                lineRenderer2.material = trajectoryMaterial;
            }
            
            if (useGradient && colorGradient != null)
            {
                lineRenderer2.colorGradient = colorGradient;
            }
        }
    }
    
    #endregion
    
    #region 弧线计算
    
    /// <summary>
    /// 计算两条弧线的路径
    /// </summary>
    private void CalculateArcPaths()
    {
        // 计算连线方向和垂直方向
        Vector3 direction = (endPosition - startPosition).normalized;
        Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0); // 2D垂直向量
        
        // 计算弧线控制点的偏移
        float distance = Vector3.Distance(startPosition, endPosition);
        float controlPointOffset = distance * curvature * 0.5f; // 根据弯曲程度计算控制点偏移
        
        // 初始化弧线点数组
        arc1Points = new Vector3[segments + 1];
        arc2Points = new Vector3[segments + 1];
        
        // 对于每条弧线，计算贝塞尔曲线路径
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            
            // 第一条弧线（向一侧偏移）
            Vector3 offset1 = perpendicular * arcOffset;
            Vector3 control1 = Vector3.Lerp(startPosition, endPosition, 0.5f) + perpendicular * controlPointOffset + offset1;
            arc1Points[i] = CalculateQuadraticBezierPoint(t, startPosition + offset1 * 0.3f, control1, endPosition + offset1 * 0.3f);
            
            // 第二条弧线（向另一侧偏移）
            Vector3 offset2 = perpendicular * -arcOffset;
            Vector3 control2 = Vector3.Lerp(startPosition, endPosition, 0.5f) + perpendicular * controlPointOffset + offset2;
            arc2Points[i] = CalculateQuadraticBezierPoint(t, startPosition + offset2 * 0.3f, control2, endPosition + offset2 * 0.3f);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"DualArcTrajectoryRenderer: 计算弧线路径完成，segments={segments}");
        }
    }
    
    /// <summary>
    /// 计算二次贝塞尔曲线上的点
    /// </summary>
    private Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        
        Vector3 point = uu * p0;           // (1-t)^2 * P0
        point += 2 * u * t * p1;           // 2(1-t)t * P1
        point += tt * p2;                   // t^2 * P2
        
        return point;
    }
    
    #endregion
    
    #region 动画
    
    /// <summary>
    /// 曳光弹动画协程
    /// </summary>
    private IEnumerator AnimationCoroutine()
    {
        currentProgress = 0f;
        float elapsedTime = 0f;
        float duration = 1f / animationSpeed;
        
        while (currentProgress < 1f)
        {
            elapsedTime += Time.deltaTime;
            currentProgress = Mathf.Clamp01(elapsedTime / duration);
            
            // 更新曳光弹显示
            UpdateTrajectoryDisplay(currentProgress);
            
            yield return null;
        }
        
        // 确保最终状态正确
        UpdateTrajectoryDisplay(1f);
        
        if (enableDebugLogs)
        {
            Debug.Log("DualArcTrajectoryRenderer: 动画播放完成");
        }
    }
    
    /// <summary>
    /// 更新弹道显示
    /// </summary>
    private void UpdateTrajectoryDisplay(float progress)
    {
        if (arc1Points == null || arc2Points == null) return;
        
        // 计算当前应该显示的点数
        int pointsToShow = Mathf.RoundToInt((segments + 1) * progress);
        pointsToShow = Mathf.Max(pointsToShow, 2); // 至少显示2个点
        
        // 更新第一条弧线
        lineRenderer1.positionCount = pointsToShow;
        for (int i = 0; i < pointsToShow; i++)
        {
            lineRenderer1.SetPosition(i, arc1Points[i]);
        }
        
        // 更新第二条弧线
        lineRenderer2.positionCount = pointsToShow;
        for (int i = 0; i < pointsToShow; i++)
        {
            lineRenderer2.SetPosition(i, arc2Points[i]);
        }
    }
    
    /// <summary>
    /// 淡出协程
    /// </summary>
    private IEnumerator FadeOutCoroutine()
    {
        float fadeTime = 0.5f;
        float elapsedTime = 0f;
        
        // 获取当前材质的颜色
        Material mat1 = lineRenderer1.material;
        Material mat2 = lineRenderer2.material;
        Color originalColor1 = mat1.color;
        Color originalColor2 = mat2.color;
        
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeTime);
            
            // 更新透明度
            Color newColor1 = originalColor1;
            newColor1.a = alpha;
            mat1.color = newColor1;
            
            Color newColor2 = originalColor2;
            newColor2.a = alpha;
            mat2.color = newColor2;
            
            yield return null;
        }
        
        // 完全隐藏
        lineRenderer1.enabled = false;
        lineRenderer2.enabled = false;
        isTrajectoryActive = false;
        currentProgress = 0f;
        
        // 恢复颜色
        mat1.color = originalColor1;
        mat2.color = originalColor2;
    }
    
    #endregion
    
    #region 设置方法
    
    /// <summary>
    /// 设置弹道宽度
    /// </summary>
    public void SetTrajectoryWidth(float width)
    {
        trajectoryWidth = width;
        UpdateLineRendererSettings();
    }
    
    /// <summary>
    /// 设置弹道颜色
    /// </summary>
    public void SetTrajectoryColor(Color color)
    {
        trajectoryColor = color;
        UpdateLineRendererSettings();
    }
    
    /// <summary>
    /// 设置弧线弯曲程度
    /// </summary>
    public void SetCurvature(float value)
    {
        curvature = Mathf.Clamp01(value);
    }
    
    /// <summary>
    /// 设置弧线偏移距离
    /// </summary>
    public void SetArcOffset(float offset)
    {
        arcOffset = offset;
    }
    
    /// <summary>
    /// 设置动画速度
    /// </summary>
    public void SetAnimationSpeed(float speed)
    {
        animationSpeed = speed;
    }
    
    /// <summary>
    /// 设置弹道材质
    /// </summary>
    public void SetTrajectoryMaterial(Material material)
    {
        trajectoryMaterial = material;
        UpdateLineRendererSettings();
    }
    
    #endregion
    
    #region 属性
    
    /// <summary>
    /// 弹道是否激活
    /// </summary>
    public bool IsTrajectoryActive => isTrajectoryActive;
    
    /// <summary>
    /// 当前动画进度
    /// </summary>
    public float CurrentProgress => currentProgress;
    
    #endregion
    
    #region 调试
    
    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;
        
        // 绘制起点和终点
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(startPosition, 0.3f);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(endPosition, 0.3f);
        
        // 绘制弧线路径
        if (arc1Points != null && arc1Points.Length > 1)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < arc1Points.Length - 1; i++)
            {
                Gizmos.DrawLine(arc1Points[i], arc1Points[i + 1]);
            }
        }
        
        if (arc2Points != null && arc2Points.Length > 1)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < arc2Points.Length - 1; i++)
            {
                Gizmos.DrawLine(arc2Points[i], arc2Points[i + 1]);
            }
        }
    }
    
    #endregion
}

