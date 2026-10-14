using UnityEngine;

/// <summary>
/// 掩体系统使用示例和测试脚本
/// </summary>
public class CoverSystemExample : MonoBehaviour
{
    [Header("测试设置")]
    [SerializeField] private bool autoTest = false;
    [SerializeField] private float testInterval = 2f;
    
    [Header("掩体测试")]
    [SerializeField] private string testPattern = "1010101010"; // 测试掩体模式
    [SerializeField] private float testDamage = 25f; // 测试伤害值
    
    private float testTimer = 0f;
    private int testStep = 0;
    
    private void Start()
    {
        if (autoTest)
        {
            Debug.Log("掩体系统自动测试开始");
        }
    }
    
    private void Update()
    {
        if (autoTest)
        {
            testTimer += Time.deltaTime;
            if (testTimer >= testInterval)
            {
                ExecuteTestStep();
                testTimer = 0f;
            }
        }
    }
    
    /// <summary>
    /// 执行测试步骤
    /// </summary>
    private void ExecuteTestStep()
    {
        if (CoverController.Instance == null)
        {
            Debug.LogError("CoverController实例不存在，无法进行测试");
            return;
        }
        
        switch (testStep)
        {
            case 0:
                TestCreateCovers();
                break;
            case 1:
                TestDamageCovers();
                break;
            case 2:
                TestCoverStates();
                break;
            case 3:
                TestClearCovers();
                break;
            case 4:
                TestQuickPatterns();
                break;
            default:
                Debug.Log("掩体系统测试完成");
                autoTest = false;
                return;
        }
        
        testStep++;
    }
    
    /// <summary>
    /// 测试创建掩体
    /// </summary>
    private void TestCreateCovers()
    {
        Debug.Log("=== 测试创建掩体 ===");
        CoverController.Instance.QuickSetCoverPattern(testPattern);
        Debug.Log($"创建掩体模式：{testPattern}");
        Debug.Log(CoverController.Instance.GetAllCoversInfo());
    }
    
    /// <summary>
    /// 测试掩体伤害
    /// </summary>
    private void TestDamageCovers()
    {
        Debug.Log("=== 测试掩体伤害 ===");
        
        var activeCovers = CoverController.Instance.ActiveCover;
        foreach (var kvp in activeCovers)
        {
            Cover cover = kvp.Value;
            cover.TakeDamage(testDamage);
            Debug.Log($"对掩体 {cover.GridIndex} 造成 {testDamage} 点伤害");
        }
        
        Debug.Log(CoverController.Instance.GetAllCoversInfo());
    }
    
    /// <summary>
    /// 测试掩体状态变化
    /// </summary>
    private void TestCoverStates()
    {
        Debug.Log("=== 测试掩体状态变化 ===");
        
        var activeCovers = CoverController.Instance.ActiveCover;
        foreach (var kvp in activeCovers)
        {
            Cover cover = kvp.Value;
            // 造成足够伤害使掩体进入破损状态
            cover.TakeDamage(cover.CurrentHealth * 0.6f);
            Debug.Log($"掩体 {cover.GridIndex} 当前状态：{cover.CurrentState}");
        }
        
        Debug.Log(CoverController.Instance.GetAllCoversInfo());
    }
    
    /// <summary>
    /// 测试清除掩体
    /// </summary>
    private void TestClearCovers()
    {
        Debug.Log("=== 测试清除掩体 ===");
        CoverController.Instance.ClearAllCovers();
        Debug.Log("所有掩体已清除");
    }
    
    /// <summary>
    /// 测试快速模式
    /// </summary>
    private void TestQuickPatterns()
    {
        Debug.Log("=== 测试快速模式 ===");
        
        // 测试连续掩体
        CoverController.Instance.QuickCreateContinuousCovers(2, 3);
        Debug.Log("创建连续掩体：从网格2开始，共3个");
        
        // 等待一下再测试间隔掩体
        CoverController.Instance.ClearAllCovers();
        CoverController.Instance.QuickCreateIntervalCovers(1, 2, 4);
        Debug.Log("创建间隔掩体：从网格1开始，间隔2，共4个");
        
        Debug.Log(CoverController.Instance.GetAllCoversInfo());
    }
    
    // 手动测试方法（可在Inspector中调用）
    
    [ContextMenu("创建测试掩体")]
    public void CreateTestCovers()
    {
        if (CoverController.Instance != null)
        {
            CoverController.Instance.QuickSetCoverPattern(testPattern);
            Debug.Log($"手动创建掩体模式：{testPattern}");
        }
    }
    
    [ContextMenu("伤害所有掩体")]
    public void DamageAllCovers()
    {
        if (CoverController.Instance != null)
        {
            var activeCovers = CoverController.Instance.ActiveCover;
            foreach (var kvp in activeCovers)
            {
                kvp.Value.TakeDamage(testDamage);
            }
            Debug.Log($"对所有掩体造成 {testDamage} 点伤害");
        }
    }
    
    [ContextMenu("摧毁所有掩体")]
    public void DestroyAllCovers()
    {
        if (CoverController.Instance != null)
        {
            var activeCovers = CoverController.Instance.ActiveCover;
            foreach (var kvp in activeCovers)
            {
                kvp.Value.TakeDamage(kvp.Value.CurrentHealth);
            }
            Debug.Log("摧毁所有掩体");
        }
    }
    
    [ContextMenu("修复所有掩体")]
    public void RepairAllCovers()
    {
        if (CoverController.Instance != null)
        {
            var activeCovers = CoverController.Instance.ActiveCover;
            foreach (var kvp in activeCovers)
            {
                kvp.Value.ResetCover();
            }
            Debug.Log("修复所有掩体");
        }
    }
    
    [ContextMenu("清除所有掩体")]
    public void ClearAllCovers()
    {
        if (CoverController.Instance != null)
        {
            CoverController.Instance.ClearAllCovers();
            Debug.Log("清除所有掩体");
        }
    }
    
    [ContextMenu("显示掩体信息")]
    public void ShowCoversInfo()
    {
        if (CoverController.Instance != null)
        {
            Debug.Log("=== 当前掩体信息 ===");
            Debug.Log(CoverController.Instance.GetAllCoversInfo());
        }
    }
    
    [ContextMenu("测试掩体伤害机制")]
    public void TestCoverDamageMechanism()
    {
        Player player = FindObjectOfType<Player>();
        if (player != null && CoverController.Instance != null)
        {
            // 创建测试掩体
            CoverController.Instance.QuickSetCoverPattern("0010000000");
            
            Debug.Log("=== 测试掩体伤害机制 ===");
            Debug.Log($"玩家当前血量：{player.CurrentHealth}");
            
            // 测试有掩体保护的伤害
            Debug.Log("测试掩体保护机制（玩家应该在网格2位置）");
            player.TakeDamage(30f);
            Debug.Log($"攻击后玩家血量：{player.CurrentHealth}");
            
            Debug.Log(CoverController.Instance.GetAllCoversInfo());
        }
    }
}
