using UnityEngine;
using System.Collections;

/// <summary>
/// 直接射击弹道渲染器
/// 从敌人位置直接射击到目标位置，支持亮度变化效果
/// </summary>
public class DirectShotTrajectoryRenderer : MonoBehaviour
{
    [Header("弹道设置")]
    [SerializeField] private float trajectoryWidth = 0.2f;          // 弹道宽度
    [SerializeField] private Color trajectoryColor = Color.white;   // 弹道颜色
    [SerializeField] private Material trajectoryMaterial;          // 弹道材质
    
    [Header("亮度变化设置")]
    [SerializeField] private float brightnessRiseTime = 0.1f;     // 亮度上升时间
    [SerializeField] private float brightnessHoldTime = 0.2f;     // 亮度保持时间
    [SerializeField] private float brightnessFadeTime = 0.3f;     // 亮度衰减时间
    [SerializeField] private float maxBrightness = 2f;            // 最大亮度倍数
    [SerializeField] private AnimationCurve brightnessCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("视觉效果")]
    [SerializeField] private bool enableParticles = true;         // 启用粒子效果
    [SerializeField] private float particleIntensity = 1f;         // 粒子强度
    [SerializeField] private ParticleSystem shotParticles;        // 射击粒子系统
    
    [Header("调试设置")]
    [SerializeField] private bool enableDebugLogs = false;
    
    // 私有变量
    private LineRenderer lineRenderer;
    private Vector3 startPosition;
    private Vector3 endPosition;
    private Coroutine brightnessCoroutine;
    private bool isTrajectoryActive = false;
    private float currentBrightness = 0f;
    private float currentAlpha = 0f;
    
    // 着色器属性ID（用于性能优化）
    private int _BrightnessID;
    private int _AlphaID;
    private int _ColorID;
    
    #region Unity生命周期
    
    private void Awake()
    {
        InitializeLineRenderer();
        CacheShaderProperties();
    }
    
    private void Update()
    {
        if (isTrajectoryActive && lineRenderer != null)
        {
            UpdateTrajectoryVisuals();
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
        lineRenderer.endWidth = trajectoryWidth;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.sortingOrder = 10;
        
        // 设置颜色渐变
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(trajectoryColor, 0.0f), new GradientColorKey(trajectoryColor, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(trajectoryColor.a, 0.0f), new GradientAlphaKey(trajectoryColor.a, 1.0f) }
        );
        lineRenderer.colorGradient = gradient;
        
        // 设置阴影
        lineRenderer.receiveShadows = false;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        
        // 默认禁用LineRenderer，只有在需要时才启用
        lineRenderer.enabled = false;
        
        // 设置材质
        if (trajectoryMaterial != null)
        {
            // 使用指定的材质
            lineRenderer.material = trajectoryMaterial;
            if (enableDebugLogs)
            {
                Debug.Log($"DirectShotTrajectoryRenderer: 使用指定材质: {trajectoryMaterial.name}");
            }
        }
        else
        {
            // 创建一个简单的材质，使用Unlit/Color着色器
            Material testMaterial = new Material(Shader.Find("Unlit/Color"));
            testMaterial.color = trajectoryColor;
            lineRenderer.material = testMaterial;
            if (enableDebugLogs)
            {
                Debug.Log("DirectShotTrajectoryRenderer: 使用默认材质");
            }
        }
        
        if (enableDebugLogs)
        {
            Debug.Log("DirectShotTrajectoryRenderer: LineRenderer已初始化");
        }
    }
    
    /// <summary>
    /// 缓存着色器属性ID
    /// </summary>
    private void CacheShaderProperties()
    {
        _BrightnessID = Shader.PropertyToID("_Brightness");
        _AlphaID = Shader.PropertyToID("_Alpha");
        _ColorID = Shader.PropertyToID("_Color");
    }
    
    #endregion
    
    #region 公共接口
    
    /// <summary>
    /// 创建直接射击弹道
    /// </summary>
    /// <param name="start">起始位置</param>
    /// <param name="end">结束位置</param>
    /// <param name="config">弹道配置</param>
    public void CreateDirectShot(Vector3 start, Vector3 end, TrajectoryEffectConfig config = null)
    {
        startPosition = start;
        endPosition = end;
        
        // 应用配置
        if (config != null)
        {
            ApplyConfig(config);
        }
        
        // 停止之前的协程
        if (brightnessCoroutine != null)
        {
            StopCoroutine(brightnessCoroutine);
        }
        
        // 设置弹道位置
        lineRenderer.SetPosition(0, startPosition);
        lineRenderer.SetPosition(1, endPosition);
        
        // 启用LineRenderer
        lineRenderer.enabled = true;
        
        // 开始亮度变化动画
        brightnessCoroutine = StartCoroutine(BrightnessAnimationCoroutine());
        
        // 启用粒子效果
        if (enableParticles && shotParticles != null)
        {
            EnableParticleEffect();
        }
        
        isTrajectoryActive = true;
        
        if (enableDebugLogs)
        {
            Debug.Log($"DirectShotTrajectoryRenderer: 创建直接射击 {start} -> {end}");
            Debug.Log($"DirectShotTrajectoryRenderer: LineRenderer enabled: {lineRenderer.enabled}");
            Debug.Log($"DirectShotTrajectoryRenderer: LineRenderer positionCount: {lineRenderer.positionCount}");
            Debug.Log($"DirectShotTrajectoryRenderer: 弹道颜色: {trajectoryColor}");
            Debug.Log($"DirectShotTrajectoryRenderer: 材质: {lineRenderer.material.name}");
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
            if (brightnessCoroutine != null)
            {
                StopCoroutine(brightnessCoroutine);
            }
            
            lineRenderer.enabled = false;
            isTrajectoryActive = false;
            
            // 禁用粒子效果
            if (shotParticles != null)
            {
                shotParticles.Stop();
            }
        }
        else
        {
            // 开始淡出动画
            if (brightnessCoroutine != null)
            {
                StopCoroutine(brightnessCoroutine);
            }
            brightnessCoroutine = StartCoroutine(FadeOutCoroutine());
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"DirectShotTrajectoryRenderer: 隐藏弹道 (immediate: {immediate})");
        }
    }
    
    /// <summary>
    /// 获取弹道是否激活
    /// </summary>
    public bool IsTrajectoryActive => isTrajectoryActive;
    
    /// <summary>
    /// 获取当前亮度
    /// </summary>
    public float CurrentBrightness => currentBrightness;
    
    /// <summary>
    /// 获取当前透明度
    /// </summary>
    public float CurrentAlpha => currentAlpha;
    
    #endregion
    
    #region 私有方法
    
    /// <summary>
    /// 应用配置
    /// </summary>
    /// <param name="config">配置</param>
    private void ApplyConfig(TrajectoryEffectConfig config)
    {
        trajectoryWidth = config.directShotWidth;
        trajectoryColor = config.directShotColor;
        trajectoryMaterial = config.directShotMaterial;
        
        brightnessRiseTime = config.brightnessRiseTime;
        brightnessHoldTime = config.brightnessHoldTime;
        brightnessFadeTime = config.brightnessFadeTime;
        maxBrightness = config.maxBrightness;
        brightnessCurve = config.brightnessCurve;
        
        enableParticles = config.enableParticles;
        particleIntensity = config.particleIntensity;
        
        // 更新LineRenderer设置
        if (lineRenderer != null)
        {
            lineRenderer.startWidth = trajectoryWidth;
            lineRenderer.endWidth = trajectoryWidth;
            
            // 设置颜色渐变
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(trajectoryColor, 0.0f), new GradientColorKey(trajectoryColor, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(trajectoryColor.a, 0.0f), new GradientAlphaKey(trajectoryColor.a, 1.0f) }
            );
            lineRenderer.colorGradient = gradient;
            
            if (trajectoryMaterial != null)
            {
                lineRenderer.material = trajectoryMaterial;
            }
        }
    }
    
    /// <summary>
    /// 亮度变化动画协程
    /// </summary>
    private IEnumerator BrightnessAnimationCoroutine()
    {
        float totalDuration = brightnessRiseTime + brightnessHoldTime + brightnessFadeTime;
        float elapsedTime = 0f;
        
        while (elapsedTime < totalDuration)
        {
            elapsedTime += Time.deltaTime;
            
            // 计算当前阶段
            if (elapsedTime <= brightnessRiseTime)
            {
                // 亮度上升阶段
                float progress = elapsedTime / brightnessRiseTime;
                currentBrightness = Mathf.Lerp(0f, maxBrightness, brightnessCurve.Evaluate(progress));
                currentAlpha = Mathf.Lerp(0f, 1f, progress);
            }
            else if (elapsedTime <= brightnessRiseTime + brightnessHoldTime)
            {
                // 亮度保持阶段
                currentBrightness = maxBrightness;
                currentAlpha = 1f;
            }
            else
            {
                // 亮度衰减阶段
                float fadeProgress = (elapsedTime - brightnessRiseTime - brightnessHoldTime) / brightnessFadeTime;
                currentBrightness = Mathf.Lerp(maxBrightness, 0f, fadeProgress);
                currentAlpha = Mathf.Lerp(1f, 0f, fadeProgress);
            }
            
            yield return null;
        }
        
        // 确保最终状态
        currentBrightness = 0f;
        currentAlpha = 0f;
        isTrajectoryActive = false;
        
        // 隐藏弹道
        lineRenderer.enabled = false;
        
        if (enableDebugLogs)
        {
            Debug.Log("DirectShotTrajectoryRenderer: 亮度动画完成");
        }
    }
    
    /// <summary>
    /// 淡出协程
    /// </summary>
    private IEnumerator FadeOutCoroutine()
    {
        float fadeTime = 0.5f; // 淡出时间
        float elapsedTime = 0f;
        float startAlpha = currentAlpha;
        
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeTime;
            
            currentAlpha = Mathf.Lerp(startAlpha, 0f, progress);
            currentBrightness = Mathf.Lerp(currentBrightness, 0f, progress);
            
            yield return null;
        }
        
        currentAlpha = 0f;
        currentBrightness = 0f;
        isTrajectoryActive = false;
        lineRenderer.enabled = false;
    }
    
    /// <summary>
    /// 更新弹道视觉效果
    /// </summary>
    private void UpdateTrajectoryVisuals()
    {
        if (lineRenderer == null) return;
        
        // 更新颜色和亮度
        Color currentColor = trajectoryColor;
        currentColor.a = currentAlpha;
        
        // 应用亮度（确保颜色值不超过1.0）
        if (currentBrightness > 0f)
        {
            currentColor.r = Mathf.Min(1f, currentColor.r * (1f + currentBrightness));
            currentColor.g = Mathf.Min(1f, currentColor.g * (1f + currentBrightness));
            currentColor.b = Mathf.Min(1f, currentColor.b * (1f + currentBrightness));
        }
        
        // 更新颜色渐变
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(currentColor, 0.0f), new GradientColorKey(currentColor, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(currentColor.a, 0.0f), new GradientAlphaKey(currentColor.a, 1.0f) }
        );
        lineRenderer.colorGradient = gradient;
        
        // 更新材质属性
        if (trajectoryMaterial != null)
        {
            trajectoryMaterial.SetFloat(_BrightnessID, 1f + currentBrightness);
            trajectoryMaterial.SetFloat(_AlphaID, currentAlpha);
            trajectoryMaterial.SetColor(_ColorID, currentColor);
        }
    }
    
    /// <summary>
    /// 启用粒子效果
    /// </summary>
    private void EnableParticleEffect()
    {
        if (shotParticles == null) return;
        
        // 设置粒子位置
        shotParticles.transform.position = startPosition;
        
        // 设置粒子方向
        Vector3 direction = (endPosition - startPosition).normalized;
        shotParticles.transform.rotation = Quaternion.LookRotation(direction);
        
        // 设置粒子强度
        var emission = shotParticles.emission;
        emission.rateOverTime = 50f * particleIntensity;
        
        // 播放粒子
        shotParticles.Play();
        
        if (enableDebugLogs)
        {
            Debug.Log("DirectShotTrajectoryRenderer: 启用粒子效果");
        }
    }
    
    #endregion
    
    #region 公共设置方法
    
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
            lineRenderer.endWidth = width;
        }
    }
    
    /// <summary>
    /// 设置弹道颜色
    /// </summary>
    /// <param name="color">颜色</param>
    public void SetTrajectoryColor(Color color)
    {
        trajectoryColor = color;
        if (lineRenderer != null)
        {
            // 设置颜色渐变
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(color, 0.0f), new GradientColorKey(color, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(color.a, 0.0f), new GradientAlphaKey(color.a, 1.0f) }
            );
            lineRenderer.colorGradient = gradient;
            
            // 同时设置材质的颜色
            if (lineRenderer.material != null)
            {
                lineRenderer.material.color = color;
            }
        }
    }
    
    /// <summary>
    /// 设置弹道材质
    /// </summary>
    /// <param name="material">材质</param>
    public void SetTrajectoryMaterial(Material material)
    {
        trajectoryMaterial = material;
        if (lineRenderer != null)
        {
            lineRenderer.material = material;
            
            // 强制设置材质颜色
            if (material != null)
            {
                material.color = trajectoryColor;
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"DirectShotTrajectoryRenderer: 设置材质 {material?.name}, 颜色: {trajectoryColor}");
            }
        }
    }
    
    /// <summary>
    /// 设置亮度变化参数
    /// </summary>
    /// <param name="riseTime">上升时间</param>
    /// <param name="holdTime">保持时间</param>
    /// <param name="fadeTime">衰减时间</param>
    /// <param name="maxBrightness">最大亮度</param>
    public void SetBrightnessSettings(float riseTime, float holdTime, float fadeTime, float maxBrightness)
    {
        brightnessRiseTime = riseTime;
        brightnessHoldTime = holdTime;
        brightnessFadeTime = fadeTime;
        this.maxBrightness = maxBrightness;
    }
    
    /// <summary>
    /// 强制设置白色材质（用于调试）
    /// </summary>
    [ContextMenu("强制设置白色材质")]
    public void ForceSetWhiteMaterial()
    {
        if (lineRenderer != null)
        {
            // 创建白色材质
            Material whiteMaterial = new Material(Shader.Find("Unlit/Color"));
            whiteMaterial.color = Color.white;
            lineRenderer.material = whiteMaterial;
            
            // 设置白色颜色渐变
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0.0f), new GradientAlphaKey(1f, 1.0f) }
            );
            lineRenderer.colorGradient = gradient;
            
            Debug.Log("DirectShotTrajectoryRenderer: 强制设置白色材质");
        }
    }
    
    #endregion
    
    #region 清理
    
    private void OnDestroy()
    {
        if (brightnessCoroutine != null)
        {
            StopCoroutine(brightnessCoroutine);
        }
        
        if (shotParticles != null)
        {
            shotParticles.Stop();
        }
    }
    
    #endregion
}
