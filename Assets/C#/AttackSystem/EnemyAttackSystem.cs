using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class EnemyAttackSystem : MonoBehaviour
{
    [Header("Attack System Settings")]
    [SerializeField] private bool enableAttacks = true;
    [SerializeField] private float attackDecisionInterval = 2f;
    [SerializeField] private float playerDetectionRange = 8f;
    
    [Header("Attack Patterns")]
    [SerializeField] private List<AttackPattern> availableAttacks = new List<AttackPattern>();
    [SerializeField] private AttackPattern currentAttack;
    
    [Header("AI Settings")]
    [SerializeField] private bool useRandomAttacks = true;
    [SerializeField] private float[] attackWeights;
    
    private Enemy owner;
    private Transform player;
    private bool isPlayerInRange = false;
    private Coroutine attackDecisionCoroutine;
    
    // 事件
    public System.Action<AttackPattern> OnAttackStarted;
    public System.Action<AttackPattern> OnAttackCompleted;
    
    private void Awake()
    {
        owner = GetComponent<Enemy>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        // 自动收集攻击模式
        if (availableAttacks.Count == 0)
        {
            CollectAttackPatterns();
        }
        
        // 设置攻击权重
        if (attackWeights == null || attackWeights.Length != availableAttacks.Count)
        {
            SetupAttackWeights();
        }
    }
    
    private void Start()
    {
        if (enableAttacks)
        {
            StartAttackDecision();
        }
    }
    
    private void Update()
    {
        CheckPlayerDistance();
    }
    
    private void CheckPlayerDistance()
    {
        if (player == null) return;
        
        float distance = Vector3.Distance(transform.position, player.position);
        isPlayerInRange = distance <= playerDetectionRange;
        
        if (isPlayerInRange && attackDecisionCoroutine == null)
        {
            StartAttackDecision();
        }
        else if (!isPlayerInRange && attackDecisionCoroutine != null)
        {
            StopAttackDecision();
        }
    }
    
    private void StartAttackDecision()
    {
        if (attackDecisionCoroutine != null)
        {
            StopCoroutine(attackDecisionCoroutine);
        }
        
        attackDecisionCoroutine = StartCoroutine(AttackDecisionLoop());
    }
    
    private void StopAttackDecision()
    {
        if (attackDecisionCoroutine != null)
        {
            StopCoroutine(attackDecisionCoroutine);
            attackDecisionCoroutine = null;
        }
    }
    
    private IEnumerator AttackDecisionLoop()
    {
        while (isPlayerInRange && enableAttacks)
        {
            // 等待决策间隔
            yield return new WaitForSeconds(attackDecisionInterval);
            
            if (!isPlayerInRange) break;
            
            // 选择并执行攻击
            SelectAndExecuteAttack();
        }
    }
    
    private void SelectAndExecuteAttack()
    {
        if (currentAttack != null && currentAttack.IsAttacking()) return;
        
        AttackPattern selectedAttack = SelectAttack();
        if (selectedAttack != null)
        {
            ExecuteAttack(selectedAttack);
        }
    }
    
    private AttackPattern SelectAttack()
    {
        if (availableAttacks.Count == 0) return null;
        
        if (useRandomAttacks)
        {
            // 使用权重随机选择
            return SelectAttackByWeight();
        }
        else
        {
            // 顺序选择
            return SelectAttackSequentially();
        }
    }
    
    private AttackPattern SelectAttackByWeight()
    {
        float totalWeight = 0f;
        foreach (float weight in attackWeights)
        {
            totalWeight += weight;
        }
        
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        
        for (int i = 0; i < availableAttacks.Count; i++)
        {
            currentWeight += attackWeights[i];
            if (randomValue <= currentWeight)
            {
                return availableAttacks[i];
            }
        }
        
        return availableAttacks[0]; // 默认返回第一个
    }
    
    private AttackPattern SelectAttackSequentially()
    {
        // 简单的循环选择
        for (int i = 0; i < availableAttacks.Count; i++)
        {
            if (availableAttacks[i].CanAttack())
            {
                return availableAttacks[i];
            }
        }
        
        return null;
    }
    
    private void ExecuteAttack(AttackPattern attack)
    {
        if (attack == null || !attack.CanAttack()) return;
        
        currentAttack = attack;
        
        // 订阅攻击事件
        attack.OnAttackStart += OnAttackStarted;
        attack.OnAttackComplete += OnAttackCompleted;
        
        // 开始攻击
        attack.StartAttack();
        
        OnAttackStarted?.Invoke(attack);
    }
    
    private void CollectAttackPatterns()
    {
        AttackPattern[] patterns = GetComponents<AttackPattern>();
        availableAttacks.AddRange(patterns);
    }
    
    private void SetupAttackWeights()
    {
        attackWeights = new float[availableAttacks.Count];
        for (int i = 0; i < attackWeights.Length; i++)
        {
            attackWeights[i] = 1f; // 默认权重为1
        }
    }
    
    // 公共方法
    public void EnableAttacks(bool enable)
    {
        enableAttacks = enable;
        
        if (enable)
        {
            StartAttackDecision();
        }
        else
        {
            StopAttackDecision();
        }
    }
    
    public void InterruptCurrentAttack()
    {
        if (currentAttack != null)
        {
            currentAttack.InterruptAttack();
        }
    }
    
    public bool IsAttacking()
    {
        return currentAttack != null && currentAttack.IsAttacking();
    }
    
    public AttackPattern GetCurrentAttack()
    {
        return currentAttack;
    }
    
    public List<AttackPattern> GetAvailableAttacks()
    {
        return availableAttacks;
    }
    
    private void OnDrawGizmosSelected()
    {
        // 显示检测范围
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, playerDetectionRange);
    }
}
