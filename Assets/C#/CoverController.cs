using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 掩体控制器 - 管理所有掩体的生成、删除和攻击检测
/// </summary>
public class CoverController : MonoBehaviour
{
    [Header("掩体网格设置")]
    [SerializeField] private bool[] coverGrid = new bool[10]; // 10个网格位置的掩体开关
    [SerializeField] private float screenWidthDivisions = 10f; // 屏幕宽度分割数
    
    [Header("掩体预制体和资源")]
    [SerializeField] private GameObject coverPrefab; // 掩体预制体
    [SerializeField] private Sprite perfectCoverSprite; // 完好状态贴图
    [SerializeField] private Sprite damagedCoverSprite; // 破损状态贴图
    
    [Header("掩体属性")]
    [SerializeField] private float defaultCoverHealth = 100f; // 默认掩体血量
    [SerializeField] private float perfectCoverHeight = 2f;   // 完好状态高度
    [SerializeField] private float damagedCoverHeight = 1.5f; // 破损状态高度
    
    [Header("调试设置")]
    [SerializeField] private bool showDebugInfo = true; // 显示调试信息
    
    // 掩体管理
    private Dictionary<int, Cover> activeCover = new Dictionary<int, Cover>(); // 活跃的掩体
    private float gridWidth; // 单个网格宽度
    
    // 单例实例
    public static CoverController Instance { get; private set; }
    
    // 事件
    public System.Action<Cover> OnCoverCreated;
    public System.Action<Cover> OnCoverDestroyed;
    public System.Action<int> OnCoverGridChanged; // 网格变化事件
    
    // 属性
    public float GridWidth => gridWidth;
    public int TotalGrids => (int)screenWidthDivisions;
    public Dictionary<int, Cover> ActiveCover => activeCover;
    
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
        
        InitializeCoverSystem();
    }
    
    private void Start()
    {
        // 根据初始网格设置生成掩体
        GenerateCoversFromGrid();
    }
    
    /// <summary>
    /// 初始化掩体系统
    /// </summary>
    private void InitializeCoverSystem()
    {
        // 计算单个网格宽度
        gridWidth = Screen.width / screenWidthDivisions;
        
        // 确保coverGrid数组长度正确
        if (coverGrid.Length != (int)screenWidthDivisions)
        {
            bool[] newGrid = new bool[(int)screenWidthDivisions];
            for (int i = 0; i < newGrid.Length && i < coverGrid.Length; i++)
            {
                newGrid[i] = coverGrid[i];
            }
            coverGrid = newGrid;
        }
        
        Debug.Log($"掩体系统初始化完成：{screenWidthDivisions} 个网格，每个网格宽度 {gridWidth}");
        Debug.Log($"屏幕宽度：{Screen.width}，X坐标范围：0 到 {Screen.width}");
        
        // 显示网格位置信息
        for (int i = 0; i < screenWidthDivisions; i++)
        {
            float centerX = (i + 0.5f) * gridWidth;
            float leftX = i * gridWidth;
            float rightX = (i + 1) * gridWidth;
            Debug.Log($"网格 {i}：中心X={centerX:F1}，范围X=[{leftX:F1}, {rightX:F1}]");
        }
    }
    
    /// <summary>
    /// 根据网格设置生成掩体
    /// </summary>
    private void GenerateCoversFromGrid()
    {
        for (int i = 0; i < coverGrid.Length; i++)
        {
            if (coverGrid[i])
            {
                CreateCover(i);
            }
        }
    }
    
    /// <summary>
    /// 创建掩体
    /// </summary>
    /// <param name="gridIndex">网格索引（0-9）</param>
    /// <param name="health">掩体血量</param>
    /// <returns>创建的掩体对象</returns>
    public Cover CreateCover(int gridIndex, float health = -1f)
    {
        // 验证网格索引
        if (gridIndex < 0 || gridIndex >= screenWidthDivisions)
        {
            Debug.LogError($"无效的网格索引：{gridIndex}，有效范围：0-{screenWidthDivisions - 1}");
            return null;
        }
        
        // 如果该位置已有掩体，先删除
        if (activeCover.ContainsKey(gridIndex))
        {
            DestroyCover(gridIndex);
        }
        
        // 创建掩体对象
        GameObject coverObj;
        if (coverPrefab != null)
        {
            coverObj = Instantiate(coverPrefab, transform);
        }
        else
        {
            // 如果没有预制体，创建基础对象
            coverObj = new GameObject($"Cover_{gridIndex}");
            coverObj.transform.SetParent(transform);
        }
        
        // 添加Cover组件
        Cover cover = coverObj.GetComponent<Cover>();
        if (cover == null)
        {
            cover = coverObj.AddComponent<Cover>();
        }
        
        // 设置掩体参数
        float coverHealth = health > 0 ? health : defaultCoverHealth;
        cover.SetupCover(gridIndex, gridWidth, coverHealth);
        
        // 设置贴图资源
        SetupCoverSprites(cover);
        
        // 订阅掩体事件
        cover.OnCoverDestroyed += HandleCoverDestroyed;
        cover.OnCoverDamaged += HandleCoverDamaged;
        cover.OnCoverStateChanged += HandleCoverStateChanged;
        
        // 添加到活跃掩体字典
        activeCover[gridIndex] = cover;
        
        // 更新网格状态
        coverGrid[gridIndex] = true;
        
        // 触发事件
        OnCoverCreated?.Invoke(cover);
        OnCoverGridChanged?.Invoke(gridIndex);
        
        if (showDebugInfo)
        {
            Debug.Log($"掩体已创建：网格 {gridIndex}，血量 {coverHealth}");
        }
        
        return cover;
    }
    
    /// <summary>
    /// 设置掩体贴图资源
    /// </summary>
    /// <param name="cover">掩体对象</param>
    private void SetupCoverSprites(Cover cover)
    {
        // 使用Cover类的公共方法设置贴图资源
        cover.SetCoverSprites(perfectCoverSprite, damagedCoverSprite, perfectCoverHeight, damagedCoverHeight);
    }
    
    /// <summary>
    /// 销毁掩体
    /// </summary>
    /// <param name="gridIndex">网格索引</param>
    public void DestroyCover(int gridIndex)
    {
        if (activeCover.ContainsKey(gridIndex))
        {
            Cover cover = activeCover[gridIndex];
            
            // 取消事件订阅
            cover.OnCoverDestroyed -= HandleCoverDestroyed;
            cover.OnCoverDamaged -= HandleCoverDamaged;
            cover.OnCoverStateChanged -= HandleCoverStateChanged;
            
            // 销毁游戏对象
            if (cover.gameObject != null)
            {
                Destroy(cover.gameObject);
            }
            
            // 从字典中移除
            activeCover.Remove(gridIndex);
            
            // 更新网格状态
            if (gridIndex >= 0 && gridIndex < coverGrid.Length)
            {
                coverGrid[gridIndex] = false;
            }
            
            // 触发事件
            OnCoverGridChanged?.Invoke(gridIndex);
            
            if (showDebugInfo)
            {
                Debug.Log($"掩体已销毁：网格 {gridIndex}");
            }
        }
    }
    
    /// <summary>
    /// 清除所有掩体
    /// </summary>
    public void ClearAllCovers()
    {
        List<int> gridIndices = new List<int>(activeCover.Keys);
        foreach (int gridIndex in gridIndices)
        {
            DestroyCover(gridIndex);
        }
        
        if (showDebugInfo)
        {
            Debug.Log("所有掩体已清除");
        }
    }
    
    /// <summary>
    /// 设置网格掩体状态
    /// </summary>
    /// <param name="gridIndex">网格索引</param>
    /// <param name="hasCover">是否有掩体</param>
    public void SetGridCover(int gridIndex, bool hasCover)
    {
        if (gridIndex < 0 || gridIndex >= coverGrid.Length) return;
        
        if (hasCover && !activeCover.ContainsKey(gridIndex))
        {
            CreateCover(gridIndex);
        }
        else if (!hasCover && activeCover.ContainsKey(gridIndex))
        {
            DestroyCover(gridIndex);
        }
    }
    
    /// <summary>
    /// 批量设置掩体网格
    /// </summary>
    /// <param name="newGrid">新的网格配置</param>
    public void SetCoverGrid(bool[] newGrid)
    {
        if (newGrid.Length != coverGrid.Length)
        {
            Debug.LogError($"网格配置长度不匹配：期望 {coverGrid.Length}，实际 {newGrid.Length}");
            return;
        }
        
        for (int i = 0; i < newGrid.Length; i++)
        {
            SetGridCover(i, newGrid[i]);
        }
        
        coverGrid = (bool[])newGrid.Clone();
        
        if (showDebugInfo)
        {
            Debug.Log($"掩体网格已更新：{string.Join(",", newGrid)}");
        }
    }
    
    
    /// <summary>
    /// 根据世界坐标获取网格索引
    /// </summary>
    /// <param name="worldPosition">世界坐标</param>
    /// <returns>网格索引</returns>
    public int GetGridIndexFromWorldPosition(Vector3 worldPosition)
    {
        // 屏幕X坐标范围：0 到 Screen.width
        int gridIndex = Mathf.FloorToInt(worldPosition.x / gridWidth);
        
        return Mathf.Clamp(gridIndex, 0, (int)screenWidthDivisions - 1);
    }
    
    /// <summary>
    /// 获取网格中心世界坐标
    /// </summary>
    /// <param name="gridIndex">网格索引</param>
    /// <returns>网格中心的世界坐标</returns>
    public Vector3 GetGridCenterWorldPosition(int gridIndex)
    {
        if (gridIndex < 0 || gridIndex >= screenWidthDivisions) return Vector3.zero;
        
        // 屏幕X坐标范围：0 到 Screen.width
        float centerX = (gridIndex + 0.5f) * gridWidth;
        
        // 获取玩家Y坐标作为参考
        Player player = FindObjectOfType<Player>();
        float playerY = player != null ? player.transform.position.y : 59.6f; // 默认使用玩家的Y坐标
        
        return new Vector3(centerX, playerY, 0f);
    }
    
    /// <summary>
    /// 获取所有掩体信息
    /// </summary>
    /// <returns>掩体信息字符串</returns>
    public string GetAllCoversInfo()
    {
        if (activeCover.Count == 0) return "当前没有掩体";
        
        var coverInfos = activeCover.Values.Select(cover => cover.GetCoverInfo());
        return string.Join("\n", coverInfos);
    }
    
    // 掩体事件处理
    private void HandleCoverDestroyed(Cover cover)
    {
        OnCoverDestroyed?.Invoke(cover);
        
        if (showDebugInfo)
        {
            Debug.Log($"掩体事件：{cover.GetCoverInfo()} 已被摧毁");
        }
    }
    
    private void HandleCoverDamaged(Cover cover, float damage)
    {
        if (showDebugInfo)
        {
            Debug.Log($"掩体事件：{cover.GetCoverInfo()} 受到 {damage} 点伤害");
        }
    }
    
    private void HandleCoverStateChanged(Cover cover, CoverState newState)
    {
        if (showDebugInfo)
        {
            Debug.Log($"掩体事件：{cover.GetCoverInfo()} 状态变为 {newState}");
        }
    }
    
    // 公共快速设置函数
    
    /// <summary>
    /// 快速创建掩体组合
    /// </summary>
    /// <param name="pattern">掩体模式字符串，如"1010101010"，1表示有掩体，0表示无掩体</param>
    public void QuickSetCoverPattern(string pattern)
    {
        if (pattern.Length != coverGrid.Length)
        {
            Debug.LogError($"掩体模式长度不匹配：期望 {coverGrid.Length}，实际 {pattern.Length}");
            return;
        }
        
        bool[] newGrid = new bool[coverGrid.Length];
        for (int i = 0; i < pattern.Length; i++)
        {
            newGrid[i] = pattern[i] == '1';
        }
        
        SetCoverGrid(newGrid);
        
        if (showDebugInfo)
        {
            Debug.Log($"快速设置掩体模式：{pattern}");
        }
    }
    
    /// <summary>
    /// 快速创建连续掩体
    /// </summary>
    /// <param name="startIndex">起始网格索引</param>
    /// <param name="count">掩体数量</param>
    public void QuickCreateContinuousCovers(int startIndex, int count)
    {
        for (int i = 0; i < count; i++)
        {
            int gridIndex = startIndex + i;
            if (gridIndex >= 0 && gridIndex < coverGrid.Length)
            {
                CreateCover(gridIndex);
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"快速创建连续掩体：从网格 {startIndex} 开始，共 {count} 个");
        }
    }
    
    /// <summary>
    /// 快速创建间隔掩体
    /// </summary>
    /// <param name="startIndex">起始网格索引</param>
    /// <param name="interval">间隔</param>
    /// <param name="count">掩体数量</param>
    public void QuickCreateIntervalCovers(int startIndex, int interval, int count)
    {
        for (int i = 0; i < count; i++)
        {
            int gridIndex = startIndex + i * interval;
            if (gridIndex >= 0 && gridIndex < coverGrid.Length)
            {
                CreateCover(gridIndex);
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"快速创建间隔掩体：从网格 {startIndex} 开始，间隔 {interval}，共 {count} 个");
        }
    }

    /// <summary>
    /// 重新初始化掩体
    /// </summary>
    /// <param name="Index">网格索引数组 范围0-9</param>
    public void ReCreateCover(int[] Index) 
    {
        if (Index.Length == 0) return;

        ClearAllCovers();
        for (int i = 0; i < Index.Length; i++) 
        {
            QuickCreateIntervalCovers(Index[i], 1, 1);
        }
    }

    // 编辑器方法
    [ContextMenu("生成测试掩体")]
    public void CreateTestCovers()
    {
        QuickSetCoverPattern("1010101010");
    }
    
    [ContextMenu("清除所有掩体")]
    public void ClearAllCoversMenu()
    {
        ClearAllCovers();
    }
    
    [ContextMenu("显示掩体信息")]
    public void ShowCoversInfo()
    {
        Debug.Log(GetAllCoversInfo());
    }
    
    // Inspector中的网格可视化
    private void OnValidate()
    {
        // 确保网格数组长度正确
        if (coverGrid.Length != (int)screenWidthDivisions)
        {
            bool[] newGrid = new bool[(int)screenWidthDivisions];
            for (int i = 0; i < newGrid.Length && i < coverGrid.Length; i++)
            {
                newGrid[i] = coverGrid[i];
            }
            coverGrid = newGrid;
        }
    }
}
