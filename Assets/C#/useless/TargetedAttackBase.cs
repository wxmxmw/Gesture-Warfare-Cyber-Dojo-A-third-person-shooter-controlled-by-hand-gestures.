using UnityEngine;
using System.Collections;

/// <summary>
/// 定点射击攻击的基础类
/// 继承自NonCancelableAttack，为定点攻击提供基础功能
/// </summary>
public abstract class TargetedAttackBase : NonCancelableAttack
{
    [Header("Targeted Attack Settings")]
    //[SerializeField] protected float preparationTime = 2f;      // 攻击准备时间
    [SerializeField] protected float attackExecutionTime = 1f;  // 攻击执行时间
    [SerializeField] protected float attackDamage = 50f;        // 攻击伤害
    //[SerializeField] protected LayerMask playerLayerMask = -1;  // 玩家层级掩码
    
    [Header("Target Settings")]
    [SerializeField] protected bool usePlayerCurrentPosition = true;  // 是否使用玩家当前位置作为目标
    [SerializeField] protected Vector3 fixedTargetPosition;           // 固定目标位置（如果不使用玩家位置）
    
    protected Vector3 targetPosition;                           // 实际目标位置
    protected Transform playerTransform;                        // 玩家变换组件
    protected bool attackPrepared = false;                      // 攻击是否已准备完毕
    
    protected override void Awake()
    {
        base.Awake();
        
        // 查找玩家对象
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogWarning($"Player not found for {gameObject.name}");
        }
    }
    
    protected override IEnumerator ExecuteAttack()
    {
        // 重置状态
        attackPrepared = false;
        
        // 确定目标位置
        DetermineTargetPosition();

        // 攻击准备阶段
        yield return StartCoroutine(PreparationPhase());
        
        if (isInterrupted)
        {
            yield break;
        }
        
        attackPrepared = true;
        
        // 执行具体攻击
        yield return StartCoroutine(ExecuteSpecificAttack());
        
        CompleteAttack();
    }
    
    /// <summary>
    /// 确定攻击目标位置
    /// </summary>
    protected virtual void DetermineTargetPosition()
    {
        if (usePlayerCurrentPosition && playerTransform != null)
        {
            targetPosition = playerTransform.position;
        }
        else
        {
            targetPosition = fixedTargetPosition;
        }
        
        Debug.Log($"{gameObject.name} targeting position: {targetPosition}");
    }
    
    /// <summary>
    /// 攻击准备阶段
    /// </summary>
    protected virtual IEnumerator PreparationPhase()
    {
        float elapsedTime = 0f;

        // 显示准备阶段视觉效果
        OnPreparationStart();
        
        while (elapsedTime < preparationTime && !isInterrupted)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / preparationTime;
            
            // 更新准备进度
            UpdatePreparationProgress(progress);
            UpdateProgressBar(progress * 0.5f); // 准备阶段占总进度的50%
            
            yield return null;
        }
        
        // 清理准备阶段效果
        OnPreparationEnd();
    }
    
    /// <summary>
    /// 执行具体的攻击实现（由子类实现）
    /// </summary>
    /// <returns></returns>
    protected abstract IEnumerator ExecuteSpecificAttack();
    
    /// <summary>
    /// 准备阶段开始时调用
    /// </summary>
    protected virtual void OnPreparationStart()
    {
        Debug.Log("OnPreparationStart111111");
        // 子类可以重写此方法添加准备阶段的视觉效果
    }
    
    /// <summary>
    /// 更新准备进度
    /// </summary>
    /// <param name="progress">进度（0-1）</param>
    protected virtual void UpdatePreparationProgress(float progress)
    {
        // 子类可以重写此方法更新准备阶段的视觉效果
    }
    
    /// <summary>
    /// 准备阶段结束时调用
    /// </summary>
    protected virtual void OnPreparationEnd()
    {
        // 子类可以重写此方法清理准备阶段的视觉效果
    }
    
    /// <summary>
    /// 检测指定位置的玩家并造成伤害
    /// </summary>
    /// <param name="position">检测位置</param>
    /// <param name="radius">检测半径</param>
    /// <returns>是否击中玩家</returns>
    protected bool DetectAndDamagePlayer(Vector3 position, float radius)
    {
        // 使用OverlapCircle检测范围内的玩家
        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, radius, playerLayerMask);
        
        bool hitPlayer = false;
        foreach (var collider in colliders)
        {
            if (collider.CompareTag("Player"))
            {
                // 获取玩家组件并造成伤害
                Player player = collider.GetComponent<Player>();
                if (player != null && !player.IsDead)
                {
                    player.TakeDamage(attackDamage);
                    hitPlayer = true;
                    Debug.Log($"{gameObject.name} hit player for {attackDamage} damage!");
                }
            }
        }
        
        return hitPlayer;
    }
    
    /// <summary>
    /// 获取目标位置
    /// </summary>
    /// <returns>目标位置</returns>
    public Vector3 GetTargetPosition()
    {
        return targetPosition;
    }
    
    /// <summary>
    /// 设置固定目标位置
    /// </summary>
    /// <param name="position">目标位置</param>
    public void SetFixedTargetPosition(Vector3 position)
    {
        fixedTargetPosition = position;
        usePlayerCurrentPosition = false;
    }
    
    /// <summary>
    /// 设置是否使用玩家当前位置作为目标
    /// </summary>
    /// <param name="usePlayerPosition">是否使用玩家位置</param>
    public void SetUsePlayerPosition(bool usePlayerPosition)
    {
        usePlayerCurrentPosition = usePlayerPosition;
    }
}
