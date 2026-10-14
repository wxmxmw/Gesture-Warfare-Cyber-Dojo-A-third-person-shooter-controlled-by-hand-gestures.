using UnityEngine;

/// <summary>
/// 新攻击系统使用示例
/// 展示如何配置和使用重构后的攻击系统
/// </summary>
public class NewAttackSystemExample : MonoBehaviour
{
    [Header("Example Settings")]
    [SerializeField] private Transform playerTransform;           // 玩家位置
    [SerializeField] private Transform testEnemyPosition;         // 测试敌人位置
    
    [Header("Attack Prefabs")]
    [SerializeField] private GameObject areaAttackPrefab;         // 区域攻击预制体
    [SerializeField] private GameObject missileAttackPrefab;      // 导弹攻击预制体
    [SerializeField] private GameObject cancelableAttackPrefab;  // 可消除攻击预制体
    
    private void Start()
    {
        // 寻找玩家
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
        
        Debug.Log("New Attack System Example loaded. Use context menu to test attacks.");
    }
    
    /// <summary>
    /// 测试区域攻击
    /// </summary>
    [ContextMenu("Test Area Attack")]
    public void TestAreaAttack()
    {
        if (areaAttackPrefab == null)
        {
            Debug.LogWarning("Area attack prefab not assigned!");
            return;
        }
        
        Vector3 startPos = testEnemyPosition != null ? testEnemyPosition.position : transform.position;
        Vector3 targetPos = playerTransform != null ? playerTransform.position : startPos + Vector3.forward * 5f;
        
        // 创建区域攻击
        GameObject areaAttack = Instantiate(areaAttackPrefab, startPos, Quaternion.identity);
        
        // 配置攻击
        AreaAttackProjectile areaScript = areaAttack.GetComponent<AreaAttackProjectile>();
        if (areaScript != null)
        {
            // 创建假的状态机和配置
            AttackActionConfig config = new AttackActionConfig
            {
                attackName = "Test Area Attack",
                attackType = AttackType.Area,
                preparationTime = 1f
            };
            
            areaScript.Initialize(null, config, startPos, targetPos);
        }
        
        Debug.Log($"Created area attack from {startPos} to {targetPos}");
    }
    
    /// <summary>
    /// 测试导弹攻击
    /// </summary>
    [ContextMenu("Test Missile Attack")]
    public void TestMissileAttack()
    {
        if (missileAttackPrefab == null)
        {
            Debug.LogWarning("Missile attack prefab not assigned!");
            return;
        }
        
        Vector3 startPos = testEnemyPosition != null ? testEnemyPosition.position : transform.position;
        Vector3 targetPos = playerTransform != null ? playerTransform.position : startPos + Vector3.forward * 5f;
        
        // 创建导弹攻击
        GameObject missileAttack = Instantiate(missileAttackPrefab, startPos, Quaternion.identity);
        
        // 配置攻击
        MissileAttackProjectile missileScript = missileAttack.GetComponent<MissileAttackProjectile>();
        if (missileScript != null)
        {
            // 创建假的状态机和配置
            AttackActionConfig config = new AttackActionConfig
            {
                attackName = "Test Missile Attack",
                attackType = AttackType.Missile,
                preparationTime = 1f
            };
            
            missileScript.Initialize(null, config, startPos, targetPos);
        }
        
        Debug.Log($"Created missile attack from {startPos} to {targetPos}");
    }
    
    /// <summary>
    /// 测试可消除攻击
    /// </summary>
    [ContextMenu("Test Cancelable Attack")]
    public void TestCancelableAttack()
    {
        if (cancelableAttackPrefab == null)
        {
            Debug.LogWarning("Cancelable attack prefab not assigned!");
            return;
        }
        
        Vector3 startPos = testEnemyPosition != null ? testEnemyPosition.position : transform.position;
        Vector3 targetPos = playerTransform != null ? playerTransform.position : startPos + Vector3.forward * 5f;
        
        // 创建可消除攻击
        GameObject cancelableAttack = Instantiate(cancelableAttackPrefab, startPos, Quaternion.identity);
        
        // 配置攻击
        CancelableAttackProjectile cancelableScript = cancelableAttack.GetComponent<CancelableAttackProjectile>();
        if (cancelableScript != null)
        {
            // 创建假的状态机和配置
            AttackActionConfig config = new AttackActionConfig
            {
                attackName = "Test Cancelable Attack",
                attackType = AttackType.Cancelable,
                preparationTime = 1f
            };
            
            cancelableScript.Initialize(null, config, startPos, targetPos);
        }
        
        Debug.Log($"Created cancelable attack from {startPos} to {targetPos}");
    }
    
    /// <summary>
    /// 创建带状态机的敌人示例
    /// </summary>
    [ContextMenu("Create Enemy With State Machine")]
    public void CreateEnemyWithStateMachine()
    {
        // 创建敌人GameObject
        GameObject enemy = new GameObject("Test Enemy");
        enemy.transform.position = testEnemyPosition != null ? testEnemyPosition.position : transform.position;
        
        // 添加新的敌人组件
        EnemyWithNewAttackSystem enemyScript = enemy.AddComponent<EnemyWithNewAttackSystem>();
        
        // 添加状态机组件
        EnemyStateMachine stateMachine = enemy.AddComponent<EnemyStateMachine>();
        
        Debug.Log("Created enemy with new attack system and state machine");
        
        // 3秒后测试攻击
        Invoke(nameof(TestEnemyAttack), 3f);
    }
    
    /// <summary>
    /// 测试敌人攻击
    /// </summary>
    private void TestEnemyAttack()
    {
        EnemyStateMachine[] stateMachines = FindObjectsOfType<EnemyStateMachine>();
        
        foreach (var stateMachine in stateMachines)
        {
            if (stateMachine.gameObject.name.Contains("Test Enemy"))
            {
                stateMachine.TriggerAttack();
                Debug.Log($"Triggered attack for {stateMachine.gameObject.name}");
                break;
            }
        }
    }
    
    /// <summary>
    /// 清理测试对象
    /// </summary>
    [ContextMenu("Cleanup Test Objects")]
    public void CleanupTestObjects()
    {
        // 清理攻击物体
        AttackProjectile[] attacks = FindObjectsOfType<AttackProjectile>();
        foreach (var attack in attacks)
        {
            if (attack.gameObject.name.Contains("Test"))
            {
                DestroyImmediate(attack.gameObject);
            }
        }
        
        // 清理测试敌人
        EnemyWithNewAttackSystem[] enemies = FindObjectsOfType<EnemyWithNewAttackSystem>();
        foreach (var enemy in enemies)
        {
            if (enemy.gameObject.name.Contains("Test Enemy"))
            {
                DestroyImmediate(enemy.gameObject);
            }
        }
        
        Debug.Log("Cleaned up test objects");
    }
}
