using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HPSilder : MonoBehaviour
{
    [Header("UI References")]
    public Image HealthBar;     //血条Image
    public Image HealthBar_Damage;      //伤害条Image
    public Image HealthBar_Heal;       //恢复条Image
    public TMP_Text Health_Text;        //Health文本
    
    [Header("Player Reference")]
    [SerializeField] private Player targetPlayer; // 目标玩家引用
    
    [Header("Animation Settings")]
    public float FadeTime = 1f;      //渐变时间
    
    // 私有变量
    private bool startDamage = false;
    private bool startHeal = false;
    [SerializeField]
    private float temp;
    
    // 玩家血量相关
    private float lastPlayerHealth = 0f;
    private float lastPlayerMaxHealth = 0f;
    
    // 属性
    public Player TargetPlayer 
    { 
        get => targetPlayer; 
        set 
        {
            if (targetPlayer != null)
            {
                // 取消订阅旧玩家的事件
                UnsubscribeFromPlayerEvents();
            }
            
            targetPlayer = value;
            
            if (targetPlayer != null)
            {
                // 订阅新玩家的事件
                SubscribeToPlayerEvents();
                // 立即更新显示
                UpdateHealthDisplay();
            }
        }
    }

    private void Start()
    {
        // 如果没有手动指定玩家，尝试自动查找
        if (targetPlayer == null)
        {
            FindPlayer();
        }
        
        // 初始化UI显示
        InitializeUI();
    }

    private void Update()
    {
        // 检查玩家血量变化
        CheckHealthChanges();
        
        // 更新动画
        FadeValue(HealthBar.fillAmount, FadeTime);
        FadeHeal(HealthBar_Heal.fillAmount, FadeTime);
    }
    
    /// <summary>
    /// 查找玩家
    /// </summary>
    private void FindPlayer()
    {
        // 首先尝试从GameManager获取
        if (GameManager.Instance != null && GameManager.Instance.Player != null)
        {
            targetPlayer = GameManager.Instance.Player;
        }
        else
        {
            // 如果GameManager中没有，尝试直接查找
            targetPlayer = FindObjectOfType<Player>();
        }
        
        if (targetPlayer != null)
        {
            SubscribeToPlayerEvents();
            UpdateHealthDisplay();
            Debug.Log("HPSilder: 找到玩家，开始监听血量变化");
        }
        else
        {
            Debug.LogWarning("HPSilder: 未找到玩家！");
        }
    }
    
    /// <summary>
    /// 订阅玩家事件
    /// </summary>
    private void SubscribeToPlayerEvents()
    {
        if (targetPlayer != null)
        {
            targetPlayer.OnPlayerDamaged += OnPlayerDamaged;
            targetPlayer.OnPlayerHealed += OnPlayerHealed;
            targetPlayer.OnPlayerDeath += OnPlayerDeath;
        }
    }
    
    /// <summary>
    /// 取消订阅玩家事件
    /// </summary>
    private void UnsubscribeFromPlayerEvents()
    {
        if (targetPlayer != null)
        {
            targetPlayer.OnPlayerDamaged -= OnPlayerDamaged;
            targetPlayer.OnPlayerHealed -= OnPlayerHealed;
            targetPlayer.OnPlayerDeath -= OnPlayerDeath;
        }
    }
    
    /// <summary>
    /// 初始化UI
    /// </summary>
    private void InitializeUI()
    {
        if (targetPlayer != null)
        {
            float healthPercentage = targetPlayer.HealthPercentage;
            HealthBar.fillAmount = healthPercentage;
            HealthBar_Damage.fillAmount = healthPercentage;
            HealthBar_Heal.fillAmount = healthPercentage;
            Health_Text.text = "HP " + Mathf.RoundToInt(targetPlayer.CurrentHealth) + "/" + Mathf.RoundToInt(targetPlayer.MaxHealth);
            
            lastPlayerHealth = targetPlayer.CurrentHealth;
            lastPlayerMaxHealth = targetPlayer.MaxHealth;
        }
        else
        {
            // 如果没有玩家，显示默认值
            HealthBar.fillAmount = 1f;
            HealthBar_Damage.fillAmount = 1f;
            HealthBar_Heal.fillAmount = 1f;
            Health_Text.text = "HP --/--";
        }
    }
    
    /// <summary>
    /// 检查血量变化
    /// </summary>
    private void CheckHealthChanges()
    {
        if (targetPlayer == null) return;
        
        // 检查最大血量是否变化
        if (lastPlayerMaxHealth != targetPlayer.MaxHealth)
        {
            lastPlayerMaxHealth = targetPlayer.MaxHealth;
            UpdateHealthDisplay();
        }
        
        // 检查当前血量是否变化
        if (lastPlayerHealth != targetPlayer.CurrentHealth)
        {
            float healthChange = targetPlayer.CurrentHealth - lastPlayerHealth;
            lastPlayerHealth = targetPlayer.CurrentHealth;
            
            if (healthChange < 0)
            {
                // 受到伤害
                OnHealthDecreased(Mathf.Abs(healthChange));
            }
            else if (healthChange > 0)
            {
                // 受到治疗
                OnHealthIncreased(healthChange);
            }
        }
    }
    
    /// <summary>
    /// 玩家受伤事件回调
    /// </summary>
    private void OnPlayerDamaged(Player player, float damage)
    {
        OnHealthDecreased(damage);
    }
    
    /// <summary>
    /// 玩家治疗事件回调
    /// </summary>
    private void OnPlayerHealed(Player player, float healAmount)
    {
        OnHealthIncreased(healAmount);
    }
    
    /// <summary>
    /// 玩家死亡事件回调
    /// </summary>
    private void OnPlayerDeath(Player player)
    {
        UpdateHealthDisplay();
        Debug.Log("HPSilder: 玩家死亡，血量条更新");
    }
    
    /// <summary>
    /// 血量减少处理
    /// </summary>
    private void OnHealthDecreased(float damage)
    {
        if (targetPlayer == null) return;
        
        // 立即更新血条和恢复条
        HealthBar.fillAmount = targetPlayer.HealthPercentage;
        HealthBar_Heal.fillAmount = HealthBar.fillAmount;
        
        // 计算伤害条需要减少的量
        temp = HealthBar_Damage.fillAmount - HealthBar.fillAmount;
        startDamage = true;
        
        // 更新文本
        UpdateHealthText();
        
        //Debug.Log($"HPSilder: 玩家受到 {damage} 点伤害");
    }
    
    /// <summary>
    /// 血量增加处理
    /// </summary>
    private void OnHealthIncreased(float healAmount)
    {
        if (targetPlayer == null) return;
        
        // 立即更新恢复条和伤害条
        HealthBar_Heal.fillAmount = targetPlayer.HealthPercentage;
        HealthBar_Damage.fillAmount = HealthBar_Heal.fillAmount;
        
        // 计算血条需要增加的量
        temp = HealthBar_Heal.fillAmount - HealthBar.fillAmount;
        startHeal = true;
        
        // 更新文本
        UpdateHealthText();
        
        Debug.Log($"HPSilder: 玩家恢复 {healAmount} 点血量");
    }
    
    /// <summary>
    /// 更新血量显示
    /// </summary>
    private void UpdateHealthDisplay()
    {
        if (targetPlayer == null) return;
        
        float healthPercentage = targetPlayer.HealthPercentage;
        HealthBar.fillAmount = healthPercentage;
        HealthBar_Damage.fillAmount = healthPercentage;
        HealthBar_Heal.fillAmount = healthPercentage;
        UpdateHealthText();
    }
    
    /// <summary>
    /// 更新血量文本
    /// </summary>
    private void UpdateHealthText()
    {
        if (targetPlayer != null)
        {
            Health_Text.text = "HP " + Mathf.RoundToInt(targetPlayer.CurrentHealth) + "/" + Mathf.RoundToInt(targetPlayer.MaxHealth);
        }
        else
        {
            Health_Text.text = "HP --/--";
        }
    }

    //伤害条缓变
    public void FadeValue(float endValue, float duration)
    {
        if (startDamage)
        {
            HealthBar_Damage.fillAmount -= (temp / duration) * Time.deltaTime;    //temp/duration使用固定渐变的时间。
            if (HealthBar_Damage.fillAmount <= endValue)        //到达设定值，关闭渐变。
            {
                startDamage = false;
            }
        }
    }
    
    //血条条缓变
    public void FadeHeal(float endValue, float duration)
    {
        if (startHeal)
        {
            HealthBar.fillAmount += (temp / duration) * Time.deltaTime;    //temp/duration使用固定渐变的时间。
            if (HealthBar.fillAmount >= endValue)        //到达设定值，关闭渐变。
            {
                startHeal = false;
            }
        }
    }
    
    /// <summary>
    /// 手动设置目标玩家
    /// </summary>
    /// <param name="player">玩家引用</param>
    public void SetTargetPlayer(Player player)
    {
        TargetPlayer = player;
    }
    
    /// <summary>
    /// 强制刷新血量显示
    /// </summary>
    public void RefreshHealthDisplay()
    {
        if (targetPlayer != null)
        {
            UpdateHealthDisplay();
        }
    }
    
    /// <summary>
    /// 设置渐变时间
    /// </summary>
    /// <param name="fadeTime">渐变时间</param>
    public void SetFadeTime(float fadeTime)
    {
        FadeTime = Mathf.Max(0f, fadeTime);
    }
    
    // 编辑器方法
    [ContextMenu("查找玩家")]
    public void FindPlayerEditor()
    {
        FindPlayer();
    }
    
    [ContextMenu("刷新血量显示")]
    public void RefreshDisplayEditor()
    {
        RefreshHealthDisplay();
    }
    
    [ContextMenu("测试伤害")]
    public void TestDamage()
    {
        if (targetPlayer != null)
        {
            targetPlayer.TakeDamage(20f);
        }
    }
    
    [ContextMenu("测试治疗")]
    public void TestHeal()
    {
        if (targetPlayer != null)
        {
            targetPlayer.Heal(20f);
        }
    }
    
    private void OnDestroy()
    {
        // 清理事件订阅
        UnsubscribeFromPlayerEvents();
    }
}
