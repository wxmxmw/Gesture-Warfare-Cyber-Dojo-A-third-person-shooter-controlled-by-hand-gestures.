using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

// 道具UI控制器
public class PowerUpUIController : MonoBehaviour
{
    [Header("霰弹UI设置")]
    [SerializeField] private GameObject shotgunUI;
    [SerializeField] private List<GameObject> shotgunAmmoList = new List<GameObject>(); // 霰弹弹药UI列表
    
    [Header("连射UI设置")]
    [SerializeField] private GameObject rapidFireUI;
    [SerializeField] private Image rapidFireKeepTime; // 连射持续时间条
    
    [Header("脉冲手雷UI设置")]
    [SerializeField] private GameObject pulseGrenadeUI;
    [SerializeField] private List<GameObject> pulseGrenadeList = new List<GameObject>(); // 脉冲手雷UI列表
    [SerializeField] private int maxGrenadeCount = 3; // 手雷上限
    
    // 私有变量
    private PowerUpManager powerUpManager;
    private Coroutine rapidFireUpdateCoroutine;
    
    // 单例
    public static PowerUpUIController Instance { get; private set; }
    
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
            return;
        }
    }
    
    private void Start()
    {
        InitializeUI();
        SetupPowerUpManagerEvents();
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    /// <summary>
    /// 初始化UI
    /// </summary>
    private void InitializeUI()
    {
        // 获取PowerUpManager引用
        powerUpManager = PowerUpManager.Instance;
        
        // 初始化所有UI为隐藏状态
        SetShotgunUIVisible(false);
        SetRapidFireUIVisible(false);
        SetPulseGrenadeUIVisible(false);
        
        // 初始化霰弹弹药显示
        UpdateShotgunAmmoDisplay(0);
        
        // 初始化脉冲手雷显示
        UpdatePulseGrenadeDisplay(0);
        
        // 初始化连射时间条
        if (rapidFireKeepTime != null)
        {
            rapidFireKeepTime.fillAmount = 0f;
        }
    }
    
    /// <summary>
    /// 设置PowerUpManager事件订阅
    /// </summary>
    private void SetupPowerUpManagerEvents()
    {
        if (powerUpManager != null)
        {
            powerUpManager.OnPowerUpActivated += OnPowerUpActivated;
            powerUpManager.OnPowerUpExpired += OnPowerUpExpired;
            powerUpManager.OnPowerUpCountChanged += OnPowerUpCountChanged;
        }
    }
    
    /// <summary>
    /// 取消事件订阅
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        if (powerUpManager != null)
        {
            powerUpManager.OnPowerUpActivated -= OnPowerUpActivated;
            powerUpManager.OnPowerUpExpired -= OnPowerUpExpired;
            powerUpManager.OnPowerUpCountChanged -= OnPowerUpCountChanged;
        }
    }
    
    /// <summary>
    /// 道具激活事件处理
    /// </summary>
    /// <param name="powerUpType">道具类型</param>
    private void OnPowerUpActivated(PowerUpType powerUpType)
    {
        switch (powerUpType)
        {
            case PowerUpType.Shotgun:
                SetShotgunUIVisible(true);
                UpdateShotgunAmmoDisplay(powerUpManager.GetShotgunAmmo());
                break;
                
            case PowerUpType.RapidFire:
                SetRapidFireUIVisible(true);
                StartRapidFireTimeUpdate();
                break;
                
            case PowerUpType.PulseGrenade:
                SetPulseGrenadeUIVisible(true);
                UpdatePulseGrenadeDisplay(powerUpManager.GetPulseGrenadeCount());
                break;
        }
    }
    
    /// <summary>
    /// 道具过期事件处理
    /// </summary>
    /// <param name="powerUpType">道具类型</param>
    private void OnPowerUpExpired(PowerUpType powerUpType)
    {
        switch (powerUpType)
        {
            case PowerUpType.Shotgun:
                SetShotgunUIVisible(false);
                break;
                
            case PowerUpType.RapidFire:
                SetRapidFireUIVisible(false);
                StopRapidFireTimeUpdate();
                break;
        }
    }
    
    /// <summary>
    /// 道具计数变化事件处理
    /// </summary>
    /// <param name="powerUpType">道具类型</param>
    /// <param name="count">数量</param>
    private void OnPowerUpCountChanged(PowerUpType powerUpType, int count)
    {
        switch (powerUpType)
        {
            case PowerUpType.Shotgun:
                UpdateShotgunAmmoDisplay(count);
                break;
                
            case PowerUpType.PulseGrenade:
                UpdatePulseGrenadeDisplay(count);
                break;
        }
    }
    
    /// <summary>
    /// 设置霰弹UI可见性
    /// </summary>
    /// <param name="visible">是否可见</param>
    private void SetShotgunUIVisible(bool visible)
    {
        if (shotgunUI != null)
        {
            shotgunUI.SetActive(visible);
        }
    }
    
    /// <summary>
    /// 设置连射UI可见性
    /// </summary>
    /// <param name="visible">是否可见</param>
    private void SetRapidFireUIVisible(bool visible)
    {
        if (rapidFireUI != null)
        {
            rapidFireUI.SetActive(visible);
        }
    }
    
    /// <summary>
    /// 设置脉冲手雷UI可见性
    /// </summary>
    /// <param name="visible">是否可见</param>
    private void SetPulseGrenadeUIVisible(bool visible)
    {
        if (pulseGrenadeUI != null)
        {
            pulseGrenadeUI.SetActive(visible);
        }
    }
    
    /// <summary>
    /// 更新霰弹弹药显示
    /// </summary>
    /// <param name="ammoCount">弹药数量</param>
    private void UpdateShotgunAmmoDisplay(int ammoCount)
    {
        if (shotgunAmmoList == null) return;
        
        for (int i = 0; i < shotgunAmmoList.Count; i++)
        {
            if (shotgunAmmoList[i] != null)
            {
                // 前ammoCount个显示，后面的隐藏
                shotgunAmmoList[i].SetActive(i < ammoCount);
            }
        }
        
        Debug.Log($"更新霰弹弹药显示: {ammoCount}/{shotgunAmmoList.Count}");
    }
    
    /// <summary>
    /// 更新脉冲手雷显示
    /// </summary>
    /// <param name="grenadeCount">手雷数量</param>
    private void UpdatePulseGrenadeDisplay(int grenadeCount)
    {
        if (pulseGrenadeList == null) return;
        
        // 限制手雷数量不超过上限
        int maxGrenades = powerUpManager != null ? powerUpManager.GetMaxGrenadeCount() : 3;
        int displayCount = Mathf.Min(grenadeCount, maxGrenades);
        
        for (int i = 0; i < pulseGrenadeList.Count; i++)
        {
            if (pulseGrenadeList[i] != null)
            {
                // 前displayCount个显示，后面的隐藏
                pulseGrenadeList[i].SetActive(i < displayCount);
            }
        }
        
        Debug.Log($"更新脉冲手雷显示: {displayCount}/{maxGrenades}");
    }
    
    /// <summary>
    /// 开始连射时间更新
    /// </summary>
    private void StartRapidFireTimeUpdate()
    {
        if (rapidFireUpdateCoroutine != null)
        {
            StopCoroutine(rapidFireUpdateCoroutine);
        }
        rapidFireUpdateCoroutine = StartCoroutine(UpdateRapidFireTime());
    }
    
    /// <summary>
    /// 停止连射时间更新
    /// </summary>
    private void StopRapidFireTimeUpdate()
    {
        if (rapidFireUpdateCoroutine != null)
        {
            StopCoroutine(rapidFireUpdateCoroutine);
            rapidFireUpdateCoroutine = null;
        }
    }
    
    /// <summary>
    /// 更新连射时间显示
    /// </summary>
    private IEnumerator UpdateRapidFireTime()
    {
        while (powerUpManager != null && powerUpManager.HasRapidFireEffect())
        {
            float remainingTime = powerUpManager.GetRapidFireTime();
            float maxTime = powerUpManager.GetRapidFireMaxTime();
            
            if (rapidFireKeepTime != null)
            {
                // 计算填充比例（剩余时间/最大时间）
                float fillAmount = Mathf.Clamp01(remainingTime / maxTime);
                rapidFireKeepTime.fillAmount = fillAmount;
                
                // 根据剩余时间调整透明度（时间越少越透明）
                Color color = rapidFireKeepTime.color;
                color.a = fillAmount; // 透明度直接等于填充比例
                rapidFireKeepTime.color = color;
            }
            
            yield return new WaitForSeconds(0.1f); // 每0.1秒更新一次
        }
        
        // 连射效果结束，重置时间条
        if (rapidFireKeepTime != null)
        {
            rapidFireKeepTime.fillAmount = 0f;
            Color color = rapidFireKeepTime.color;
            color.a = 0f;
            rapidFireKeepTime.color = color;
        }
    }
    
    // 编辑器辅助方法
    [ContextMenu("Test Shotgun UI")]
    public void TestShotgunUI()
    {
        SetShotgunUIVisible(true);
        UpdateShotgunAmmoDisplay(5);
    }
    
    [ContextMenu("Test Rapid Fire UI")]
    public void TestRapidFireUI()
    {
        SetRapidFireUIVisible(true);
        StartRapidFireTimeUpdate();
    }
    
    [ContextMenu("Test Pulse Grenade UI")]
    public void TestPulseGrenadeUI()
    {
        SetPulseGrenadeUIVisible(true);
        UpdatePulseGrenadeDisplay(2);
    }
    
    [ContextMenu("Hide All UI")]
    public void HideAllUI()
    {
        SetShotgunUIVisible(false);
        SetRapidFireUIVisible(false);
        SetPulseGrenadeUIVisible(false);
    }
}
