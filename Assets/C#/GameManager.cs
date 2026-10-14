using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI; // Added for List

/// <summary>
/// 游戏管理器
/// 控制游戏的整体流程和状态
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Game State")]
    [SerializeField] private bool gameStarted = false;
    [SerializeField] private bool gamePaused = false;
    [SerializeField] private bool gameEnded = false;
    [SerializeField] private bool gestureTutorialCompleted = false;
    private bool isInGestureTutorial = false;
    private HandGestureState[] gestureTutorialSequence;
    private static readonly HandGestureState[] sniperGestureTutorialSequenceTemplate = new HandGestureState[]
    {
        HandGestureState.Aiming,
        HandGestureState.Shooting,
        HandGestureState.PulseGrenade,
        HandGestureState.Reload,
        HandGestureState.Pause
    };
    private static readonly HandGestureState[] pistolGestureTutorialSequenceTemplate = new HandGestureState[]
    {
        HandGestureState.Shooting,
        HandGestureState.PulseGrenade,
        HandGestureState.Reload,
        HandGestureState.Pause
    };
    private int currentTutorialIndex = 0;
    private int gestureTutorialCompletedFrame = -1;

    [Header("Game Components")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private StartButton startButton;
    [SerializeField] private GameObject startMenuUI; // 开始菜单UI
    [SerializeField] private GameObject gameOverUI; // 游戏结束UI
    [SerializeField] private GameObject settlementUI; // 结算界面UI
    [SerializeField] private Player player; // 玩家引用
    [SerializeField] private GameOverUIController gameOverUIController; // 游戏结束UI控制器
    [SerializeField] public RawImage BackgroundTexture; // 背景材质
    [SerializeField] public Camera BackgroundCamera; // 背景摄像机

    private RenderTexture rt = null;

    [SerializeField] private GameObject StartButton_Prefab; // 开始按钮 预制体

    // 添加结算界面相关变量
    private bool isShowingSettlement = false;
    private int completedLevelIndex = -1;

    [Header("BGM Settings")]
    [SerializeField] private BGMManager.BGMType menuBGMType = BGMManager.BGMType.Menu;
    [SerializeField] private BGMManager.BGMType battleBGMType = BGMManager.BGMType.Battle;
    [SerializeField] private BGMManager.BGMType gameOverBGMType = BGMManager.BGMType.GameOver;

    [Header("Game Over Settings")]
    [SerializeField] private float gameOverDelay = 2f; // 游戏结束延迟时间

    // 组件引用
    private BGMManager bgmManager;
    private BackgroundCameraController backgroundCameraController;
    private InputManager_Hand handInputManager;
    private HandGestureUI handGestureUI;
    private HandGestureController handGestureController;

    // 事件
    public System.Action OnGameStarted;
    public System.Action OnGamePaused;
    public System.Action OnGameResumed;
    public System.Action OnGameEnded;
    public System.Action<GameOverReason> OnGameOver; // 游戏结束事件，包含结束原因

    // 单例
    public static GameManager Instance { get; private set; }

    // 属性
    public bool GameStarted => gameStarted;
    public bool GamePaused => gamePaused;
    public bool GameEnded => gameEnded;
    public Player Player => player; // 添加公共Player属性
    public bool GestureTutorialCompleted => gestureTutorialCompleted;
    public bool IsInGestureTutorial => isInGestureTutorial;
    public int GestureTutorialCompletedFrame => gestureTutorialCompletedFrame;

    // 游戏结束原因枚举
    public enum GameOverReason
    {
        PlayerDeath,    // 玩家死亡
        AllEnemiesDefeated, // 所有敌人被消灭
        ManualEnd       // 手动结束
    }

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

        InitializeGameManager();
    }

    private void Start()
    {
        // 初始化游戏状态
        InitializeGameState();
    }

    /// <summary>
    /// 初始化游戏管理器
    /// </summary>
    private void InitializeGameManager()
    {
        // 获取组件引用
        if (enemySpawner == null)
        {
            enemySpawner = FindObjectOfType<EnemySpawner>();
        }

        if (startButton == null)
        {
            startButton = FindObjectOfType<StartButton>();
        }

        if (player == null)
        {
            player = FindObjectOfType<Player>();
        }

        if (backgroundCameraController == null)
        {
            backgroundCameraController = FindObjectOfType<BackgroundCameraController>();
        }

        if (gameOverUIController == null)
        {
            gameOverUIController = FindObjectOfType<GameOverUIController>();
        }

        bgmManager = BGMManager.Instance;
        handInputManager = InputManager_Hand.Instance;
        if (handInputManager == null)
        {
            handInputManager = FindObjectOfType<InputManager_Hand>();
        }
        if (handInputManager != null)
        {
            handInputManager.OnCurrentHandGestureChanged += OnHandGestureChangedForTutorial;
        }
        if (handGestureUI == null)
        {
            handGestureUI = FindObjectOfType<HandGestureUI>();
        }
        if (handGestureController == null)
        {
            handGestureController = FindObjectOfType<HandGestureController>();
        }

        // 订阅事件
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        if (handInputManager != null)
        {
            handInputManager.OnCurrentHandGestureChanged -= OnHandGestureChangedForTutorial;
        }
    }

    /// <summary>
    /// 订阅事件
    /// </summary>
    private void SubscribeToEvents()
    {
        // 订阅开始按钮事件
        if (startButton != null)
        {
            startButton.OnButtonActivated += OnStartButtonActivated;
        }

        // 订阅玩家死亡事件
        if (player != null)
        {
            player.OnPlayerDeath += OnPlayerDeath;
        }

        // 订阅敌人生成器事件
        if (enemySpawner != null)
        {
            enemySpawner.OnAllEnemiesDefeated += OnAllEnemiesDefeated;
            // 添加关卡相关事件订阅
            enemySpawner.OnLevelCompleted += OnLevelCompleted;
            enemySpawner.OnLevelStarted += OnLevelStarted;
            enemySpawner.OnAllLevelsCompleted += OnAllLevelsCompleted;
        }
    }

    /// <summary>
    /// 初始化游戏状态
    /// </summary>
    private void InitializeGameState()
    {
        // 确保游戏开始时是暂停状态
        gameStarted = false;
        gamePaused = false;
        gameEnded = false;

        // 停止敌人生成
        if (enemySpawner != null)
        {
            enemySpawner.StopSpawning();
        }
        // 播放菜单BGM
        if (bgmManager != null)
        {
            bgmManager.PlayBGM(menuBGMType);
        }
        // 显示开始菜单UI，隐藏游戏结束UI
        if (startMenuUI != null)
        {
            startMenuUI.SetActive(true);
        }
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(false);
        }

        rt = new RenderTexture(Screen.width, Screen.height, 0);
        BackgroundTexture.texture = rt;
        BackgroundCamera.targetTexture = rt;

        Debug.Log("Game initialized in menu state");
    }

    private void StartGestureTutorial()
    {
        if (isInGestureTutorial) return;
        Time.timeScale = 1f;
        isInGestureTutorial = true;
        gestureTutorialCompleted = false;
        currentTutorialIndex = 0;
        if (handGestureController == null)
        {
            handGestureController = FindObjectOfType<HandGestureController>();
        }
        if (handGestureController != null && handGestureController.isSniper)
        {
            gestureTutorialSequence = sniperGestureTutorialSequenceTemplate;
        }
        else
        {
            gestureTutorialSequence = pistolGestureTutorialSequenceTemplate;
        }
        if (GestureFunctionController.Instance != null)
        {
            GestureFunctionController.Instance.enableAiming = false;
        }
        if (handGestureUI == null)
        {
            handGestureUI = FindObjectOfType<HandGestureUI>();
        }
        ShowGestureTutorialStep();
    }

    private void ShowGestureTutorialStep()
    {
        if (handGestureUI == null) return;
        if (currentTutorialIndex < 0 || currentTutorialIndex >= gestureTutorialSequence.Length)
        {
            handGestureUI.SetGestureTutorialMessage(string.Empty);
            return;
        }
        HandGestureState target = gestureTutorialSequence[currentTutorialIndex];
        string name = GetGestureDisplayName(target);
        string message = "手势教学 " + (currentTutorialIndex + 1) + "/" + gestureTutorialSequence.Length + "：请做出「" + name + "」手势\n<size=16>PC端请保持手部距离屏幕35-50cm</size>";
        handGestureUI.SetGestureTutorialMessage(message);
    }

    private string GetGestureDisplayName(HandGestureState state)
    {
        switch (state)
        {
            case HandGestureState.Aiming:
                return "狙击瞄准（狙击枪模式特有手势，仅有食指与大拇指伸直）";
            case HandGestureState.Shooting:
                return "射击(狙击模式需弯曲拇指；默认手枪模式拇指弯曲伸直都可)";
            case HandGestureState.PulseGrenade:
                return "脉冲手雷（握拳，确保拇指指尖与其他四指指尖贴合！）";
            case HandGestureState.Reload:
                return "装弹（点赞手势，拇指指向向上）";
            case HandGestureState.Pause:
                return "暂停（五指张开保持2s以上）";
            default:
                return state.ToString();
        }
    }

    private void CompleteGestureTutorial()
    {
        if (!isInGestureTutorial) return;
        gestureTutorialCompletedFrame = Time.frameCount;
        isInGestureTutorial = false;
        gestureTutorialCompleted = true;
        if (GestureFunctionController.Instance != null)
        {
            GestureFunctionController.Instance.enableAiming = true;
        }
        if (handGestureUI != null)
        {
            handGestureUI.SetGestureTutorialMessage(string.Empty);
        }
        StartGameInternal();
    }

    private void OnHandGestureChangedForTutorial(HandGestureState gestureState, int handIndex)
    {
        if (!isInGestureTutorial) return;
        if (gestureState == HandGestureState.None) return;
        if (handInputManager == null) return;
        int targetHandIndex = handInputManager.TargetHandIndex;
        if (targetHandIndex == 0 || targetHandIndex != handIndex) return;
        if (currentTutorialIndex < 0 || currentTutorialIndex >= gestureTutorialSequence.Length) return;
        HandGestureState target = gestureTutorialSequence[currentTutorialIndex];
        if (gestureState != target) return;
        currentTutorialIndex++;
        if (currentTutorialIndex >= gestureTutorialSequence.Length)
        {
            CompleteGestureTutorial();
        }
        else
        {
            ShowGestureTutorialStep();
        }
    }

    /// <summary>
    /// 开始游戏
    /// </summary>
    public void StartGame()
    {
        if (gameStarted || gameEnded) return;
        if (!gestureTutorialCompleted)
        {
            StartGestureTutorial();
            return;
        }
        StartGameInternal();
    }

    private void StartGameInternal()
    {
        if (gameStarted || gameEnded) return;

        Time.timeScale = 1f;
        gameStarted = true;
        gamePaused = false;
        gameEnded = false;

        Debug.Log("Game Started!");

        // 隐藏开始菜单UI
        if (startMenuUI != null)
        {
            startMenuUI.SetActive(false);
        }

        // 开始敌人生成
        if (enemySpawner != null)
        {
            enemySpawner.StartSpawning();
        }

        // 切换到战斗BGM
        if (bgmManager != null)
        {
            bgmManager.PlayBGM(battleBGMType);
        }

        // 触发游戏开始事件
        OnGameStarted?.Invoke();
    }

    /// <summary>
    /// 暂停游戏
    /// </summary>
    public void PauseGame()
    {
        if (!gameStarted || gamePaused || gameEnded) return;

        gamePaused = true;

        // 暂停敌人生成
        if (enemySpawner != null)
        {
            enemySpawner.StopSpawning();
        }

        // 暂停BGM
        if (bgmManager != null)
        {
            bgmManager.PauseBGM();
        }

        // 触发暂停事件
        OnGamePaused?.Invoke();

        Debug.Log("Game Paused");
    }

    /// <summary>
    /// 恢复游戏
    /// </summary>
    public void ResumeGame()
    {
        if (!gameStarted || !gamePaused || gameEnded) return;

        gamePaused = false;

        // 恢复敌人生成
        if (enemySpawner != null)
        {
            enemySpawner.StartSpawning();
        }

        // 恢复BGM
        if (bgmManager != null)
        {
            bgmManager.ResumeBGM();
        }

        // 触发恢复事件
        OnGameResumed?.Invoke();

        Debug.Log("Game Resumed");
    }

    /// <summary>
    /// 游戏结束
    /// </summary>
    /// <param name="reason">结束原因</param>
    public void GameOver(GameOverReason reason)
    {
        if (gameEnded) return;

        gameEnded = true;
        gameStarted = false;
        gamePaused = false;

        Debug.Log($"Game Over! Reason: {reason}");

        // 停止敌人生成
        if (enemySpawner != null)
        {
            enemySpawner.StopSpawning();
        }

        // 停止所有敌人动作
        StopAllEnemyActions();

        // 播放游戏结束BGM
        if (bgmManager != null)
        {
            bgmManager.PlayBGM(gameOverBGMType);
        }

        // 触发游戏结束事件
        OnGameOver?.Invoke(reason);
        OnGameEnded?.Invoke();

        // 检查是否所有关卡都完成了
        if (reason == GameOverReason.AllEnemiesDefeated)
        {
            // 所有关卡完成，显示结算界面并激活ReStart按钮
            ShowSettlementUIForGameCompletion();
        }
        else
        {
            // 其他情况（如玩家死亡），延迟后可以重新开始游戏
            StartCoroutine(GameOverSequence());
        }
    }

    /// <summary>
    /// 游戏结束序列
    /// </summary>
    private IEnumerator GameOverSequence()
    {
        yield return new WaitForSeconds(gameOverDelay);

        // 这里可以添加重新开始游戏的逻辑
        // 比如显示重新开始按钮等
    }

    /// <summary>
    /// 停止所有敌人动作
    /// </summary>
    private void StopAllEnemyActions()
    {
        // 停止所有敌人的移动和攻击
        EnemyWithNewAttackSystem[] enemies = FindObjectsOfType<EnemyWithNewAttackSystem>();
        foreach (var enemy in enemies)
        {
            if (enemy != null && !enemy.IsDead)
            {
                // 停止敌人的状态机
                var stateMachine = enemy.GetStateMachine();
                if (stateMachine != null)
                {
                    stateMachine.InterruptAttack();
                }
            }
        }

        // 停止所有Boss的动作
        BossController[] bosses = FindObjectsOfType<BossController>();
        foreach (var boss in bosses)
        {
            if (boss != null && !boss.IsDead)
            {
                // 这里可以添加停止Boss动作的逻辑
                // boss.StopAllActions();
            }
        }
    }

    /// <summary>
    /// 重新开始游戏
    /// </summary>
    public void RestartGame()
    {
        // 隐藏游戏结束UI
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(false);
        }

        // 清理所有敌人
        if (enemySpawner != null)
        {
            enemySpawner.ClearAllEnemies();
            enemySpawner.ClearAllBosses();
        }

        // 重置玩家状态
        if (player != null && player.IsDead)
        {
            player.Respawn(Vector3.zero); // 在原点重生
        }

        // 重新初始化游戏状态
        InitializeGameState();

        Debug.Log("Game Restarted");
    }

    /// <summary>
    /// 玩家死亡回调 - 修改为不直接触发GameOver
    /// </summary>
    private void OnPlayerDeath(Player deadPlayer)
    {
        Debug.Log("Player died, showing game over UI...");

        ShowGameOverUI();
        // 延迟显示UI，给死亡动画一些时间
        // Invoke(nameof(ShowGameOverUI), 1f);
    }

    /// <summary>
    /// 所有敌人被消灭回调 - 修改现有方法
    /// </summary>
    public void OnAllEnemiesDefeated()
    {
        // 只有在所有关卡都完成时才触发游戏结束
        if (enemySpawner != null && !enemySpawner.HasNextLevel())
        {
            Debug.Log("All enemies defeated, triggering game over...");
            GameOver(GameOverReason.AllEnemiesDefeated);
        }
    }

    /// <summary>
    /// 关卡完成回调
    /// </summary>
    private void OnLevelCompleted(int levelIndex)
    {
        Debug.Log($"Level {levelIndex + 1} completed!");

        // 保存完成的关卡索引
        completedLevelIndex = levelIndex;

        // 关闭自动开始下一关卡
        if (enemySpawner != null)
        {
            // 这里需要添加一个方法来设置autoStartNextLevel
            // enemySpawner.SetAutoStartNextLevel(false);
        }

        // 显示结算界面
        ShowSettlementUI();

        ChangeNextBackground();
    }

    /// <summary>
    /// 关卡开始回调
    /// </summary>
    private void OnLevelStarted(int levelIndex)
    {
        Debug.Log($"Level {levelIndex + 1} started!");
        // 这里可以添加关卡开始的特殊效果
    }

    /// <summary>
    /// 所有关卡完成回调
    /// </summary>
    private void OnAllLevelsCompleted()
    {
        Debug.Log("All levels completed! Triggering game over...");
        GameOver(GameOverReason.AllEnemiesDefeated);
    }

    /// <summary>
    /// 开始按钮激活回调
    /// </summary>
    private void OnStartButtonActivated(StartButton button)
    {
        Debug.Log("Start button activated, starting game...");
        StartGame();
    }

    // 添加关卡管理的公共方法
    public int GetCurrentLevelIndex()
    {
        if (enemySpawner != null)
        {
            return enemySpawner.GetCurrentLevelIndex();
        }
        return 0;
    }

    public int GetTotalLevelCount()
    {
        if (enemySpawner != null)
        {
            return enemySpawner.GetTotalLevelCount();
        }
        return 0;
    }

    public bool HasNextLevel()
    {
        if (enemySpawner != null)
        {
            return enemySpawner.HasNextLevel();
        }
        return false;
    }

    /// <summary>
    /// 显示结算界面
    /// </summary>
    public void ShowSettlementUI()
    {
        if (settlementUI != null)
        {
            settlementUI.SetActive(true);
            isShowingSettlement = true;

            // 更新结算界面内容
            UpdateSettlementContent();

            // 重置射击按钮，确保每次结算都有可交互的按钮
            SettlementUIController settlementController = settlementUI.GetComponent<SettlementUIController>();
            if (settlementController != null)
            {
                settlementController.ResetAndShowStartButton();
            }

            Debug.Log("Settlement UI shown");
        }
    }

    /// <summary>
    /// 隐藏结算界面
    /// </summary>
    private void HideSettlementUI()
    {
        if (settlementUI != null)
        {
            settlementUI.SetActive(false);
            isShowingSettlement = false;

            Debug.Log("Settlement UI hidden");
        }
    }

    /// <summary>
    /// 获取当前关卡击杀记录
    /// </summary>
    public List<EnemySpawner.KillRecord> GetCurrentLevelKillRecords()
    {
        if (enemySpawner != null)
        {
            return enemySpawner.GetCurrentLevelKillRecords();
        }
        return new List<EnemySpawner.KillRecord>();
    }

    /// <summary>
    /// 获取当前关卡总分数
    /// </summary>
    public int GetCurrentLevelTotalScore()
    {
        if (enemySpawner != null)
        {
            return enemySpawner.GetCurrentLevelTotalScore();
        }
        return 0;
    }

    /// <summary>
    /// 更新结算界面内容
    /// </summary>
    private void UpdateSettlementContent()
    {
        if (settlementUI != null)
        {
            SettlementUIController settlementController = settlementUI.GetComponent<SettlementUIController>();
            if (settlementController != null)
            {
                List<EnemySpawner.KillRecord> killRecords = GetCurrentLevelKillRecords();
                int totalScore = GetCurrentLevelTotalScore();

                settlementController.SetSettlementData($"Level {completedLevelIndex + 1} Completed!", killRecords, totalScore);
            }
        }
    }

    /// <summary>
    /// 开始下一关卡（从结算界面调用）
    /// </summary>
    public void StartNextLevelFromSettlement()
    {
        if (isShowingSettlement && enemySpawner != null)
        {
            // 隐藏结算界面
            HideSettlementUI();

            // autoStartNextLevel=true 时，下一关已在关卡完成时自动开始，无需重复调用
            // autoStartNextLevel=false 时，需要在此手动启动下一关
            if (!enemySpawner.GetautoStartNextLevel())
            {
                enemySpawner.StartNextLevel();
            }

            Debug.Log("Starting next level from settlement");
        }
    }

    /// <summary>
    /// 重新开始当前关卡（从结算界面调用）
    /// </summary>
    public void RestartCurrentLevelFromSettlement()
    {
        if (isShowingSettlement && enemySpawner != null)
        {
            // 隐藏结算界面
            HideSettlementUI();

            // 重新开始当前关卡
            enemySpawner.StartCurrentLevel();

            Debug.Log("Restarting current level from settlement");
        }
    }

    /// <summary>
    /// 显示游戏结束UI
    /// </summary>
    public void ShowGameOverUI()
    {
        // 停止所有敌人动作
        StopAllEnemyActions();

        if (gameOverUIController != null)
        {
            gameOverUIController.ShowGameOverUI();
        }
        else if (gameOverUI != null)
        {
            // 如果没有GameOverUIController，使用旧的UI
            gameOverUI.SetActive(true);
        }

        // 播放游戏结束BGM
        if (bgmManager != null)
        {
            bgmManager.PlayBGM(gameOverBGMType);
        }
    }

    /// <summary>
    /// 隐藏游戏结束UI
    /// </summary>
    public void HideGameOverUI()
    {
        if (gameOverUIController != null)
        {
            gameOverUIController.HideGameOverUI();
        }
        else if (gameOverUI != null)
        {
            gameOverUI.SetActive(false);
        }
    }

    /// <summary>
    /// 重头再来 - 从关卡1重新开始
    /// </summary>
    public void RestartFromBeginning()
    {
        // 隐藏游戏结束UI
        HideGameOverUI();

        // 显示结算界面
        // ShowSettlementUI();
        // 不显示了

        // 重置游戏状态
        gameEnded = false;
        gameStarted = false;
        gamePaused = false;

        // 清理所有敌人
        if (enemySpawner != null)
        {
            enemySpawner.ClearAllEnemies();
            enemySpawner.ClearAllBosses();
            enemySpawner.StartCurrentLevel(); // 重置到第一关
        }

        // 重置玩家状态
        if (player != null)
        {
            player.RestartFromBeginning();
        }

        Debug.Log("Game restarted from beginning");

        CreateStartButton();
    }

    public void ChangeNextBackground()
    {
        backgroundCameraController.SwitchToNextScene();
    }

    // 创建开始按钮 同时会订阅开始按钮被击中事件来开始游戏
    public void CreateStartButton()
    {
        Instantiate(StartButton_Prefab);
    }

    // 将背景变成黑色幕布
    public void BackgroundChangeToBlackCurtain()
    {
        BackgroundTexture.color = Color.black;
    }

    // 将背景切换为原来的色彩（纯白 就是正常显示texture来的图像）
    public void BackgroundChangeToWhiteCurtain()
    {
        StartCoroutine(BackgroundFadeToWhiteCoroutine());
    }

    /// <summary>
    /// 背景颜色渐变到白色的协程
    /// </summary>
    private IEnumerator BackgroundFadeToWhiteCoroutine()
    {
        // 记录起始颜色
        Color startColor = BackgroundTexture.color;
        Color targetColor = Color.white;

        // 渐变时间
        float fadeDuration = 1.0f;
        float elapsedTime = 0f;

        // 执行渐变
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeDuration;

            // 使用平滑插值
            BackgroundTexture.color = Color.Lerp(startColor, targetColor, progress);

            yield return null;
        }

        // 确保最终颜色准确
        BackgroundTexture.color = targetColor;
    }

    /// <summary>
    /// 显示游戏完成的结算界面
    /// </summary>
    private void ShowSettlementUIForGameCompletion()
    {
        if (settlementUI != null)
        {
            settlementUI.SetActive(true);
            isShowingSettlement = true;

            // 获取结算界面控制器
            SettlementUIController settlementController = settlementUI.GetComponent<SettlementUIController>();
            if (settlementController != null)
            {
                // 激活ReStart按钮（UI点击备用方案）
                settlementController.ShowReStartButton();

                // 重置射击按钮，确保玩家可以通过射击关闭结算界面
                settlementController.ResetAndShowStartButton();
            }

            Debug.Log("Settlement UI shown for game completion with ReStart button");
        }
    }
}

