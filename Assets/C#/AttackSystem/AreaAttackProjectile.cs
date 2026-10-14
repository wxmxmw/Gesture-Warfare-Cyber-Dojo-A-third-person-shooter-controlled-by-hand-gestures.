using UnityEngine;
using System.Collections;

/// <summary>
/// 区域攻击物体
/// 在目标位置显示黑圆（攻击范围）和红圆（警告），红圆缩小到黑圆大小时攻击
/// </summary>
public class AreaAttackProjectile : AttackProjectile
{
    [Header("Area Attack Settings")]
    [SerializeField] private float attackRadius = 2f;              // 攻击半径（黑圆）
    [SerializeField] private float warningRadius = 5f;             // 警告半径（红圆初始大小）
    [SerializeField] private float attackDuration = 2f;            // 攻击持续时间（红圆缩小时间）
    
    [Header("Visual Settings")]
    [SerializeField] private Color attackAreaColor = Color.black;   // 攻击区域颜色
    [SerializeField] private Color warningColor = Color.red;        // 警告颜色
    [SerializeField] private float warningAlpha = 0.6f;            // 警告透明度

    [SerializeField] private bool drawAreaOrUsePrefabsAnimation = true; // 控制区域显示方式 true=直接作图 false=使用预制体动画
    [SerializeField] private AnimationClip AttackAnimation;        // 攻击动画
    [SerializeField] private Animator AttackAnimator;

    [Header("Prefab Settings")]
    [SerializeField] private GameObject attackAreaPrefab;          // 攻击区域预制体（可选）
    [SerializeField] private GameObject warningAreaPrefab;         // 警告区域预制体（可选）
    
    // 运行时创建的视觉对象
    private GameObject attackAreaVisual;
    private GameObject warningAreaVisual;
    private SpriteRenderer attackAreaRenderer;
    private SpriteRenderer warningAreaRenderer;

    [Header("弹道效果设置")]
    [SerializeField] private TrajectoryEffectConfig trajectoryConfig = new TrajectoryEffectConfig(); // 弹道效果配置
    [SerializeField] private bool enableTrajectory = true;          // 启用弹道效果
    [SerializeField] private GameObject trajectoryPrefab;           // 弹道预制体
    [SerializeField] private Vector3 defaultStartPosition = new Vector3(-10, 5, 0); // 默认起始位置（屏幕外）
    [SerializeField] private float defaultTrajectoryDuration = 1.5f; // 默认弹道持续时间

    [Header("弹道视觉效果")]
    [SerializeField] private Material trajectoryMaterial;           // 弹道材质
    [SerializeField] private Gradient trajectoryColorGradient = new Gradient();
    [SerializeField] private float trajectoryWidth = 0.3f;         // 弹道宽度
    [SerializeField] private float trajectoryHeight = 5f;          // 弹道高度

    // 弹道相关变量
    private MagicTrajectoryRenderer magicTrajectoryRenderer;
    private DirectShotTrajectoryRenderer directShotRenderer;
    private DualArcTrajectoryRenderer dualArcRenderer;
    private Vector3 trajectoryStartPosition;
    private float trajectoryDuration = 1.5f;
    private bool hasCustomStartPosition = false;
    
    // 敌人位置（用于DirectShot弹道）
    private Vector3 enemyPosition;


    protected void Awake()
    {
        InitializeTrajectory();
    }
    
    /// <summary>
    /// 重写初始化方法，设置敌人位置
    /// </summary>
    public override void Initialize(EnemyStateMachine stateMachine, AttackActionConfig config, Vector3 start, Vector3 target)
    {
        // 调用父类初始化
        base.Initialize(stateMachine, config, start, target);
        
        // 设置敌人位置（对于区域攻击，start是目标位置，我们需要从状态机获取敌人位置）
        if (ownerStateMachine != null)
        {
            enemyPosition = ownerStateMachine.transform.position;
        }
        else
        {
            // 如果没有状态机，使用传入的start作为敌人位置
            enemyPosition = start;
        }
        
    }
    
    /// <summary>
    /// 重写Boss初始化方法
    /// </summary>
    public override void InitializeByBoss(Vector3 start, Vector3 target)
    {
        // 调用父类初始化
        base.InitializeByBoss(start, target);
        
        // 对于Boss攻击，start就是敌人位置
        enemyPosition = start;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Boss敌人位置设置为: {enemyPosition}");
        }
    }

    // 子类在此处仅重写攻击逻辑 其余都和父类相同
    protected override IEnumerator ExecuteAttack()
    {
        if (drawAreaOrUsePrefabsAnimation)
        {
            // 创建视觉效果
            CreateVisualEffects();

            // 等待攻击持续时间，同时缩小警告圆圈
            yield return StartCoroutine(AttackSequence());

            // 执行伤害检测
            if (!isAttackInterrupted)
            {
                bool hitPlayer = DetectAndDamagePlayer(targetPosition, attackRadius);

                // 显示攻击效果
                ShowAttackEffect(hitPlayer);

                // 等待一小段时间显示效果
                yield return new WaitForSeconds(0.3f);
            }

            // 完成攻击
            CompleteAttack();
        }
        else  // 使用预制体动画 + 动画事件完成区域攻击的判断
        {
            AttackAnimator.Play(AttackAnimation.name);

            // 后续逻辑通过动画事件触发
            // 触发DetectAndDamagePlayer(targetPosition, attackRadius)检测玩家 CompleteAttack()完成攻击
            // AniEvent_DetectAndDamage()

        }
    }
    
    /// <summary>
    /// 创建视觉效果
    /// </summary>
    private void CreateVisualEffects()
    {
        // 创建攻击区域（黑圆）
        if (attackAreaPrefab != null)
        {
            attackAreaVisual = Instantiate(attackAreaPrefab, targetPosition, Quaternion.identity);
        }
        else
        {
            attackAreaVisual = CreateCircleObject("AttackArea", targetPosition, attackRadius, attackAreaColor, 1f);
        }
        
        attackAreaRenderer = attackAreaVisual.GetComponent<SpriteRenderer>();
        if (attackAreaRenderer != null)
        {
            attackAreaRenderer.sortingOrder = 1; // 在下层
        }
        
        // 创建警告区域（红圆）
        if (warningAreaPrefab != null)
        {
            warningAreaVisual = Instantiate(warningAreaPrefab, targetPosition, Quaternion.identity);
        }
        else
        {
            warningAreaVisual = CreateCircleObject("WarningArea", targetPosition, warningRadius, warningColor, warningAlpha);
        }
        
        warningAreaRenderer = warningAreaVisual.GetComponent<SpriteRenderer>();
        if (warningAreaRenderer != null)
        {
            warningAreaRenderer.sortingOrder = 2; // 在上层
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Created area attack visuals at {targetPosition}");
        }
    }
    
    /// <summary>
    /// 攻击序列：警告圆圈缩小
    /// </summary>
    /// <returns></returns>
    private IEnumerator AttackSequence()
    {
        float elapsedTime = 0f;
        float startRadius = warningRadius;
        float endRadius = attackRadius;
        
        while (elapsedTime < attackDuration && !isAttackInterrupted)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / attackDuration;
            
            // 更新警告圆圈大小
            float currentRadius = Mathf.Lerp(startRadius, endRadius, progress);
            UpdateWarningAreaSize(currentRadius);
            
            yield return null;
        }
        
        // 确保最终大小正确
        if (!isAttackInterrupted)
        {
            UpdateWarningAreaSize(endRadius);
        }
    }
    
    /// <summary>
    /// 更新警告区域大小
    /// </summary>
    /// <param name="radius">新半径</param>
    private void UpdateWarningAreaSize(float radius)
    {
        if (warningAreaVisual != null)
        {
            float scale = radius / warningRadius;
            warningAreaVisual.transform.localScale = Vector3.one * scale;
        }
    }
    
    /// <summary>
    /// 显示攻击效果
    /// </summary>
    /// <param name="hitPlayer">是否击中玩家</param>
    private void ShowAttackEffect(bool hitPlayer)
    {
        if (attackAreaRenderer != null)
        {
            StartCoroutine(FlashAttackArea());
        }
        
    }
    
    /// <summary>
    /// 攻击区域闪烁效果
    /// </summary>
    /// <returns></returns>
    private IEnumerator FlashAttackArea()
    {
        if (attackAreaRenderer == null) yield break;
        
        Color originalColor = attackAreaRenderer.color;
        Color flashColor = Color.white;
        
        // 闪烁3次
        for (int i = 0; i < 3; i++)
        {
            attackAreaRenderer.color = flashColor;
            yield return new WaitForSeconds(0.1f);
            attackAreaRenderer.color = originalColor;
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    /// <summary>
    /// 创建圆形对象
    /// </summary>
    /// <param name="name">对象名称</param>
    /// <param name="position">位置</param>
    /// <param name="radius">半径</param>
    /// <param name="color">颜色</param>
    /// <param name="alpha">透明度</param>
    /// <returns>创建的游戏对象</returns>
    private GameObject CreateCircleObject(string name, Vector3 position, float radius, Color color, float alpha)
    {
        GameObject circleObj = new GameObject(name);
        circleObj.transform.position = position;
        
        // 添加SpriteRenderer
        SpriteRenderer renderer = circleObj.AddComponent<SpriteRenderer>();
        
        // 创建圆形贴图
        Texture2D texture = CreateCircleTexture(radius, color, alpha);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 32);
        
        renderer.sprite = sprite;
        
        return circleObj;
    }
    
    /// <summary>
    /// 创建圆形贴图
    /// </summary>
    /// <param name="radius">半径</param>
    /// <param name="color">颜色</param>
    /// <param name="alpha">透明度</param>
    /// <returns>圆形贴图</returns>
    private Texture2D CreateCircleTexture(float radius, Color color, float alpha)
    {
        int size = Mathf.CeilToInt(radius * 64); // 64像素每单位
        size = Mathf.Max(size, 32); // 最小32像素
        
        Texture2D texture = new Texture2D(size, size);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float textureRadius = size / 2f - 2; // 留一点边距
        
        Color finalColor = color;
        finalColor.a = alpha;
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2 point = new Vector2(x, y);
                float distance = Vector2.Distance(point, center);
                
                if (distance <= textureRadius)
                {
                    // 边缘部分
                    if (distance >= textureRadius - 2)
                    {
                        texture.SetPixel(x, y, finalColor);
                    }
                    else
                    {
                        // 内部半透明
                        Color fillColor = finalColor;
                        fillColor.a *= 0.3f;
                        texture.SetPixel(x, y, fillColor);
                    }
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        texture.Apply();
        return texture;
    }
    
    /// <summary>
    /// 清理视觉效果
    /// </summary>
    private void CleanupVisuals()
    {
        if (attackAreaVisual != null)
        {
            Destroy(attackAreaVisual);
        }
        
        if (warningAreaVisual != null)
        {
            Destroy(warningAreaVisual);
        }
    }
    
    /// <summary>
    /// 中断攻击时清理
    /// </summary>
    public override void InterruptAttack()
    {
        CleanupVisuals();
        // 隐藏弹道
        HideAllTrajectories(true);
        base.InterruptAttack();
    }
    
    /// <summary>
    /// 销毁时清理
    /// </summary>
    protected override void OnDestroy()
    {
        CleanupVisuals();
        // 清理弹道
        HideAllTrajectories(true);
        base.OnDestroy();
    }
    
    /// <summary>
    /// 隐藏所有弹道
    /// </summary>
    /// <param name="immediate">是否立即隐藏</param>
    private void HideAllTrajectories(bool immediate = false)
    {
        if (magicTrajectoryRenderer != null)
        {
            magicTrajectoryRenderer.HideTrajectory(immediate);
        }
        
        if (directShotRenderer != null)
        {
            directShotRenderer.HideTrajectory(immediate);
        }
        
        if (dualArcRenderer != null)
        {
            dualArcRenderer.HideTrajectory(immediate);
        }
    }
    
    // 公共设置方法
    public void SetAttackRadius(float radius) => attackRadius = radius;
    public void SetWarningRadius(float radius) => warningRadius = radius;
    public void SetAttackDuration(float duration) => attackDuration = duration;
    
    /// <summary>
    /// 设置弹道效果类型
    /// </summary>
    /// <param name="effectType">弹道效果类型</param>
    public void SetTrajectoryEffectType(TrajectoryEffectType effectType)
    {
        trajectoryConfig.effectType = effectType;
        
        // 重新初始化弹道系统
        if (enableTrajectory)
        {
            // 清理现有弹道
            HideAllTrajectories(true);
            
            // 重新初始化
            InitializeTrajectory();
        }
    }
    
    /// <summary>
    /// 设置弹道配置
    /// </summary>
    /// <param name="config">弹道配置</param>
    public void SetTrajectoryConfig(TrajectoryEffectConfig config)
    {
        trajectoryConfig = config;
        
        // 重新初始化弹道系统
        if (enableTrajectory)
        {
            // 清理现有弹道
            HideAllTrajectories(true);
            
            // 重新初始化
            InitializeTrajectory();
        }
    }
    
    /// <summary>
    /// 获取当前弹道效果类型
    /// </summary>
    public TrajectoryEffectType GetCurrentTrajectoryEffectType()
    {
        return trajectoryConfig.effectType;
    }
    
    /// <summary>
    /// 设置敌人位置（用于DirectShot弹道）
    /// </summary>
    /// <param name="position">敌人位置</param>
    public void SetEnemyPosition(Vector3 position)
    {
        enemyPosition = position;
        
        if (enableDebugLogs)
        {
            //Debug.Log($"[{gameObject.name}] 敌人位置设置为: {enemyPosition}");
        }
    }
    
    /// <summary>
    /// 测试弹道显示（用于调试）
    /// </summary>
    [ContextMenu("测试弹道显示")]
    public void TestTrajectoryDisplay()
    {
        if (!enableTrajectory)
        {
            Debug.LogWarning($"[{gameObject.name}] 弹道未启用");
            return;
        }
        
        Vector3 startPos;
        Vector3 endPos = targetPosition;
        
        // 根据弹道类型确定起始位置
        if (trajectoryConfig.effectType == TrajectoryEffectType.DirectShot || trajectoryConfig.effectType == TrajectoryEffectType.DualArc)
        {
            // 直接射击和双弧曳光弹：从敌人位置开始
            startPos = enemyPosition;
        }
        else
        {
            // 魔法弹道：从屏幕外或自定义位置开始
            startPos = hasCustomStartPosition ? trajectoryStartPosition : defaultStartPosition;
        }
        
        Debug.Log($"[{gameObject.name}] 测试弹道显示: {startPos} -> {endPos}, 类型: {trajectoryConfig.effectType}");
        
        // 根据弹道类型显示不同的弹道效果
        switch (trajectoryConfig.effectType)
        {
            case TrajectoryEffectType.MagicTrajectory:
                if (magicTrajectoryRenderer != null)
                {
                    magicTrajectoryRenderer.CreateTrajectory(startPos, endPos, trajectoryDuration);
                    Debug.Log($"[{gameObject.name}] 魔法弹道已创建");
                }
                else
                {
                    Debug.LogError($"[{gameObject.name}] 魔法弹道渲染器为空");
                }
                break;
            case TrajectoryEffectType.DirectShot:
                if (directShotRenderer != null)
                {
                    directShotRenderer.CreateDirectShot(startPos, endPos, trajectoryConfig);
                    Debug.Log($"[{gameObject.name}] 直接射击弹道已创建");
                }
                else
                {
                    Debug.LogError($"[{gameObject.name}] 直接射击弹道渲染器为空");
                }
                break;
            case TrajectoryEffectType.DualArc:
                if (dualArcRenderer != null)
                {
                    dualArcRenderer.CreateDualArc(startPos, endPos, trajectoryConfig);
                    Debug.Log($"[{gameObject.name}] 双弧曳光弹已创建");
                }
                else
                {
                    Debug.LogError($"[{gameObject.name}] 双弧曳光弹渲染器为空");
                }
                break;
        }
    }
    
    // 编辑器辅助
    private void OnDrawGizmosSelected()
    {
        // 显示攻击范围
        Gizmos.color = attackAreaColor;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
        
        // 显示警告范围
        Gizmos.color = warningColor;
        Gizmos.DrawWireSphere(transform.position, warningRadius);

        // 显示弹道起始点
        if (enableTrajectory)
        {
            Gizmos.color = Color.cyan;
            Vector3 startPos = hasCustomStartPosition ? trajectoryStartPosition : defaultStartPosition;
            Gizmos.DrawWireSphere(startPos, 0.5f);
            Gizmos.DrawLine(startPos, transform.position);
        }
    }

    // 动画事件引用 检测敌人
    private void AniEvent_DetectAndDamage() 
    {
        // 执行伤害检测
        if (!isAttackInterrupted)
        {
            bool hitPlayer = DetectAndDamagePlayer(targetPosition, attackRadius);

            // 显示攻击效果
            ShowAttackEffect(hitPlayer);
        }

        // 完成攻击
        // CompleteAttack();

    }


    /// <summary>
    /// 扩展的初始化方法
    /// </summary>
    private void InitializeTrajectory()
    {
        if (!enableTrajectory) return;
        
        // 根据配置创建对应的弹道渲染器
        switch (trajectoryConfig.effectType)
        {
            case TrajectoryEffectType.MagicTrajectory:
                InitializeMagicTrajectory();
                break;
            case TrajectoryEffectType.DirectShot:
                InitializeDirectShotTrajectory();
                break;
            case TrajectoryEffectType.DualArc:
                InitializeDualArcTrajectory();
                break;
        }
        

    }
    
    /// <summary>
    /// 初始化魔法弹道渲染器
    /// </summary>
    private void InitializeMagicTrajectory()
    {
        // 创建弹道渲染器
        if (trajectoryPrefab != null)
        {
            GameObject trajectoryObj = Instantiate(trajectoryPrefab, transform);
            magicTrajectoryRenderer = trajectoryObj.GetComponent<MagicTrajectoryRenderer>();
        }
        else
        {
            // 动态创建弹道渲染器
            GameObject trajectoryObj = new GameObject("MagicTrajectory");
            trajectoryObj.transform.SetParent(transform);
            magicTrajectoryRenderer = trajectoryObj.AddComponent<MagicTrajectoryRenderer>();
            
            // 配置弹道渲染器
            ConfigureMagicTrajectoryRenderer();
        }
    }
    
    /// <summary>
    /// 初始化直接射击弹道渲染器
    /// </summary>
    private void InitializeDirectShotTrajectory()
    {
        // 创建直接射击弹道渲染器
        GameObject trajectoryObj = new GameObject("DirectShotTrajectory");
        trajectoryObj.transform.SetParent(transform);
        directShotRenderer = trajectoryObj.AddComponent<DirectShotTrajectoryRenderer>();
        
        // 配置直接射击渲染器
        ConfigureDirectShotRenderer();
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] 直接射击弹道渲染器初始化完成: {directShotRenderer != null}");
        }
    }
    
    /// <summary>
    /// 初始化双弧曳光弹渲染器
    /// </summary>
    private void InitializeDualArcTrajectory()
    {
        // 创建双弧曳光弹渲染器
        GameObject trajectoryObj = new GameObject("DualArcTrajectory");
        trajectoryObj.transform.SetParent(transform);
        dualArcRenderer = trajectoryObj.AddComponent<DualArcTrajectoryRenderer>();
        
        // 配置双弧渲染器
        ConfigureDualArcRenderer();
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] 双弧曳光弹渲染器初始化完成: {dualArcRenderer != null}");
        }
    }
    
    /// <summary>
    /// 配置魔法弹道渲染器
    /// </summary>
    private void ConfigureMagicTrajectoryRenderer()
    {
        if (magicTrajectoryRenderer == null) return;
        
        // 通过反射或直接访问设置属性
        // 注意：这里需要根据MagicTrajectoryRenderer的实际实现来调整
        magicTrajectoryRenderer.SetTrajectoryColor(trajectoryColorGradient);
        magicTrajectoryRenderer.SetTrajectoryWidth(trajectoryWidth);
        
        // 如果有material，设置material
        if (trajectoryMaterial != null)
        {
            LineRenderer lr = magicTrajectoryRenderer.GetComponent<LineRenderer>();
            if (lr != null)
            {
                lr.material = trajectoryMaterial;
            }
        }
    }
    
    /// <summary>
    /// 配置直接射击渲染器
    /// </summary>
    private void ConfigureDirectShotRenderer()
    {
        if (directShotRenderer == null) return;
        
        // 应用配置
        directShotRenderer.SetTrajectoryWidth(trajectoryConfig.directShotWidth);
        directShotRenderer.SetTrajectoryColor(trajectoryConfig.directShotColor);
        directShotRenderer.SetTrajectoryMaterial(trajectoryConfig.directShotMaterial);
        directShotRenderer.SetBrightnessSettings(
            trajectoryConfig.brightnessRiseTime,
            trajectoryConfig.brightnessHoldTime,
            trajectoryConfig.brightnessFadeTime,
            trajectoryConfig.maxBrightness
        );
    }
    
    /// <summary>
    /// 配置双弧曳光弹渲染器
    /// </summary>
    private void ConfigureDualArcRenderer()
    {
        if (dualArcRenderer == null) return;
        
        // 应用配置
        dualArcRenderer.SetTrajectoryWidth(trajectoryConfig.dualArcWidth);
        dualArcRenderer.SetTrajectoryColor(trajectoryConfig.dualArcColor);
        dualArcRenderer.SetCurvature(trajectoryConfig.dualArcCurvature);
        dualArcRenderer.SetArcOffset(trajectoryConfig.dualArcOffset);
        dualArcRenderer.SetAnimationSpeed(trajectoryConfig.dualArcAnimationSpeed);
        
        if (trajectoryConfig.dualArcMaterial != null)
        {
            dualArcRenderer.SetTrajectoryMaterial(trajectoryConfig.dualArcMaterial);
        }
    }
    
    /// <summary>
    /// 动画事件 - 设置弹道起始位置
    /// </summary>
    /// <param name="position">起始位置（世界坐标）</param>
    public void AniEvent_SetTrajectoryStartPosition(Vector3 position)
    {
        trajectoryStartPosition = position;
        hasCustomStartPosition = true;
        
        if (enableDebugLogs)
        {
            //Debug.Log($"[{gameObject.name}] 设置弹道起始位置: {position}");
        }
    }
    
    /// <summary>
    /// 动画事件 - 设置弹道起始位置（字符串参数版本，用于动画事件）
    /// </summary>
    /// <param name="positionString">位置字符串，格式: "x,y,z"</param>
    public void AniEvent_SetTrajectoryStartPosition(string positionString)
    {
        if (string.IsNullOrEmpty(positionString)) return;
        
        string[] coords = positionString.Split(',');
        if (coords.Length >= 3)
        {
            if (float.TryParse(coords[0], out float x) &&
                float.TryParse(coords[1], out float y) &&
                float.TryParse(coords[2], out float z))
            {
                AniEvent_SetTrajectoryStartPosition(new Vector3(x, y, z));
            }
        }
    }
    
    /// <summary>
    /// 动画事件 - 触发弹道显示
    /// </summary>
    public void AniEvent_ShowTrajectory()
    {
        if (!enableTrajectory) return;
        
        Vector3 startPos;
        Vector3 endPos = targetPosition;
        
        // 根据弹道类型确定起始位置
        if (trajectoryConfig.effectType == TrajectoryEffectType.DirectShot || trajectoryConfig.effectType == TrajectoryEffectType.DualArc)
        {
            // 直接射击和双弧曳光弹：从敌人位置开始
            startPos = enemyPosition;
        }
        else
        {
            // 魔法弹道：从屏幕外或自定义位置开始
            startPos = hasCustomStartPosition ? trajectoryStartPosition : defaultStartPosition;
        }
        
        // 根据弹道类型显示不同的弹道效果
        switch (trajectoryConfig.effectType)
        {
            case TrajectoryEffectType.MagicTrajectory:
                if (magicTrajectoryRenderer != null)
                {
                    magicTrajectoryRenderer.CreateTrajectory(startPos, endPos, trajectoryDuration);
                }
                break;
            case TrajectoryEffectType.DirectShot:
                if (directShotRenderer != null)
                {
                    directShotRenderer.CreateDirectShot(startPos, endPos, trajectoryConfig);
                }
                break;
            case TrajectoryEffectType.DualArc:
                if (dualArcRenderer != null)
                {
                    dualArcRenderer.CreateDualArc(startPos, endPos, trajectoryConfig);
                }
                break;
        }
        
        if (enableDebugLogs)
        {
            //Debug.Log($"[{gameObject.name}] 显示弹道: {startPos} -> {endPos}, 类型: {trajectoryConfig.effectType}");
            //Debug.Log($"[{gameObject.name}] directShotRenderer != null: {directShotRenderer != null}");
            //Debug.Log($"[{gameObject.name}] magicTrajectoryRenderer != null: {magicTrajectoryRenderer != null}");
            //Debug.Log($"[{gameObject.name}] dualArcRenderer != null: {dualArcRenderer != null}");
        }
    }
    
    /// <summary>
    /// 动画事件 - 设置弹道持续时间
    /// </summary>
    /// <param name="duration">持续时间（秒）</param>
    public void AniEvent_SetTrajectoryDuration(float duration)
    {
        trajectoryDuration = Mathf.Max(0.1f, duration);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] 设置弹道持续时间: {duration}秒");
        }
    }
    
    /// <summary>
    /// 动画事件 - 立即显示完整弹道
    /// </summary>
    public void AniEvent_ShowCompleteTrajectory()
    {
        if (!enableTrajectory) return;
        
        Vector3 startPos;
        Vector3 endPos = targetPosition;
        
        // 根据弹道类型确定起始位置
        if (trajectoryConfig.effectType == TrajectoryEffectType.DirectShot || trajectoryConfig.effectType == TrajectoryEffectType.DualArc)
        {
            // 直接射击和双弧曳光弹：从敌人位置开始
            startPos = enemyPosition;
        }
        else
        {
            // 魔法弹道：从屏幕外或自定义位置开始
            startPos = hasCustomStartPosition ? trajectoryStartPosition : defaultStartPosition;
        }
        
        // 根据弹道类型显示不同的弹道效果
        switch (trajectoryConfig.effectType)
        {
            case TrajectoryEffectType.MagicTrajectory:
                if (magicTrajectoryRenderer != null)
                {
                    magicTrajectoryRenderer.ShowCompleteTrajectory(startPos, endPos);
                }
                break;
            case TrajectoryEffectType.DirectShot:
                if (directShotRenderer != null)
                {
                    directShotRenderer.CreateDirectShot(startPos, endPos, trajectoryConfig);
                }
                break;
            case TrajectoryEffectType.DualArc:
                if (dualArcRenderer != null)
                {
                    dualArcRenderer.CreateDualArc(startPos, endPos, trajectoryConfig);
                }
                break;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] 立即显示完整弹道: {startPos} -> {endPos}, 类型: {trajectoryConfig.effectType}");
        }
    }
    
    /// <summary>
    /// 动画事件 - 隐藏弹道
    /// </summary>
    public void AniEvent_HideTrajectory()
    {
        // 根据弹道类型隐藏对应的弹道
        switch (trajectoryConfig.effectType)
        {
            case TrajectoryEffectType.MagicTrajectory:
                if (magicTrajectoryRenderer != null)
                {
                    magicTrajectoryRenderer.HideTrajectory();
                }
                break;
            case TrajectoryEffectType.DirectShot:
                if (directShotRenderer != null)
                {
                    directShotRenderer.HideTrajectory();
                }
                break;
            case TrajectoryEffectType.DualArc:
                if (dualArcRenderer != null)
                {
                    dualArcRenderer.HideTrajectory();
                }
                break;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] 隐藏弹道 - 类型: {trajectoryConfig.effectType}");
        }
    }
    
    /// <summary>
    /// 动画事件 - 立即隐藏弹道
    /// </summary>
    public void AniEvent_HideTrajectoryImmediate()
    {
        // 根据弹道类型立即隐藏对应的弹道
        switch (trajectoryConfig.effectType)
        {
            case TrajectoryEffectType.MagicTrajectory:
                if (magicTrajectoryRenderer != null)
                {
                    magicTrajectoryRenderer.HideTrajectory(true);
                }
                break;
            case TrajectoryEffectType.DirectShot:
                if (directShotRenderer != null)
                {
                    directShotRenderer.HideTrajectory(true);
                }
                break;
            case TrajectoryEffectType.DualArc:
                if (dualArcRenderer != null)
                {
                    dualArcRenderer.HideTrajectory(true);
                }
                break;
        }
        

    }
    
    /// <summary>
    /// 设置弹道起始位置
    /// </summary>
    /// <param name="position">起始位置</param>
    public void SetTrajectoryStartPosition(Vector3 position)
    {
        trajectoryStartPosition = position;
        hasCustomStartPosition = true;
    }
    
    /// <summary>
    /// 设置弹道持续时间
    /// </summary>
    /// <param name="duration">持续时间</param>
    public void SetTrajectoryDuration(float duration)
    {
        trajectoryDuration = Mathf.Max(0.1f, duration);
    }
    
    /// <summary>
    /// 获取弹道是否激活
    /// </summary>
    public bool IsTrajectoryActive()
    {
        switch (trajectoryConfig.effectType)
        {
            case TrajectoryEffectType.MagicTrajectory:
                return magicTrajectoryRenderer != null && magicTrajectoryRenderer.IsTrajectoryActive;
            case TrajectoryEffectType.DirectShot:
                return directShotRenderer != null && directShotRenderer.IsTrajectoryActive;
            case TrajectoryEffectType.DualArc:
                return dualArcRenderer != null && dualArcRenderer.IsTrajectoryActive;
            default:
                return false;
        }
    }
    
    /// <summary>
    /// 获取弹道进度
    /// </summary>
    public float GetTrajectoryProgress()
    {
        switch (trajectoryConfig.effectType)
        {
            case TrajectoryEffectType.MagicTrajectory:
                return magicTrajectoryRenderer != null ? magicTrajectoryRenderer.TrajectoryProgress : 0f;
            case TrajectoryEffectType.DirectShot:
                return directShotRenderer != null ? directShotRenderer.CurrentBrightness : 0f;
            case TrajectoryEffectType.DualArc:
                return dualArcRenderer != null ? dualArcRenderer.CurrentProgress : 0f;
            default:
                return 0f;
        }
    }
    
    /// <summary>
    /// 启用/禁用弹道效果
    /// </summary>
    /// <param name="enabled">是否启用</param>
    public void SetTrajectoryEnabled(bool enabled)
    {
        enableTrajectory = enabled;
        
        if (!enabled)
        {
            HideAllTrajectories(true);
        }
    }
    

    
    /// <summary>
    /// 扩展的Awake方法
    /// </summary>
    private void AwakeTrajectoryExtension()
    {
        InitializeTrajectory();
    }
    
    /// <summary>
    /// 扩展的中断攻击方法
    /// </summary>
    public void InterruptAttackWithTrajectory()
    {
        // 隐藏弹道
        HideAllTrajectories(true);
        
        // 调用原始的中断方法
        InterruptAttack();
    }
    
    /// <summary>
    /// 扩展的销毁方法
    /// </summary>
    private void OnDestroyTrajectoryExtension()
    {
        // 清理弹道
        HideAllTrajectories(true);
    }
    
    /// <summary>
    /// 动画事件 - 设置弹道终止位置
    /// </summary>
    /// <param name="position">终止位置（世界坐标）</param>
    public void AniEvent_SetTrajectoryEndPosition(Vector3 position)
    {
        targetPosition = position;
    
        if (enableDebugLogs)
        {
            //Debug.Log($"[{gameObject.name}] 设置弹道终止位置: {position}");
        }
    }

    /// <summary>
    /// 动画事件 - 设置弹道终止位置（字符串参数版本）
    /// </summary>
    /// <param name="positionString">位置字符串，格式: "x,y,z"</param>
    public void AniEvent_SetTrajectoryEndPosition(string positionString)
    {
        if (string.IsNullOrEmpty(positionString)) return;
        
        string[] coords = positionString.Split(',');
        if (coords.Length >= 3)
        {
            if (float.TryParse(coords[0], out float x) &&
                float.TryParse(coords[1], out float y) &&
                float.TryParse(coords[2], out float z))
            {
                AniEvent_SetTrajectoryEndPosition(new Vector3(x, y, z));
            }
        }
    }

    /// <summary>
    /// 设置弹道终止位置（公共接口）
    /// </summary>
    /// <param name="position">终止位置</param>
    public void SetTrajectoryEndPosition(Vector3 position)
    {
        targetPosition = position;
    }


    public float getAttackRadius() 
    {
        return attackRadius;
    }

}
