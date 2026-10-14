using UnityEngine;

/// <summary>
/// 攻击标靶组件
/// 可消除攻击中的标靶，玩家需要按顺序击中来取消攻击
/// </summary>
public class AttackTarget : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private float hitRadius = 0.5f;              // 击中半径
    [SerializeField] private LayerMask bulletLayerMask = -1;      // 子弹层级掩码
    [SerializeField] private bool enableDebugLogs = true;         // 是否启用调试日志
    int hittimes = 0; //被击中次数
    [SerializeField] private int hituptimes = 3; // 需要的被击中次数

    [Header("Visual Settings")]
    [SerializeField] private float pulseSpeed = 2f;              // 脉冲速度
    [SerializeField] private float pulseIntensity = 0.3f;        // 脉冲强度
    [SerializeField] private Color inactiveColor = Color.gray;    // 未激活颜色
    [SerializeField] private Color startColor = Color.white;     // 开始颜色（剩余时间最多时）
    [SerializeField] private Color endColor = Color.red;          // 结束颜色（剩余时间为0时）
    [SerializeField] private Color hitColor = Color.green;       // 击中颜色
    
    [Header("Progress Bar Settings")]
    [SerializeField] private bool showProgressBar = true;         // 是否显示进度条
    [SerializeField] private float progressBarRadius = 1.5f;      // 进度条半径
    [SerializeField] private Color progressBarColor = Color.red;  // 进度条颜色
    
    [Header("Connection Line Settings")]
    [SerializeField] private bool showConnectionLine = true;     // 是否显示连接线
    [SerializeField] private float lineWidth = 0.1f;             // 线条宽度
    [SerializeField] private Color lineColor = Color.yellow;     // 线条颜色
    [SerializeField] private float flashSpeed = 2f;             // 闪烁速度
    [SerializeField] private float flashIntensity = 0.5f;        // 闪烁强度
    [SerializeField] private Material lineMaterial;             // 线条材质
    
    [Header("Hit Effects Settings")]
    [SerializeField] private GameObject hitParticlePrefab;      // 击中粒子效果预制体
    [SerializeField] private float shakeIntensity = 0.3f;       // 震动强度
    [SerializeField] private float shakeDuration = 0.2f;        // 震动持续时间
    [SerializeField] private int shakeFrequency = 10;           // 震动频率
    
    [Header("Animation Settings")]
    [SerializeField] private float appearAnimationDuration = 0.5f;     // 出现动画持续时间
    [SerializeField] private float disappearAnimationDuration = 0.5f;  // 消失动画持续时间
    [SerializeField] private AnimationCurve appearCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 出现动画曲线
    [SerializeField] private AnimationCurve disappearCurve = AnimationCurve.EaseInOut(0, 1, 1, 0); // 消失动画曲线
    [SerializeField] private Vector3 appearStartScale = Vector3.zero;   // 出现动画起始缩放
    [SerializeField] private float appearRotationSpeed = 360f;          // 出现时旋转速度
    [SerializeField] private GameObject disappearEffectPrefab;          // 消失特效预制体
    [SerializeField] private bool enableAppearAnimation = true;         // 是否启用出现动画
    [SerializeField] private bool enableDisappearAnimation = true;      // 是否启用消失动画

    // 状态变量
    private CancelableAttackProjectile ownerAttack;
    private int targetIndex;
    private bool isActive = false;
    private bool isHit = false;
    private Color originalColor;
    private SpriteRenderer targetRenderer;
    private Collider2D targetCollider2D;
    private CircularProgressBar progressBar;
    
    // 动画相关变量
    private Vector3 originalScale;
    private bool isPlayingAppearAnimation = false;
    private bool isPlayingDisappearAnimation = false;
    private Coroutine appearAnimationCoroutine;
    private Coroutine disappearAnimationCoroutine;
    
    // 虚线段相关
    private LineRenderer connectionLine;
    private Transform playerTransform;
    private GameObject lineObject;
    
    // 震动效果相关
    private Vector3 originalPosition;
    private bool isShaking = false;
    private Coroutine shakeCoroutine;
    
    // 事件
    public System.Action<AttackTarget, int> OnTargetHit;
    
    private void Awake()
    {
        // 获取组件
        targetRenderer = GetComponent<SpriteRenderer>();
        targetCollider2D = GetComponent<Collider2D>();
        
        // 如果没有2D碰撞体，添加一个
        if (targetCollider2D == null)
        {
            CircleCollider2D circleCollider = gameObject.AddComponent<CircleCollider2D>();
            circleCollider.radius = hitRadius;
            circleCollider.isTrigger = true;
            targetCollider2D = circleCollider;
        }
        
        // 确保碰撞体是触发器
        targetCollider2D.isTrigger = true;
        
        // 设置标签
        gameObject.tag = "AttackTarget";
        
        // 早期初始化原始缩放和位置
        originalPosition = transform.position;
        originalScale = transform.localScale;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Awake - Original scale set to: {originalScale}");
        }
    }
    
    private void Start()
    {
        // 保存原始颜色
        if (targetRenderer != null)
        {
            originalColor = targetRenderer.color;
        }
        
        // 创建进度条
        if (showProgressBar)
        {
            CreateProgressBar();
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Start - Original color set, scale is: {originalScale}");
        }
        
        // 初始状态为未激活
        //SetActive(false);
        //Debug.Log(2);
    }
    
    private void Update()
    {
        // 如果激活且未被击中，更新颜色和脉冲效果
        if (isActive && !isHit && targetRenderer != null)
        {
            UpdateColorBasedOnTime();
            UpdatePulseEffect();
        }
        
        // 更新进度条
        if (progressBar != null && isActive && !isHit)
        {
            UpdateProgressBar();
        }
        
        // 更新虚线段
        if (connectionLine != null && isActive && !isHit)
        {
            UpdateConnectionLine();
        }
    }
    
    /// <summary>
    /// 初始化标靶
    /// </summary>
    /// <param name="attack">所属攻击</param>
    /// <param name="index">标靶索引</param>
    /// <param name="color">标靶颜色</param>
    public void Initialize(CancelableAttackProjectile attack, int index, Color color)
    {
        ownerAttack = attack;
        targetIndex = index;
        
        // 设置颜色
        if (targetRenderer != null)
        {
            Material material = targetRenderer.material;
            targetRenderer.color = color;
            originalColor = color;
            // 不再直接设置activeColor，而是使用动态颜色
            targetRenderer.material = material;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Target initialized: index {index}");
        }
    }
    
    /// <summary>
    /// 设置标靶激活状态
    /// </summary>
    /// <param name="active">是否激活</param>
    public void SetActive(bool active)
    {
        if (active)
        {
            // 激活标靶
            ActivateTarget();
        }
        else
        {
            // 停用标靶
            DeactivateTarget();
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Target {(active ? "activated" : "deactivated")}");
        }
    }
    
    /// <summary>
    /// 激活标靶（带出现动画）
    /// </summary>
    private void ActivateTarget()
    {
        isActive = true;
        
        // 停止之前的动画
        if (appearAnimationCoroutine != null)
        {
            StopCoroutine(appearAnimationCoroutine);
        }
        
        // 设置碰撞体激活状态（但在动画播放期间暂时禁用）
        if (targetCollider2D != null)
        {
            targetCollider2D.enabled = false; // 动画播放完成后再启用
        }
        
        // 设置进度条激活状态
        if (progressBar != null)
        {
            progressBar.SetActive(true);
        }
        
        // 如果需要显示连接线，创建连接线
        if (showConnectionLine)
        {
            CreateConnectionLine();
        }
        
        // 播放出现动画
        if (enableAppearAnimation)
        {
            appearAnimationCoroutine = StartCoroutine(PlayAppearAnimation());
        }
        else
        {
            // 如果不播放动画，直接设置为正常状态
            CompleteAppearAnimation();
        }
    }
    
    /// <summary>
    /// 停用标靶
    /// </summary>
    private void DeactivateTarget()
    {
        isActive = false;
        
        // 停止所有动画
        if (appearAnimationCoroutine != null)
        {
            StopCoroutine(appearAnimationCoroutine);
            appearAnimationCoroutine = null;
        }
        
        if (targetRenderer != null)
        {
            targetRenderer.color = inactiveColor;
        }
        
        // 设置碰撞体激活状态
        if (targetCollider2D != null)
        {
            targetCollider2D.enabled = false;
        }
        
        // 设置进度条激活状态
        if (progressBar != null)
        {
            progressBar.SetActive(false);
        }
        
        // 设置虚线段激活状态
        if (connectionLine != null)
        {
            connectionLine.enabled = false;
        }
        
        // 销毁连接线
        DestroyConnectionLine();
        
        // 重置缩放
        transform.localScale = originalScale;
    }
    
    /// <summary>
    /// 标靶被击中
    /// </summary>
    public void OnHit()
    {
        hittimes++;
        
        // 如果未达到击中次数上限，播放部分击中效果
        if (hittimes < hituptimes)
        {
            PlayPartialHitEffect();
            return;
        }

        if (isHit) return;
        
        isHit = true;
        isActive = false;
        
        // 禁用碰撞体
        if (targetCollider2D != null)
        {
            targetCollider2D.enabled = false;
        }
        
        // 隐藏连接线
        if (connectionLine != null)
        {
            connectionLine.enabled = false;
        }
        
        // 触发击中事件
        OnTargetHit?.Invoke(this, targetIndex);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Target hit!");
        }
        
        // 播放消失动画
        if (enableDisappearAnimation)
        {
            disappearAnimationCoroutine = StartCoroutine(PlayDisappearAnimation());
        }
        else
        {
            // 如果不播放动画，直接播放击中效果
            StartCoroutine(HitEffect());
        }
    }
    
    /// <summary>
    /// 根据剩余时间更新颜色
    /// </summary>
    private void UpdateColorBasedOnTime()
    {
        if (ownerAttack == null) return;
        
        float remainingTime = ownerAttack.GetRemainingTime();
        float totalTime = ownerAttack.GetAttackDuration();
        
        if (totalTime <= 0) return;
        
        // 计算剩余时间比例 (1.0 = 剩余时间最多, 0.0 = 剩余时间为0)
        float timeRatio = remainingTime / totalTime;
        
        // 在开始颜色和结束颜色之间插值
        Color currentColor = Color.Lerp(endColor, startColor, timeRatio);
        
        // 应用脉冲效果到基础颜色
        float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseIntensity + 1f;
        currentColor = currentColor * pulse;
        currentColor.a = 1f; // 保持透明度为1
        
        targetRenderer.color = currentColor;
    }
    
    /// <summary>
    /// 更新脉冲效果（现在只用于未激活状态）
    /// </summary>
    private void UpdatePulseEffect()
    {
        // 这个方法现在主要用于未激活状态的脉冲效果
        // 激活状态的颜色变化由UpdateColorBasedOnTime处理
    }
    
    /// <summary>
    /// 创建进度条
    /// </summary>
    private void CreateProgressBar()
    {
        if (progressBar != null) return;
        
        // 创建进度条对象
        GameObject progressBarObj = new GameObject("ProgressBar");
        progressBarObj.transform.SetParent(transform);
        progressBarObj.transform.localPosition = Vector3.zero;
        
        // 添加进度条组件
        progressBar = progressBarObj.AddComponent<CircularProgressBar>();
        
        // 设置进度条参数
        progressBar.SetSize(progressBarRadius,10f);
        progressBar.SetColors(progressBarColor, Color.gray);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Progress bar created");
        }
    }
    
    /// <summary>
    /// 更新进度条
    /// </summary>
    private void UpdateProgressBar()
    {
        if (progressBar == null || ownerAttack == null) return;
        
        // 获取剩余时间
        float remainingTime = ownerAttack.GetRemainingTime();
        float totalTime = ownerAttack.GetAttackDuration();
        
        if (totalTime > 0)
        {
            // 计算当前时间（总时间 - 剩余时间）
            float currentTime = totalTime - remainingTime;
            progressBar.SetCurrentTime(currentTime);
        }
    }
    
    /// <summary>
    /// 击中效果协程
    /// </summary>
    /// <returns></returns>
    private System.Collections.IEnumerator HitEffect()
    {
        // 简单的缩放效果
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * 1.2f;
        
        float duration = 0.2f;
        float elapsedTime = 0f;
        
        // 放大
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            
            transform.localScale = Vector3.Lerp(originalScale, targetScale, progress);
            
            yield return null;
        }
        
        // 缩小回原大小
        elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            
            transform.localScale = Vector3.Lerp(targetScale, originalScale, progress);
            
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
    
    /// <summary>
    /// 触发器进入事件（检测子弹或玩家攻击）
    /// </summary>
    /// <param name="other">其他碰撞体</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive || isHit) return;
        
        // 检查是否是子弹或玩家攻击
        if (IsValidHit(other))
        {
            OnHit();
        }
    }
    
    /// <summary>
    /// 检查是否是有效击中
    /// </summary>
    /// <param name="other">碰撞体</param>
    /// <returns>是否有效</returns>
    private bool IsValidHit(Collider2D other)
    {
        // 检查层级掩码
        int layer = other.gameObject.layer;
        if ((bulletLayerMask.value & (1 << layer)) == 0)
        {
            return false;
        }
        
        // 检查标签（可以检查子弹、玩家等）
        return other.CompareTag("PlayerAttack");
    }
    
    /// <summary>
    /// 3D触发器进入事件（兼容3D碰撞体）
    /// </summary>
    /// <param name="other">其他碰撞体</param>
    private void OnTriggerEnter(Collider other)
    {
        if (!isActive || isHit) return;
        
        // 简单检查标签
        if (other.CompareTag("PlayerAttack"))
        {
            OnHit();
        }
    }
    
    // 公共访问器
    public bool IsActive() => isActive;
    public bool IsHit() => isHit;
    public int GetTargetIndex() => targetIndex;
    public CancelableAttackProjectile GetOwnerAttack() => ownerAttack;
    
    // 公共设置方法
    public void SetHitRadius(float radius)
    {
        hitRadius = radius;
        if (targetCollider2D is CircleCollider2D circleCollider)
        {
            circleCollider.radius = radius;
        }
    }
    
    public void SetColors(Color inactive, Color start, Color end, Color hit)
    {
        inactiveColor = inactive;
        startColor = start;
        endColor = end;
        hitColor = hit;
    }
    
    /// <summary>
    /// 设置动态颜色范围
    /// </summary>
    /// <param name="start">开始颜色（剩余时间最多时）</param>
    /// <param name="end">结束颜色（剩余时间为0时）</param>
    public void SetDynamicColors(Color start, Color end)
    {
        startColor = start;
        endColor = end;
    }
    
    /// <summary>
    /// 创建连接线
    /// </summary>
    private void CreateConnectionLine()
    {
        // 如果连接线已存在，先销毁
        if (connectionLine != null)
        {
            DestroyConnectionLine();
        }
        
        // 查找玩家对象
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning($"[{gameObject.name}] Player not found for connection line");
                }
                return;
            }
        }
        
        // 创建连接线对象
        lineObject = new GameObject("ConnectionLine");
        lineObject.transform.SetParent(transform);
        
        // 添加LineRenderer组件
        connectionLine = lineObject.AddComponent<LineRenderer>();
        
        // 配置LineRenderer
        connectionLine.material = lineMaterial != null ? lineMaterial : CreateDefaultLineMaterial();
        connectionLine.material.color = lineColor;
        connectionLine.startWidth = lineWidth;
        connectionLine.endWidth = lineWidth;
        connectionLine.positionCount = 2;
        connectionLine.useWorldSpace = true;
        connectionLine.sortingOrder = -1; // 确保线条在背景上显示
        
        // 设置虚线效果
        connectionLine.material.mainTextureScale = new Vector2(1, 1);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Connection line created");
        }
    }
    
    /// <summary>
    /// 更新连接线
    /// </summary>
    private void UpdateConnectionLine()
    {
        if (connectionLine == null || playerTransform == null) return;
        
        // 更新线条位置
        connectionLine.SetPosition(0, playerTransform.position);
        connectionLine.SetPosition(1, transform.position);
        
        // 更新闪烁效果
        float flash = Mathf.Sin(Time.time * flashSpeed) * flashIntensity + (1f - flashIntensity);
        Color currentColor = lineColor;
        currentColor.a = flash;
        connectionLine.material.color = currentColor;
    }
    
    /// <summary>
    /// 销毁连接线
    /// </summary>
    private void DestroyConnectionLine()
    {
        if (lineObject != null)
        {
            DestroyImmediate(lineObject);
            lineObject = null;
            connectionLine = null;
        }
    }
    
    /// <summary>
    /// 创建默认线条材质
    /// </summary>
    /// <returns>默认材质</returns>
    private Material CreateDefaultLineMaterial()
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = lineColor;
        return mat;
    }
    
    /// <summary>
    /// 设置玩家引用
    /// </summary>
    /// <param name="player">玩家Transform</param>
    public void SetPlayerReference(Transform player)
    {
        playerTransform = player;
    }
    
    /// <summary>
    /// 设置连接线参数
    /// </summary>
    /// <param name="width">线条宽度</param>
    /// <param name="color">线条颜色</param>
    /// <param name="speed">闪烁速度</param>
    /// <param name="intensity">闪烁强度</param>
    public void SetConnectionLineSettings(float width, Color color, float speed, float intensity)
    {
        lineWidth = width;
        lineColor = color;
        flashSpeed = speed;
        flashIntensity = intensity;
        
        // 如果连接线已存在，更新其设置
        if (connectionLine != null)
        {
            connectionLine.startWidth = lineWidth;
            connectionLine.endWidth = lineWidth;
            connectionLine.material.color = lineColor;
        }
    }
    
    /// <summary>
    /// 设置是否显示连接线
    /// </summary>
    /// <param name="show">是否显示</param>
    public void SetConnectionLineVisible(bool show)
    {
        showConnectionLine = show;
        
        if (!show && connectionLine != null)
        {
            connectionLine.enabled = false;
        }
        else if (show && isActive && connectionLine != null)
        {
            connectionLine.enabled = true;
        }
    }
    
    /// <summary>
    /// 播放部分击中效果（未达到击中次数上限时）
    /// </summary>
    private void PlayPartialHitEffect()
    {
        // 播放粒子爆炸效果
        PlayHitParticleEffect();
        
        // 开始震动效果
        StartShakeEffect();
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Partial hit! ({hittimes}/{hituptimes})");
        }
    }
    
    /// <summary>
    /// 播放击中粒子效果
    /// </summary>
    private void PlayHitParticleEffect()
    {
        if (hitParticlePrefab != null)
        {
            // 在标靶位置实例化粒子效果
            GameObject particleEffect = Instantiate(hitParticlePrefab, transform.position, Quaternion.identity);
            
            // 设置粒子效果的父对象为标靶，这样会跟随标靶移动
            particleEffect.transform.SetParent(transform);
            
            // 获取粒子系统组件并播放
            ParticleSystem particleSystem = particleEffect.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                particleSystem.Play();
                
                // 粒子播放完成后销毁对象
                StartCoroutine(DestroyParticleAfterPlay(particleEffect, particleSystem));
            }
            else
            {
                // 如果没有粒子系统组件，延迟销毁
                Destroy(particleEffect, 2f);
            }
        }
    }
    
    /// <summary>
    /// 粒子播放完成后销毁
    /// </summary>
    /// <param name="particleObject">粒子对象</param>
    /// <param name="particleSystem">粒子系统</param>
    /// <returns></returns>
    private System.Collections.IEnumerator DestroyParticleAfterPlay(GameObject particleObject, ParticleSystem particleSystem)
    {
        // 等待粒子系统播放完成
        while (particleSystem.isPlaying)
        {
            yield return null;
        }
        
        // 销毁粒子对象
        if (particleObject != null)
        {
            Destroy(particleObject);
        }
    }
    
    /// <summary>
    /// 开始震动效果
    /// </summary>
    private void StartShakeEffect()
    {
        // 如果已经在震动，停止之前的震动
        if (isShaking && shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }
        
        // 开始新的震动
        shakeCoroutine = StartCoroutine(ShakeEffect());
    }
    
    /// <summary>
    /// 震动效果协程
    /// </summary>
    /// <returns></returns>
    private System.Collections.IEnumerator ShakeEffect()
    {
        isShaking = true;
        Vector3 startPosition = transform.position;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < shakeDuration)
        {
            elapsedTime += Time.deltaTime;
            
            // 计算震动偏移
            float shakeAmount = shakeIntensity * (1f - elapsedTime / shakeDuration);
            Vector3 randomOffset = new Vector3(
                Random.Range(-shakeAmount, shakeAmount),
                Random.Range(-shakeAmount, shakeAmount),
                0f
            );
            
            // 应用震动
            transform.position = startPosition + randomOffset;
            
            yield return null;
        }
        
        // 震动结束，回到原始位置
        transform.position = originalPosition;
        isShaking = false;
        shakeCoroutine = null;
    }
    
    /// <summary>
    /// 设置击中粒子效果预制体
    /// </summary>
    /// <param name="particlePrefab">粒子预制体</param>
    public void SetHitParticlePrefab(GameObject particlePrefab)
    {
        hitParticlePrefab = particlePrefab;
    }
    
    /// <summary>
    /// 设置震动效果参数
    /// </summary>
    /// <param name="intensity">震动强度</param>
    /// <param name="duration">震动持续时间</param>
    public void SetShakeSettings(float intensity, float duration)
    {
        shakeIntensity = intensity;
        shakeDuration = duration;
    }
    
    /// <summary>
    /// 获取当前击中次数
    /// </summary>
    /// <returns>当前击中次数</returns>
    public int GetHitTimes()
    {
        return hittimes;
    }
    
    /// <summary>
    /// 获取需要的击中次数
    /// </summary>
    /// <returns>需要的击中次数</returns>
    public int GetRequiredHitTimes()
    {
        return hituptimes;
    }
    
    /// <summary>
    /// 设置需要的击中次数
    /// </summary>
    /// <param name="requiredHits">需要的击中次数</param>
    public void SetRequiredHitTimes(int requiredHits)
    {
        hituptimes = requiredHits;
    }
    
    /// <summary>
    /// 播放出现动画
    /// </summary>
    /// <returns></returns>
    private System.Collections.IEnumerator PlayAppearAnimation()
    {
        isPlayingAppearAnimation = true;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Starting appear animation - StartScale: {appearStartScale}, TargetScale: {originalScale}");
        }
        
        // 设置初始状态
        transform.localScale = appearStartScale;
        if (targetRenderer != null)
        {
            Color initialColor = originalColor;
            initialColor.a = 0f; // 开始时透明
            targetRenderer.color = initialColor;
        }
        
        float elapsedTime = 0f;
        Vector3 targetScale = originalScale;
        
        // 安全检查：如果目标缩放为零，使用默认值
        if (targetScale == Vector3.zero)
        {
            targetScale = Vector3.one;
            if (enableDebugLogs)
            {
                Debug.LogWarning($"[{gameObject.name}] Target scale was zero, using Vector3.one as fallback");
            }
        }
        
        // 播放出现动画
        while (elapsedTime < appearAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / appearAnimationDuration;
            
            // 使用动画曲线计算进度
            float curveProgress = appearCurve.Evaluate(progress);
            
            // 缩放动画
            transform.localScale = Vector3.Lerp(appearStartScale, targetScale, curveProgress);
            
            // 旋转动画
            if (appearRotationSpeed > 0)
            {
                float rotationAmount = appearRotationSpeed * Time.deltaTime;
                transform.Rotate(0, 0, rotationAmount);
            }
            
            // 透明度动画
            if (targetRenderer != null)
            {
                Color currentColor = originalColor;
                currentColor.a = Mathf.Lerp(0f, 1f, curveProgress);
                targetRenderer.color = currentColor;
            }
            
            yield return null;
        }
        
        // 确保最终状态正确
        transform.localScale = targetScale;
        if (targetRenderer != null)
        {
            targetRenderer.color = originalColor;
        }
        
        // 完成出现动画
        CompleteAppearAnimation();
        
        isPlayingAppearAnimation = false;
        appearAnimationCoroutine = null;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Appear animation completed");
        }
    }
    
    /// <summary>
    /// 完成出现动画后的设置
    /// </summary>
    private void CompleteAppearAnimation()
    {
        // 确保缩放和颜色正确
        transform.localScale = originalScale;
        if (targetRenderer != null)
        {
            targetRenderer.color = originalColor;
        }
        
        // 启用碰撞体
        if (targetCollider2D != null)
        {
            targetCollider2D.enabled = true;
        }
        
        // 启用连接线
        if (connectionLine != null)
        {
            connectionLine.enabled = true;
        }
    }
    
    /// <summary>
    /// 播放消失动画（爆炸效果）
    /// </summary>
    /// <returns></returns>
    private System.Collections.IEnumerator PlayDisappearAnimation()
    {
        isPlayingDisappearAnimation = true;
        
        // 变为击中颜色
        if (targetRenderer != null)
        {
            targetRenderer.color = hitColor;
        }
        
        // 创建消失特效
        CreateDisappearEffect();
        
        float elapsedTime = 0f;
        Vector3 startScale = transform.localScale;
        Vector3 explosionScale = startScale * 2f; // 爆炸时放大2倍
        
        // 第一阶段：快速放大（爆炸效果）
        float explosionDuration = disappearAnimationDuration * 0.3f; // 30%的时间用于爆炸
        while (elapsedTime < explosionDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / explosionDuration;
            
            // 快速放大
            transform.localScale = Vector3.Lerp(startScale, explosionScale, progress);
            
            // 颜色闪烁效果
            if (targetRenderer != null)
            {
                float flash = Mathf.Sin(progress * Mathf.PI * 6f) * 0.5f + 0.5f; // 快速闪烁
                Color flashColor = Color.Lerp(hitColor, Color.white, flash);
                targetRenderer.color = flashColor;
            }
            
            yield return null;
        }
        
        // 第二阶段：缩小并淡出
        float fadeOutDuration = disappearAnimationDuration * 0.7f; // 70%的时间用于淡出
        elapsedTime = 0f;
        
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeOutDuration;
            
            // 使用动画曲线计算进度
            float curveProgress = disappearCurve.Evaluate(progress);
            
            // 缩小动画
            transform.localScale = Vector3.Lerp(explosionScale, Vector3.zero, curveProgress);
            
            // 透明度动画
            if (targetRenderer != null)
            {
                Color currentColor = hitColor;
                currentColor.a = Mathf.Lerp(1f, 0f, curveProgress);
                targetRenderer.color = currentColor;
            }
            
            // 旋转动画
            float rotationAmount = 720f * Time.deltaTime; // 快速旋转
            transform.Rotate(0, 0, rotationAmount);
            
            yield return null;
        }
        
        // 确保最终状态
        transform.localScale = Vector3.zero;
        if (targetRenderer != null)
        {
            Color finalColor = hitColor;
            finalColor.a = 0f;
            targetRenderer.color = finalColor;
        }
        
        isPlayingDisappearAnimation = false;
        disappearAnimationCoroutine = null;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Disappear animation completed");
        }
        
        // 播放原有的击中效果（如果需要）
        // StartCoroutine(HitEffect());
    }
    
    /// <summary>
    /// 创建消失特效
    /// </summary>
    private void CreateDisappearEffect()
    {
        if (disappearEffectPrefab != null)
        {
            // 使用预制体创建特效
            GameObject effect = Instantiate(disappearEffectPrefab, transform.position, Quaternion.identity);
            
            // 获取粒子系统并播放
            ParticleSystem particleSystem = effect.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                particleSystem.Play();
                
                // 粒子播放完成后销毁
                StartCoroutine(DestroyParticleAfterPlay(effect, particleSystem));
            }
            else
            {
                // 如果没有粒子系统，延迟销毁
                Destroy(effect, 2f);
            }
        }
        else
        {
            // 创建默认的爆炸特效
            StartCoroutine(CreateDefaultDisappearEffect());
        }
    }
    
    /// <summary>
    /// 创建默认消失特效
    /// </summary>
    /// <returns></returns>
    private System.Collections.IEnumerator CreateDefaultDisappearEffect()
    {
        // 创建多个小球模拟爆炸碎片
        int fragmentCount = 8;
        for (int i = 0; i < fragmentCount; i++)
        {
            GameObject fragment = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fragment.name = "DisappearFragment";
            fragment.transform.position = transform.position;
            fragment.transform.localScale = Vector3.one * 0.2f;
            
            // 移除碰撞体
            Collider fragmentCollider = fragment.GetComponent<Collider>();
            if (fragmentCollider != null)
            {
                Destroy(fragmentCollider);
            }
            
            // 设置颜色
            Renderer fragmentRenderer = fragment.GetComponent<Renderer>();
            if (fragmentRenderer != null)
            {
                fragmentRenderer.material.color = hitColor;
            }
            
            // 计算随机方向
            float angle = (360f / fragmentCount) * i + Random.Range(-30f, 30f);
            Vector3 direction = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad),
                0f
            );
            
            // 启动碎片动画
            StartCoroutine(AnimateFragment(fragment, direction));
        }
        
        yield return null;
    }
    
    /// <summary>
    /// 动画化碎片
    /// </summary>
    /// <param name="fragment">碎片对象</param>
    /// <param name="direction">移动方向</param>
    /// <returns></returns>
    private System.Collections.IEnumerator AnimateFragment(GameObject fragment, Vector3 direction)
    {
        float duration = 1f;
        float speed = 5f;
        Vector3 startPos = fragment.transform.position;
        Vector3 startScale = fragment.transform.localScale;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < duration && fragment != null)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            
            // 移动
            fragment.transform.position = startPos + direction * speed * elapsedTime;
            
            // 缩小
            fragment.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, progress);
            
            // 透明度
            Renderer renderer = fragment.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color color = renderer.material.color;
                color.a = Mathf.Lerp(1f, 0f, progress);
                renderer.material.color = color;
            }
            
            yield return null;
        }
        
        // 销毁碎片
        if (fragment != null)
        {
            Destroy(fragment);
        }
    }
    
    /// <summary>
    /// 设置动画参数
    /// </summary>
    /// <param name="appearDuration">出现动画持续时间</param>
    /// <param name="disappearDuration">消失动画持续时间</param>
    public void SetAnimationDurations(float appearDuration, float disappearDuration)
    {
        appearAnimationDuration = appearDuration;
        disappearAnimationDuration = disappearDuration;
    }
    
    /// <summary>
    /// 设置出现动画参数
    /// </summary>
    /// <param name="startScale">起始缩放</param>
    /// <param name="rotationSpeed">旋转速度</param>
    public void SetAppearAnimationSettings(Vector3 startScale, float rotationSpeed)
    {
        appearStartScale = startScale;
        appearRotationSpeed = rotationSpeed;
    }
    
    /// <summary>
    /// 设置消失特效预制体
    /// </summary>
    /// <param name="effectPrefab">特效预制体</param>
    public void SetDisappearEffectPrefab(GameObject effectPrefab)
    {
        disappearEffectPrefab = effectPrefab;
    }
    
    /// <summary>
    /// 启用/禁用动画
    /// </summary>
    /// <param name="enableAppear">是否启用出现动画</param>
    /// <param name="enableDisappear">是否启用消失动画</param>
    public void SetAnimationEnabled(bool enableAppear, bool enableDisappear)
    {
        enableAppearAnimation = enableAppear;
        enableDisappearAnimation = enableDisappear;
    }
    
    /// <summary>
    /// 检查是否正在播放动画
    /// </summary>
    /// <returns>是否正在播放动画</returns>
    public bool IsPlayingAnimation()
    {
        return isPlayingAppearAnimation || isPlayingDisappearAnimation;
    }
    
    /// <summary>
    /// 强制停止所有动画
    /// </summary>
    public void StopAllAnimations()
    {
        if (appearAnimationCoroutine != null)
        {
            StopCoroutine(appearAnimationCoroutine);
            appearAnimationCoroutine = null;
            isPlayingAppearAnimation = false;
        }
        
        if (disappearAnimationCoroutine != null)
        {
            StopCoroutine(disappearAnimationCoroutine);
            disappearAnimationCoroutine = null;
            isPlayingDisappearAnimation = false;
        }
        
        // 重置到原始状态
        transform.localScale = originalScale;
        if (targetRenderer != null && !isHit)
        {
            targetRenderer.color = isActive ? originalColor : inactiveColor;
        }
    }
    
    // 编辑器辅助
    private void OnDrawGizmosSelected()
    {
        // 显示击中半径
        Gizmos.color = isActive ? Color.red : Color.gray;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
        
        // 显示标靶索引
        if (Application.isPlaying)
        {
            Vector3 labelPos = transform.position + Vector3.up * (hitRadius + 0.5f);
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(labelPos, $"Target {targetIndex}");
            #endif
        }
    }
}
