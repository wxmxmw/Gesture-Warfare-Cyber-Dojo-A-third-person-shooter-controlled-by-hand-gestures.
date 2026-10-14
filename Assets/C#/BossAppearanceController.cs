using UnityEngine;
using System.Collections;

/// <summary>
/// Boss出现效果控制器
/// 用于控制Boss出现时的网格线Shader效果
/// </summary>
public class BossAppearanceController : MonoBehaviour
{
    [Header("材质设置")]
    [SerializeField] private Material gridMaterial;           // 网格线材质
    [SerializeField] private SpriteRenderer[] targetRenderers; // 目标渲染器数组
    [SerializeField] private bool autoFindRenderers = true;   // 自动查找渲染器
    
    [Header("动画设置")]
    [SerializeField] private AnimationCurve appearanceCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float totalDuration = 3f;       // 总持续时间
    [SerializeField] private float gridFadeDelay = 2f;       // 网格线开始褪色的延迟
    
    [Header("网格线设置")]
    [SerializeField] private Color gridColor = new Color(0, 1, 1, 1);
    [SerializeField] private float gridThickness = 0.02f;
    [SerializeField] private float gridScale = 1f;
    [SerializeField] private bool enableFlow = true;
    [SerializeField] private float flowSpeed = 1f;
    [SerializeField] private Vector2 flowDirection = Vector2.right;
    [SerializeField] private float gridIntensity = 1f;
    [SerializeField] private float gridGlow = 1f;
    
    [Header("调试设置")]
    [SerializeField] private bool playOnStart = false;
    [SerializeField] private bool showDebugLog = true;
    
    // 私有变量
    private Material[] originalMaterials;
    private Material[] gridMaterials;
    private Coroutine appearanceCoroutine;
    private bool isPlaying = false;
    
    // Shader属性ID（优化性能）
    private static readonly int MainAlphaID = Shader.PropertyToID("_MainAlpha");
    private static readonly int GridAlphaID = Shader.PropertyToID("_GridAlpha");
    private static readonly int OverallAlphaID = Shader.PropertyToID("_OverallAlpha");
    private static readonly int GridColorID = Shader.PropertyToID("_GridColor");
    private static readonly int GridThicknessID = Shader.PropertyToID("_GridThickness");
    private static readonly int GridScaleID = Shader.PropertyToID("_GridScale");
    private static readonly int EnableFlowID = Shader.PropertyToID("_EnableFlow");
    private static readonly int FlowSpeedID = Shader.PropertyToID("_FlowSpeed");
    private static readonly int FlowDirectionID = Shader.PropertyToID("_FlowDirection");
    private static readonly int GridIntensityID = Shader.PropertyToID("_GridIntensity");
    private static readonly int GridGlowID = Shader.PropertyToID("_GridGlow");
    
    #region Unity生命周期
    
    private void Awake()
    {
        InitializeController();
    }
    
    private void Start()
    {
        if (playOnStart)
        {
            PlayAppearanceEffect();
        }
    }
    
    private void OnValidate()
    {
        // 在编辑器中实时更新材质参数
        if (Application.isPlaying && gridMaterials != null)
        {
            UpdateGridMaterialProperties();
        }
    }
    
    #endregion
    
    #region 初始化
    
    /// <summary>
    /// 初始化控制器
    /// </summary>
    private void InitializeController()
    {
        // 自动查找SpriteRenderer
        if (autoFindRenderers)
        {
            targetRenderers = GetComponentsInChildren<SpriteRenderer>();
        }
        
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            Debug.LogWarning("BossAppearanceController: 未找到目标SpriteRenderer组件");
            return;
        }
        
        // 保存原始材质
        originalMaterials = new Material[targetRenderers.Length];
        gridMaterials = new Material[targetRenderers.Length];
        
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] != null)
            {
                originalMaterials[i] = targetRenderers[i].material;
                
                // 创建网格线材质实例
                if (gridMaterial != null)
                {
                    gridMaterials[i] = new Material(gridMaterial);
                    // 设置主贴图为原始贴图
                    if (originalMaterials[i].mainTexture != null)
                    {
                        gridMaterials[i].mainTexture = originalMaterials[i].mainTexture;
                    }
                }
            }
        }
        
        // 初始化材质属性
        UpdateGridMaterialProperties();
        
        if (showDebugLog)
        {
            Debug.Log($"BossAppearanceController: 已初始化 {targetRenderers.Length} 个渲染器");
        }
    }
    
    #endregion
    
    #region 公共接口
    
    /// <summary>
    /// 播放Boss出现效果
    /// </summary>
    public void PlayAppearanceEffect()
    {
        if (isPlaying)
        {
            StopAppearanceEffect();
        }
        
        appearanceCoroutine = StartCoroutine(AppearanceEffectCoroutine());
    }
    
    /// <summary>
    /// 停止Boss出现效果
    /// </summary>
    public void StopAppearanceEffect()
    {
        if (appearanceCoroutine != null)
        {
            StopCoroutine(appearanceCoroutine);
            appearanceCoroutine = null;
        }
        
        isPlaying = false;
        RestoreOriginalMaterials();
    }
    
    /// <summary>
    /// 设置网格线透明度（用于动画系统调用）
    /// </summary>
    /// <param name="alpha">透明度值 (0-1)</param>
    public void SetGridAlpha(float alpha)
    {
        if (gridMaterials != null)
        {
            foreach (Material mat in gridMaterials)
            {
                if (mat != null)
                {
                    mat.SetFloat(GridAlphaID, alpha);
                }
            }
        }
    }
    
    /// <summary>
    /// 设置主贴图透明度（用于动画系统调用）
    /// </summary>
    /// <param name="alpha">透明度值 (0-1)</param>
    public void SetMainAlpha(float alpha)
    {
        if (gridMaterials != null)
        {
            foreach (Material mat in gridMaterials)
            {
                if (mat != null)
                {
                    mat.SetFloat(MainAlphaID, alpha);
                }
            }
        }
    }
    
    /// <summary>
    /// 设置整体透明度（用于动画系统调用）
    /// </summary>
    /// <param name="alpha">透明度值 (0-1)</param>
    public void SetOverallAlpha(float alpha)
    {
        if (gridMaterials != null)
        {
            foreach (Material mat in gridMaterials)
            {
                if (mat != null)
                {
                    mat.SetFloat(OverallAlphaID, alpha);
                }
            }
        }
    }
    
    /// <summary>
    /// 设置网格线颜色
    /// </summary>
    /// <param name="color">网格线颜色</param>
    public void SetGridColor(Color color)
    {
        gridColor = color;
        if (gridMaterials != null)
        {
            foreach (Material mat in gridMaterials)
            {
                if (mat != null)
                {
                    mat.SetColor(GridColorID, color);
                }
            }
        }
    }
    
    /// <summary>
    /// 应用网格线材质
    /// </summary>
    public void ApplyGridMaterials()
    {
        if (targetRenderers != null && gridMaterials != null)
        {
            for (int i = 0; i < targetRenderers.Length; i++)
            {
                if (targetRenderers[i] != null && gridMaterials[i] != null)
                {
                    targetRenderers[i].material = gridMaterials[i];
                }
            }
        }
    }
    
    /// <summary>
    /// 恢复原始材质
    /// </summary>
    public void RestoreOriginalMaterials()
    {
        if (targetRenderers != null && originalMaterials != null)
        {
            for (int i = 0; i < targetRenderers.Length; i++)
            {
                if (targetRenderers[i] != null && originalMaterials[i] != null)
                {
                    targetRenderers[i].material = originalMaterials[i];
                }
            }
        }
    }
    
    #endregion
    
    #region 私有方法
    
    /// <summary>
    /// Boss出现效果协程
    /// </summary>
    private IEnumerator AppearanceEffectCoroutine()
    {
        isPlaying = true;
                
        // 应用网格线材质
        ApplyGridMaterials();
        
        float elapsedTime = 0f;
        
        // 第一阶段：完全透明到显示网格线 (0% - 40%)
        float phase1Duration = totalDuration * 0.4f;
        while (elapsedTime < phase1Duration)
        {
            float progress = elapsedTime / phase1Duration;
            float curveValue = appearanceCurve.Evaluate(progress);
            
            SetOverallAlpha(curveValue);
            SetMainAlpha(0f);
            SetGridAlpha(curveValue);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // 第二阶段：网格线完全显示，主贴图逐渐显示 (40% - 70%)
        float phase2Start = phase1Duration;
        float phase2Duration = totalDuration * 0.3f;
        while (elapsedTime < phase2Start + phase2Duration)
        {
            float progress = (elapsedTime - phase2Start) / phase2Duration;
            float curveValue = appearanceCurve.Evaluate(progress);
            
            SetOverallAlpha(1f);
            SetMainAlpha(curveValue);
            SetGridAlpha(1f);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // 第三阶段：主贴图完全显示，等待网格线褪色 (70% - 100%)
        float phase3Start = phase2Start + phase2Duration;
        float phase3Duration = totalDuration * 0.3f;
        while (elapsedTime < totalDuration)
        {
            float progress = (elapsedTime - phase3Start) / phase3Duration;
            float curveValue = 1f - appearanceCurve.Evaluate(progress);
            
            SetOverallAlpha(1f);
            SetMainAlpha(1f);
            SetGridAlpha(curveValue);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // 最终状态：恢复原始材质
        RestoreOriginalMaterials();
        
        isPlaying = false;
        
    }
    
    /// <summary>
    /// 更新网格线材质属性
    /// </summary>
    private void UpdateGridMaterialProperties()
    {
        if (gridMaterials == null) return;
        
        foreach (Material mat in gridMaterials)
        {
            if (mat != null)
            {
                mat.SetColor(GridColorID, gridColor);
                mat.SetFloat(GridThicknessID, gridThickness);
                mat.SetFloat(GridScaleID, gridScale);
                mat.SetFloat(EnableFlowID, enableFlow ? 1f : 0f);
                mat.SetFloat(FlowSpeedID, flowSpeed);
                mat.SetVector(FlowDirectionID, new Vector4(flowDirection.x, flowDirection.y, 0, 0));
                mat.SetFloat(GridIntensityID, gridIntensity);
                mat.SetFloat(GridGlowID, gridGlow);
            }
        }
    }
    
    #endregion
    
    #region 编辑器工具
    
#if UNITY_EDITOR
    [ContextMenu("预览出现效果")]
    private void PreviewAppearanceEffect()
    {
        if (Application.isPlaying)
        {
            PlayAppearanceEffect();
        }
        else
        {
            Debug.Log("请在运行时使用此功能");
        }
    }
    
    [ContextMenu("应用网格线材质")]
    private void ApplyGridMaterialsEditor()
    {
        if (Application.isPlaying)
        {
            ApplyGridMaterials();
        }
    }
    
    [ContextMenu("恢复原始材质")]
    private void RestoreOriginalMaterialsEditor()
    {
        if (Application.isPlaying)
        {
            RestoreOriginalMaterials();
        }
    }
#endif
    
    #endregion
}
