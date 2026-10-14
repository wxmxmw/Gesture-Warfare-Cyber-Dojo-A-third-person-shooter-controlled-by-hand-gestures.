using UnityEngine;

/// <summary>
/// 音游系统测试脚本
/// 用于快速测试音游功能，不需要预制体
/// </summary>
public class RhythmGameTester : MonoBehaviour
{
    [Header("测试设置")]
    [SerializeField] private KeyCode startGameKey = KeyCode.Space;
    [SerializeField] private KeyCode stopGameKey = KeyCode.Escape;
    
    [Header("音游参数测试")]
    [SerializeField] private float testCircleRadius = 150f;
    [SerializeField] private float testArcAngleRange = 60f;
    [SerializeField] private float testSectorJudgmentAngle = 30f;
    [SerializeField] private float testArcSpawnInterval = 1.5f;
    [SerializeField] private float testGameDuration = 20f;
    
    private RhythmGameController rhythmController;
    private bool isInitialized = false;
    
    private void Start()
    {
        InitializeRhythmGame();
    }
    
    private void Update()
    {
        if (!isInitialized) return;
        
        // 开始游戏
        if (Input.GetKeyDown(startGameKey))
        {
            StartTestGame();
        }
        
        // 停止游戏
        if (Input.GetKeyDown(stopGameKey))
        {
            StopTestGame();
        }
        
        // 显示状态信息
        if (rhythmController != null && rhythmController.IsGameRunning)
        {
            DisplayGameInfo();
        }
    }
    
    /// <summary>
    /// 初始化音游系统
    /// </summary>
    private void InitializeRhythmGame()
    {
        // 查找或创建音游控制器
        rhythmController = FindObjectOfType<RhythmGameController>();
        
        if (rhythmController == null)
        {
            // 创建新的音游控制器
            GameObject controllerObj = new GameObject("RhythmGameController_Tester");
            rhythmController = controllerObj.AddComponent<RhythmGameController>();
            
            Debug.Log("Created RhythmGameController for testing");
        }
        
        // 设置参数
        SetupRhythmGameParameters();
        
        // 订阅事件
        rhythmController.OnRhythmGameStarted += OnGameStarted;
        rhythmController.OnRhythmGameEnded += OnGameEnded;
        rhythmController.OnJudgmentResult += OnJudgmentResult;
        
        isInitialized = true;
        
        Debug.Log($"音游测试器初始化完成！按 {startGameKey} 开始游戏，按 {stopGameKey} 停止游戏");
    }
    
    /// <summary>
    /// 设置音游参数
    /// </summary>
    private void SetupRhythmGameParameters()
    {
        if (rhythmController == null) return;
        
        // 通过反射设置私有字段，或者添加公共设置方法
        // 这里我们直接在Inspector中调整参数
        Debug.Log($"音游参数设置：圆圈半径={testCircleRadius}, 圆弧角度={testArcAngleRange}, 判定角度={testSectorJudgmentAngle}");
    }
    
    /// <summary>
    /// 开始测试游戏
    /// </summary>
    private void StartTestGame()
    {
        if (rhythmController == null) return;
        
        if (rhythmController.IsGameRunning)
        {
            Debug.Log("游戏已在运行中！");
            return;
        }
        
        Debug.Log("开始音游测试...");
        rhythmController.StartRhythmGame(
            RhythmGameController.RhythmGameExitCondition.Timer,
            testGameDuration,
            10
        );
    }
    
    /// <summary>
    /// 停止测试游戏
    /// </summary>
    private void StopTestGame()
    {
        if (rhythmController == null) return;
        
        if (!rhythmController.IsGameRunning)
        {
            Debug.Log("游戏未在运行！");
            return;
        }
        
        Debug.Log("停止音游测试...");
        rhythmController.StopRhythmGame();
    }
    
    /// <summary>
    /// 显示游戏信息
    /// </summary>
    private void DisplayGameInfo()
    {
        if (rhythmController == null) return;
        
        // 每5秒显示一次状态
        if (Time.time % 5f < 0.1f)
        {
            Debug.Log($"音游状态 - 时间: {rhythmController.GameTimer:F1}s, 消除数: {rhythmController.ArcsDestroyed}");
        }
    }
    
    /// <summary>
    /// 游戏开始事件
    /// </summary>
    private void OnGameStarted()
    {
        Debug.Log("🎵 音游开始！准备瞄准圆弧！");
    }
    
    /// <summary>
    /// 游戏结束事件
    /// </summary>
    private void OnGameEnded()
    {
        Debug.Log($"🎵 音游结束！最终成绩 - 消除数: {rhythmController.ArcsDestroyed}, 游戏时间: {rhythmController.GameTimer:F1}s");
    }
    
    /// <summary>
    /// 判定结果事件
    /// </summary>
    /// <param name="success">是否成功</param>
    private void OnJudgmentResult(bool success)
    {
        if (success)
        {
            Debug.Log("💥 完美击中！所有敌人受到伤害！");
        }
        else
        {
            Debug.Log("❌ 未命中！玩家受到伤害！");
        }
    }
    
    private void OnDestroy()
    {
        // 清理事件订阅
        if (rhythmController != null)
        {
            rhythmController.OnRhythmGameStarted -= OnGameStarted;
            rhythmController.OnRhythmGameEnded -= OnGameEnded;
            rhythmController.OnJudgmentResult -= OnJudgmentResult;
        }
    }
    
    // Inspector按钮方法
    [ContextMenu("开始音游测试")]
    private void StartGameFromInspector()
    {
        StartTestGame();
    }
    
    [ContextMenu("停止音游测试")]
    private void StopGameFromInspector()
    {
        StopTestGame();
    }
    
    private void OnGUI()
    {
        if (!isInitialized) return;
        
        // 显示简单的UI信息
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("音游测试器", GUI.skin.box);
        
        if (rhythmController != null)
        {
            if (rhythmController.IsGameRunning)
            {
                GUILayout.Label($"游戏运行中...");
                GUILayout.Label($"时间: {rhythmController.GameTimer:F1}s");
                GUILayout.Label($"消除数: {rhythmController.ArcsDestroyed}");
                
                if (GUILayout.Button("停止游戏 (Esc)"))
                {
                    StopTestGame();
                }
            }
            else
            {
                GUILayout.Label($"游戏未运行");
                GUILayout.Label($"圆圈半径: {testCircleRadius}");
                GUILayout.Label($"圆弧角度: {testArcAngleRange}°");
                GUILayout.Label($"判定角度: {testSectorJudgmentAngle}°");
                
                if (GUILayout.Button("开始游戏 (Space)"))
                {
                    StartTestGame();
                }
            }
        }
        
        GUILayout.EndArea();
    }
}
