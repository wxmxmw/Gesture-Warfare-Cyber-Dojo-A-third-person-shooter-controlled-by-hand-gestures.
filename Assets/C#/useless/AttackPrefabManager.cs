using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 攻击预制体管理器
/// 管理和创建独立的攻击预制体，可以被Boss或其他系统使用
/// </summary>
public class AttackPrefabManager : MonoBehaviour
{
    [Header("Attack Prefab Settings")]
    [SerializeField] private List<AttackPrefabData> availableAttacks = new List<AttackPrefabData>();
    
    [Header("Runtime Settings")]
    [SerializeField] private Transform attackContainer;  // 攻击实例的父对象
    [SerializeField] private bool cleanupOnDestroy = true; // 销毁时是否清理所有攻击实例
    
    // 运行时变量
    private List<GameObject> activeAttackInstances = new List<GameObject>();
    
    private void Awake()
    {
        // 如果没有设置攻击容器，创建一个
        if (attackContainer == null)
        {
            GameObject container = new GameObject("Attack Container");
            container.transform.SetParent(transform);
            attackContainer = container.transform;
        }
    }
    
    private void OnDestroy()
    {
        if (cleanupOnDestroy)
        {
            CleanupAllAttacks();
        }
    }
    
    /// <summary>
    /// 创建攻击实例
    /// </summary>
    /// <param name="attackName">攻击名称</param>
    /// <param name="position">创建位置</param>
    /// <param name="targetPosition">目标位置</param>
    /// <returns>创建的攻击游戏对象</returns>
    public GameObject CreateAttack(string attackName, Vector3 position, Vector3 targetPosition)
    {
        AttackPrefabData attackData = GetAttackData(attackName);
        if (attackData == null)
        {
            Debug.LogWarning($"Attack '{attackName}' not found in available attacks!");
            return null;
        }
        
        return CreateAttack(attackData, position, targetPosition);
    }
    
    /// <summary>
    /// 创建攻击实例
    /// </summary>
    /// <param name="attackData">攻击数据</param>
    /// <param name="position">创建位置</param>
    /// <param name="targetPosition">目标位置</param>
    /// <returns>创建的攻击游戏对象</returns>
    public GameObject CreateAttack(AttackPrefabData attackData, Vector3 position, Vector3 targetPosition)
    {
        if (attackData.attackPrefab == null)
        {
            Debug.LogWarning($"Attack prefab is null for '{attackData.attackName}'!");
            return null;
        }
        
        // 实例化攻击预制体
        GameObject attackInstance = Instantiate(attackData.attackPrefab, position, Quaternion.identity, attackContainer);
        attackInstance.name = $"{attackData.attackName}_Instance";
        
        // 配置攻击实例
        ConfigureAttackInstance(attackInstance, attackData, targetPosition);
        
        // 添加到活动实例列表
        activeAttackInstances.Add(attackInstance);
        
        // 订阅攻击完成事件以便清理
        TargetedAttackBase attackScript = attackInstance.GetComponent<TargetedAttackBase>();
        if (attackScript != null)
        {
            attackScript.OnAttackComplete += (attack) => OnAttackCompleted(attackInstance);
            attackScript.OnAttackInterrupted += (attack) => OnAttackCompleted(attackInstance);
        }
        
        Debug.Log($"Created attack '{attackData.attackName}' at {position} targeting {targetPosition}");
        
        return attackInstance;
    }
    
    /// <summary>
    /// 创建范围攻击
    /// </summary>
    /// <param name="position">攻击位置</param>
    /// <param name="targetPosition">目标位置</param>
    /// <param name="customSettings">自定义设置</param>
    /// <returns>创建的攻击实例</returns>
    public GameObject CreateAreaAttack(Vector3 position, Vector3 targetPosition, AreaAttackSettings customSettings = null)
    {
        AttackPrefabData areaAttackData = GetAttackData("AreaAttack");
        if (areaAttackData == null)
        {
            // 如果没有预设，创建默认的区域攻击
            areaAttackData = CreateDefaultAreaAttackData();
        }
        
        GameObject attackInstance = CreateAttack(areaAttackData, position, targetPosition);
        
        // 应用自定义设置
        if (customSettings != null && attackInstance != null)
        {
            AreaTargetedAttack areaAttack = attackInstance.GetComponent<AreaTargetedAttack>();
            if (areaAttack != null)
            {
                ApplyAreaAttackSettings(areaAttack, customSettings);
            }
        }
        
        return attackInstance;
    }
    
    /// <summary>
    /// 创建导弹攻击
    /// </summary>
    /// <param name="position">发射位置</param>
    /// <param name="targetPosition">目标位置</param>
    /// <param name="customSettings">自定义设置</param>
    /// <returns>创建的攻击实例</returns>
    public GameObject CreateMissileAttack(Vector3 position, Vector3 targetPosition, MissileAttackSettings customSettings = null)
    {
        AttackPrefabData missileAttackData = GetAttackData("MissileAttack");
        if (missileAttackData == null)
        {
            // 如果没有预设，创建默认的导弹攻击
            missileAttackData = CreateDefaultMissileAttackData();
        }
        
        GameObject attackInstance = CreateAttack(missileAttackData, position, targetPosition);
        
        // 应用自定义设置
        if (customSettings != null && attackInstance != null)
        {
            MissileTargetedAttack missileAttack = attackInstance.GetComponent<MissileTargetedAttack>();
            if (missileAttack != null)
            {
                ApplyMissileAttackSettings(missileAttack, customSettings);
            }
        }
        
        return attackInstance;
    }
    
    /// <summary>
    /// 配置攻击实例
    /// </summary>
    /// <param name="attackInstance">攻击实例</param>
    /// <param name="attackData">攻击数据</param>
    /// <param name="targetPosition">目标位置</param>
    private void ConfigureAttackInstance(GameObject attackInstance, AttackPrefabData attackData, Vector3 targetPosition)
    {
        TargetedAttackBase attackScript = attackInstance.GetComponent<TargetedAttackBase>();
        if (attackScript != null)
        {
            // 设置固定目标位置
            attackScript.SetFixedTargetPosition(targetPosition);
            attackScript.SetUsePlayerPosition(false);
            
            // 应用攻击数据中的设置
            if (attackData.overrideDamage)
            {
                // 这里需要根据具体的攻击类型来设置伤害
                // 由于基类没有直接的设置伤害方法，可能需要扩展
            }
        }
    }
    
    /// <summary>
    /// 应用区域攻击设置
    /// </summary>
    /// <param name="areaAttack">区域攻击脚本</param>
    /// <param name="settings">设置</param>
    private void ApplyAreaAttackSettings(AreaTargetedAttack areaAttack, AreaAttackSettings settings)
    {
        if (settings.overrideRadius)
        {
            areaAttack.SetAttackRadius(settings.attackRadius);
        }
        
        if (settings.overrideIndicatorRadius)
        {
            areaAttack.SetIndicatorStartRadius(settings.indicatorStartRadius);
        }
    }
    
    /// <summary>
    /// 应用导弹攻击设置
    /// </summary>
    /// <param name="missileAttack">导弹攻击脚本</param>
    /// <param name="settings">设置</param>
    private void ApplyMissileAttackSettings(MissileTargetedAttack missileAttack, MissileAttackSettings settings)
    {
        if (settings.overrideSpeed)
        {
            missileAttack.SetMissileSpeed(settings.missileSpeed);
        }
        
        if (settings.overrideCurveHeight)
        {
            missileAttack.SetCurveHeight(settings.curveHeight);
        }
        
        if (settings.overrideDamageRadius)
        {
            missileAttack.SetDamageRadius(settings.damageRadius);
        }
    }
    
    /// <summary>
    /// 获取攻击数据
    /// </summary>
    /// <param name="attackName">攻击名称</param>
    /// <returns>攻击数据</returns>
    private AttackPrefabData GetAttackData(string attackName)
    {
        foreach (var attackData in availableAttacks)
        {
            if (attackData.attackName == attackName)
            {
                return attackData;
            }
        }
        return null;
    }
    
    /// <summary>
    /// 创建默认的区域攻击数据
    /// </summary>
    /// <returns>区域攻击数据</returns>
    private AttackPrefabData CreateDefaultAreaAttackData()
    {
        GameObject prefab = new GameObject("DefaultAreaAttack");
        prefab.AddComponent<AreaTargetedAttack>();
        
        AttackPrefabData data = new AttackPrefabData
        {
            attackName = "AreaAttack",
            attackPrefab = prefab,
            attackType = AttackType.Area,
            overrideDamage = false,
            customDamage = 50f
        };
        
        return data;
    }
    
    /// <summary>
    /// 创建默认的导弹攻击数据
    /// </summary>
    /// <returns>导弹攻击数据</returns>
    private AttackPrefabData CreateDefaultMissileAttackData()
    {
        GameObject prefab = new GameObject("DefaultMissileAttack");
        prefab.AddComponent<MissileTargetedAttack>();
        
        AttackPrefabData data = new AttackPrefabData
        {
            attackName = "MissileAttack",
            attackPrefab = prefab,
            attackType = AttackType.Missile,
            overrideDamage = false,
            customDamage = 60f
        };
        
        return data;
    }
    
    /// <summary>
    /// 攻击完成时的回调
    /// </summary>
    /// <param name="attackInstance">攻击实例</param>
    private void OnAttackCompleted(GameObject attackInstance)
    {
        if (activeAttackInstances.Contains(attackInstance))
        {
            activeAttackInstances.Remove(attackInstance);
        }
        
        // 延迟销毁以确保所有回调都执行完成
        if (attackInstance != null)
        {
            Destroy(attackInstance, 0.1f);
        }
    }
    
    /// <summary>
    /// 清理所有活动的攻击实例
    /// </summary>
    public void CleanupAllAttacks()
    {
        foreach (var attackInstance in activeAttackInstances)
        {
            if (attackInstance != null)
            {
                Destroy(attackInstance);
            }
        }
        activeAttackInstances.Clear();
    }
    
    /// <summary>
    /// 中断所有活动的攻击
    /// </summary>
    public void InterruptAllAttacks()
    {
        foreach (var attackInstance in activeAttackInstances)
        {
            if (attackInstance != null)
            {
                TargetedAttackBase attackScript = attackInstance.GetComponent<TargetedAttackBase>();
                if (attackScript != null)
                {
                    attackScript.InterruptAttack();
                }
            }
        }
    }
    
    /// <summary>
    /// 获取所有可用的攻击名称
    /// </summary>
    /// <returns>攻击名称列表</returns>
    public List<string> GetAvailableAttackNames()
    {
        List<string> names = new List<string>();
        foreach (var attackData in availableAttacks)
        {
            names.Add(attackData.attackName);
        }
        return names;
    }
}

/// <summary>
/// 攻击预制体数据
/// </summary>
[System.Serializable]
public class AttackPrefabData
{
    [Header("Basic Settings")]
    public string attackName = "Default Attack";           // 攻击名称
    public GameObject attackPrefab;                        // 攻击预制体
    public AttackType attackType = AttackType.Area;       // 攻击类型
    
    [Header("Damage Override")]
    public bool overrideDamage = false;                    // 是否覆盖伤害
    public float customDamage = 50f;                       // 自定义伤害值
    
    [Header("Description")]
    [TextArea(2, 4)]
    public string description = "";                        // 攻击描述
}

/// <summary>
/// 攻击类型枚举
/// </summary>
public enum AttackType
{
    Area,      // 区域攻击
    Missile,   // 导弹攻击
    Cancelable // 可消除攻击
}

/// <summary>
/// 区域攻击设置
/// </summary>
[System.Serializable]
public class AreaAttackSettings
{
    public bool overrideRadius = false;
    public float attackRadius = 2f;
    
    public bool overrideIndicatorRadius = false;
    public float indicatorStartRadius = 5f;
}

/// <summary>
/// 导弹攻击设置
/// </summary>
[System.Serializable]
public class MissileAttackSettings
{
    public bool overrideSpeed = false;
    public float missileSpeed = 10f;
    
    public bool overrideCurveHeight = false;
    public float curveHeight = 3f;
    
    public bool overrideDamageRadius = false;
    public float damageRadius = 1.5f;
}
