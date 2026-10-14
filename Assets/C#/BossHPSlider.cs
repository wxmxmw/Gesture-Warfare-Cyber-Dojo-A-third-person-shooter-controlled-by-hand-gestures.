using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossHPSlider : MonoBehaviour
{
    [Header("UI References")]
    public Image HealthBar;         // 血条Image
    public Image HealthBar_Damage;  // 伤害条Image
    public Image HealthBar_Heal;    // 恢复条Image
    public Image HealthBar_BackGround;// 血条背景Image
    public TMP_Text Health_Text;    // Health文本
    public TMP_Text Boss_Name_Text; // Boss名称文本（可选）
    public GameObject BossUI_Panel; // Boss UI面板（用于显示/隐藏整个UI）
    
    [Header("Boss Reference")]
    [SerializeField] private BossController targetBoss; // 目标Boss引用
    
    [Header("Animation Settings")]
    public float FadeTime = 1f;     // 渐变时间
    
    [Header("Auto Detection Settings")]
    [SerializeField] private bool autoFindBoss = true; // 是否自动查找Boss
    [SerializeField] private float detectionInterval = 0.5f; // 检测间隔
    
    // 私有变量
    private bool startDamage = false;
    private bool startHeal = false;
    [SerializeField]
    private float temp;
    
    // Boss血量相关
    private float lastBossHealth = 0f;
    private float lastBossMaxHealth = 0f;
    
    // Boss检测相关
    private float detectionTimer = 0f;
    
    // UI显示状态
    private bool isBossUIVisible = true;
    
    // 属性
    public BossController TargetBoss 
    { 
        get => targetBoss; 
        set 
        {
            if (targetBoss != null)
            {
                // 取消订阅旧Boss的事件
                UnsubscribeFromBossEvents();
            }
            
            targetBoss = value;
            
            if (targetBoss != null)
            {
                // 订阅新Boss的事件
                SubscribeToBossEvents();
                // 立即更新显示
                UpdateHealthDisplay();
                ShowBossUI();
            }
            else
            {
                HideBossUI();
            }
        }
    }

    private void Start()
    {
        // 初始时隐藏Boss UI
        HideBossUI();
        
        // 如果启用自动查找Boss
        if (autoFindBoss)
        {
            FindActiveBoss();
        }
        else if (targetBoss != null)
        {
            // 如果手动指定了Boss，立即初始化
            SubscribeToBossEvents();
            UpdateHealthDisplay();
            ShowBossUI();
        }
        
        // 初始化UI显示
        InitializeUI();
    }

    private void Update()
    {
        // 自动检测Boss
        if (autoFindBoss)
        {
            detectionTimer += Time.deltaTime;
            if (detectionTimer >= detectionInterval)
            {
                detectionTimer = 0f;
                CheckForBossChanges();
            }
        }
        
        // 检查Boss血量变化
        CheckHealthChanges();
        
        // 更新动画（只有在有Boss时才更新）
        if (targetBoss != null && isBossUIVisible)
        {
            FadeValue(HealthBar.fillAmount, FadeTime);
            FadeHeal(HealthBar_Heal.fillAmount, FadeTime);
        }
    }
    
    /// <summary>
    /// 查找活跃的Boss
    /// </summary>
    private void FindActiveBoss()
    {
        BossController[] allBosses = FindObjectsOfType<BossController>();
        BossController activeBoss = null;
        
        foreach (var boss in allBosses)
        {
            if (boss != null && !boss.IsDead)
            {
                activeBoss = boss;
                break; // 找到第一个活跃的Boss就使用它
            }
        }
        
        // 更新目标Boss
        if (activeBoss != targetBoss)
        {
            TargetBoss = activeBoss; // 这会自动处理UI的显示/隐藏
            
            if (activeBoss != null)
            {
                Debug.Log($"BossHPSlider: 找到活跃Boss - {activeBoss.name}");
            }
            else
            {
                Debug.Log("BossHPSlider: 没有找到活跃的Boss，隐藏血条UI");
            }
        }
    }
    
    /// <summary>
    /// 检查Boss变化
    /// </summary>
    private void CheckForBossChanges()
    {
        // 如果当前Boss已死亡或被销毁，查找新的Boss
        if (targetBoss == null || targetBoss.IsDead)
        {
            FindActiveBoss();
        }
    }
    
    /// <summary>
    /// 订阅Boss事件
    /// </summary>
    private void SubscribeToBossEvents()
    {
        if (targetBoss != null)
        {
            targetBoss.OnBossDamaged += OnBossDamaged;
            targetBoss.OnBossDeath += OnBossDeath;
        }
    }
    
    /// <summary>
    /// 取消订阅Boss事件
    /// </summary>
    private void UnsubscribeFromBossEvents()
    {
        if (targetBoss != null)
        {
            targetBoss.OnBossDamaged -= OnBossDamaged;
            targetBoss.OnBossDeath -= OnBossDeath;
        }
    }
    
    /// <summary>
    /// 显示Boss UI
    /// </summary>
    private void ShowBossUI()
    {
        if (!isBossUIVisible)
        {
            if (BossUI_Panel != null)
            {
                BossUI_Panel.SetActive(true);
            }
            else
            {
                // 如果没有指定面板，直接控制各个UI元素
                SetUIElementsActive(true);
            }
            
            isBossUIVisible = true;
            Debug.Log("BossHPSlider: 显示Boss血条UI");
        }
    }
    
    /// <summary>
    /// 隐藏Boss UI
    /// </summary>
    private void HideBossUI()
    {
        if (isBossUIVisible)
        {
            if (BossUI_Panel != null)
            {
                BossUI_Panel.SetActive(false);
            }
            else
            {
                // 如果没有指定面板，直接控制各个UI元素
                SetUIElementsActive(false);
            }
            
            isBossUIVisible = false;
            Debug.Log("BossHPSlider: 隐藏Boss血条UI");
        }
    }
    
    /// <summary>
    /// 设置UI元素的激活状态
    /// </summary>
    private void SetUIElementsActive(bool active)
    {
        if (HealthBar != null) HealthBar.gameObject.SetActive(active);
        if (HealthBar_Damage != null) HealthBar_Damage.gameObject.SetActive(active);
        if (HealthBar_Heal != null) HealthBar_Heal.gameObject.SetActive(active);
        if (HealthBar_BackGround != null) HealthBar_BackGround.gameObject.SetActive(active);
        if (Health_Text != null) Health_Text.gameObject.SetActive(active);
        if (Boss_Name_Text != null) Boss_Name_Text.gameObject.SetActive(active);
    }
    
    /// <summary>
    /// 初始化UI
    /// </summary>
    private void InitializeUI()
    {
        if (targetBoss != null)
        {
            float healthPercentage = targetBoss.HealthPercentage;
            HealthBar.fillAmount = healthPercentage;
            HealthBar_Damage.fillAmount = healthPercentage;
            HealthBar_Heal.fillAmount = healthPercentage;
            
            UpdateHealthText();
            UpdateBossNameText();
            
            lastBossHealth = targetBoss.CurrentHealth;
            lastBossMaxHealth = targetBoss.MaxHealth;
        }
        else
        {
            // 如果没有Boss，显示默认值
            if (HealthBar != null) HealthBar.fillAmount = 1f;
            if (HealthBar_Damage != null) HealthBar_Damage.fillAmount = 1f;
            if (HealthBar_Heal != null) HealthBar_Heal.fillAmount = 1f;
            if (Health_Text != null) Health_Text.text = "HP --/--";
            if (Boss_Name_Text != null) Boss_Name_Text.text = "Boss";
        }
    }
    
    /// <summary>
    /// 检查血量变化
    /// </summary>
    private void CheckHealthChanges()
    {
        if (targetBoss == null) return;
        
        // 检查最大血量是否变化
        if (lastBossMaxHealth != targetBoss.MaxHealth)
        {
            lastBossMaxHealth = targetBoss.MaxHealth;
            UpdateHealthDisplay();
        }
        
        // 检查当前血量是否变化
        if (lastBossHealth != targetBoss.CurrentHealth)
        {
            float healthChange = targetBoss.CurrentHealth - lastBossHealth;
            lastBossHealth = targetBoss.CurrentHealth;
            
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
    /// Boss受伤事件回调
    /// </summary>
    private void OnBossDamaged(BossController boss, float damage)
    {
        OnHealthDecreased(damage);
    }
    
    /// <summary>
    /// Boss死亡事件回调
    /// </summary>
    private void OnBossDeath(BossController boss)
    {
        UpdateHealthDisplay();
        Debug.Log($"BossHPSlider: Boss {boss.name} 死亡，血量条更新");
        
        // Boss死亡后，延迟查找新Boss
        StartCoroutine(DelayedBossCheck());
    }
    
    /// <summary>
    /// 延迟检查Boss
    /// </summary>
    private System.Collections.IEnumerator DelayedBossCheck()
    {
        yield return new WaitForSeconds(1f); // 等待1秒后再检查
        
        if (autoFindBoss)
        {
            FindActiveBoss();
        }
        else
        {
            TargetBoss = null; // 这会自动隐藏UI
        }
    }
    
    /// <summary>
    /// 血量减少处理
    /// </summary>
    private void OnHealthDecreased(float damage)
    {
        if (targetBoss == null) return;
        
        // 立即更新血条和恢复条
        HealthBar.fillAmount = targetBoss.HealthPercentage;
        HealthBar_Heal.fillAmount = HealthBar.fillAmount;
        
        // 计算伤害条需要减少的量
        temp = HealthBar_Damage.fillAmount - HealthBar.fillAmount;
        startDamage = true;
        
        // 更新文本
        UpdateHealthText();
        
    }
    
    /// <summary>
    /// 血量增加处理
    /// </summary>
    private void OnHealthIncreased(float healAmount)
    {
        if (targetBoss == null) return;
        
        // 立即更新恢复条和伤害条
        HealthBar_Heal.fillAmount = targetBoss.HealthPercentage;
        HealthBar_Damage.fillAmount = HealthBar_Heal.fillAmount;
        
        // 计算血条需要增加的量
        temp = HealthBar_Heal.fillAmount - HealthBar.fillAmount;
        startHeal = true;
        
        // 更新文本
        UpdateHealthText();
        
        Debug.Log($"BossHPSlider: Boss恢复 {healAmount} 点血量");
    }
    
    /// <summary>
    /// 更新血量显示
    /// </summary>
    private void UpdateHealthDisplay()
    {
        if (targetBoss == null) return;
        
        float healthPercentage = targetBoss.HealthPercentage;
        HealthBar.fillAmount = healthPercentage;
        HealthBar_Damage.fillAmount = healthPercentage;
        HealthBar_Heal.fillAmount = healthPercentage;
        UpdateHealthText();
        UpdateBossNameText();
    }
    
    /// <summary>
    /// 更新血量文本
    /// </summary>
    private void UpdateHealthText()
    {
        if (Health_Text != null)
        {
            if (targetBoss != null)
            {
                Health_Text.text = "HP " + Mathf.RoundToInt(targetBoss.CurrentHealth) + "/" + Mathf.RoundToInt(targetBoss.MaxHealth);
            }
            else
            {
                Health_Text.text = "HP --/--";
            }
        }
    }
    
    /// <summary>
    /// 更新Boss名称文本
    /// </summary>
    private void UpdateBossNameText()
    {
        if (Boss_Name_Text != null)
        {
            if (targetBoss != null)
            {
                Boss_Name_Text.text = targetBoss.name.Replace("(Clone)", "").Trim();
            }
            else
            {
                Boss_Name_Text.text = "Boss";
            }
        }
    }

    // 伤害条缓变
    public void FadeValue(float endValue, float duration)
    {
        if (startDamage)
        {
            HealthBar_Damage.fillAmount -= (temp / duration) * Time.deltaTime;
            if (HealthBar_Damage.fillAmount <= endValue)
            {
                startDamage = false;
            }
        }
    }
    
    // 血条缓变
    public void FadeHeal(float endValue, float duration)
    {
        if (startHeal)
        {
            HealthBar.fillAmount += (temp / duration) * Time.deltaTime;
            if (HealthBar.fillAmount >= endValue)
            {
                startHeal = false;
            }
        }
    }
    
    /// <summary>
    /// 手动设置目标Boss
    /// </summary>
    /// <param name="boss">Boss引用</param>
    public void SetTargetBoss(BossController boss)
    {
        TargetBoss = boss;
    }
    
    /// <summary>
    /// 强制刷新血量显示
    /// </summary>
    public void RefreshHealthDisplay()
    {
        if (targetBoss != null)
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
    
    /// <summary>
    /// 设置是否自动查找Boss
    /// </summary>
    /// <param name="autoFind">是否自动查找</param>
    public void SetAutoFindBoss(bool autoFind)
    {
        autoFindBoss = autoFind;
        if (autoFind)
        {
            FindActiveBoss();
        }
    }
    
    /// <summary>
    /// 设置检测间隔
    /// </summary>
    /// <param name="interval">检测间隔</param>
    public void SetDetectionInterval(float interval)
    {
        detectionInterval = Mathf.Max(0.1f, interval);
    }
    
    /// <summary>
    /// 获取当前是否有活跃Boss
    /// </summary>
    /// <returns>是否有活跃Boss</returns>
    public bool HasActiveBoss()
    {
        return targetBoss != null && !targetBoss.IsDead;
    }
    
    /// <summary>
    /// 强制检查Boss状态并更新UI显示
    /// </summary>
    public void ForceCheckBossStatus()
    {
        if (autoFindBoss)
        {
            FindActiveBoss();
        }
        else if (targetBoss == null || targetBoss.IsDead)
        {
            HideBossUI();
        }
    }
    
    /// <summary>
    /// 强制隐藏UI（用于外部调用）
    /// </summary>
    public void ForceHideUI()
    {
        TargetBoss = null; // 这会自动隐藏UI
    }
    
    // 编辑器方法
    [ContextMenu("查找Boss")]
    public void FindBossEditor()
    {
        FindActiveBoss();
    }
    
    [ContextMenu("刷新血量显示")]
    public void RefreshDisplayEditor()
    {
        RefreshHealthDisplay();
    }
    
    [ContextMenu("测试Boss伤害")]
    public void TestBossDamage()
    {
        if (targetBoss != null)
        {
            targetBoss.TakeDamage(50f);
        }
    }
    
    [ContextMenu("显示Boss UI")]
    public void ShowBossUIEditor()
    {
        ShowBossUI();
        InitializeUI();
    }
    
    [ContextMenu("隐藏Boss UI")]
    public void HideBossUIEditor()
    {
        HideBossUI();
    }
    
    private void OnDestroy()
    {
        // 清理事件订阅
        UnsubscribeFromBossEvents();
    }
    
    private void OnDisable()
    {
        // 当组件被禁用时，清理事件订阅
        UnsubscribeFromBossEvents();
    }
    
    private void OnEnable()
    {
        // 当组件被启用时，重新订阅事件（如果有目标Boss）
        if (targetBoss != null)
        {
            SubscribeToBossEvents();
            UpdateHealthDisplay();
        }
    }
}