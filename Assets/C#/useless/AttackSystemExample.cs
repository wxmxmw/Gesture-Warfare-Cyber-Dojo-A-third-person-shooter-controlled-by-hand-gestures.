using UnityEngine;

/// <summary>
/// 定点射击攻击系统使用示例
/// 演示如何配置和使用新的攻击系统
/// </summary>
public class AttackSystemExample : MonoBehaviour
{
    [Header("Example Settings")]
    [SerializeField] private Enemy enemyToConfig;                    // 要配置的敌人
    [SerializeField] private AttackPrefabManager attackManager;      // 攻击管理器
    [SerializeField] private Transform testTarget;                   // 测试目标
    
    private void Start()
    {
        // 演示如何配置敌人的定点攻击系统
        if (enemyToConfig != null)
        {
            ConfigureEnemyAttacks();
        }
        
        // 演示如何创建独立的攻击预制体
        if (attackManager != null && testTarget != null)
        {
            // 延迟3秒后演示攻击
            Invoke(nameof(DemonstrateAttacks), 3f);
        }
    }
    
    /// <summary>
    /// 配置敌人的定点攻击
    /// </summary>
    private void ConfigureEnemyAttacks()
    {
        // 启用定点攻击系统
        enemyToConfig.SetTargetedAttacksEnabled(true);
        
        // 创建区域攻击配置
        var areaAttackConfig = new TargetedAttackConfig
        {
            attackName = "PowerfulAreaAttack",
            attackType = AttackType.Area,
            weight = 2f,
            description = "强力区域攻击，范围较大",
            areaSettings = new AreaAttackSettings
            {
                overrideRadius = true,
                attackRadius = 3f,
                overrideIndicatorRadius = true,
                indicatorStartRadius = 8f
            }
        };
        
        // 创建导弹攻击配置
        var missileAttackConfig = new TargetedAttackConfig
        {
            attackName = "FastMissileAttack",
            attackType = AttackType.Missile,
            weight = 1f,
            description = "快速导弹攻击，飞行速度快",
            missileSettings = new MissileAttackSettings
            {
                overrideSpeed = true,
                missileSpeed = 15f,
                overrideCurveHeight = true,
                curveHeight = 2f,
                overrideDamageRadius = true,
                damageRadius = 2.5f
            }
        };
        
        // 添加攻击配置到敌人
        enemyToConfig.AddTargetedAttackConfig(areaAttackConfig);
        enemyToConfig.AddTargetedAttackConfig(missileAttackConfig);
        
        Debug.Log("Enemy attack system configured successfully!");
    }
    
    /// <summary>
    /// 演示攻击系统
    /// </summary>
    private void DemonstrateAttacks()
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = testTarget.position;
        
        // 演示创建区域攻击
        CreateAreaAttackExample(startPos, targetPos);
        
        // 3秒后创建导弹攻击
        Invoke(nameof(CreateMissileAttackExample), 3f);
    }
    
    /// <summary>
    /// 创建区域攻击示例
    /// </summary>
    private void CreateAreaAttackExample(Vector3 startPos, Vector3 targetPos)
    {
        var areaSettings = new AreaAttackSettings
        {
            overrideRadius = true,
            attackRadius = 2.5f,
            overrideIndicatorRadius = true,
            indicatorStartRadius = 6f
        };
        
        GameObject areaAttack = attackManager.CreateAreaAttack(startPos, targetPos, areaSettings);
        if (areaAttack != null)
        {
            Debug.Log("Area attack created successfully!");
            
            // 启动攻击
            TargetedAttackBase attackScript = areaAttack.GetComponent<TargetedAttackBase>();
            if (attackScript != null)
            {
                attackScript.StartAttack();
            }
        }
    }
    
    /// <summary>
    /// 创建导弹攻击示例
    /// </summary>
    private void CreateMissileAttackExample()
    {
        Vector3 startPos = transform.position + Vector3.right * 5f; // 稍微偏移一点位置
        Vector3 targetPos = testTarget.position;
        
        var missileSettings = new MissileAttackSettings
        {
            overrideSpeed = true,
            missileSpeed = 12f,
            overrideCurveHeight = true,
            curveHeight = 4f,
            overrideDamageRadius = true,
            damageRadius = 2f
        };
        
        GameObject missileAttack = attackManager.CreateMissileAttack(startPos, targetPos, missileSettings);
        if (missileAttack != null)
        {
            Debug.Log("Missile attack created successfully!");
            
            // 启动攻击
            TargetedAttackBase attackScript = missileAttack.GetComponent<TargetedAttackBase>();
            if (attackScript != null)
            {
                attackScript.StartAttack();
            }
        }
    }
    
    // 测试方法 - 可以在编辑器中调用
    [ContextMenu("Test Area Attack")]
    public void TestAreaAttack()
    {
        if (attackManager != null && testTarget != null)
        {
            CreateAreaAttackExample(transform.position, testTarget.position);
        }
    }
    
    [ContextMenu("Test Missile Attack")]
    public void TestMissileAttack()
    {
        if (attackManager != null && testTarget != null)
        {
            CreateMissileAttackExample();
        }
    }
    
    [ContextMenu("Trigger Enemy Attack")]
    public void TriggerEnemyAttack()
    {
        if (enemyToConfig != null && testTarget != null)
        {
            // 手动触发敌人攻击
            enemyToConfig.TriggerTargetedAttack("PowerfulAreaAttack", testTarget.position);
        }
    }
}
