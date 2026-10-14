using UnityEngine;
using System.Collections;

/// <summary>
/// 手势功能控制器 - 处理各种手势触发的功能
/// 设计原则：轻量级、低计算量、功能分离
/// </summary>
public class GestureFunctionController : MonoBehaviour
{
    [Header("功能开关")]
    public bool enablePulseGrenade = true; // 启用脉冲手雷功能
    public bool enableHealthRecover = true; // 启用生命值恢复功能
    public bool enablePause = true; // 启用暂停功能
    public bool enableReload = true; // 启用装弹功能
    public bool enableAiming = true; // 启用瞄准功能

    [Header("调试设置")]
    public bool showDebugLogs = true; // 显示调试日志

    // 单例实例
    public static GestureFunctionController Instance { get; private set; }
    // 功能状态
    private bool isPaused = false;
    private bool isAiming = false;
    private float noHandStartTime = -1f;
    private bool autoPausedByNoHand = false;
    private const float NO_HAND_PAUSE_DURATION = 3f;

    [Header("功能参数")]
    public float shootCooldown = 0.4f; // 射击冷却时间
    private float lastShootTime = 0f;  // 上次射击时间
    public float pulseGrenadeCooldown = 2f;
    private float lastPulseGrenadeTime = -999f;
    public GameObject pauseOverlay; // 暂停界面
    void Awake()
    {
        // 单例模式实现
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
    }

    void Start()
    {
        // 订阅手势变化事件
        if (InputManager_Hand.Instance != null)
        {
            InputManager_Hand.Instance.OnCurrentHandGestureChanged += OnHandGestureChanged;
        }
        else
        {
            Debug.LogWarning("[GestureFunctionController] InputManager_Hand.Instance 未找到，将在Start后尝试重新订阅");
            // 延迟订阅，防止初始化顺序问题
            StartCoroutine(DelayedSubscription());
        }
    }

    void Update()
    {
        if (!enablePause) return;
        if (InputManager_Hand.Instance == null) return;

        GameManager gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            if (!gameManager.GameStarted || gameManager.GameEnded || gameManager.IsInGestureTutorial)
            {
                noHandStartTime = -1f;
                return;
            }

            if (Time.timeScale == 0f && !gameManager.GamePaused)
            {
                noHandStartTime = -1f;
                return;
            }
        }
        else
        {
            if (Time.timeScale == 0f)
            {
                noHandStartTime = -1f;
                return;
            }
        }

        int targetHandIndex = InputManager_Hand.Instance.TargetHandIndex;
        bool hasHand = targetHandIndex != 0 && InputManager_Hand.Instance.HasHandData(targetHandIndex);

        if (hasHand)
        {
            noHandStartTime = -1f;
            if (autoPausedByNoHand && isPaused)
            {
                bool wasPaused = isPaused;
                TriggerPause();
                if (wasPaused && !isPaused)
                {
                    autoPausedByNoHand = false;
                }
            }
            return;
        }

        if (isPaused) return;

        if (noHandStartTime < 0f)
        {
            noHandStartTime = Time.unscaledTime;
            return;
        }

        if (Time.unscaledTime - noHandStartTime >= NO_HAND_PAUSE_DURATION)
        {
            bool wasPaused = isPaused;
            TriggerPause();
            if (!wasPaused && isPaused)
            {
                autoPausedByNoHand = true;
            }
            noHandStartTime = -1f;
        }
    }

    void OnDestroy()
    {
        // 取消订阅事件
        if (InputManager_Hand.Instance != null)
        {
            InputManager_Hand.Instance.OnCurrentHandGestureChanged -= OnHandGestureChanged;
        }
    }

    /// <summary>
    /// 延迟订阅事件（解决初始化顺序问题）
    /// </summary>
    IEnumerator DelayedSubscription()
    {
        yield return new WaitForSeconds(0.1f);

        if (InputManager_Hand.Instance != null)
        {
            InputManager_Hand.Instance.OnCurrentHandGestureChanged += OnHandGestureChanged;
            Debug.Log("[GestureFunctionController] 延迟订阅成功");
        }
    }

    /// <summary>
    /// 手势变化事件处理
    /// </summary>
    /// <param name="gestureState">手势状态</param>
    /// <param name="handIndex">手部索引(1=左手，2=右手)</param>
    void OnHandGestureChanged(HandGestureState gestureState, int handIndex)
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            if (gameManager.GestureTutorialCompletedFrame == Time.frameCount) return;
            if (gameManager.IsInGestureTutorial) return;
           // if (!gameManager.GameStarted || gameManager.GameEnded) return;
            if (gameManager.GamePaused && gestureState != HandGestureState.Pause) return;
        }

        switch (gestureState)
        {
            case HandGestureState.Shooting:
                if (enableAiming) // 使用瞄准开关来控制射击功能
                    TriggerShooting();
                break;

            case HandGestureState.PulseGrenade:
                if (enablePulseGrenade)
                    TriggerPulseGrenade();
                break;
/*
            case HandGestureState.HealthRecover:
                //if (enableHealthRecover)
                // TriggerHealthRecover();
                break;
*/
            case HandGestureState.Pause:
                if (enablePause)
                    TriggerPause();
                break;

            case HandGestureState.Reload:
                //if (enableReload)
                // TriggerReload();
                break;

            case HandGestureState.Aiming:
                //if (enableAiming)
                //TriggerAiming();
                break;
                /*   
                case HandGestureState.None:
                    // 手势结束，可以在这里处理一些清理工作
                    if (isAiming)
                    {
                        StopAiming();
                    }
                    break;*/
        }
    }

    /// <summary>
    /// 触发射击功能
    /// </summary>
    void TriggerShooting()
    {
        // 增加额外的冷却时间检查，防止事件触发频率过高
        if (Time.time - lastShootTime < shootCooldown)
        {
            return; // 如果未达到冷却时间，则忽略此次触发
        }
        lastShootTime = Time.time;

        if (showDebugLogs)
            Debug.Log("[GestureFunctionController] 射击功能触发！");

        // 使用协程来处理射击效果，防止阻塞主进程
        StartCoroutine(ShootingEffect());
    }

    /// <summary>
    /// 射击效果协程
    /// </summary>
    IEnumerator ShootingEffect()
    {
        if (showDebugLogs)
            Debug.Log("[GestureFunctionController] 射击效果开始");

        // 调用RaycastEffectController的射线效果功能
        if (RaycastEffectController.Instance != null)
        {
            RaycastEffectController.Instance.TriggerShootingRay();
        }
        else
        {
            Debug.LogWarning("[GestureFunctionController] RaycastEffectController.Instance 未找到");
        }
        CrosshairController.Instance.Shoot();
        // 等待射线效果完成（可以根据需要调整时间）
        yield return new WaitForSeconds(RaycastEffectController.Instance.rayDuration);

        if (showDebugLogs)
            Debug.Log("[GestureFunctionController] 射击效果结束");
    }

    /// <summary>
    /// 触发脉冲手雷功能
    /// </summary>
    void TriggerPulseGrenade()
    {
        if (Time.time - lastPulseGrenadeTime < pulseGrenadeCooldown)
        {
            return;
        }
        lastPulseGrenadeTime = Time.time;

        if (showDebugLogs)
            Debug.Log("[GestureFunctionController] 脉冲手雷功能触发！");

        // 这里可以添加脉冲手雷的具体实现
        // 例如：创建爆炸效果、播放音效、扣除弹药等

        // 示例：简单的视觉反馈
        StartCoroutine(PulseGrenadeEffect());
    }

    /// <summary>
    /// 脉冲手雷效果协程
    /// </summary>
    IEnumerator PulseGrenadeEffect()
    {

        // 简单的屏幕闪烁效果示例
        if (showDebugLogs)
            Debug.Log("[GestureFunctionController] 脉冲手雷爆炸效果开始");

        // 这里可以添加具体的爆炸效果
        yield return new WaitForSeconds(0.5f);

        if (showDebugLogs)
            Debug.Log("[GestureFunctionController] 脉冲手雷爆炸效果结束");

    }

    /// <summary>
    /// 触发生命值恢复功能
    /// </summary>
    void TriggerHealthRecover()
    {
        if (showDebugLogs)
            Debug.Log("[GestureFunctionController] 生命值恢复功能触发！");

        // 这里可以添加生命值恢复的具体实现
        // 例如：增加玩家血量、播放恢复音效、显示恢复特效等

        StartCoroutine(HealthRecoverEffect());
    }

    /// <summary>
    /// 生命值恢复效果协程
    /// </summary>
    IEnumerator HealthRecoverEffect()
    {
        if (showDebugLogs)
            Debug.Log("[GestureFunctionController] 生命值恢复效果开始");

        // 这里可以添加具体的恢复效果
        yield return new WaitForSeconds(1.0f);

        if (showDebugLogs)
            Debug.Log("[GestureFunctionController] 生命值恢复效果结束");
    }

    /// <summary>
    /// 触发暂停功能
    /// </summary>
    void TriggerPause()
    {
        if (GameManager.Instance != null && GameManager.Instance.GestureTutorialCompletedFrame == Time.frameCount)
        {
            return;
        }

        if (GameManager.Instance != null)
        {
            if (!GameManager.Instance.GameStarted || GameManager.Instance.GameEnded)
            {
                return;
            }
        }

        isPaused = !isPaused; // 切换暂停状态

        if (isPaused)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PauseGame();
            }
            if (pauseOverlay != null)
            {
                pauseOverlay.SetActive(true);
            }
            Time.timeScale = 0f; // 暂停游戏时间
            if (showDebugLogs)
                Debug.Log("[GestureFunctionController] 游戏暂停");
        }
        else
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResumeGame();
            }
            if (pauseOverlay != null)
            {
                pauseOverlay.SetActive(false);
            }
            Time.timeScale = 1f; // 恢复游戏时间
            lastShootTime = Time.time;
            if (showDebugLogs)
                Debug.Log("[GestureFunctionController] 游戏恢复");
        }
    }

    /// <summary>
    /// 触发装弹功能
    /// </summary>
    void TriggerReload()
    {
        if (showDebugLogs)
            Debug.Log("[GestureFunctionController] 装弹功能触发！");

        // 这里可以添加装弹的具体实现
        // 例如：重置弹药数量、播放装弹音效、显示装弹动画等

        StartCoroutine(ReloadEffect());
    }

    /// <summary>
    /// 装弹效果协程
    /// </summary>
    IEnumerator ReloadEffect()
    {
        if (showDebugLogs)
            Debug.Log("[GestureFunctionController] 装弹动画开始");

        // 这里可以添加具体的装弹效果
        yield return new WaitForSeconds(0.8f);

        if (showDebugLogs)
            Debug.Log("[GestureFunctionController] 装弹完成");
    }

    /// <summary>
    /// 触发瞄准功能
    /// </summary>
    void TriggerAiming()
    {
        if (!isAiming)
        {
            isAiming = true;
            if (showDebugLogs)
                Debug.Log("[GestureFunctionController] 开始瞄准");

            // 这里可以添加瞄准的具体实现
            // 例如：显示瞄准镜、调整摄像头视角、提高准确度等
        }
    }

    /// <summary>
    /// 停止瞄准功能
    /// </summary>
    void StopAiming()
    {
        if (isAiming)
        {
            isAiming = false;
            if (showDebugLogs)
                Debug.Log("[GestureFunctionController] 停止瞄准");

            // 这里可以添加停止瞄准的具体实现
            // 例如：隐藏瞄准镜、恢复摄像头视角等
        }
    }

    /// <summary>
    /// 获取当前暂停状态
    /// </summary>
    public bool IsPaused => isPaused;

    /// <summary>
    /// 获取当前瞄准状态
    /// </summary>
    public bool IsAiming => isAiming;
}
