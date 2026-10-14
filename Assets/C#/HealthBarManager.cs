using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 血条管理器
/// 管理所有敌人的血条显示
/// </summary>
public class HealthBarManager : MonoBehaviour
{
    [Header("Health Bar Settings")]
    [SerializeField] private GameObject healthBarPrefab; // 血条预制体
    [SerializeField] private float healthBarHeight = 2f; // 血条在敌人头顶的高度
    [SerializeField] private float healthBarScale = 0.01f; // 血条缩放比例
    [SerializeField] private bool showHealthBarWhenFull = false; // 满血时是否显示血条
    [SerializeField] private float hideDelay = 2f; // 受伤后血条显示时间
    
    [Header("Visual Settings")]
    [SerializeField] private Color fullHealthColor = Color.green;
    [SerializeField] private Color lowHealthColor = Color.red;
    [SerializeField] private float lowHealthThreshold = 0.3f; // 低血量阈值
    
    // 血条字典
    private Dictionary<GameObject, HealthBarData> healthBars = new Dictionary<GameObject, HealthBarData>();
    
    // 单例
    public static HealthBarManager Instance { get; private set; }
    
    // 血条数据结构
    [System.Serializable]
    public class HealthBarData
    {
        public GameObject healthBarObject;
        public Slider healthSlider;
        public Image fillImage;
        public Canvas healthBarCanvas;
        public Coroutine hideCoroutine;
        public bool isVisible;
        
        public HealthBarData(GameObject obj, Slider slider, Image fill, Canvas canvas)
        {
            healthBarObject = obj;
            healthSlider = slider;
            fillImage = fill;
            healthBarCanvas = canvas;
            isVisible = true;
        }
    }
    
    private void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // 如果没有预制体，创建默认血条
        if (healthBarPrefab == null)
        {
            CreateDefaultHealthBarPrefab();
        }
    }
    
    /// <summary>
    /// 为敌人创建血条
    /// </summary>
    /// <param name="enemy">敌人对象</param>
    /// <param name="maxHealth">最大血量</param>
    /// <param name="currentHealth">当前血量</param>
    public void CreateHealthBar(GameObject enemy, float maxHealth, float currentHealth)
    {
        if (enemy == null || healthBars.ContainsKey(enemy)) return;
        
        // 创建血条对象
        GameObject healthBarObj = Instantiate(healthBarPrefab);
        
        // 设置血条为世界空间
        Canvas healthBarCanvas = healthBarObj.GetComponent<Canvas>();
        if (healthBarCanvas == null)
        {
            healthBarCanvas = healthBarObj.AddComponent<Canvas>();
        }
        
        healthBarCanvas.renderMode = RenderMode.WorldSpace;
        healthBarCanvas.worldCamera = Camera.main;
        
        // 获取血条组件
        Slider healthSlider = healthBarObj.GetComponentInChildren<Slider>();
        Image fillImage = healthSlider?.fillRect?.GetComponent<Image>();
        
        if (healthSlider == null || fillImage == null)
        {
            Debug.LogError("Health bar prefab is missing required components!");
            Destroy(healthBarObj);
            return;
        }
        
        // 设置血条数据
        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
        
        // 设置血条位置
        UpdateHealthBarPosition(healthBarObj, enemy);
        
        // 设置血条缩放
        healthBarObj.transform.localScale = Vector3.one * healthBarScale;
        
        // 初始时隐藏血条（如果满血且设置为不显示满血血条）
        bool shouldShow = !showHealthBarWhenFull && currentHealth >= maxHealth ? false : true;
        healthBarObj.SetActive(shouldShow);
        
        // 存储血条数据
        HealthBarData healthBarData = new HealthBarData(healthBarObj, healthSlider, fillImage, healthBarCanvas);
        healthBarData.isVisible = shouldShow;
        healthBars[enemy] = healthBarData;
        
        Debug.Log($"Created health bar for {enemy.name}");
    }
    
    /// <summary>
    /// 更新敌人血量
    /// </summary>
    /// <param name="enemy">敌人对象</param>
    /// <param name="currentHealth">当前血量</param>
    /// <param name="maxHealth">最大血量</param>
    public void UpdateHealthBar(GameObject enemy, float currentHealth, float maxHealth)
    {
        if (enemy == null || !healthBars.ContainsKey(enemy)) return;
        
        HealthBarData healthBarData = healthBars[enemy];
        
        // 更新血量值
        healthBarData.healthSlider.maxValue = maxHealth;
        healthBarData.healthSlider.value = currentHealth;
        
        // 更新血条颜色
        float healthPercentage = currentHealth / maxHealth;
        healthBarData.fillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, healthPercentage);
        
        // 显示血条
        if (!healthBarData.isVisible)
        {
            healthBarData.healthBarObject.SetActive(true);
            healthBarData.isVisible = true;
        }
        
        // 重置隐藏计时器
        if (healthBarData.hideCoroutine != null)
        {
            StopCoroutine(healthBarData.hideCoroutine);
        }
        
        // 如果满血且设置为不显示满血血条，延迟隐藏
        if (!showHealthBarWhenFull && healthPercentage >= 1f)
        {
            healthBarData.hideCoroutine = StartCoroutine(HideHealthBarAfterDelay(healthBarData));
        }
        else
        {
            // 受伤后延迟隐藏血条
            healthBarData.hideCoroutine = StartCoroutine(HideHealthBarAfterDelay(healthBarData));
        }
    }
    
    /// <summary>
    /// 强制隐藏血条（隐身状态专用，阻止受伤时自动显示）
    /// </summary>
    public void ForceHideHealthBar(GameObject enemy)
    {
        if (enemy == null || !healthBars.ContainsKey(enemy)) return;

        HealthBarData healthBarData = healthBars[enemy];

        if (healthBarData.hideCoroutine != null)
        {
            StopCoroutine(healthBarData.hideCoroutine);
            healthBarData.hideCoroutine = null;
        }

        if (healthBarData.healthBarObject != null)
        {
            healthBarData.healthBarObject.SetActive(false);
            healthBarData.isVisible = false;
        }
    }

    /// <summary>
    /// 移除敌人血条
    /// </summary>
    /// <param name="enemy">敌人对象</param>
    public void RemoveHealthBar(GameObject enemy)
    {
        if (enemy == null || !healthBars.ContainsKey(enemy)) return;
        
        HealthBarData healthBarData = healthBars[enemy];
        
        // 停止隐藏协程
        if (healthBarData.hideCoroutine != null)
        {
            StopCoroutine(healthBarData.hideCoroutine);
        }
        
        // 销毁血条对象
        if (healthBarData.healthBarObject != null)
        {
            Destroy(healthBarData.healthBarObject);
        }
        
        // 从字典中移除
        healthBars.Remove(enemy);
        
        Debug.Log($"Removed health bar for {enemy.name}");
    }
    
    /// <summary>
    /// 更新血条位置
    /// </summary>
    private void UpdateHealthBarPosition(GameObject healthBarObj, GameObject enemy)
    {
        if (healthBarObj == null || enemy == null) return;
        
        Vector3 enemyPosition = enemy.transform.position;
        Vector3 healthBarPosition = new Vector3(enemyPosition.x, enemyPosition.y + healthBarHeight, enemyPosition.z);
        
        healthBarObj.transform.position = healthBarPosition;
        
    }
    
    /// <summary>
    /// 延迟隐藏血条
    /// </summary>
    private System.Collections.IEnumerator HideHealthBarAfterDelay(HealthBarData healthBarData)
    {
        yield return new WaitForSeconds(hideDelay);
        
        if (healthBarData.healthBarObject != null)
        {
            healthBarData.healthBarObject.SetActive(false);
            healthBarData.isVisible = false;
        }
    }
    
    /// <summary>
    /// 创建默认血条预制体
    /// </summary>
    private void CreateDefaultHealthBarPrefab()
    {
        // 创建血条预制体
        GameObject healthBarObj = new GameObject("HealthBar");
        
        // 添加Canvas组件
        Canvas canvas = healthBarObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        // 添加CanvasScaler组件
        CanvasScaler scaler = healthBarObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;
        
        // 添加GraphicRaycaster组件
        healthBarObj.AddComponent<GraphicRaycaster>();
        
        // 创建背景
        GameObject background = new GameObject("Background");
        background.transform.SetParent(healthBarObj.transform);
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.5f);
        
        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        
        // 创建血条Slider
        GameObject sliderObj = new GameObject("HealthSlider");
        sliderObj.transform.SetParent(healthBarObj.transform);
        Slider slider = sliderObj.AddComponent<Slider>();
        
        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = Vector2.zero;
        sliderRect.anchorMax = Vector2.one;
        sliderRect.sizeDelta = Vector2.zero;
        sliderRect.anchoredPosition = Vector2.zero;
        
        // 创建Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;
        fillAreaRect.anchoredPosition = Vector2.zero;
        
        // 创建Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = Color.green;
        
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;
        
        // 设置Slider
        slider.fillRect = fillRect;
        slider.targetGraphic = fillImage;
        
        // 设置血条大小
        RectTransform healthBarRect = healthBarObj.GetComponent<RectTransform>();
        healthBarRect.sizeDelta = new Vector2(100, 10);
        
        // 保存为预制体引用
        healthBarPrefab = healthBarObj;
        
        Debug.Log("Created default health bar prefab");
    }
    
    private void Update()
    {
        // 更新所有血条位置
        foreach (var kvp in healthBars)
        {
            if (kvp.Key != null && kvp.Value.healthBarObject != null)
            {
                UpdateHealthBarPosition(kvp.Value.healthBarObject, kvp.Key);
            }
        }
    }
    
    private void OnDestroy()
    {
        // 清理所有血条
        foreach (var kvp in healthBars)
        {
            if (kvp.Value.healthBarObject != null)
            {
                Destroy(kvp.Value.healthBarObject);
            }
        }
        healthBars.Clear();
    }
}
