using UnityEngine;
using System.Collections;

/// <summary>
/// 攻击投射物基类
/// 所有攻击都通过独立的GameObject进行，挂载此组件或其子类
/// </summary>
public abstract class AttackProjectile : MonoBehaviour
{
    [Header("Attack Projectile Settings")]
    [SerializeField] protected float damage = 50f;
    [SerializeField] protected LayerMask playerLayerMask = -1;
    [SerializeField] protected bool enableDebugLogs = true;
    
    // 攻击配置和状态
    protected EnemyStateMachine ownerStateMachine;
    protected AttackActionConfig attackConfig;
    protected Vector3 startPosition;
    protected Vector3 targetPosition;
    protected bool isAttackCompleted = false;
    protected bool isAttackInterrupted = false;
    
    // 事件
    public System.Action<AttackProjectile> OnAttackCompleted;
    public System.Action<AttackProjectile> OnAttackInterrupted;
    public System.Action<AttackProjectile, bool> OnPlayerHit; // 攻击物体，是否击中玩家
    
    /// <summary>
    /// 初始化攻击物体
    /// </summary>
    /// <param name="stateMachine">所属的状态机</param>
    /// <param name="config">攻击配置</param>
    /// <param name="start">起始位置</param>
    /// <param name="target">目标位置</param>
    public virtual void Initialize(EnemyStateMachine stateMachine, AttackActionConfig config, Vector3 start, Vector3 target)
    {
        ownerStateMachine = stateMachine;
        attackConfig = config;
        startPosition = start;
        targetPosition = target;
        
        // 设置初始位置
        transform.position = startPosition;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Attack projectile initialized: {config.attackName}");
        }
        
        // 开始攻击逻辑
        StartCoroutine(ExecuteAttack());
    }

    public virtual void InitializeByBoss(Vector3 start, Vector3 target)
    {
        startPosition = start;
        targetPosition = target;

        // 设置初始位置
        transform.position = startPosition;

        // 开始攻击逻辑
        StartCoroutine(ExecuteAttack());
    }


    /// <summary>
    /// 执行攻击的主要逻辑（由子类实现）
    /// </summary>
    /// <returns></returns>
    protected abstract IEnumerator ExecuteAttack();
    
    /// <summary>
    /// 检测并伤害玩家
    /// </summary>
    /// <param name="position">检测位置</param>
    /// <param name="radius">检测半径</param>
    /// <returns>是否击中玩家</returns>
    protected bool DetectAndDamagePlayer(Vector3 position, float radius)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, radius, playerLayerMask);
        bool hitPlayer = false;
        
        foreach (var collider in colliders)
        {
            if (collider.CompareTag("Player"))
            {
                Player player = collider.GetComponent<Player>();
                if (player != null && !player.IsDead)
                {
                    // 直接调用玩家的TakeDamage方法，掩体检测逻辑在玩家端处理
                    player.TakeDamage(damage);
                    hitPlayer = true;
                    
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[{gameObject.name}] Hit player for {damage} damage!");
                    }
                }
            }
        }
        
        // 触发击中事件
        OnPlayerHit?.Invoke(this, hitPlayer);
        
        return hitPlayer;
    }
    
    /// <summary>
    /// 完成攻击
    /// </summary>
    protected virtual void CompleteAttack()
    {
        if (isAttackCompleted) return;
        
        isAttackCompleted = true;
        
        // 触发完成事件
        OnAttackCompleted?.Invoke(this);
                
        // 销毁攻击物体
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 中断攻击
    /// </summary>
    public virtual void InterruptAttack()
    {
        if (isAttackCompleted || isAttackInterrupted) return;
        
        isAttackInterrupted = true;
        
        // 触发中断事件
        OnAttackInterrupted?.Invoke(this);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Attack interrupted: {attackConfig.attackName}");
        }
        
        // 停止所有协程
        StopAllCoroutines();
        
        // 销毁攻击物体
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 将当前伤害乘以指定倍率（用于Boss调整攻击强度）
    /// </summary>
    public void MultiplyDamage(float multiplier)
    {
        damage *= multiplier;
    }

    /// <summary>
    /// 获取攻击配置
    /// </summary>
    /// <returns>攻击配置</returns>
    public AttackActionConfig GetAttackConfig()
    {
        return attackConfig;
    }
    
    /// <summary>
    /// 获取所属状态机
    /// </summary>
    /// <returns>状态机</returns>
    public EnemyStateMachine GetOwnerStateMachine()
    {
        return ownerStateMachine;
    }
    
    /// <summary>
    /// 检查攻击是否已完成
    /// </summary>
    /// <returns>是否已完成</returns>
    public bool IsCompleted()
    {
        return isAttackCompleted;
    }
    
    /// <summary>
    /// 检查攻击是否被中断
    /// </summary>
    /// <returns>是否被中断</returns>
    public bool IsInterrupted()
    {
        return isAttackInterrupted;
    }
    
    /// <summary>
    /// 在销毁时清理
    /// </summary>
    protected virtual void OnDestroy()
    {
        // 如果还没有完成或中断，确保触发完成事件
        if (!isAttackCompleted && !isAttackInterrupted)
        {
            OnAttackCompleted?.Invoke(this);
        }
    }
}
