using UnityEngine;

/// <summary>
/// 光束效果组件
/// 用于显示从标靶到玩家的光束动画
/// </summary>
public class BeamEffect : MonoBehaviour
{
    [Header("Beam Settings")]
    [SerializeField] private float beamDuration = 1f;           // 光束持续时间
    [SerializeField] private float maxLength = 10f;            // 最大光束长度
    [SerializeField] private Color beamColor = Color.yellow;      // 光束颜色
    [SerializeField] private float beamWidth = 0.1f;           // 光束宽度
    [SerializeField] private float animationSpeed = 2f;         // 动画速度
    [SerializeField] private bool enableDebugLogs = false;     // 是否启用调试日志
    
    [Header("Visual Settings")]
    [SerializeField] private Material beamMaterial;            // 光束材质
    [SerializeField] private Texture2D beamTexture;             // 光束纹理
    
    // 运行时变量
    private LineRenderer lineRenderer;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private Transform targetTransform;
    private Vector3 startPosition;
    private Vector3 endPosition;
    private float elapsedTime;
    private bool isActive = false;
    
    private void Awake()
    {
        CreateBeamRenderer();
    }
    
    private void Update()
    {
        if (isActive)
        {
            UpdateBeamAnimation();
        }
    }
    
    /// <summary>
    /// 创建光束渲染器
    /// </summary>
    private void CreateBeamRenderer()
    {
        // 创建LineRenderer用于光束
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        
        // 设置LineRenderer属性
        lineRenderer.material = beamMaterial != null ? beamMaterial : CreateDefaultBeamMaterial();
        lineRenderer.startColor = beamColor;
        lineRenderer.endColor = beamColor;
        lineRenderer.startWidth = beamWidth;
        lineRenderer.endWidth = beamWidth;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.sortingOrder = 10;
        
        // 设置纹理
        if (beamTexture != null)
        {
            lineRenderer.material.mainTexture = beamTexture;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Beam renderer created");
        }
    }
    
    /// <summary>
    /// 创建默认光束材质
    /// </summary>
    /// <returns>默认材质</returns>
    private Material CreateDefaultBeamMaterial()
    {
        Material mat = new Material(Shader.Find("Custom/BeamShader"));
        if (mat.shader == null)
        {
            // 如果自定义着色器不存在，使用默认着色器
            mat = new Material(Shader.Find("Sprites/Default"));
        }
        
        mat.color = beamColor;
        mat.SetFloat("_Intensity", 1f);
        mat.SetFloat("_Speed", animationSpeed);
        
        return mat;
    }
    
    /// <summary>
    /// 启动光束效果
    /// </summary>
    /// <param name="startPos">起始位置</param>
    /// <param name="endPos">结束位置</param>
    /// <param name="target">目标Transform（可选）</param>
    public void StartBeam(Vector3 startPos, Vector3 endPos, Transform target = null)
    {
        startPosition = startPos;
        endPosition = endPos;
        targetTransform = target;
        
        // 设置初始位置
        transform.position = startPosition;
        
        // 激活光束
        isActive = true;
        elapsedTime = 0f;
        
        // 设置LineRenderer初始位置
        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, startPosition);
            lineRenderer.SetPosition(1, startPosition);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Beam started from {startPos} to {endPos}");
        }
    }
    
    /// <summary>
    /// 更新光束动画
    /// </summary>
    private void UpdateBeamAnimation()
    {
        elapsedTime += Time.deltaTime;
        float progress = elapsedTime / beamDuration;
        
        // 更新目标位置（如果目标还在移动）
        if (targetTransform != null)
        {
            endPosition = targetTransform.position;
        }
        
        // 计算光束方向
        Vector3 direction = (endPosition - startPosition).normalized;
        float totalDistance = Vector3.Distance(startPosition, endPosition);
        
        // 计算当前光束长度（从0增长到总距离）
        float currentLength = totalDistance * progress;
        Vector3 currentEndPosition = startPosition + direction * currentLength;
        
        // 更新LineRenderer
        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, startPosition);
            lineRenderer.SetPosition(1, currentEndPosition);
            
            // 更新颜色和透明度
            Color currentColor = beamColor;
            currentColor.a = CalculateBeamAlpha(progress);
            lineRenderer.startColor = currentColor;
            lineRenderer.endColor = currentColor;
            
            // 更新宽度
            float currentWidth = beamWidth * CalculateBeamWidth(progress);
            lineRenderer.startWidth = currentWidth;
            lineRenderer.endWidth = currentWidth;
        }
        
        // 检查是否完成
        if (progress >= 1f)
        {
            StopBeam();
        }
    }
    
    /// <summary>
    /// 计算光束长度
    /// </summary>
    /// <param name="progress">进度</param>
    /// <returns>当前长度</returns>
    private float CalculateBeamLength(float progress)
    {
        // 直接计算到目标位置的距离，不需要来回动画
        float totalDistance = Vector3.Distance(startPosition, endPosition);
        return totalDistance;
    }
    
    /// <summary>
    /// 计算光束透明度
    /// </summary>
    /// <param name="progress">进度</param>
    /// <returns>透明度</returns>
    private float CalculateBeamAlpha(float progress)
    {
        // 在开始和结束时淡入淡出
        float fadeIn = Mathf.Clamp01(progress / 0.1f);
        float fadeOut = Mathf.Clamp01((1f - progress) / 0.1f);
        return Mathf.Min(fadeIn, fadeOut);
    }
    
    /// <summary>
    /// 计算光束宽度
    /// </summary>
    /// <param name="progress">进度</param>
    /// <returns>宽度倍数</returns>
    private float CalculateBeamWidth(float progress)
    {
        // 在中间时宽度最大，两端较细
        float widthMultiplier = Mathf.Sin(progress * Mathf.PI);
        return Mathf.Max(0.1f, widthMultiplier); // 确保最小宽度
    }
    
    /// <summary>
    /// 停止光束效果
    /// </summary>
    public void StopBeam()
    {
        isActive = false;
        
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Beam stopped");
        }
        
        // 延迟销毁
        Destroy(gameObject, 0.1f);
    }
    
    /// <summary>
    /// 设置光束参数
    /// </summary>
    /// <param name="duration">持续时间</param>
    /// <param name="maxLen">最大长度</param>
    /// <param name="color">颜色</param>
    /// <param name="width">宽度</param>
    public void SetBeamParameters(float duration, float maxLen, Color color, float width)
    {
        beamDuration = duration;
        maxLength = maxLen;
        beamColor = color;
        beamWidth = width;
        
        if (lineRenderer != null)
        {
            lineRenderer.startColor = beamColor;
            lineRenderer.endColor = beamColor;
            lineRenderer.startWidth = beamWidth;
            lineRenderer.endWidth = beamWidth;
        }
    }
    
    /// <summary>
    /// 设置光束材质
    /// </summary>
    /// <param name="material">材质</param>
    public void SetBeamMaterial(Material material)
    {
        beamMaterial = material;
        if (lineRenderer != null)
        {
            lineRenderer.material = material;
        }
    }
    
    // 公共访问器
    public bool IsActive() => isActive;
    public float GetProgress() => elapsedTime / beamDuration;
    public Vector3 GetStartPosition() => startPosition;
    public Vector3 GetEndPosition() => endPosition;
    
    // 编辑器辅助
    private void OnDrawGizmosSelected()
    {
        if (isActive)
        {
            Gizmos.color = beamColor;
            Gizmos.DrawLine(startPosition, endPosition);
        }
    }
}
