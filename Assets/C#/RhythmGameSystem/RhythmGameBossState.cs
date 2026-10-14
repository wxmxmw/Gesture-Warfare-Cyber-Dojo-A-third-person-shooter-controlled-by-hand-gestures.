using UnityEngine;

/// <summary>
/// 音游Boss状态
/// 将音游机制集成到Boss状态机中的状态类
/// </summary>
public class RhythmGameBossState : IBossState
{
    [Header("音游设置")]
    private RhythmGameController.RhythmGameExitCondition exitCondition = RhythmGameController.RhythmGameExitCondition.Timer;
    private float gameDuration = 15f;
    private int targetArcsToDestroy = 8;
    
    private RhythmGameController rhythmGameController;
    private bool gameStarted = false;
    private bool gameCompleted = false;
    
    // 构造函数
    public RhythmGameBossState(RhythmGameController.RhythmGameExitCondition condition = RhythmGameController.RhythmGameExitCondition.Timer, 
                              float duration = 15f, int targetCount = 8)
    {
        exitCondition = condition;
        gameDuration = duration;
        targetArcsToDestroy = targetCount;
    }
    
    public void EnterState(BossController bossController)
    {
        Debug.Log($"Boss {bossController.name} entered Rhythm Game State");
        
        // 获取或创建音游控制器
        rhythmGameController = FindOrCreateRhythmGameController(bossController);
        
        if (rhythmGameController != null)
        {
            // 订阅游戏结束事件
            rhythmGameController.OnRhythmGameEnded += OnRhythmGameCompleted;
            
            // 启动音游
            rhythmGameController.StartRhythmGame(exitCondition, gameDuration, targetArcsToDestroy);
            gameStarted = true;
            
            Debug.Log($"Rhythm Game started with condition: {exitCondition}, duration: {gameDuration}, target: {targetArcsToDestroy}");
        }
        else
        {
            Debug.LogError("Failed to find or create RhythmGameController!");
            // 如果无法创建音游控制器，直接退出状态
            ExitToNextState(bossController);
        }
    }
    
    public void UpdateState(BossController bossController)
    {
        // 检查游戏是否完成
        if (gameCompleted)
        {
            ExitToNextState(bossController);
            return;
        }
        
        // 检查音游控制器状态
        if (rhythmGameController != null && !rhythmGameController.IsGameRunning && gameStarted)
        {
            // 游戏意外停止，退出状态
            ExitToNextState(bossController);
        }
    }
    
    public void ExitState(BossController bossController)
    {
        Debug.Log($"Boss {bossController.name} exited Rhythm Game State");
        
        // 取消订阅事件
        if (rhythmGameController != null)
        {
            rhythmGameController.OnRhythmGameEnded -= OnRhythmGameCompleted;
            
            // 如果游戏还在运行，停止它
            if (rhythmGameController.IsGameRunning)
            {
                rhythmGameController.StopRhythmGame();
            }
        }
        
        gameStarted = false;
        gameCompleted = false;
    }
    
    /// <summary>
    /// 查找或创建音游控制器
    /// </summary>
    /// <param name="bossController">Boss控制器</param>
    /// <returns>音游控制器</returns>
    private RhythmGameController FindOrCreateRhythmGameController(BossController bossController)
    {
        // 首先尝试在Boss身上查找
        RhythmGameController controller = bossController.GetComponent<RhythmGameController>();
        
        if (controller == null)
        {
            // 在场景中查找
            controller = Object.FindObjectOfType<RhythmGameController>();
        }
        
        if (controller == null)
        {
            // 创建新的音游控制器
            GameObject rhythmGameObj = new GameObject("RhythmGameController");
            controller = rhythmGameObj.AddComponent<RhythmGameController>();
            
            // 设置父物体为Boss（可选）
            rhythmGameObj.transform.SetParent(bossController.transform);
            
            Debug.Log("Created new RhythmGameController");
        }
        
        return controller;
    }
    
    /// <summary>
    /// 音游完成回调
    /// </summary>
    private void OnRhythmGameCompleted()
    {
        Debug.Log("Rhythm Game completed!");
        gameCompleted = true;
    }
    
    /// <summary>
    /// 退出到下一个状态
    /// </summary>
    /// <param name="bossController">Boss控制器</param>
    private void ExitToNextState(BossController bossController)
    {
        // 根据Boss类型决定下一个状态
        if (bossController is TestBoss testBoss)
        {
            // 回到TestBoss的准备状态
            bossController.GetComponent<BossStateMachine>().ChangeState(new TestBossPreparationState());
        }
        else if (bossController is TestBoss1 testBoss1)
        {
            // 回到TestBoss1的准备状态
            bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss1PreparationState());
        }
        else
        {
            // 通用处理 - 可以根据需要修改
            Debug.LogWarning($"Unknown boss type: {bossController.GetType().Name}, staying in current state");
        }
    }
}

/// <summary>
/// TestBoss的音游状态（继承通用音游状态）
/// </summary>
public class TestBossRhythmGameState : RhythmGameBossState
{
    public TestBossRhythmGameState(RhythmGameController.RhythmGameExitCondition condition = RhythmGameController.RhythmGameExitCondition.Timer, 
                                  float duration = 20f, int targetCount = 10) 
        : base(condition, duration, targetCount)
    {
    }
    
    public new void EnterState(BossController bossController)
    {
        Debug.Log("TestBoss entered Rhythm Game State");
        
        // 播放特殊动画（如果有）
        if (bossController is TestBoss testBoss)
        {
            testBoss.PlayAnimation(4); // 播放特殊动画
        }
        
        // 调用基类方法
        base.EnterState(bossController);
    }
}

/// <summary>
/// TestBoss1的音游状态（继承通用音游状态）
/// 注意：TestBoss1现在直接在SpecialState中集成音游，这个类保留用于其他需要的情况
/// </summary>
public class TestBoss1RhythmGameState : RhythmGameBossState
{
    public TestBoss1RhythmGameState(RhythmGameController.RhythmGameExitCondition condition = RhythmGameController.RhythmGameExitCondition.Timer, 
                                   float duration = 25f, int targetCount = 12) 
        : base(condition, duration, targetCount)
    {
    }
    
    public new void EnterState(BossController bossController)
    {
        Debug.Log("TestBoss1 entered standalone Rhythm Game State");
        
        // 播放特殊动画（如果有）
        if (bossController is TestBoss1 testBoss1)
        {
            testBoss1.PlayAnimation(4); // 播放特殊动画
        }
        
        // 调用基类方法
        base.EnterState(bossController);
    }
}
