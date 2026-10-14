using UnityEngine;

/// <summary>
/// 掩体状态枚举
/// </summary>
public enum CoverState
{
    Perfect,    // 完好状态（血量50%以上）
    Damaged,    // 破损状态（血量50%以下）
    Destroyed   // 消失状态（血量≤0）
}

/// <summary>
/// 掩体类 - 管理单个掩体的血量、状态和渲染
/// </summary>
public class Cover : MonoBehaviour
{
    [Header("掩体属性")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    
    [Header("掩体外观设置")]
    [SerializeField] private Sprite perfectSprite;      // 完好状态贴图
    [SerializeField] private Sprite damagedSprite;      // 破损状态贴图
    [SerializeField] private float perfectHeight = 2f;  // 完好状态高度
    [SerializeField] private float damagedHeight = 1.5f; // 破损状态高度
    
    [Header("掩体位置")]
    [SerializeField] private int gridIndex;             // 掩体所在的网格索引（0-9）
    [SerializeField] private float gridWidth;          // 单个网格的宽度
    
    // 组件引用
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    
    // 当前状态
    private CoverState currentState = CoverState.Perfect;
    
    // 事件
    public System.Action<Cover> OnCoverDestroyed;
    public System.Action<Cover, float> OnCoverDamaged;
    public System.Action<Cover, CoverState> OnCoverStateChanged;
    
    // 属性
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercentage => currentHealth / maxHealth;
    public CoverState CurrentState => currentState;
    public int GridIndex => gridIndex;
    public float GridWidth => gridWidth;
    
    private void Awake()
    {
        InitializeCover();
    }
    
    /// <summary>
    /// 初始化掩体
    /// </summary>
    private void InitializeCover()
    {
        // 获取或添加组件
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        
        boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider2D>();
        }
        
        // 设置初始血量
        currentHealth = maxHealth;
        
        // 设置标签和层级
        gameObject.tag = "Cover";
        gameObject.layer = LayerMask.NameToLayer("Default");
        
        // 设置渲染层级（确保在玩家后面）
        spriteRenderer.sortingOrder = -1;
        
        // 初始化外观
        UpdateCoverAppearance();
    }
    
    /// <summary>
    /// 设置掩体参数
    /// </summary>
    /// <param name="index">网格索引</param>
    /// <param name="width">网格宽度</param>
    /// <param name="health">最大血量</param>
    public void SetupCover(int index, float width, float health = 100f)
    {
        gridIndex = index;
        gridWidth = width;
        maxHealth = health;
        currentHealth = maxHealth;
        
        // 设置位置
        UpdatePosition();
        
        // 更新外观
        UpdateCoverAppearance();
        
        Debug.Log($"掩体已设置：网格索引 {gridIndex}，宽度 {gridWidth}，血量 {maxHealth}");
    }
    
    /// <summary>
    /// 设置掩体贴图资源
    /// </summary>
    /// <param name="perfectSpr">完好状态贴图</param>
    /// <param name="damagedSpr">破损状态贴图</param>
    /// <param name="perfectH">完好状态高度</param>
    /// <param name="damagedH">破损状态高度</param>
    public void SetCoverSprites(Sprite perfectSpr, Sprite damagedSpr, float perfectH, float damagedH)
    {
        perfectSprite = perfectSpr;
        damagedSprite = damagedSpr;
        perfectHeight = perfectH;
        damagedHeight = damagedH;
        
        // 更新外观
        UpdateCoverAppearance();
    }
    
    /// <summary>
    /// 更新掩体位置
    /// </summary>
    private void UpdatePosition()
    {
        // 计算掩体在屏幕中的X位置（屏幕X坐标范围：0 到 Screen.width）
        float centerX = (gridIndex + 0.5f) * gridWidth;
        
        // 设置位置（Y坐标与玩家相同，Z坐标为0）
        Vector3 playerPos = FindObjectOfType<Player>()?.transform.position ?? Vector3.zero;
        transform.position = new Vector3(centerX, playerPos.y, 0f);
    }
    
    /// <summary>
    /// 更新掩体外观
    /// </summary>
    private void UpdateCoverAppearance()
    {
        if (spriteRenderer == null) return;
        
        // 设置Draw Mode为Sliced
        spriteRenderer.drawMode = SpriteDrawMode.Sliced;
        
        // 根据状态设置贴图和尺寸
        switch (currentState)
        {
            case CoverState.Perfect:
                spriteRenderer.sprite = perfectSprite;
                spriteRenderer.size = new Vector2(gridWidth, perfectHeight);
                break;
                
            case CoverState.Damaged:
                spriteRenderer.sprite = damagedSprite;
                spriteRenderer.size = new Vector2(gridWidth, damagedHeight);
                break;
                
            case CoverState.Destroyed:
                // 销毁状态下隐藏掩体
                gameObject.SetActive(false);
                break;
        }
        
        // 更新碰撞体尺寸
        if (boxCollider != null && currentState != CoverState.Destroyed)
        {
            boxCollider.size = spriteRenderer.size;
        }
    }
    
    /// <summary>
    /// 掩体受到伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    public void TakeDamage(float damage)
    {
        if (currentState == CoverState.Destroyed) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Max(0f, currentHealth);
        
        // 触发受伤事件
        OnCoverDamaged?.Invoke(this, damage);
        
        // 检查状态变化
        CoverState newState = CalculateCoverState();
        if (newState != currentState)
        {
            currentState = newState;
            OnCoverStateChanged?.Invoke(this, currentState);
            UpdateCoverAppearance();
            
            // 如果掩体被摧毁
            if (currentState == CoverState.Destroyed)
            {
                OnCoverDestroyed?.Invoke(this);
            }
        }
        
        //Debug.Log($"掩体 {gridIndex} 受到 {damage} 点伤害，剩余血量：{currentHealth}/{maxHealth}");
    }
    
    /// <summary>
    /// 根据血量计算掩体状态
    /// </summary>
    /// <returns>掩体状态</returns>
    private CoverState CalculateCoverState()
    {
        if (currentHealth <= 0f)
        {
            return CoverState.Destroyed;
        }
        else if (HealthPercentage < 0.5f)
        {
            return CoverState.Damaged;
        }
        else
        {
            return CoverState.Perfect;
        }
    }
    
    /// <summary>
    /// 修复掩体
    /// </summary>
    /// <param name="repairAmount">修复量</param>
    public void Repair(float repairAmount)
    {
        if (currentState == CoverState.Destroyed) return;
        
        float oldHealth = currentHealth;
        currentHealth += repairAmount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        
        // 检查状态变化
        CoverState newState = CalculateCoverState();
        if (newState != currentState)
        {
            currentState = newState;
            OnCoverStateChanged?.Invoke(this, currentState);
            UpdateCoverAppearance();
        }
        
        Debug.Log($"掩体 {gridIndex} 修复了 {currentHealth - oldHealth} 点血量");
    }
    
    /// <summary>
    /// 重置掩体到完好状态
    /// </summary>
    public void ResetCover()
    {
        currentHealth = maxHealth;
        currentState = CoverState.Perfect;
        gameObject.SetActive(true);
        UpdateCoverAppearance();
        
        Debug.Log($"掩体 {gridIndex} 已重置到完好状态");
    }
    
    /// <summary>
    /// 检查点是否在掩体范围内
    /// </summary>
    /// <param name="worldPosition">世界坐标点</param>
    /// <returns>是否在掩体范围内</returns>
    public bool IsPointInCover(Vector3 worldPosition)
    {
        if (currentState == CoverState.Destroyed) return false;
        
        // 检查X坐标是否在掩体范围内
        float coverLeft = transform.position.x - gridWidth * 0.5f;
        float coverRight = transform.position.x + gridWidth * 0.5f;
        
        return worldPosition.x >= coverLeft && worldPosition.x <= coverRight;
    }
    
    /// <summary>
    /// 获取掩体信息字符串
    /// </summary>
    /// <returns>掩体信息</returns>
    public string GetCoverInfo()
    {
        return $"掩体 {gridIndex}: {currentState} ({currentHealth:F1}/{maxHealth})";
    }
    
    // 编辑器方法
    [ContextMenu("测试伤害")]
    public void TestDamage()
    {
        TakeDamage(25f);
    }
    
    [ContextMenu("完全修复")]
    public void TestRepair()
    {
        ResetCover();
    }
    
    [ContextMenu("摧毁掩体")]
    public void TestDestroy()
    {
        TakeDamage(currentHealth);
    }
}
