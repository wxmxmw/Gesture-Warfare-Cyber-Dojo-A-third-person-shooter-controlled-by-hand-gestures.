using UnityEngine;
using System.Collections.Generic;

public abstract class BossController : MonoBehaviour
{
    [Header("Boss Stats")]
    [SerializeField] protected float maxHealth = 500f;
    [SerializeField] protected float currentHealth;
    //[SerializeField] protected float bossLevel = 1;
    
    [Header("Animation System")]
    [SerializeField] protected Animator bossAnimator;
    //[SerializeField] protected RuntimeAnimatorController animatorController;

    [Header("Animation Sequences")]
    //[SerializeField] protected AnimationSequence[] animationSequences;
    //[SerializeField] protected bool useAnimationSequences = false;
    
    [Header("Movement")]
    [SerializeField] protected float rotationSpeed = 90f;
    [SerializeField] protected Transform target; // 玩家摄像机位置
    
    [Header("Z-Axis Movement")]
    [SerializeField] protected bool isMovingInZ = true;
    [SerializeField] protected float zMoveSpeed = 1f;
    [SerializeField] protected float minZDistance = 8f;
    [SerializeField] protected float maxZDistance = 25f;
    
    [Header("XOY Movement")]
    [SerializeField] protected bool isMovingInXOY = true;
    [SerializeField] protected float xoyMoveSpeed = 1.5f;
    [SerializeField] protected MovementPattern xoyMovementPattern = MovementPattern.Random;
    
    [Header("Random Movement Settings")]
    [SerializeField] protected float randomMoveRadius = 15f;
    [SerializeField] protected float randomMoveInterval = 4f;
    
    [Header("Path Movement Settings")]
    [SerializeField] protected EnemyPathController pathController;
    [SerializeField] protected float pathFollowSpeed = 2f;
    
    [Header("Screen-Based Path Settings")]
    [SerializeField] protected bool useScreenBasedPath = true;        // 是否使用基于屏幕的路径
    [SerializeField] protected int pathPointCount = 3;              // 路径点数量
    [SerializeField] protected float screenEdgeMargin = 2f;         // 距屏幕边缘的最小间隔
    [SerializeField] protected float pathMoveSpeed = 3f;            // 路径移动速度
    [SerializeField] protected float pathMoveDuration = 2f;          // 单次路径移动持续时间
    
    [Header("Body Parts")]
    [SerializeField] protected List<BodyPart> bodyParts = new List<BodyPart>();
    
    [Header("Visual Effects")]
    [SerializeField] protected GameObject bloodEffectPrefab;
    [SerializeField] protected float bloodEffectDuration = 1f;
    [SerializeField] protected Color hitFlashColor = Color.red;
    [SerializeField] protected float hitFlashDuration = 0.1f;
    
    [Header("AI Settings")]
    [SerializeField] protected float decisionInterval = 2f; // AI决策间隔
    [SerializeField] protected float attackRange = 10f; // 攻击范围
    [SerializeField] protected bool enableDebugLogs = true; // 是否启用调试日志
    
    // 护甲伤害倍率
    protected Dictionary<ArmorType, float> armorDamageMultiplier = new Dictionary<ArmorType, float>()
    {
        { ArmorType.WeakPoint, 1.5f },
        { ArmorType.Light, 1.0f },
        { ArmorType.Medium, 0.8f },
        { ArmorType.Heavy, 0.5f },
        { ArmorType.Unattackable, 0.0f }
    };
    
    // 状态机
    protected BossStateMachine stateMachine;
    
    // 公共属性
    public bool IsDead { get; protected set; } = false;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercentage => currentHealth / maxHealth;
    public Transform Target => target;
    public float AttackRange => attackRange;
    
    // 事件
    public System.Action<BossController> OnBossDeath;
    public System.Action<BossController, float> OnBossDamaged;
    public System.Action<BodyPart> OnBodyPartDestroyed;
    
    // 私有变量
    protected float currentDistanceToTarget;
    protected Vector3 randomTargetPosition;
    protected float randomMoveTimer;
    protected int currentPathIndex = 0; // 被状态机移动征用了
    protected Vector3 basicSpawnPosition;
    protected bool isPlayingAnimation = false;
    protected Coroutine animationSequenceCoroutine;
    protected float aiDecisionTimer;
    
    // 屏幕路径移动相关
    protected List<Vector3> screenPathPoints = new List<Vector3>();
    protected bool isMovingOnPath = false;
    protected Vector3 currentPathTarget;
    protected float pathMoveTimer;
    protected bool pathMoveCompleted = false;
    
    [Header("Score Settings")]
    [SerializeField] private int scoreValue = 100; // Boss分数

    // 获取分数
    public int GetScore()
    {
        return scoreValue;
    }
    
    // 设置分数
    public void SetScore(int score)
    {
        scoreValue = score;
    }

    // 移动模式枚举
    public enum MovementPattern
    {
        Random,     // 随机移动
        Path        // 固定路径
    }
    
    protected virtual void Awake()
    {
        basicSpawnPosition = transform.position;
        currentHealth = maxHealth;
        InitializeBodyParts();
        SetupTarget();
        InitializeMovement();
        InitializeAnimation();
    }
    
    protected virtual void Start()
    {
        InitializeStateMachine();

        // 创建血条
        if (HealthBarManager.Instance != null)
        {
            HealthBarManager.Instance.CreateHealthBar(gameObject, maxHealth, currentHealth);
        }

        // 为所有部位添加EnemyBodyPart组件
        foreach (var bodyPart in bodyParts)
        {
            if (bodyPart.partObject != null)
            {
                var bodyPartComponent = bodyPart.partObject.GetComponent<EnemyBodyPart>();
                if (bodyPartComponent == null)
                {
                    bodyPartComponent = bodyPart.partObject.AddComponent<EnemyBodyPart>();
                }
                bodyPartComponent.Initialize(this, bodyPart);
            }
        }
        
        // 开始AI决策
        aiDecisionTimer = decisionInterval;
        
        // 初始化屏幕路径点
        if (useScreenBasedPath)
        {
            CalculateScreenPathPoints();
        }
    }
    
    protected virtual void Update()
    {
        if (!IsDead && target != null)
        {
            UpdateDistanceToTarget();
            
            if (isMovingInZ)
            {
                MoveInZ();
            }
            
            if (isMovingInXOY)
            {
                MoveInXOY();
            }
            
            // 更新屏幕路径移动
            if (useScreenBasedPath && isMovingOnPath)
            {
                UpdateScreenPathMovement();
            }
            
            // AI决策
            UpdateAI();
        }
    }
    
    // 初始化状态机
    protected virtual void InitializeStateMachine()
    {
        stateMachine = GetComponent<BossStateMachine>();
        if (stateMachine == null)
        {
            stateMachine = gameObject.AddComponent<BossStateMachine>();
        }
    }
    
    // AI决策更新
    protected virtual void UpdateAI()
    {
        aiDecisionTimer -= Time.deltaTime;
        if (aiDecisionTimer <= 0f)
        {
            MakeAIDecision();
            aiDecisionTimer = decisionInterval;
        }
    }
    
    // AI决策逻辑 - 子类重写
    protected virtual void MakeAIDecision()
    {
        // 基础AI逻辑
        if (currentDistanceToTarget <= attackRange)
        {
            // 在攻击范围内，可以攻击
            Debug.Log($"Boss {name} is in attack range, can attack!");
        }
        else
        {
            // 不在攻击范围内，需要接近玩家
            Debug.Log($"Boss {name} is too far, need to approach!");
        }
    }
    
    // 移动方法
    protected virtual void MoveInZ()
    {
        if (target == null || !isMovingInZ) return;
        
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        float zDirection = Mathf.Sign(directionToTarget.z);
        
        Vector3 zMovement = Vector3.forward * zDirection * zMoveSpeed * Time.deltaTime;
        transform.position += zMovement;
    }
    
    protected virtual void MoveInXOY()
    {
        // 如果正在执行屏幕路径移动，不执行其他移动
        if (useScreenBasedPath && isMovingOnPath)
        {
            return;
        }
        
        switch (xoyMovementPattern)
        {
            case MovementPattern.Random:
                MoveRandomlyInXOY();
                break;
            case MovementPattern.Path:
                if (useScreenBasedPath)
                {
                    // 屏幕路径移动由状态机触发，这里不执行
                    return;
                }
                else
                {
                    MoveAlongPath();
                }
                break;
        }
    }
    
    protected virtual void MoveRandomlyInXOY()
    {
        randomMoveTimer += Time.deltaTime;
        
        if (Vector3.Distance(transform.position, randomTargetPosition) < 0.5f || randomMoveTimer >= randomMoveInterval)
        {
            SetNewRandomTarget();
        }
        
        Vector3 direction = (randomTargetPosition - transform.position).normalized;
        Vector3 xoyMovement = new Vector3(direction.x, direction.y, 0) * xoyMoveSpeed * Time.deltaTime;
        transform.position += xoyMovement;
    }
    
    protected virtual void MoveAlongPath()
    {
        if (pathController == null) return;
        
        Vector3 currentPathPoint = pathController.GetPathPoint(currentPathIndex) - transform.position + basicSpawnPosition;
        Vector3 direction = (currentPathPoint - transform.position).normalized;
        
        Vector3 xoyMovement = new Vector3(direction.x, direction.y, 0) * pathFollowSpeed * Time.deltaTime;
        transform.position += xoyMovement;
        
        float distanceToPathPoint = Vector3.Distance(
            new Vector3(transform.position.x, transform.position.y, 0),
            new Vector3(currentPathPoint.x, currentPathPoint.y, 0)
        );
        
        if (distanceToPathPoint < 0.5f)
        {
            currentPathIndex = (currentPathIndex + 1) % pathController.GetPathPointCount();
        }
    }
    
    protected virtual void SetNewRandomTarget()
    {
        if (target == null) return;
        
        Vector2 randomCircle = Random.insideUnitCircle * randomMoveRadius;
        randomTargetPosition = new Vector3(
            target.position.x + randomCircle.x,
            target.position.y + randomCircle.y,
            transform.position.z
        );
        
        randomMoveTimer = 0f;
    }
    
    // 伤害系统
    public virtual void TakeDamage(float baseDamage, BodyPart hitPart = null)
    {
        if (IsDead) return;

        if (hitPart != null && hitPart.enableDestruction && hitPart.partHealth <= 0) 
        {
            // 如果部位已经破坏 不通过这个部位执行伤害
            return;
        }

        float finalDamage = 0f;
        
        if (hitPart != null)
        {
            float armorMultiplier = armorDamageMultiplier[hitPart.armorType];
            finalDamage = baseDamage * armorMultiplier * hitPart.damageMultiplier;
            
            if (armorMultiplier <= 0f)
            {
                Debug.Log($"Hit {hitPart.partName} but it's unattackable!");
                return;
            }
            
            StartCoroutine(FlashBodyPart(hitPart));
            
            if (hitPart.enableDestruction)
            {
                HandleBodyPartDamage(hitPart, finalDamage);
            }
        }
        else
        {
            finalDamage = baseDamage;
        }
        
        currentHealth -= finalDamage;
        OnBossDamaged?.Invoke(this, finalDamage);
        
        // 更新血条
        if (HealthBarManager.Instance != null)
        {
            HealthBarManager.Instance.UpdateHealthBar(gameObject, currentHealth, maxHealth);
        }
        
        if (hitPart != null && bloodEffectPrefab != null)
        {
            ShowBloodEffect(hitPart.partObject.transform.position);
        }
        
        if (currentHealth <= 0 && !IsDead)
        {
            Die();
        }
        
        Debug.Log($"Boss took {finalDamage:F1} damage. Health: {currentHealth:F1}/{maxHealth}");
    }
    
    // 死亡系统
    protected virtual void Die()
    {
        IsDead = true;
        OnBossDeath?.Invoke(this);
        
        // 移除血条
        if (HealthBarManager.Instance != null)
        {
            HealthBarManager.Instance.RemoveHealthBar(gameObject);
        }
        
        StartCoroutine(DeathSequence());
    }
    
    protected virtual System.Collections.IEnumerator DeathSequence()
    {
        // 播放死亡动画
        PlayDeathAnimation();
        
        while (isPlayingAnimation)
        {
            yield return null;
        }
        
        // 死亡效果
        yield return new WaitForSeconds(10f);
        Destroy(gameObject);
    }
    
    // 动画系统 - 子类实现
    protected virtual void InitializeAnimation() { }
    protected virtual void PlayDeathAnimation() { }
    
    // 部位系统
    protected virtual void InitializeBodyParts()
    {
        if (bodyParts.Count == 0)
        {
            AutoDetectBodyParts();
        }
        
        foreach (var bodyPart in bodyParts)
        {
            if (bodyPart.partObject != null && bodyPart.partCollider == null)
            {
                var collider = bodyPart.partObject.GetComponent<Collider2D>();
                if (collider == null)
                {
                    collider = bodyPart.partObject.AddComponent<CircleCollider2D>();
                }
                bodyPart.partCollider = collider;
                bodyPart.partObject.tag = "Boss";
            }
        }
    }
    
    protected virtual void AutoDetectBodyParts()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            ArmorType defaultArmor = ArmorType.Medium;
            float defaultMultiplier = 1f;
            bool enableDestruction = true;
            float partHealth = 150f;
            
            string childName = child.name.ToLower();
            if (childName.Contains("head"))
            {
                defaultArmor = ArmorType.WeakPoint;
                defaultMultiplier = 2f;
                partHealth = 100f;
            }
            else if (childName.Contains("core"))
            {
                defaultArmor = ArmorType.Heavy;
                defaultMultiplier = 1.5f;
                partHealth = 200f;
            }
            
            bodyParts.Add(new BodyPart(child.name, child.gameObject, defaultArmor, defaultMultiplier, enableDestruction, partHealth));
        }
    }
    
    protected virtual void HandleBodyPartDamage(BodyPart bodyPart, float damage)
    {
        if (!bodyPart.enableDestruction) return;

        if (bodyPart.partHealth <= 0) return;

        bodyPart.partHealth -= damage;
        
        if (bodyPart.partHealth <= 0)
        {
            DestroyBodyPart(bodyPart);
        }
    }
    
    protected virtual void DestroyBodyPart(BodyPart bodyPart)
    {
        OnBodyPartDestroyed?.Invoke(bodyPart);
        
        var bodyPartComponent = bodyPart.partObject.GetComponent<EnemyBodyPart>();
        if (bodyPartComponent != null)
        {
            bodyPartComponent.OnPartDestroyed();
        }
        
        Debug.Log($"Boss body part {bodyPart.partName} has been destroyed!");
    }
    
    protected virtual System.Collections.IEnumerator FlashBodyPart(BodyPart bodyPart)
    {
        if (bodyPart.partObject == null) yield break;
        
        SpriteRenderer spriteRenderer = bodyPart.partObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) yield break;
        
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = hitFlashColor;
        
        yield return new WaitForSeconds(hitFlashDuration);
        spriteRenderer.color = originalColor;
    }
    
    protected virtual void ShowBloodEffect(Vector3 position)
    {
        if (bloodEffectPrefab != null)
        {
            GameObject bloodEffect = Instantiate(bloodEffectPrefab, position, Quaternion.identity);
            Destroy(bloodEffect, bloodEffectDuration);
        }
    }
    
    // 其他初始化方法
    protected virtual void SetupTarget()
    {
        if (target == null)
        {
            Camera mainCamera = Camera.main ?? FindObjectOfType<Camera>();
            if (mainCamera != null)
            {
                target = mainCamera.transform;
            }
        }
    }
    
    protected virtual void InitializeMovement()
    {
        if (xoyMovementPattern == MovementPattern.Random)
        {
            SetNewRandomTarget();
        }
        
        if (xoyMovementPattern == MovementPattern.Path && pathController != null)
        {
            currentPathIndex = 0;
        }
    }
    
    private void UpdateDistanceToTarget()
    {
        if (target == null) return;
        currentDistanceToTarget = transform.position.z - target.position.z;
        
        if (currentDistanceToTarget <= minZDistance)
        {
            isMovingInZ = false;
        }
        else if (currentDistanceToTarget >= maxZDistance)
        {
            isMovingInZ = true;
        }
    }
    
    // 公共方法
    public virtual BodyPart GetBodyPartByCollider(Collider2D collider)
    {
        foreach (var bodyPart in bodyParts)
        {
            if (bodyPart.partCollider == collider)
                return bodyPart;
        }
        return null;
    }
    
    public virtual float GetDistanceToTarget()
    {
        return currentDistanceToTarget;
    }
    
    // 编辑器辅助方法
    [ContextMenu("Setup Default Body Parts")]
    public virtual void SetupDefaultBodyParts()
    {
        bodyParts.Clear();
        bodyParts.Add(new BodyPart("Core", null, ArmorType.Heavy, 1.5f, true, 200f));
        bodyParts.Add(new BodyPart("Head", null, ArmorType.WeakPoint, 2f, true, 100f));
        bodyParts.Add(new BodyPart("LeftArm", null, ArmorType.Medium, 1f, true, 120f));
        bodyParts.Add(new BodyPart("RightArm", null, ArmorType.Medium, 1f, true, 120f));
    }

    // 虚方法，子类实现具体的动画播放逻辑
    public virtual void PlayAnimation(int index)
    {
        // 基类中为空实现，子类根据需要重写
    }

    // 查找玩家的方法
    public Player FindPlayer()
    {
        return FindObjectOfType<Player>();
    }
    
    /// <summary>
    /// 计算基于屏幕范围的路径点
    /// </summary>
    protected virtual void CalculateScreenPathPoints()
    {
        screenPathPoints.Clear();
        
        if (target == null) return;
        
        // 获取屏幕边界
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;
        
        // 计算屏幕边界（世界坐标）
        float screenHeight = 2f * mainCamera.orthographicSize;
        float screenWidth = screenHeight * mainCamera.aspect;
        
        // 计算可用移动范围
        float availableWidth = screenWidth - 2f * screenEdgeMargin;
        float availableHeight = screenHeight - 2f * screenEdgeMargin;
        
        // 计算路径点间距
        float stepSize = availableWidth / (pathPointCount - 1);
        
        // 生成路径点
        for (int i = 0; i < pathPointCount; i++)
        {
            float x = target.position.x - screenWidth / 2f + screenEdgeMargin + stepSize * i;
            float y = target.position.y;
            float z = transform.position.z; // 保持当前Z轴位置
            
            Vector3 pathPoint = new Vector3(x, y, z);
            screenPathPoints.Add(pathPoint);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Calculated {screenPathPoints.Count} screen path points");
        }
    }
    
    /// <summary>
    /// 更新屏幕路径移动
    /// </summary>
    protected virtual void UpdateScreenPathMovement()
    {
        if (!isMovingOnPath || screenPathPoints.Count == 0) return;
        
        pathMoveTimer += Time.deltaTime;
        
        // 移动到目标点
        Vector3 direction = (currentPathTarget - transform.position).normalized;
        Vector3 movement = direction * pathMoveSpeed * Time.deltaTime;
        transform.position += movement;
        
        // 检查是否到达目标点或超时
        float distanceToTarget = Vector3.Distance(transform.position, currentPathTarget);
        if (distanceToTarget < 0.5f || pathMoveTimer >= pathMoveDuration)
        {
            // 移动完成
            CompletePathMove();
        }
    }
    
    /// <summary>
    /// 完成路径移动
    /// </summary>
    protected virtual void CompletePathMove()
    {
        isMovingOnPath = false;
        pathMoveCompleted = true;
        pathMoveTimer = 0f;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Path move completed");
        }
    }

    // 销毁Boss的方法
    public virtual void DestroyBoss()
    {
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 开始屏幕路径移动（由状态机调用）
    /// </summary>
    /// <param name="pathIndex">路径点索引，-1表示随机选择，-2表示顺序选择</param>
    public virtual void StartScreenPathMove(int pathIndex = -1)
    {
        if (!useScreenBasedPath || screenPathPoints.Count == 0 || xoyMovementPattern != MovementPattern.Path)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"[{gameObject.name}] Cannot start screen path move: not configured or no path points");
            }
            return;
        }
        
        // 选择路径点
        int selectedIndex;
        if (pathIndex >= 0 && pathIndex < screenPathPoints.Count)
        {
            selectedIndex = pathIndex;
            currentPathIndex = selectedIndex;
        }
        else if (pathIndex == -2) 
        {
            currentPathIndex = (currentPathIndex + 1) % screenPathPoints.Count;
            selectedIndex = currentPathIndex;
        }
        else
        {
            // 随机选择一个路径点
            selectedIndex = Random.Range(0, screenPathPoints.Count);
            currentPathIndex = selectedIndex;
        }

        // 开始移动
        currentPathTarget = screenPathPoints[selectedIndex];
        isMovingOnPath = true;
        pathMoveCompleted = false;
        pathMoveTimer = 0f;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Started screen path move to point {selectedIndex}");
        }
    }
    
    /// <summary>
    /// 检查路径移动是否完成
    /// </summary>
    /// <returns>是否完成</returns>
    public virtual bool IsPathMoveCompleted()
    {
        return pathMoveCompleted;
    }
    
    /// <summary>
    /// 重置路径移动状态
    /// </summary>
    public virtual void ResetPathMoveState()
    {
        pathMoveCompleted = false;
        isMovingOnPath = false;
        pathMoveTimer = 0f;
    }
    
    /// <summary>
    /// 重新计算屏幕路径点（用于动态调整）
    /// </summary>
    public virtual void RecalculateScreenPathPoints()
    {
        if (useScreenBasedPath)
        {
            CalculateScreenPathPoints();
        }
    }


}


