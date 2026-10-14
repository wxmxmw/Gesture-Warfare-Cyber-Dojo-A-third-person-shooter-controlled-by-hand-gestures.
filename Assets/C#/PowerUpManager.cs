using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// 道具类型枚举
public enum PowerUpType
{
    Shotgun,        // 霰弹
    RapidFire,      // 连射
    HealthPack,     // 回血
    PulseGrenade    // 脉冲手雷
}

// 道具数据结构
[System.Serializable]
public class PowerUpData
{
    [Header("道具基础设置")]
    public PowerUpType powerUpType;
    public string powerUpName;
    public float dropChance = 0.05f; // 掉落概率，默认5%
    public GameObject powerUpPrefab; // 道具预制体
    
    [Header("霰弹设置")]
    public int shotgunAmmo = 10; // 霰弹弹药数量
    public float shotgunScaleMultiplier = 3f; // 霰弹扩大倍数
    
    [Header("连射设置")]
    public float rapidFireDuration = 10f; // 连射持续时间
    public float rapidFireInterval = 0.3f; // 连射间隔
    
    [Header("回血设置")]
    public float healAmount = 50f; // 回血量
    
    [Header("脉冲手雷设置")]
    public float pulseDamage = 100f; // 脉冲伤害
    public float pulseRadius = 50f; // 脉冲范围
}

// 道具效果状态
[System.Serializable]
public class PowerUpEffect
{
    public PowerUpType powerUpType;
    public float remainingTime; // 剩余时间
    public int remainingCount; // 剩余次数
    public bool isActive; // 是否激活
    
    public PowerUpEffect(PowerUpType type, float time, int count)
    {
        powerUpType = type;
        remainingTime = time;
        remainingCount = count;
        isActive = true;
    }
}

// 道具管理器
public class PowerUpManager : MonoBehaviour
{
    [Header("道具配置")]
    [SerializeField] private List<PowerUpData> powerUpConfigs = new List<PowerUpData>();
    
    [Header("掉落设置")]
    [SerializeField] private bool enablePowerUpDrop = true;
    [SerializeField] private float globalDropChanceMultiplier = 1f;
    
    // 当前激活的道具效果
    private List<PowerUpEffect> activePowerUps = new List<PowerUpEffect>();
    
    // 道具引用
    private crosshairTarget crosshair;
    private Player player;
    private EnemySpawner enemySpawner;
    
    // 连射相关
    private Coroutine rapidFireCoroutine;
    private bool isRapidFireActive = false;
    
    // 脉冲手雷计数
    private int pulseGrenadeCount = 0;
    
    // 单例
    public static PowerUpManager Instance { get; private set; }
    
    // 事件
    public System.Action<PowerUpType> OnPowerUpActivated;
    public System.Action<PowerUpType> OnPowerUpExpired;
    public System.Action<PowerUpType, int> OnPowerUpCountChanged; // 道具计数变化
    
    private void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        InitializePowerUpManager();
    }
    
    private void Start()
    {
        SetupReferences();
        SetupGestureDetection(); // 添加手势检测设置
    }
    
    private void Update()
    {
        UpdateActivePowerUps();
    }
    
    /// <summary>
    /// 初始化道具管理器
    /// </summary>
    private void InitializePowerUpManager()
    {
        // 初始化默认道具配置
        if (powerUpConfigs.Count == 0)
        {
            CreateDefaultPowerUpConfigs();
        }
        
        // 初始化脉冲手雷数量
        pulseGrenadeCount = 0;
        
        Debug.Log("PowerUpManager初始化完成，手势检测已启用");
    }
    
    /// <summary>
    /// 设置组件引用
    /// </summary>
    private void SetupReferences()
    {
        if (crosshair == null)
        {
            crosshair = FindObjectOfType<crosshairTarget>();
        }
        
        if (player == null)
        {
            player = FindObjectOfType<Player>();
        }
        
        if (enemySpawner == null)
        {
            enemySpawner = FindObjectOfType<EnemySpawner>();
        }
    }
    
    /// <summary>
    /// 创建默认道具配置
    /// </summary>
    private void CreateDefaultPowerUpConfigs()
    {
        // 霰弹配置
        PowerUpData shotgunConfig = new PowerUpData();
        shotgunConfig.powerUpType = PowerUpType.Shotgun;
        shotgunConfig.powerUpName = "霰弹枪";
        shotgunConfig.dropChance = 0.03f;
        shotgunConfig.shotgunAmmo = 10; // 霰弹数量设为10
        shotgunConfig.shotgunScaleMultiplier = 3f;
        powerUpConfigs.Add(shotgunConfig);
        
        // 连射配置
        PowerUpData rapidFireConfig = new PowerUpData();
        rapidFireConfig.powerUpType = PowerUpType.RapidFire;
        rapidFireConfig.powerUpName = "连射";
        rapidFireConfig.dropChance = 0.04f;
        rapidFireConfig.rapidFireDuration = 10f; // 连射持续时间10秒
        rapidFireConfig.rapidFireInterval = 0.3f;
        powerUpConfigs.Add(rapidFireConfig);
        
        // 回血配置
        PowerUpData healthConfig = new PowerUpData();
        healthConfig.powerUpType = PowerUpType.HealthPack;
        healthConfig.powerUpName = "医疗包";
        healthConfig.dropChance = 0.05f;
        healthConfig.healAmount = 50f;
        powerUpConfigs.Add(healthConfig);
        
        // 脉冲手雷配置
        PowerUpData pulseConfig = new PowerUpData();
        pulseConfig.powerUpType = PowerUpType.PulseGrenade;
        pulseConfig.powerUpName = "脉冲手雷";
        pulseConfig.dropChance = 0.02f;
        pulseConfig.pulseDamage = 100f;
        pulseConfig.pulseRadius = 50f;
        powerUpConfigs.Add(pulseConfig);
    }
    
    /// <summary>
    /// 尝试掉落道具
    /// </summary>
    /// <param name="dropPosition">掉落位置</param>
    public void TryDropPowerUp(Vector3 dropPosition)
    {
        if (!enablePowerUpDrop || powerUpConfigs.Count == 0) return;
        
        foreach (PowerUpData config in powerUpConfigs)
        {
            float dropRoll = Random.value;
            float finalDropChance = config.dropChance * globalDropChanceMultiplier;
            
            if (dropRoll <= finalDropChance)
            {
                SpawnPowerUp(config, dropPosition);
                Debug.Log($"掉落道具: {config.powerUpName} at {dropPosition}");
                return; // 只掉落一个道具
            }
        }
    }
    
    /// <summary>
    /// 生成道具
    /// </summary>
    /// <param name="config">道具配置</param>
    /// <param name="position">生成位置</param>
    private void SpawnPowerUp(PowerUpData config, Vector3 position)
    {
        GameObject powerUpObject = null;
        
        if (config.powerUpPrefab != null)
        {
            // 使用预制体
            powerUpObject = Instantiate(config.powerUpPrefab, position, Quaternion.identity);
        }
        else
        {
            // 创建默认道具对象
            powerUpObject = CreateDefaultPowerUpObject(config, position);
        }
        
        if (powerUpObject != null)
        {
            PowerUpPickup pickup = powerUpObject.GetComponent<PowerUpPickup>();
            if (pickup == null)
            {
                pickup = powerUpObject.AddComponent<PowerUpPickup>();
            }
            pickup.Initialize(config);
        }
    }
    
    /// <summary>
    /// 创建默认道具对象
    /// </summary>
    /// <param name="config">道具配置</param>
    /// <param name="position">位置</param>
    /// <returns>道具对象</returns>
    private GameObject CreateDefaultPowerUpObject(PowerUpData config, Vector3 position)
    {
        GameObject powerUpObject = new GameObject($"PowerUp_{config.powerUpName}");
        powerUpObject.transform.position = position;
        
        // 添加视觉组件
        SpriteRenderer spriteRenderer = powerUpObject.AddComponent<SpriteRenderer>();
        
        // 设置不同道具的颜色
        switch (config.powerUpType)
        {
            case PowerUpType.Shotgun:
                spriteRenderer.color = Color.red;
                break;
            case PowerUpType.RapidFire:
                spriteRenderer.color = Color.yellow;
                break;
            case PowerUpType.HealthPack:
                spriteRenderer.color = Color.green;
                break;
            case PowerUpType.PulseGrenade:
                spriteRenderer.color = Color.cyan;
                break;
        }
        
        // 添加碰撞体
        CircleCollider2D collider = powerUpObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 1f;
        
        // 设置层级和标签
        powerUpObject.layer = 0; // Default层
        powerUpObject.tag = "PowerUp";
        
        return powerUpObject;
    }
    
    /// <summary>
    /// 激活道具效果
    /// </summary>
    /// <param name="config">道具配置</param>
    public void ActivatePowerUp(PowerUpData config)
    {
        switch (config.powerUpType)
        {
            case PowerUpType.Shotgun:
                ActivateShotgun(config);
                break;
            case PowerUpType.RapidFire:
                ActivateRapidFire(config);
                break;
            case PowerUpType.HealthPack:
                ActivateHealthPack(config);
                break;
            case PowerUpType.PulseGrenade:
                ActivatePulseGrenade(config);
                break;
        }
        
        OnPowerUpActivated?.Invoke(config.powerUpType);
        Debug.Log($"激活道具: {config.powerUpName}");
    }
    
    /// <summary>
    /// 激活霰弹效果
    /// </summary>
    /// <param name="config">道具配置</param>
    private void ActivateShotgun(PowerUpData config)
    {
        // 添加或更新霰弹效果
        PowerUpEffect existingEffect = GetActivePowerUp(PowerUpType.Shotgun);
        if (existingEffect != null)
        {
            // 霰弹数量设为固定值，不超过最大值
            int maxAmmo = config.shotgunAmmo;
            existingEffect.remainingCount = Mathf.Min(existingEffect.remainingCount, maxAmmo);
        }
        else
        {
            PowerUpEffect shotgunEffect = new PowerUpEffect(PowerUpType.Shotgun, 0f, config.shotgunAmmo);
            activePowerUps.Add(shotgunEffect);
            
            // 扩大准星
            if (crosshair != null)
            {
                crosshair.transform.localScale *= config.shotgunScaleMultiplier;
            }
        }
        
        OnPowerUpCountChanged?.Invoke(PowerUpType.Shotgun, GetActivePowerUp(PowerUpType.Shotgun).remainingCount);
    }
    
    /// <summary>
    /// 激活连射效果
    /// </summary>
    /// <param name="config">道具配置</param>
    private void ActivateRapidFire(PowerUpData config)
    {
        // 添加或更新连射效果
        PowerUpEffect existingEffect = GetActivePowerUp(PowerUpType.RapidFire);
        if (existingEffect != null)
        {
            // 连射时间设为固定值，不超过最大持续时间
            float maxDuration = config.rapidFireDuration;
            existingEffect.remainingTime = Mathf.Min(existingEffect.remainingTime, maxDuration);
        }
        else
        {
            PowerUpEffect rapidFireEffect = new PowerUpEffect(PowerUpType.RapidFire, config.rapidFireDuration, 0);
            activePowerUps.Add(rapidFireEffect);
            
            // 开始连射
            if (rapidFireCoroutine != null)
            {
                StopCoroutine(rapidFireCoroutine);
            }
            rapidFireCoroutine = StartCoroutine(RapidFireCoroutine(config.rapidFireInterval));
        }
    }
    
    /// <summary>
    /// 激活回血效果
    /// </summary>
    /// <param name="config">道具配置</param>
    private void ActivateHealthPack(PowerUpData config)
    {
        if (player != null)
        {
            player.Heal(config.healAmount);
            Debug.Log($"玩家回血: {config.healAmount}");
        }
    }
    
    /// <summary>
    /// 激活脉冲手雷效果
    /// </summary>
    /// <param name="config">道具配置</param>
    private void ActivatePulseGrenade(PowerUpData config)
    {
        // 检查手雷数量上限
        int maxGrenades = 3; // 手雷上限
        if (pulseGrenadeCount < maxGrenades)
        {
            pulseGrenadeCount++;
            OnPowerUpCountChanged?.Invoke(PowerUpType.PulseGrenade, pulseGrenadeCount);
            Debug.Log($"获得脉冲手雷，当前数量: {pulseGrenadeCount}/{maxGrenades}");
        }
        else
        {
            Debug.Log($"手雷数量已达上限 ({maxGrenades})，无法获得更多手雷");
        }
    }
    
    /// <summary>
    /// 使用脉冲手雷（由手势触发）
    /// </summary>
    public void UsePulseGrenade()
    {
        if (pulseGrenadeCount <= 0) 
        {
            Debug.Log("没有脉冲手雷可用");
            return;
        }
        
        PowerUpData pulseConfig = GetPowerUpConfig(PowerUpType.PulseGrenade);
        if (pulseConfig == null) 
        {
            Debug.LogError("脉冲手雷配置未找到");
            return;
        }
        
        pulseGrenadeCount--;
        OnPowerUpCountChanged?.Invoke(PowerUpType.PulseGrenade, pulseGrenadeCount);
        
        
        // 对所有敌人造成伤害
        DealPulseDamageToAllEnemies(pulseConfig.pulseDamage);
        
        Debug.Log($"使用脉冲手雷，剩余数量: {pulseGrenadeCount}");
    }
    
    /// <summary>
    /// 对所有敌人造成脉冲伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    private void DealPulseDamageToAllEnemies(float damage)
    {
        // 对普通敌人造成伤害
        EnemyWithNewAttackSystem[] enemies = FindObjectsOfType<EnemyWithNewAttackSystem>();
        foreach (var enemy in enemies)
        {
            if (enemy != null && !enemy.IsDead)
            {
                enemy.TakeDamage(damage);
            }
        }
        
        // 对Boss造成伤害
        BossController[] bosses = FindObjectsOfType<BossController>();
        foreach (var boss in bosses)
        {
            if (boss != null && !boss.IsDead)
            {
                boss.TakeDamage(damage);
            }
        }
        
        Debug.Log($"脉冲手雷对 {enemies.Length} 个敌人和 {bosses.Length} 个Boss造成了 {damage} 点伤害");
    }
    
    /// <summary>
    /// 连射协程
    /// </summary>
    /// <param name="interval">射击间隔</param>
    /// <returns></returns>
    private IEnumerator RapidFireCoroutine(float interval)
    {
        isRapidFireActive = true;
        
        while (GetActivePowerUp(PowerUpType.RapidFire) != null)
        {
            // 自动射击
            if (crosshair != null)
            {
                crosshair.ActivateCollider();
            }
            
            yield return new WaitForSeconds(interval);
        }
        
        isRapidFireActive = false;
        rapidFireCoroutine = null;
    }
    
    /// <summary>
    /// 更新激活的道具效果
    /// </summary>
    private void UpdateActivePowerUps()
    {
        for (int i = activePowerUps.Count - 1; i >= 0; i--)
        {
            PowerUpEffect effect = activePowerUps[i];
            
            // 更新时间限制的道具
            if (effect.remainingTime > 0)
            {
                effect.remainingTime -= Time.deltaTime;
                
                if (effect.remainingTime <= 0)
                {
                    // 道具效果过期
                    ExpirePowerUp(effect);
                    activePowerUps.RemoveAt(i);
                }
            }
        }
    }
    
    /// <summary>
    /// 道具效果过期
    /// </summary>
    /// <param name="effect">道具效果</param>
    private void ExpirePowerUp(PowerUpEffect effect)
    {
        switch (effect.powerUpType)
        {
            case PowerUpType.Shotgun:
                // 恢复准星大小
                if (crosshair != null)
                {
                    PowerUpData config = GetPowerUpConfig(PowerUpType.Shotgun);
                    if (config != null)
                    {
                        crosshair.transform.localScale /= config.shotgunScaleMultiplier;
                    }
                }
                break;
                
            case PowerUpType.RapidFire:
                // 停止连射
                if (rapidFireCoroutine != null)
                {
                    StopCoroutine(rapidFireCoroutine);
                    rapidFireCoroutine = null;
                }
                isRapidFireActive = false;
                break;
        }
        
        OnPowerUpExpired?.Invoke(effect.powerUpType);
        Debug.Log($"道具效果过期: {effect.powerUpType}");
    }
    
    /// <summary>
    /// 消耗霰弹弹药
    /// </summary>
    public void ConsumeShotgunAmmo()
    {
        PowerUpEffect shotgunEffect = GetActivePowerUp(PowerUpType.Shotgun);
        if (shotgunEffect != null && shotgunEffect.remainingCount > 0)
        {
            shotgunEffect.remainingCount--;
            OnPowerUpCountChanged?.Invoke(PowerUpType.Shotgun, shotgunEffect.remainingCount);
            
            if (shotgunEffect.remainingCount <= 0)
            {
                // 弹药用完，移除效果
                ExpirePowerUp(shotgunEffect);
                activePowerUps.Remove(shotgunEffect);
            }
        }
    }
    
    /// <summary>
    /// 获取激活的道具效果
    /// </summary>
    /// <param name="powerUpType">道具类型</param>
    /// <returns>道具效果</returns>
    private PowerUpEffect GetActivePowerUp(PowerUpType powerUpType)
    {
        return activePowerUps.Find(effect => effect.powerUpType == powerUpType);
    }
    
    /// <summary>
    /// 获取道具配置
    /// </summary>
    /// <param name="powerUpType">道具类型</param>
    /// <returns>道具配置</returns>
    private PowerUpData GetPowerUpConfig(PowerUpType powerUpType)
    {
        return powerUpConfigs.Find(config => config.powerUpType == powerUpType);
    }
    
    /// <summary>
    /// 检查是否有激活的霰弹效果
    /// </summary>
    /// <returns>是否有霰弹效果</returns>
    public bool HasShotgunEffect()
    {
        PowerUpEffect effect = GetActivePowerUp(PowerUpType.Shotgun);
        return effect != null && effect.remainingCount > 0;
    }
    
    /// <summary>
    /// 检查是否有激活的连射效果
    /// </summary>
    /// <returns>是否有连射效果</returns>
    public bool HasRapidFireEffect()
    {
        return GetActivePowerUp(PowerUpType.RapidFire) != null;
    }
    
    /// <summary>
    /// 获取脉冲手雷数量
    /// </summary>
    /// <returns>脉冲手雷数量</returns>
    public int GetPulseGrenadeCount()
    {
        return pulseGrenadeCount;
    }
    
    /// <summary>
    /// 获取霰弹剩余弹药
    /// </summary>
    /// <returns>剩余弹药数</returns>
    public int GetShotgunAmmo()
    {
        PowerUpEffect effect = GetActivePowerUp(PowerUpType.Shotgun);
        return effect != null ? effect.remainingCount : 0;
    }
    
    /// <summary>
    /// 获取连射剩余时间
    /// </summary>
    /// <returns>剩余时间</returns>
    public float GetRapidFireTime()
    {
        PowerUpEffect effect = GetActivePowerUp(PowerUpType.RapidFire);
        return effect != null ? effect.remainingTime : 0f;
    }
    
    /// <summary>
    /// 获取连射最大时间
    /// </summary>
    /// <returns>连射最大时间</returns>
    public float GetRapidFireMaxTime()
    {
        PowerUpData config = GetPowerUpConfig(PowerUpType.RapidFire);
        return config != null ? config.rapidFireDuration : 10f;
    }
    
    /// <summary>
    /// 获取手雷数量上限
    /// </summary>
    /// <returns>手雷数量上限</returns>
    public int GetMaxGrenadeCount()
    {
        return 3; // 手雷上限
    }
    
    /// <summary>
    /// 检查是否达到手雷上限
    /// </summary>
    /// <returns>是否达到上限</returns>
    public bool IsGrenadeAtMaxCapacity()
    {
        return pulseGrenadeCount >= GetMaxGrenadeCount();
    }
    
    /// <summary>
    /// 检查是否有脉冲手雷可用
    /// </summary>
    /// <returns>是否有脉冲手雷可用</returns>
    public bool HasPulseGrenadeAvailable()
    {
        return pulseGrenadeCount > 0;
    }
    
    /// <summary>
    /// 获取脉冲手雷状态信息
    /// </summary>
    /// <returns>脉冲手雷状态字符串</returns>
    public string GetPulseGrenadeStatus()
    {
        if (pulseGrenadeCount <= 0)
        {
            return "没有脉冲手雷";
        }
        else
        {
            return $"脉冲手雷: {pulseGrenadeCount}个";
        }
    }
    
    // 编辑器辅助方法
    [ContextMenu("Test Drop Power Up")]
    public void TestDropPowerUp()
    {
        if (player != null)
        {
            TryDropPowerUp(player.transform.position + Vector3.up * 2f);
        }
    }
    
    [ContextMenu("Test Use Pulse Grenade")]
    public void TestUsePulseGrenade()
    {
        UsePulseGrenade();
    }

    [ContextMenu("Add Pulse Grenade")]
    public void TestAddPulseGrenade()
    {
        if (pulseGrenadeCount < GetMaxGrenadeCount())
        {
            pulseGrenadeCount++;
            OnPowerUpCountChanged?.Invoke(PowerUpType.PulseGrenade, pulseGrenadeCount);
            Debug.Log($"添加脉冲手雷，当前数量: {pulseGrenadeCount}");
        }
        else
        {
            Debug.Log("脉冲手雷数量已达上限");
        }
    }

    [ContextMenu("Test Gesture Detection")]
    public void TestGestureDetection()
    {
        // 模拟手势检测
        OnHandGestureChanged(HandGestureState.PulseGrenade, 1);
    }

    [ContextMenu("Show Power Up Status")]
    public void ShowPowerUpStatus()
    {
        Debug.Log($"霰弹弹药: {GetShotgunAmmo()}");
        Debug.Log($"连射剩余时间: {GetRapidFireTime():F1}秒");
        Debug.Log($"脉冲手雷: {pulseGrenadeCount}个");
        Debug.Log($"脉冲手雷可用: {HasPulseGrenadeAvailable()}");
    }

    /// <summary>
    /// 设置手势检测
    /// </summary>
    private void SetupGestureDetection()
    {
        // 获取InputManager_Hand实例
        InputManager_Hand inputManager = InputManager_Hand.Instance;
        if (inputManager != null)
        {
            // 订阅手势状态变化事件
            inputManager.OnCurrentHandGestureChanged += OnHandGestureChanged;
            Debug.Log("PowerUpManager: 已订阅手势检测事件");
        }
        else
        {
            Debug.LogWarning("PowerUpManager: InputManager_Hand实例未找到，无法检测脉冲手雷手势");
        }
    }

    /// <summary>
    /// 手势状态变化回调
    /// </summary>
    /// <param name="newGesture">新的手势状态</param>
    /// <param name="handIndex">手部索引(1=左手，2=右手)</param>
    private void OnHandGestureChanged(HandGestureState newGesture, int handIndex)
    {
        // 检测脉冲手雷手势 (PulseGrenade = 3)
        if (newGesture == HandGestureState.PulseGrenade)
        {
            // 检查是否有脉冲手雷可以使用
            if (pulseGrenadeCount > 0)
            {
                UsePulseGrenade();
                Debug.Log($"检测到脉冲手雷手势，使用脉冲手雷，剩余数量: {pulseGrenadeCount}");
            }
            else
            {
                Debug.Log("检测到脉冲手雷手势，但没有脉冲手雷可用");
            }
        }
    }

    // 在OnDestroy方法中添加事件取消订阅
    private void OnDestroy()
    {
        // 取消手势检测事件订阅
        InputManager_Hand inputManager = InputManager_Hand.Instance;
        if (inputManager != null)
        {
            inputManager.OnCurrentHandGestureChanged -= OnHandGestureChanged;
            //Debug.Log("PowerUpManager: 已取消手势检测事件订阅");
        }
    }
}

