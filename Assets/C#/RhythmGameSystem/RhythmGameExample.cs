using UnityEngine;

/// <summary>
/// 音游系统使用示例
/// 演示如何在不同场景下使用音游机制
/// </summary>
public class RhythmGameExample : MonoBehaviour
{
    [Header("示例设置")]
    [SerializeField] private KeyCode testKey = KeyCode.R;
    [SerializeField] private bool autoTest = false;
    [SerializeField] private float autoTestInterval = 30f;
    
    private RhythmGameController rhythmController;
    private float autoTestTimer;
    
    private void Start()
    {
        // 查找或创建音游控制器
        rhythmController = FindObjectOfType<RhythmGameController>();
        
        if (rhythmController == null)
        {
            // 如果没有找到，创建一个新的
            GameObject controllerObj = new GameObject("RhythmGameController");
            rhythmController = controllerObj.AddComponent<RhythmGameController>();
            Debug.Log("Created new RhythmGameController for example");
        }
        
        // 订阅事件
        if (rhythmController != null)
        {
            rhythmController.OnRhythmGameStarted += OnRhythmGameStarted;
            rhythmController.OnRhythmGameEnded += OnRhythmGameEnded;
            rhythmController.OnJudgmentResult += OnJudgmentResult;
        }
    }
    
    private void Update()
    {
        // 按键测试
        if (Input.GetKeyDown(testKey))
        {
            TestRhythmGame();
        }
        
        // 自动测试
        if (autoTest)
        {
            autoTestTimer += Time.deltaTime;
            if (autoTestTimer >= autoTestInterval)
            {
                autoTestTimer = 0f;
                TestRhythmGame();
            }
        }
    }
    
    /// <summary>
    /// 测试音游功能
    /// </summary>
    private void TestRhythmGame()
    {
        if (rhythmController == null) return;
        
        if (rhythmController.IsGameRunning)
        {
            Debug.Log("Rhythm game already running, stopping it...");
            rhythmController.StopRhythmGame();
        }
        else
        {
            Debug.Log("Starting rhythm game test...");
            StartRandomRhythmGame();
        }
    }
    
    /// <summary>
    /// 启动随机配置的音游
    /// </summary>
    private void StartRandomRhythmGame()
    {
        // 随机选择退出条件
        RhythmGameController.RhythmGameExitCondition[] conditions = {
            RhythmGameController.RhythmGameExitCondition.Timer,
            RhythmGameController.RhythmGameExitCondition.ArcsDestroyed,
            RhythmGameController.RhythmGameExitCondition.AllEnemiesDead
        };
        
        var randomCondition = conditions[Random.Range(0, conditions.Length)];
        float duration = Random.Range(15f, 30f);
        int targetCount = Random.Range(5, 15);
        
        Debug.Log($"Starting rhythm game with condition: {randomCondition}, duration: {duration:F1}s, target: {targetCount}");
        
        rhythmController.StartRhythmGame(randomCondition, duration, targetCount);
    }
    
    /// <summary>
    /// 音游开始事件
    /// </summary>
    private void OnRhythmGameStarted()
    {
        Debug.Log("🎵 Rhythm Game Started! Get ready to aim!");
        
        // 可以在这里添加音效、UI提示等
        ShowGameStartNotification();
    }
    
    /// <summary>
    /// 音游结束事件
    /// </summary>
    private void OnRhythmGameEnded()
    {
        Debug.Log("🎵 Rhythm Game Ended!");
        
        // 显示结果统计
        if (rhythmController != null)
        {
            Debug.Log($"Final Stats - Arcs Destroyed: {rhythmController.ArcsDestroyed}, Time Played: {rhythmController.GameTimer:F1}s");
        }
        
        ShowGameEndNotification();
    }
    
    /// <summary>
    /// 判定结果事件
    /// </summary>
    /// <param name="success">是否成功</param>
    private void OnJudgmentResult(bool success)
    {
        if (success)
        {
            Debug.Log("💥 Perfect Hit! Enemies damaged!");
            ShowSuccessEffect();
        }
        else
        {
            Debug.Log("❌ Miss! Player takes damage!");
            ShowFailureEffect();
        }
    }
    
    /// <summary>
    /// 显示游戏开始通知
    /// </summary>
    private void ShowGameStartNotification()
    {
        // 这里可以显示UI提示、播放音效等
        // 例如：UIManager.ShowNotification("Rhythm Game Started!");
    }
    
    /// <summary>
    /// 显示游戏结束通知
    /// </summary>
    private void ShowGameEndNotification()
    {
        // 这里可以显示结果界面、播放结束音效等
        // 例如：UIManager.ShowGameResult(rhythmController.ArcsDestroyed);
    }
    
    /// <summary>
    /// 显示成功效果
    /// </summary>
    private void ShowSuccessEffect()
    {
        // 这里可以播放成功音效、显示粒子效果等
        // 例如：AudioManager.PlaySFX("perfect_hit");
        // 例如：EffectManager.PlayEffect("success_explosion", transform.position);
    }
    
    /// <summary>
    /// 显示失败效果
    /// </summary>
    private void ShowFailureEffect()
    {
        // 这里可以播放失败音效、显示屏幕震动等
        // 例如：AudioManager.PlaySFX("miss");
        // 例如：CameraShake.Shake(0.2f, 0.1f);
    }
    
    private void OnDestroy()
    {
        // 取消事件订阅
        if (rhythmController != null)
        {
            rhythmController.OnRhythmGameStarted -= OnRhythmGameStarted;
            rhythmController.OnRhythmGameEnded -= OnRhythmGameEnded;
            rhythmController.OnJudgmentResult -= OnJudgmentResult;
        }
    }
    
    // 编辑器测试方法
    [ContextMenu("Test Rhythm Game")]
    private void TestRhythmGameFromContext()
    {
        TestRhythmGame();
    }
    
    [ContextMenu("Start Timer Mode")]
    private void StartTimerMode()
    {
        if (rhythmController != null)
        {
            rhythmController.StartRhythmGame(RhythmGameController.RhythmGameExitCondition.Timer, 20f, 10);
        }
    }
    
    [ContextMenu("Start Arc Count Mode")]
    private void StartArcCountMode()
    {
        if (rhythmController != null)
        {
            rhythmController.StartRhythmGame(RhythmGameController.RhythmGameExitCondition.ArcsDestroyed, 60f, 10);
        }
    }
    
    [ContextMenu("Start All Enemies Dead Mode")]
    private void StartAllEnemiesDeadMode()
    {
        if (rhythmController != null)
        {
            rhythmController.StartRhythmGame(RhythmGameController.RhythmGameExitCondition.AllEnemiesDead, 120f, 20);
        }
    }
}
