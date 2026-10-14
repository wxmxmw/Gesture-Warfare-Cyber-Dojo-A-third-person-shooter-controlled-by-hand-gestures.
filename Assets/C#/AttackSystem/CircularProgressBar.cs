using UnityEngine;

/// <summary>
/// 圆形进度条组件
/// 用于显示标靶的剩余时间
/// 直接在3D空间中渲染，不使用Canvas
/// </summary>
public class CircularProgressBar : MonoBehaviour
{
    [Header("Progress Bar Settings")]
    [SerializeField] private float radius = 1.5f;          // 进度条半径
    [SerializeField] private float thickness = 0.3f;        // 进度条厚度（增加厚度）
    [SerializeField] private Color progressColor = Color.red; // 进度条颜色
    [SerializeField] private Color backgroundColor = Color.gray; // 背景颜色
    [SerializeField] private bool enableDebugLogs = false;   // 是否启用调试日志
    
    [Header("Animation Settings")]
    [SerializeField] private float animationSpeed = 2f;     // 动画速度
    [SerializeField] private bool usePulseEffect = true;    // 是否使用脉冲效果
    [SerializeField] private float pulseIntensity = 0.3f;    // 脉冲强度
    
    // 运行时变量
    private float currentProgress = 0f;
    private float targetProgress = 0f;
    private float maxTime = 10f;
    private float currentTime = 0f;
    private bool isActive = false;
    
    // 3D渲染组件
    private MeshRenderer progressRenderer;
    private MeshFilter progressMeshFilter;
    private Material progressMaterial;
    
    private void Awake()
    {
        // 延迟创建3D进度条，避免在Awake中创建
        StartCoroutine(DelayedCreateProgressBar());
        
        // 设置初始状态
        SetProgress(0f);
        SetActive(true); // 默认激活
    }
    
    /// <summary>
    /// 延迟创建进度条
    /// </summary>
    private System.Collections.IEnumerator DelayedCreateProgressBar()
    {
        yield return new WaitForEndOfFrame();
        Create3DProgressBar();
    }
    
    private void Update()
    {
        if (isActive)
        {
            // 更新进度
            UpdateProgress();
            
            // 更新视觉效果
            UpdateVisuals();
        }

    }
    
    /// <summary>
    /// 创建3D进度条
    /// </summary>
    private void Create3DProgressBar()
    {
        try
        {
            // 添加MeshFilter组件
            progressMeshFilter = gameObject.AddComponent<MeshFilter>();
            
            // 添加MeshRenderer组件
            progressRenderer = gameObject.AddComponent<MeshRenderer>();
            
            // 创建圆形进度条网格
            if (progressMeshFilter != null)
            {
                progressMeshFilter.mesh = CreateCircleProgressMesh();
            }
            
            // 创建材质
            Shader standardShader = Shader.Find("Standard");
            if (standardShader != null)
            {
                progressMaterial = new Material(standardShader);
                progressMaterial.color = progressColor;
                progressMaterial.SetFloat("_Mode", 3); // 设置为透明模式
                progressMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                progressMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                progressMaterial.SetInt("_ZWrite", 0);
                progressMaterial.DisableKeyword("_ALPHATEST_ON");
                progressMaterial.EnableKeyword("_ALPHABLEND_ON");
                progressMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                progressMaterial.renderQueue = 3000;
                
                if (progressRenderer != null)
                {
                    progressRenderer.material = progressMaterial;
                }
            }
            else
            {
                Debug.LogError($"[{gameObject.name}] Standard shader not found!");
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"[{gameObject.name}] Created 3D circular progress bar");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[{gameObject.name}] Error creating 3D progress bar: {e.Message}");
        }
    }
    
    /// <summary>
    /// 创建圆形进度条网格
    /// </summary>
    /// <returns>圆形进度条网格</returns>
    private Mesh CreateCircleProgressMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "CircleProgressMesh";
        
        int segments = 64; // 圆形分段数
        int verticesCount = (segments + 1) * 2; // 内外圆顶点数，+1 因为需要闭合
        
        Vector3[] vertices = new Vector3[verticesCount];
        Vector2[] uvs = new Vector2[verticesCount];
        int[] triangles = new int[segments * 6];
        
        float angleStep = 360f / segments;
        
        // 创建内外圆顶点
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            
            // 外圆顶点
            vertices[i * 2] = new Vector3(cos * radius, sin * radius, 0);
            uvs[i * 2] = new Vector2(0.5f + cos * 0.5f, 0.5f + sin * 0.5f);
            
            // 内圆顶点
            vertices[i * 2 + 1] = new Vector3(cos * (radius - thickness), sin * (radius - thickness), 0);
            uvs[i * 2 + 1] = new Vector2(0.5f + cos * 0.4f, 0.5f + sin * 0.4f);
        }
        
        // 创建三角形
        for (int i = 0; i < segments; i++)
        {
            int baseIndex = i * 6;
            int vertexIndex = i * 2;
            
            // 第一个三角形
            triangles[baseIndex] = vertexIndex;
            triangles[baseIndex + 1] = vertexIndex + 2;
            triangles[baseIndex + 2] = vertexIndex + 1;
            
            // 第二个三角形
            triangles[baseIndex + 3] = vertexIndex + 1;
            triangles[baseIndex + 4] = vertexIndex + 2;
            triangles[baseIndex + 5] = vertexIndex + 3;
        }
        
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        
        return mesh;
    }
    
    /// <summary>
    /// 初始化进度条
    /// </summary>
    /// <param name="maxTime">最大时间</param>
    /// <param name="currentTime">当前时间</param>
    public void Initialize(float maxTime, float currentTime = 0f)
    {
        this.maxTime = maxTime;
        this.currentTime = currentTime;
        this.targetProgress = Mathf.Clamp01(currentTime / maxTime);
        this.currentProgress = targetProgress;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Progress bar initialized: {currentTime}/{maxTime}");
        }
    }
    
    /// <summary>
    /// 设置进度条激活状态
    /// </summary>
    /// <param name="active">是否激活</param>
    public void SetActive(bool active)
    {
        isActive = active;
        gameObject.SetActive(active);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Progress bar {(active ? "activated" : "deactivated")}");
        }
    }
    
    /// <summary>
    /// 设置当前时间
    /// </summary>
    /// <param name="time">当前时间</param>
    public void SetCurrentTime(float time)
    {
        currentTime = time;
        // 反转进度：时间越多，进度条越少
        targetProgress = Mathf.Clamp01(1f - (time / maxTime));
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Time updated: {time}/{maxTime}, progress: {targetProgress:F2}");
        }
    }
    
    /// <summary>
    /// 测试进度条（用于调试）
    /// </summary>
    /// <param name="progress">测试进度值</param>
    public void TestProgress(float progress)
    {
        SetProgress(progress);
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Testing progress: {progress:F2}");
        }
    }
    
    /// <summary>
    /// 设置进度（0-1）
    /// </summary>
    /// <param name="progress">进度值</param>
    public void SetProgress(float progress)
    {
        // 反转进度：从1到0
        targetProgress = Mathf.Clamp01(1f - progress);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Progress set to: {progress:F2} (reversed to: {targetProgress:F2})");
        }
    }
    
    /// <summary>
    /// 更新进度
    /// </summary>
    private void UpdateProgress()
    {
        // 平滑过渡到目标进度
        currentProgress = Mathf.Lerp(currentProgress, targetProgress, Time.deltaTime * animationSpeed);
        
        // 更新3D进度条显示
        Update3DProgressBar();
    }
    
    /// <summary>
    /// 更新3D进度条显示
    /// </summary>
    private void Update3DProgressBar()
    {
        if (progressMeshFilter == null) return;
        
        // 重新创建网格以显示当前进度
        Mesh mesh = CreateProgressMesh(currentProgress);
        progressMeshFilter.mesh = mesh;
    }
    
    /// <summary>
    /// 创建带进度的圆形网格
    /// </summary>
    /// <param name="progress">进度值 (0-1)</param>
    /// <returns>进度网格</returns>
    private Mesh CreateProgressMesh(float progress)
    {
        Mesh mesh = new Mesh();
        mesh.name = "ProgressMesh";
        
        int segments = 64;
        float progressAngle = progress * 360f; // 进度角度
        int activeSegments = Mathf.RoundToInt(segments * progress);
        
        if (activeSegments <= 0)
        {
            // 没有进度时返回空网格
            mesh.vertices = new Vector3[0];
            mesh.triangles = new int[0];
            return mesh;
        }
        
        // 确保至少有1个段
        activeSegments = Mathf.Max(1, activeSegments);
        
        int verticesCount = (activeSegments + 1) * 2; // +1 因为需要闭合
        Vector3[] vertices = new Vector3[verticesCount];
        Vector2[] uvs = new Vector2[verticesCount];
        int[] triangles = new int[activeSegments * 6];
        
        float angleStep = progressAngle / activeSegments;
        
        // 创建进度圆环顶点（从顶部开始，顺时针）
        for (int i = 0; i <= activeSegments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad - Mathf.PI / 2f; // 从顶部开始
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            
            // 外圆顶点
            vertices[i * 2] = new Vector3(cos * radius, sin * radius, 0);
            uvs[i * 2] = new Vector2(0.5f + cos * 0.5f, 0.5f + sin * 0.5f);
            
            // 内圆顶点
            vertices[i * 2 + 1] = new Vector3(cos * (radius - thickness), sin * (radius - thickness), 0);
            uvs[i * 2 + 1] = new Vector2(0.5f + cos * 0.4f, 0.5f + sin * 0.4f);
        }
        
        // 创建三角形
        for (int i = 0; i < activeSegments; i++)
        {
            int baseIndex = i * 6;
            int vertexIndex = i * 2;
            
            // 第一个三角形
            triangles[baseIndex] = vertexIndex;
            triangles[baseIndex + 1] = vertexIndex + 2;
            triangles[baseIndex + 2] = vertexIndex + 1;
            
            // 第二个三角形
            triangles[baseIndex + 3] = vertexIndex + 1;
            triangles[baseIndex + 4] = vertexIndex + 2;
            triangles[baseIndex + 5] = vertexIndex + 3;
        }
        
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        
        return mesh;
    }
    
    /// <summary>
    /// 更新视觉效果
    /// </summary>
    private void UpdateVisuals()
    {
        if (progressMaterial == null) return;
        
        // 根据进度改变颜色（反转逻辑）
        float progressRatio = currentProgress;
        Color currentColor;
        
        // 当进度条接近0时显示警告颜色
        if (progressRatio < 0.2f)
        {
            // 显示红色警告
            currentColor = Color.Lerp(Color.red, progressColor, progressRatio / 0.2f);
        }
        else
        {
            // 正常颜色渐变
            currentColor = Color.Lerp(backgroundColor, progressColor, progressRatio);
        }
        
        progressMaterial.color = currentColor;
        
        // 移除脉冲效果，避免影响AttackTarget物体大小
        // 脉冲效果可以通过材质属性或其他方式实现，而不是修改transform
    }
    
    /// <summary>
    /// 获取当前进度
    /// </summary>
    /// <returns>当前进度值</returns>
    public float GetCurrentProgress() => currentProgress;
    
    /// <summary>
    /// 获取剩余时间
    /// </summary>
    /// <returns>剩余时间</returns>
    public float GetRemainingTime() => maxTime - currentTime;
    
    /// <summary>
    /// 获取总时间
    /// </summary>
    /// <returns>总时间</returns>
    public float GetTotalTime() => maxTime;
    
    /// <summary>
    /// 设置进度条颜色
    /// </summary>
    /// <param name="progressColor">进度颜色</param>
    /// <param name="backgroundColor">背景颜色</param>
    public void SetColors(Color progressColor, Color backgroundColor)
    {
        this.progressColor = progressColor;
        this.backgroundColor = backgroundColor;
        
        if (progressMaterial != null)
        {
            progressMaterial.color = progressColor;
        }
    }
    
    /// <summary>
    /// 设置进度条大小
    /// </summary>
    /// <param name="radius">半径</param>
    /// <param name="thickness">厚度</param>
    public void SetSize(float radius, float thickness)
    {
        this.radius = radius;
        this.thickness = thickness;
        
        // 重新创建网格
        if (progressMeshFilter != null)
        {
            progressMeshFilter.mesh = CreateCircleProgressMesh();
        }
    }
    
    // 编辑器辅助
    private void OnDrawGizmosSelected()
    {
        // 显示进度条范围
        Gizmos.color = progressColor;
        Gizmos.DrawWireSphere(transform.position, radius);
        
        // 显示进度条厚度
        Gizmos.color = backgroundColor;
        Gizmos.DrawWireSphere(transform.position, radius - thickness);
    }
}