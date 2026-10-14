using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 魔法弹道渲染器
/// 用于渲染弯曲的丝带状法术轨迹
/// </summary>
public class MagicTrajectoryRenderer : MonoBehaviour
{
    [Header("弹道设置")]
    [SerializeField] private AnimationCurve trajectoryHeightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float maxTrajectoryHeight = 5f;     // 弹道最大高度
    [SerializeField] private float trajectoryWidth = 0.3f;      // 丝带宽度
    [SerializeField] private int trajectorySegments = 50;       // 轨迹分段数（越多越平滑）
    
    [Header("视觉效果")]
    [SerializeField] private Material trajectoryMaterial;       // 轨迹材质
    [SerializeField] private Gradient trajectoryColor = new Gradient();
    [SerializeField] private AnimationCurve widthCurve = AnimationCurve.Linear(0, 1, 1, 0.1f);
    [SerializeField] private bool useWorldSpace = true;
    [SerializeField] private int sortingOrder = 5;
    
    [Header("动画设置")]
    [SerializeField] private float appearDuration = 1f;        // 弹道出现时间
    [SerializeField] private AnimationCurve appearCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool fadeOut = true;              // 是否淡出
    [SerializeField] private float fadeOutDuration = 0.5f;     // 淡出时间
    
    [Header("魔法效果")]
    [SerializeField] private bool enableMagicEffect = true;    // 启用魔法效果
    [SerializeField] private float magicWaveSpeed = 2f;        // 魔法波动速度
    [SerializeField] private float magicWaveAmplitude = 0.1f;  // 魔法波动幅度
    [SerializeField] private int magicWaveCount = 3;           // 魔法波数量
    
    [Header("调试设置")]
    [SerializeField] private bool showDebugGizmos = false;
    [SerializeField] private bool enableDebugLogs = false;
    
    // 私有变量
    private LineRenderer lineRenderer;
    private Vector3 startPosition;
    private Vector3 endPosition;
    private Vector3[] trajectoryPoints;
    private Vector3[] originalTrajectoryPoints;
    private Coroutine trajectoryCoroutine;
    private bool isTrajectoryActive = false;
    private float trajectoryProgress = 0f;
    
    // 魔法效果变量
    private float magicTimer = 0f;
    
    #region Unity生命周期
    
    private void Awake()
    {
        InitializeLineRenderer();
    }
    
    private void Update()
    {
        if (isTrajectoryActive && enableMagicEffect)
        {
            UpdateMagicEffect();
        }
    }
    
    #endregion
    
    #region 初始化
    
    /// <summary>
    /// 初始化LineRenderer组件
    /// </summary>
    private void InitializeLineRenderer()
    {
        // 获取或创建LineRenderer
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }
        
        // 配置LineRenderer
        lineRenderer.material = trajectoryMaterial;
        lineRenderer.startWidth = trajectoryWidth;
        lineRenderer.endWidth = trajectoryWidth * 0.1f;
        lineRenderer.positionCount = 0;
        lineRenderer.useWorldSpace = useWorldSpace;
        lineRenderer.sortingOrder = sortingOrder;
        lineRenderer.widthCurve = widthCurve;
        lineRenderer.colorGradient = trajectoryColor;
        
        // 设置阴影
        lineRenderer.receiveShadows = false;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        
        if (enableDebugLogs)
        {
            Debug.Log("MagicTrajectoryRenderer: LineRenderer已初始化");
        }
    }
    
    #endregion
    
    #region 公共接口
    
    /// <summary>
    /// 创建弹道轨迹
    /// </summary>
    /// <param name="start">起始位置</param>
    /// <param name="end">结束位置</param>
    /// <param name="duration">弹道持续时间</param>
    public void CreateTrajectory(Vector3 start, Vector3 end, float duration = -1)
    {
        startPosition = start;
        endPosition = end;
        
        if (duration > 0)
        {
            appearDuration = duration;
        }
        
        // 停止之前的弹道
        if (trajectoryCoroutine != null)
        {
            StopCoroutine(trajectoryCoroutine);
        }
        
        // 计算弹道路径
        CalculateTrajectoryPath();
        
        // 开始弹道动画
        trajectoryCoroutine = StartCoroutine(TrajectoryAnimationCoroutine());
        
        if (enableDebugLogs)
        {
            Debug.Log($"MagicTrajectoryRenderer: 创建弹道 {start} -> {end}, 持续时间: {appearDuration}秒");
        }
    }
    
    /// <summary>
    /// 立即显示完整弹道
    /// </summary>
    /// <param name="start">起始位置</param>
    /// <param name="end">结束位置</param>
    public void ShowCompleteTrajectory(Vector3 start, Vector3 end)
    {
        startPosition = start;
        endPosition = end;
        
        CalculateTrajectoryPath();
        DisplayTrajectory(1f);
        isTrajectoryActive = true;
        
        if (enableDebugLogs)
        {
            Debug.Log($"MagicTrajectoryRenderer: 立即显示完整弹道 {start} -> {end}");
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
            lineRenderer.positionCount = 0;
            isTrajectoryActive = false;
            
            if (trajectoryCoroutine != null)
            {
                StopCoroutine(trajectoryCoroutine);
                trajectoryCoroutine = null;
            }
        }
        else if (fadeOut)
        {
            StartCoroutine(FadeOutTrajectory());
        }
        else
        {
            HideTrajectory(true);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log("MagicTrajectoryRenderer: 隐藏弹道");
        }
    }
    
    /// <summary>
    /// 设置弹道颜色
    /// </summary>
    /// <param name="gradient">颜色渐变</param>
    public void SetTrajectoryColor(Gradient gradient)
    {
        trajectoryColor = gradient;
        if (lineRenderer != null)
        {
            lineRenderer.colorGradient = gradient;
        }
    }
    
    /// <summary>
    /// 设置弹道宽度
    /// </summary>
    /// <param name="width">宽度</param>
    public void SetTrajectoryWidth(float width)
    {
        trajectoryWidth = width;
        if (lineRenderer != null)
        {
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width * 0.1f;
        }
    }
    
    /// <summary>
    /// 获取弹道是否激活
    /// </summary>
    public bool IsTrajectoryActive => isTrajectoryActive;
    
    /// <summary>
    /// 获取弹道进度 (0-1)
    /// </summary>
    public float TrajectoryProgress => trajectoryProgress;
    
    #endregion
    
    #region 私有方法
    
    /// <summary>
    /// 计算弹道路径
    /// </summary>
    private void CalculateTrajectoryPath()
    {
        trajectoryPoints = new Vector3[trajectorySegments + 1];
        originalTrajectoryPoints = new Vector3[trajectorySegments + 1];
        
        Vector3 direction = endPosition - startPosition;
        float distance = direction.magnitude;
        
        // 计算控制点（贝塞尔曲线）
        Vector3 midPoint = startPosition + direction * 0.5f;
        Vector3 heightOffset = Vector3.up * maxTrajectoryHeight * trajectoryHeightCurve.Evaluate(0.5f);
        
        // 添加一些随机性让轨迹更自然
        Vector3 randomOffset = new Vector3(
            Random.Range(-distance * 0.1f, distance * 0.1f),
            Random.Range(0, maxTrajectoryHeight * 0.3f),
            Random.Range(-distance * 0.1f, distance * 0.1f)
        );
        
        Vector3 controlPoint = midPoint + heightOffset + randomOffset;
        
        // 使用二次贝塞尔曲线生成轨迹点
        for (int i = 0; i <= trajectorySegments; i++)
        {
            float t = (float)i / trajectorySegments;
            
            // 二次贝塞尔曲线公式
            Vector3 point = Mathf.Pow(1 - t, 2) * startPosition +
                           2 * (1 - t) * t * controlPoint +
                           Mathf.Pow(t, 2) * endPosition;
            
            // 应用高度曲线
            float heightMultiplier = trajectoryHeightCurve.Evaluate(t);
            point.y += heightMultiplier * maxTrajectoryHeight * 0.2f; // 额外的高度变化
            
            trajectoryPoints[i] = point;
            originalTrajectoryPoints[i] = point;
        }
    }
    
    /// <summary>
    /// 弹道动画协程
    /// </summary>
    private IEnumerator TrajectoryAnimationCoroutine()
    {
        isTrajectoryActive = true;
        float elapsedTime = 0f;
        
        while (elapsedTime < appearDuration)
        {
            elapsedTime += Time.deltaTime;
            trajectoryProgress = appearCurve.Evaluate(elapsedTime / appearDuration);
            
            DisplayTrajectory(trajectoryProgress);
            
            yield return null;
        }
        
        // 确保最终状态正确
        trajectoryProgress = 1f;
        DisplayTrajectory(1f);
        
        // 如果设置了淡出，等待一段时间后开始淡出
        if (fadeOut)
        {
            yield return new WaitForSeconds(0.2f);
            yield return StartCoroutine(FadeOutTrajectory());
        }
    }
    
    /// <summary>
    /// 显示弹道
    /// </summary>
    /// <param name="progress">显示进度 (0-1)</param>
    private void DisplayTrajectory(float progress)
    {
        if (trajectoryPoints == null) return;
        
        int pointsToShow = Mathf.RoundToInt(trajectoryPoints.Length * progress);
        pointsToShow = Mathf.Max(pointsToShow, 2); // 至少显示2个点
        
        lineRenderer.positionCount = pointsToShow;
        
        for (int i = 0; i < pointsToShow; i++)
        {
            lineRenderer.SetPosition(i, trajectoryPoints[i]);
        }
    }
    
    /// <summary>
    /// 更新魔法效果
    /// </summary>
    private void UpdateMagicEffect()
    {
        if (trajectoryPoints == null || originalTrajectoryPoints == null) return;
        
        magicTimer += Time.deltaTime * magicWaveSpeed;
        
        // 应用魔法波动效果
        for (int i = 0; i < trajectoryPoints.Length; i++)
        {
            Vector3 originalPoint = originalTrajectoryPoints[i];
            
            // 计算波动偏移
            float wavePhase = magicTimer + (float)i / trajectoryPoints.Length * magicWaveCount * Mathf.PI * 2;
            Vector3 waveOffset = new Vector3(
                Mathf.Sin(wavePhase) * magicWaveAmplitude,
                Mathf.Cos(wavePhase * 1.5f) * magicWaveAmplitude * 0.5f,
                Mathf.Sin(wavePhase * 0.8f) * magicWaveAmplitude * 0.3f
            );
            
            trajectoryPoints[i] = originalPoint + waveOffset;
        }
        
        // 更新LineRenderer
        if (lineRenderer.positionCount > 0)
        {
            for (int i = 0; i < lineRenderer.positionCount; i++)
            {
                lineRenderer.SetPosition(i, trajectoryPoints[i]);
            }
        }
    }
    
    /// <summary>
    /// 淡出弹道协程
    /// </summary>
    private IEnumerator FadeOutTrajectory()
    {
        if (lineRenderer == null) yield break;
        
        Gradient originalGradient = lineRenderer.colorGradient;
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = 1f - (elapsedTime / fadeOutDuration);
            
            // 创建新的渐变色，调整透明度
            Gradient fadedGradient = new Gradient();
            GradientColorKey[] colorKeys = originalGradient.colorKeys;
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[originalGradient.alphaKeys.Length];
            
            for (int i = 0; i < originalGradient.alphaKeys.Length; i++)
            {
                alphaKeys[i] = new GradientAlphaKey(originalGradient.alphaKeys[i].alpha * alpha, originalGradient.alphaKeys[i].time);
            }
            
            fadedGradient.SetKeys(colorKeys, alphaKeys);
            lineRenderer.colorGradient = fadedGradient;
            
            yield return null;
        }
        
        // 完全隐藏
        HideTrajectory(true);
        
        // 恢复原始颜色
        lineRenderer.colorGradient = originalGradient;
        
        isTrajectoryActive = false;
    }
    
    #endregion
    
    #region Gizmos
    
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        // 绘制起始和结束点
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(startPosition, 0.2f);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(endPosition, 0.2f);
        
        // 绘制弹道路径
        if (trajectoryPoints != null && trajectoryPoints.Length > 1)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < trajectoryPoints.Length - 1; i++)
            {
                Gizmos.DrawLine(trajectoryPoints[i], trajectoryPoints[i + 1]);
            }
        }
    }
    
    #endregion
}
