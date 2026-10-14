using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 敌人状态机系统
/// 管理敌人的不同状态：正常、攻击准备、攻击中
/// </summary>
public class EnemyStateMachine : MonoBehaviour
{
    [Header("State Machine Settings")]
    [SerializeField] private EnemyStateType initialState = EnemyStateType.Normal;
    [SerializeField] private bool enableDebugLogs = true;
    
    [Header("Attack Configuration")]
    [SerializeField] private List<AttackActionConfig> attackActions = new List<AttackActionConfig>();
    [SerializeField] private float attackCooldown = 3f;
    [SerializeField] private float attackRange = 1000f;
    
    // 状态相关
    private Dictionary<EnemyStateType, EnemyState> states = new Dictionary<EnemyStateType, EnemyState>();
    private EnemyState currentState;
    [SerializeField] private EnemyStateType currentStateType;
    
    // 攻击相关
    private int currentAttackIndex = 0;
    private float lastAttackTime = 0f;
    private GameObject currentAttackObject;
    
    // 组件引用
    private EnemyWithNewAttackSystem enemyComponent;
    private Transform playerTransform;
    
    // 事件
    public System.Action<EnemyStateType, EnemyStateType> OnStateChanged; // 旧状态，新状态
    public System.Action<AttackActionConfig> OnAttackStarted;
    public System.Action<AttackActionConfig> OnAttackCompleted;
    
    // 添加新的状态变量
    private bool appearAnimationCompleted = false;
    private AttackActionConfig currentAttackConfig;

    private void Awake()
    {
        enemyComponent = GetComponent<EnemyWithNewAttackSystem>();
        InitializeStates();
        FindPlayer();
    }
    
    private void Start()
    {
        // 不立即切换到初始状态，等待appear动画完成
        // ChangeState(initialState);
    }

    /// <summary>
    /// Appear动画完成回调
    /// </summary>
    public void OnAppearAnimationCompleted()
    {
        appearAnimationCompleted = true;
        // 现在可以切换到初始状态
        ChangeState(initialState);
        Debug.Log($"[{gameObject.name}] Appear animation completed, switching to normal state");
    }

    private void Update()
    {
        // 更新当前状态
        currentState?.UpdateState();
        
        // 检查攻击触发条件
        if (currentStateType == EnemyStateType.Normal)
        {
            CheckAttackTrigger();
        }
    }
    
    /// <summary>
    /// 初始化所有状态
    /// </summary>
    private void InitializeStates()
    {
        states[EnemyStateType.Normal] = new EnemyNormalState(this);
        states[EnemyStateType.AttackPreparation] = new EnemyAttackPreparationState(this);
        states[EnemyStateType.Attacking] = new EnemyAttackingState(this);
    }
    
    /// <summary>
    /// 寻找玩家
    /// </summary>
    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }
    
    /// <summary>
    /// 切换状态
    /// </summary>
    /// <param name="newStateType">新状态类型</param>
    public void ChangeState(EnemyStateType newStateType)
    {
        EnemyStateType oldStateType = currentStateType;
        
        // 退出当前状态
        currentState?.ExitState();
        
        // 切换到新状态
        currentStateType = newStateType;
        currentState = states[newStateType];
        
        // 进入新状态
        currentState?.EnterState();
        
        // 触发状态改变事件
        OnStateChanged?.Invoke(oldStateType, newStateType);
        
    }
    
    /// <summary>
    /// 检查攻击触发条件
    /// </summary>
    private void CheckAttackTrigger()
    {
        // 如果appear动画还没播放完成，不触发攻击
        if (!appearAnimationCompleted) return;
        
        // 检查冷却时间
        if (Time.time < lastAttackTime + attackCooldown)
            return;
            
        // 检查玩家距离
        if (playerTransform == null || Vector3.Distance(transform.position, playerTransform.position) > attackRange)
            return;
            
        // 检查是否有可用的攻击
        if (attackActions.Count == 0)
            return;
            
        // 触发攻击
        TriggerAttack();
    }
    
    /// <summary>
    /// 触发攻击
    /// </summary>
    public void TriggerAttack()
    {
        if (attackActions.Count == 0) return;
        
        // 选择攻击（这里简单使用循环，可以根据需要改为随机或权重选择）
        AttackActionConfig selectedAttack = attackActions[currentAttackIndex];
        currentAttackIndex = (currentAttackIndex + 1) % attackActions.Count;
        currentAttackConfig = selectedAttack; // 保存当前攻击配置
        
        // 更新攻击时间
        lastAttackTime = Time.time;
        
        // 切换到攻击准备状态
        ChangeState(EnemyStateType.AttackPreparation);

        StartAttackPreparation(selectedAttack);

        // 触发攻击开始事件
        OnAttackStarted?.Invoke(selectedAttack);

        Debug.Log($"[{gameObject.name}] Triggered attack: {selectedAttack.attackName}");
    }
    
    /// <summary>
    /// 开始攻击准备
    /// </summary>
    /// <param name="attackConfig">攻击配置</param>
    public void StartAttackPreparation(AttackActionConfig attackConfig)
    {
        // 播放攻击准备动画
        if (enemyComponent != null)
        {
            enemyComponent.PlayAttackPreparationAnimation();
        }
        
        // 启动准备计时器
        StartCoroutine(AttackPreparationCoroutine(attackConfig));
    }
    
    /// <summary>
    /// 攻击准备协程
    /// </summary>
    /// <param name="attackConfig">攻击配置</param>
    /// <returns></returns>
    private System.Collections.IEnumerator AttackPreparationCoroutine(AttackActionConfig attackConfig)
    {
        yield return new WaitForSeconds(attackConfig.preparationTime);
        
        // 准备完成，开始攻击
        if (currentStateType == EnemyStateType.AttackPreparation)
        {
            StartAttack(attackConfig);
        }
    }
    
    /// <summary>
    /// 开始攻击
    /// </summary>
    /// <param name="attackConfig">攻击配置</param>
    public void StartAttack(AttackActionConfig attackConfig)
    {
        // 切换到攻击状态
        ChangeState(EnemyStateType.Attacking);
        
        // 创建攻击物体
        CreateAttackObject(attackConfig);
    }
    
    /// <summary>
    /// 创建攻击物体
    /// </summary>
    /// <param name="attackConfig">攻击配置</param>
    private void CreateAttackObject(AttackActionConfig attackConfig)
    {
        Vector3 attackPosition = transform.position;
        Vector3 targetPosition = playerTransform != null ? playerTransform.position : transform.position;
        
        // 根据攻击类型创建不同的攻击物体
        GameObject attackPrefab = null;
        
        switch (attackConfig.attackType)
        {
            case AttackType.Area:
                attackPrefab = attackConfig.areaAttackPrefab;
                attackPosition = targetPosition; // 区域攻击在目标位置创建
                break;
            case AttackType.Missile:
                attackPrefab = attackConfig.missileAttackPrefab;
                break;
            case AttackType.Cancelable:
                attackPrefab = attackConfig.cancelableAttackPrefab;
                break;
        }
        
        if (attackPrefab != null)
        {
            // 创建攻击物体
            currentAttackObject = Instantiate(attackPrefab, attackPosition, Quaternion.identity);
            
            // 配置攻击物体
            AttackProjectile attackProjectile = currentAttackObject.GetComponent<AttackProjectile>();
            if (attackProjectile != null)
            {
                attackProjectile.Initialize(this, attackConfig, attackPosition, targetPosition);
                attackProjectile.OnAttackCompleted += OnAttackObjectCompleted;
            }
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] Attack prefab not found for {attackConfig.attackType}");
            CompleteAttack(attackConfig);
        }
    }
    
    /// <summary>
    /// 攻击物体完成回调
    /// </summary>
    /// <param name="attackProjectile">攻击物体</param>
    private void OnAttackObjectCompleted(AttackProjectile attackProjectile)
    {
        if (currentAttackObject == attackProjectile.gameObject)
        {
            currentAttackObject = null;
            CompleteAttack(attackProjectile.GetAttackConfig());
        }
    }
    
    /// <summary>
    /// 完成攻击
    /// </summary>
    /// <param name="attackConfig">攻击配置</param>
    public void CompleteAttack(AttackActionConfig attackConfig)
    {
        // 触发攻击完成事件
        OnAttackCompleted?.Invoke(attackConfig);
        // 回到正常状态
        ChangeState(EnemyStateType.Normal);
        
    }
    
    /// <summary>
    /// 强制中断攻击
    /// </summary>
    public void InterruptAttack()
    {
        if (currentAttackObject != null)
        {
            AttackProjectile attackProjectile = currentAttackObject.GetComponent<AttackProjectile>();
            if (attackProjectile != null)
            {
                attackProjectile.InterruptAttack();
            }
            
            // 特殊处理MissileProjectile（如果存在）
            MissileProjectile missileProjectile = currentAttackObject.GetComponent<MissileProjectile>();
            if (missileProjectile != null)
            {
                missileProjectile.InterruptMissile();
            }
        }
        
        // 清理所有可能存在的导弹对象
        CleanupAllMissiles();
        
        // 回到正常状态
        ChangeState(EnemyStateType.Normal);
    }

    /// <summary>
    /// 强制中断攻击准备
    /// </summary>
    public void InterruptAttackPreparation()
    {
        // 停止攻击准备协程
        StopAllCoroutines();
        
        // 清理所有可能存在的导弹对象
        CleanupAllMissiles();
        
        // 回到正常状态
        ChangeState(EnemyStateType.Normal);
        
        Debug.Log($"[{gameObject.name}] Attack preparation interrupted");
    }
    
    /// <summary>
    /// 清理所有相关的导弹对象
    /// </summary>
    private void CleanupAllMissiles()
    {
        // 查找场景中所有导弹
        MissileProjectile[] allMissiles = FindObjectsOfType<MissileProjectile>();
        Vector3 enemyPosition = transform.position;
        
        foreach (var missile in allMissiles)
        {
            if (missile != null)
            {
                float distance = Vector3.Distance(missile.transform.position, enemyPosition);
                // 只清理距离较近的导弹（可能是这个敌人发射的）
                // 这样可以避免影响其他敌人的导弹
                if (distance < 15f) // 15单位范围内的导弹
                {
                    missile.InterruptMissile();
                }
            }
        }
    }

    // 公共访问器
    public EnemyStateType GetCurrentStateType() => currentStateType;
    public EnemyWithNewAttackSystem GetEnemyComponent() => enemyComponent;
    public Transform GetPlayerTransform() => playerTransform;
    public bool IsAttacking() => currentStateType == EnemyStateType.Attacking;
    public bool IsAppearAnimationCompleted() => appearAnimationCompleted;
    public AttackActionConfig GetCurrentAttackConfig() => currentAttackConfig;
}

/// <summary>
/// 敌人状态类型枚举
/// </summary>
public enum EnemyStateType
{
    Normal,              // 正常状态
    AttackPreparation,   // 攻击准备状态
    Attacking            // 攻击中状态
}

/// <summary>
/// 攻击行动配置
/// </summary>
[System.Serializable]
public class AttackActionConfig
{
    [Header("Basic Settings")]
    public string attackName = "Default Attack";
    public AttackType attackType = AttackType.Area;
    public float preparationTime = 2f;
    
    [Header("Attack Prefabs")]
    public GameObject areaAttackPrefab;
    public GameObject missileAttackPrefab;
    public GameObject cancelableAttackPrefab;
    
    [Header("Description")]
    [TextArea(2, 3)]
    public string description = "";
}
