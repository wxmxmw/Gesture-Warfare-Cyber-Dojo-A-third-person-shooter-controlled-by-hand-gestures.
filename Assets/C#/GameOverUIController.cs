using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 游戏结束UI控制器
/// 处理玩家死亡后的复活和重头再来功能
/// </summary>
public class GameOverUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button reviveButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private TMP_Text reviveButtonText;
    [SerializeField] private TMP_Text restartButtonText;
    [SerializeField] private TMP_Text gameOverTitle;
    [SerializeField] private TMP_Text remainCoinNumber;

    [Header("Revive Settings")]
    [SerializeField] private int reviveCoinsRequired = 1; // 复活需要的硬币数量
    [SerializeField] private string reviveButtonTextFormat = "投币复活 ({0})";
    [SerializeField] private string noCoinsText = "硬币不足";
    
    [Header("Game References")]
    [SerializeField] private Player player;
    [SerializeField] private GameManager gameManager;
    
    // 私有变量
    private int currentCoins = 3; // 当前硬币数量
    
    // 属性
    public int CurrentCoins => currentCoins;
    public int ReviveCoinsRequired => reviveCoinsRequired;
    public bool CanRevive => currentCoins >= reviveCoinsRequired;
    
    private void Awake()
    {
        InitializeUI();
    }
    
    private void Start()
    {
        // 自动查找组件
        if (player == null)
        {
            player = FindObjectOfType<Player>();
        }
        
        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
        }
        
        // 订阅玩家事件
        if (player != null)
        {
            player.OnPlayerDeath += OnPlayerDeath;
            player.OnPlayerRevived += OnPlayerRevived;
        }
        
        // 初始化UI状态
        HideGameOverUI();
    }
    
    /// <summary>
    /// 初始化UI
    /// </summary>
    private void InitializeUI()
    {
        // 设置按钮事件
        if (reviveButton != null)
        {
            reviveButton.onClick.AddListener(OnReviveButtonClicked);
        }
        
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartButtonClicked);
        }
        
        // 设置按钮文本
        UpdateReviveButtonText();
        UpdateRestartButtonText();
    }
    
    /// <summary>
    /// 显示游戏结束UI
    /// </summary>
    public void ShowGameOverUI()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        
        // 更新UI状态
        UpdateReviveButtonText();
        UpdateButtonInteractability();
        
        // 暂停游戏
        Time.timeScale = 0f;
        
        Debug.Log("Game Over UI shown");
    }
    
    /// <summary>
    /// 隐藏游戏结束UI
    /// </summary>
    public void HideGameOverUI()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        // 恢复游戏时间
        Time.timeScale = 1f;
        
        Debug.Log("Game Over UI hidden");
    }
    
    /// <summary>
    /// 复活按钮点击事件
    /// </summary>
    private void OnReviveButtonClicked()
    {
        if (!CanRevive)
        {
            Debug.Log("Not enough coins to revive!");
            // 这里可以播放硬币不足的音效或显示提示
            return;
        }
        
        // 消耗硬币
        ConsumeCoins(reviveCoinsRequired);
        
        // 复活玩家
        if (player != null)
        {
            player.Revive();
        }
        
        // 隐藏UI
        HideGameOverUI();
        
        Debug.Log("Player revived!");
    }
    
    /// <summary>
    /// 重头再来按钮点击事件
    /// </summary>
    private void OnRestartButtonClicked()
    {
        // 隐藏游戏结束UI
        HideGameOverUI();
        
        // 触发结算界面
        if (gameManager != null)
        {
            gameManager.RestartFromBeginning();
        }
        
        Debug.Log("Restarting from beginning...");
    }
    
    /// <summary>
    /// 玩家死亡事件回调
    /// </summary>
    private void OnPlayerDeath(Player deadPlayer)
    {
        // 移除这里的延迟调用，因为GameManager已经处理了
        // Invoke(nameof(ShowGameOverUI), 1f);
    }
    
    /// <summary>
    /// 玩家复活事件回调
    /// </summary>
    private void OnPlayerRevived(Player revivedPlayer)
    {
        // 玩家复活后隐藏UI
        HideGameOverUI();
    }
    
    /// <summary>
    /// 更新复活按钮文本
    /// </summary>
    private void UpdateReviveButtonText()
    {
        if (reviveButtonText != null)
        {
            if (CanRevive)
            {
                reviveButtonText.text = string.Format(reviveButtonTextFormat, reviveCoinsRequired);
            }
            else
            {
                reviveButtonText.text = noCoinsText;
            }
        }
        remainCoinNumber.text = "Remain Coins: " + currentCoins;
    }
    
    /// <summary>
    /// 更新重头再来按钮文本
    /// </summary>
    private void UpdateRestartButtonText()
    {
        if (restartButtonText != null)
        {
            restartButtonText.text = "Restart";
        }
    }
    
    /// <summary>
    /// 更新按钮交互状态
    /// </summary>
    private void UpdateButtonInteractability()
    {
        if (reviveButton != null)
        {
            reviveButton.interactable = CanRevive;
        }
        
        if (restartButton != null)
        {
            restartButton.interactable = true; // 重头再来总是可用
        }
    }
    
    /// <summary>
    /// 添加硬币
    /// </summary>
    /// <param name="amount">硬币数量</param>
    public void AddCoins(int amount)
    {
        currentCoins += amount;
        UpdateReviveButtonText();
        UpdateButtonInteractability();
        
        Debug.Log($"Added {amount} coins. Total: {currentCoins}");
    }
    
    /// <summary>
    /// 消耗硬币
    /// </summary>
    /// <param name="amount">消耗的硬币数量</param>
    /// <returns>是否成功消耗</returns>
    public bool ConsumeCoins(int amount)
    {
        if (currentCoins >= amount)
        {
            currentCoins -= amount;
            UpdateReviveButtonText();
            UpdateButtonInteractability();
            
            Debug.Log($"Consumed {amount} coins. Remaining: {currentCoins}");
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 设置硬币数量
    /// </summary>
    /// <param name="amount">硬币数量</param>
    public void SetCoins(int amount)
    {
        currentCoins = Mathf.Max(0, amount);
        UpdateReviveButtonText();
        UpdateButtonInteractability();
    }
    
    /// <summary>
    /// 设置复活需要的硬币数量
    /// </summary>
    /// <param name="amount">硬币数量</param>
    public void SetReviveCoinsRequired(int amount)
    {
        reviveCoinsRequired = Mathf.Max(1, amount);
        UpdateReviveButtonText();
        UpdateButtonInteractability();
    }
    
    // 编辑器方法
    [ContextMenu("测试显示游戏结束UI")]
    public void TestShowGameOverUI()
    {
        ShowGameOverUI();
    }
    
    [ContextMenu("测试隐藏游戏结束UI")]
    public void TestHideGameOverUI()
    {
        HideGameOverUI();
    }
    
    [ContextMenu("添加1个硬币")]
    public void TestAddCoin()
    {
        AddCoins(1);
    }
    
    [ContextMenu("消耗1个硬币")]
    public void TestConsumeCoin()
    {
        ConsumeCoins(1);
    }
    
    private void OnDestroy()
    {
        // 清理事件订阅
        if (player != null)
        {
            player.OnPlayerDeath -= OnPlayerDeath;
            player.OnPlayerRevived -= OnPlayerRevived;
        }
    }
}
