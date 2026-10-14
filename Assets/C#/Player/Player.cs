using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    [Header("Player Identity")]
    [SerializeField] private string playerName = "Player";
    
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    private Vector3 lastValidPosition; // 保存最后一个有效位置
    private Vector3 deathPosition; // 死亡位置，用于复活

    private bool isDead = false;
    private SpriteRenderer playerSprite;
    private Collider2D playerCollider2D;

    // 事件
    public System.Action<Player> OnPlayerDeath;
    public System.Action<Player, float> OnPlayerDamaged;
    public System.Action<Player, float> OnPlayerHealed;
    public System.Action<Player> OnPlayerRevived; // 新增复活事件
    
    // 属性
    public bool IsDead => isDead;
    public float HealthPercentage => currentHealth / maxHealth;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public string PlayerName => playerName;
    public Vector3 DeathPosition => deathPosition; // 死亡位置属性
    
    private void Awake()
    {
        InitializePlayer();
    }
    
    private void Start()
    {
    }

    private void Update()
    {
        // 如果玩家死亡，不处理移动
        if (isDead) return;
        
        // 检查是否有手部数据可用
        if (InputManager_Hand.Instance != null && (InputManager_Hand.Instance.HasHandData(1) || InputManager_Hand.Instance.HasHandData(2)))
        {
            Vector3 newPosition = InputManager_Hand.Instance.GetPlayerPosition();
            if (newPosition.x != 0)
            {
                // 更新位置并保存为最后有效位置
                transform.position = newPosition;
                lastValidPosition = newPosition;
            }
            else//手腕出屏幕
            {
                transform.position = lastValidPosition;
            }
        }
        else
        {
            // 当手部数据不可用时，保持在最后有效位置
            transform.position = lastValidPosition;
        }
    }

    private void InitializePlayer()
    {
        currentHealth = maxHealth;
        lastValidPosition = transform.position; // 初始化为当前位置

        // 获取或创建SpriteRenderer
        playerSprite = GetComponent<SpriteRenderer>();
        if (playerSprite == null)
        {
            playerSprite = gameObject.AddComponent<SpriteRenderer>();
        }
        
        // 设置标签
        gameObject.tag = "Player";
        
        // 添加碰撞体
        if (playerCollider2D == null)
        {
            gameObject.GetComponent<CircleCollider2D>();
        }
    }
            
    // 公共方法
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        // 检查掩体保护
        if (CoverController.Instance != null)
        {
            // 获取玩家当前位置对应的网格索引
            int playerGridIndex = CoverController.Instance.GetGridIndexFromWorldPosition(transform.position);
                        
            // 检查该网格是否有掩体
            if (CoverController.Instance.ActiveCover.ContainsKey(playerGridIndex))
            {
                Cover protectingCover = CoverController.Instance.ActiveCover[playerGridIndex];
                                
                // 检查掩体是否有效且玩家在掩体范围内
                if (protectingCover.CurrentState != CoverState.Destroyed && 
                    protectingCover.IsPointInCover(transform.position))
                {
                    // 掩体保护玩家，伤害掩体而不是玩家
                    protectingCover.TakeDamage(damage);
                    
                    return; // 直接返回，不伤害玩家
                }
                else
                {
                    //Debug.Log($"掩体无效或玩家不在掩体范围内");
                }
            }
            else
            {
            }
        }
        
        // 没有掩体保护，直接伤害玩家
        currentHealth -= damage;
        
        // 触发受伤事件
        OnPlayerDamaged?.Invoke(this, damage);
        
        // 检查死亡
        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            Die();
        }
        
    }
    
    public void Heal(float healAmount)
    {
        if (isDead) return;
        
        float oldHealth = currentHealth;
        currentHealth += healAmount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        
        float actualHeal = currentHealth - oldHealth;
        if (actualHeal > 0f)
        {
            OnPlayerHealed?.Invoke(this, actualHeal);
        }
    }
    
    public void SetMaxHealth(float newMaxHealth)
    {
        maxHealth = newMaxHealth;
        
        // 如果当前血量超过新的最大血量，调整当前血量
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
    }
    
    /// <summary>
    /// 复活玩家（在原地满血复活）
    /// </summary>
    public void Revive()
    {
        if (!isDead) return;
        
        // 恢复血量
        currentHealth = maxHealth;
        isDead = false;
        
        // 在死亡位置复活
        transform.position = deathPosition;
        lastValidPosition = deathPosition;
        
        // 恢复显示
        if (playerSprite != null)
        {
            playerSprite.enabled = true;
        }
        
        // 触发复活事件
        OnPlayerRevived?.Invoke(this);
        
        Debug.Log($"Player {playerName} has been revived at position {deathPosition}!");
    }
    
    /// <summary>
    /// 重头再来（重置到初始状态）
    /// </summary>
    public void RestartFromBeginning()
    {
        if (!isDead) return;
        
        // 恢复血量
        currentHealth = maxHealth;
        isDead = false;
        
        // 重置到初始位置
        transform.position = Vector3.zero;
        lastValidPosition = Vector3.zero;
        
        // 恢复显示
        if (playerSprite != null)
        {
            playerSprite.enabled = true;
        }
        
        // 触发复活事件
        OnPlayerRevived?.Invoke(this);
        
        Debug.Log($"Player {playerName} has restarted from the beginning!");
    }
    
    public void Respawn(Vector3 respawnPosition)
    {
        if (!isDead) return;
        
        transform.position = respawnPosition;
        currentHealth = maxHealth;
        isDead = false;
        
        // 恢复显示
        if (playerSprite != null)
        {
            playerSprite.enabled = true;
        }
        
        Debug.Log($"Player {playerName} has respawned!");
    }
    
    // 私有方法
    private void Die()
    {
        isDead = true;
        deathPosition = transform.position; // 记录死亡位置
        
        OnPlayerDeath?.Invoke(this);
        
        // 隐藏玩家
        if (playerSprite != null)
        {
            playerSprite.enabled = false;
        }
        
        Debug.Log($"Player {playerName} has died at position {deathPosition}!");
        
        // 移除这里的ShowGameOverUI调用，让GameManager统一处理
        // ShowGameOverUI();
    }
    
    // 移除ShowGameOverUI方法，因为GameManager会处理
    // private void ShowGameOverUI()
    // {
    //     if (GameManager.Instance != null)
    //     {
    //         GameManager.Instance.ShowGameOverUI();
    //     }
    // }
    
    // 编辑器方法
    [ContextMenu("Reset Health")]
    public void ResetHealth()
    {
        currentHealth = maxHealth;
    }
    
    [ContextMenu("Take 20 Damage")]
    public void TakeTestDamage()
    {
        TakeDamage(20f);
    }
    
    [ContextMenu("Heal 20 HP")]
    public void TakeTestHeal()
    {
        Heal(20f);
    }
    
    [ContextMenu("Test Death")]
    public void TestDeath()
    {
        currentHealth = 0f;
        Die();
    }
    
    [ContextMenu("Test Revive")]
    public void TestRevive()
    {
        Revive();
    }
}
