using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // 添加UI命名空间
using Mediapipe; // 添加Mediapipe命名空间

public class HandGestureController : MonoBehaviour
{
    public MyHandTrackingSolution handTrackingSolution; // 手部追踪解决方案

    [Header("武器设置")]
    public bool isSniper = false; // 是否为狙击枪模式

    void Awake()
    {
        // 简单的单例检查，防止场景中有多个实例导致UI绑定错误
        var controllers = FindObjectsOfType<HandGestureController>();
        if (controllers.Length > 1)
        {
            Debug.LogError($"[HandGestureController] 警告：场景中存在 {controllers.Length} 个 HandGestureController 实例！请确保只有一个。");
        }
    }

    // 射击冷却时间 - 已移动到 GestureFunctionController 控制
    // public float shootCooldown = 0.05f;
    // private float lastShootTime = 0f;

    // 手势状态持续时间跟踪 - 分别为左右手创建独立的计时器
    private float[] pulseGrenadeStartTime = new float[] { -1f, -1f }; // [0]左手, [1]右手
    private float[] healthRecoverStartTime = new float[] { -1f, -1f };
    private float[] pauseStartTime = new float[] { -1f, -1f };
    private float[] reloadStartTime = new float[] { -1f, -1f };

    private bool[] pulseGrenadeTriggered = new bool[] { false, false };
    private bool[] healthRecoverTriggered = new bool[] { false, false };
    private bool[] pauseTriggered = new bool[] { false, false };
    private bool[] reloadTriggered = new bool[] { false, false };

    // 手势持续时间阈值 - 降低阈值以提高触发准确性
    private const float PULSE_GRENADE_DURATION = 0.5f; // 脉冲手雷需要保持0.5秒
    private const float HEALTH_RECOVER_DURATION = 0.5f; // 生命值恢复需要保持0.5秒  
    private const float PAUSE_DURATION = 2.5f; // 暂停需要保持2.5秒
    private const float RELOAD_DURATION = 1.5f; // 装弹需要保持1.5秒

    void Update()
    {
        if (InputManager_Hand.Instance == null) return;

        // 获取当前活跃的手部索引
        int targetHandIndex = InputManager_Hand.Instance.TargetHandIndex;

        // 只检测targetHandIndex指定的手部状态
        if (targetHandIndex == 1) // 左手活跃
        {
            if (InputManager_Hand.Instance.HasHandData(1))
            {
                // 检测左手手势并更新状态
                DetectAndUpdateGestureState(1);
            }
            else
            {
                InputManager_Hand.Instance.LeftHandGestureState = HandGestureState.None;
                ResetGestureTimersForHand(1);
            }
            // 右手设置为None
            InputManager_Hand.Instance.RightHandGestureState = HandGestureState.None;
            ResetGestureTimersForHand(2);
        }
        else if (targetHandIndex == 2) // 右手活跃
        {
            if (InputManager_Hand.Instance.HasHandData(2))
            {
                // 检测右手手势并更新状态
                DetectAndUpdateGestureState(2);
            }
            else
            {
                InputManager_Hand.Instance.RightHandGestureState = HandGestureState.None;
                ResetGestureTimersForHand(2);
            }
            // 左手设置为None
            InputManager_Hand.Instance.LeftHandGestureState = HandGestureState.None;
            ResetGestureTimersForHand(1);
        }
        else // 没有活跃的手部数据
        {
            // 两只手都设置为None
            InputManager_Hand.Instance.LeftHandGestureState = HandGestureState.None;
            InputManager_Hand.Instance.RightHandGestureState = HandGestureState.None;
            ResetGestureTimers();
        }
    }

    /// <summary>
    /// 检测并更新手势状态
    /// </summary>
    /// <param name="handIndex">手的索引（1 表示左手，2 表示右手）</param>
    void DetectAndUpdateGestureState(int handIndex)
    {
        // 调试日志：输出当前模式（每60帧输出一次，避免刷屏）
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[HandGestureController] 当前武器模式: {(isSniper ? "狙击枪" : "手枪")}");
        }

        // 按优先级检测手势 
        // 1. 检测脉冲手雷手势（拇指食指中指捏合）
        int arrayIndex = handIndex - 1; // 转换为数组索引 (1->0, 2->1)
        if (InputManager_Hand.Instance.IsPulseGrenadeGesture(handIndex))
        {
            if (pulseGrenadeStartTime[arrayIndex] < 0)
            {
                pulseGrenadeStartTime[arrayIndex] = Time.unscaledTime;
                pulseGrenadeTriggered[arrayIndex] = false;
            }

            if (!pulseGrenadeTriggered[arrayIndex] && Time.unscaledTime - pulseGrenadeStartTime[arrayIndex] >= PULSE_GRENADE_DURATION)
            {
                SetHandGestureState(handIndex, HandGestureState.PulseGrenade);
                pulseGrenadeTriggered[arrayIndex] = true;
                pulseGrenadeStartTime[arrayIndex] = -1f; // 触发后重置计时器

            }
            return;
        }
        else
        {
            pulseGrenadeStartTime[arrayIndex] = -1f;
            pulseGrenadeTriggered[arrayIndex] = false;
        }
        // 2. 检测射击手势
        bool isShooting = false;
        // 根据武器类型选择不同的射击检测逻辑
        if (isSniper)
        {
            // 狙击枪模式：
            // 瞄准：拇指竖起（原 Aiming 手势）
            // 射击：拇指弯曲（原 Shooting 手势）
            // 注意：这里需要严格区分，避免误判

            if (InputManager_Hand.Instance.IsAimingGesture(handIndex))
            {
                // 瞄准手势：食指伸直，拇指伸直（90度），其他手指弯曲
                SetHandGestureState(handIndex, HandGestureState.Aiming);
                return;
            }
            else if (InputManager_Hand.Instance.IsSniperShootingGesture(handIndex))
            {
                // 狙击射击手势：食指伸直，拇指弯曲，其他手指弯曲
                isShooting = true;
            }
        }
        else
        {
            // 普通手枪模式：
            // 只要食指伸直，其他三指弯曲，无论拇指是否弯曲都算射击
            if (InputManager_Hand.Instance.IsShootingGesture(handIndex))
            {
                isShooting = true;
            }
        }

        if (isShooting)
        {
            // 直接设置状态，冷却时间由 GestureFunctionController 统一控制
            SetHandGestureState(handIndex, HandGestureState.Shooting);
            return;
        }
        /*
        // 3. 跳过检测生命值恢复手势（OK手势）
        if (InputManager_Hand.Instance.IsHealthRecoverGesture(handIndex))
        {
            if (healthRecoverStartTime[arrayIndex] < 0)
            {
                healthRecoverStartTime[arrayIndex] = Time.unscaledTime;
                healthRecoverTriggered[arrayIndex] = false;
            }

            if (!healthRecoverTriggered[arrayIndex] && Time.unscaledTime - healthRecoverStartTime[arrayIndex] >= HEALTH_RECOVER_DURATION)
            {
                SetHandGestureState(handIndex, HandGestureState.HealthRecover);
                healthRecoverTriggered[arrayIndex] = true;
                healthRecoverStartTime[arrayIndex] = -1f; // 触发后重置计时器
            }
            return;
        }
        else
        {
            healthRecoverStartTime[arrayIndex] = -1f;
            healthRecoverTriggered[arrayIndex] = false;
        }*/

        // 4. 检测暂停手势（五指张开）
        if (InputManager_Hand.Instance.IsPauseGesture(handIndex))
        {
            if (pauseStartTime[arrayIndex] < 0)
            {
                pauseStartTime[arrayIndex] = Time.unscaledTime;
                pauseTriggered[arrayIndex] = false;
            }

            if (!pauseTriggered[arrayIndex] && Time.unscaledTime - pauseStartTime[arrayIndex] >= PAUSE_DURATION)
            {
                SetHandGestureState(handIndex, HandGestureState.Pause);
                pauseTriggered[arrayIndex] = true;
                pauseStartTime[arrayIndex] = -1f; // 触发后重置计时器
            }
            return;
        }
        else
        {
            pauseStartTime[arrayIndex] = -1f;
            pauseTriggered[arrayIndex] = false;
        }

        // 5. 检测装弹手势（点赞手势）
        if (InputManager_Hand.Instance.IsReloadGesture(handIndex))
        {
            if (reloadStartTime[arrayIndex] < 0)
            {
                reloadStartTime[arrayIndex] = Time.unscaledTime;
                reloadTriggered[arrayIndex] = false;
            }

            if (!reloadTriggered[arrayIndex] && Time.unscaledTime - reloadStartTime[arrayIndex] >= RELOAD_DURATION)
            {
                SetHandGestureState(handIndex, HandGestureState.Reload);
                reloadTriggered[arrayIndex] = true;
                reloadStartTime[arrayIndex] = -1f; // 触发后重置计时器
            }
            return;
        }
        else
        {
            reloadStartTime[arrayIndex] = -1f;
            reloadTriggered[arrayIndex] = false;
        }

        /*
        // 6. 检测瞄准手势（手枪手势） - 已合并入手击手势
        if (InputManager_Hand.Instance.IsAimingGesture(handIndex))
        {
            SetHandGestureState(handIndex, HandGestureState.Aiming);
            //Debug.Log($"{handName}保持瞄准手势触发");
            return;
        }
        */

        // 如果没有检测到任何手势，设置为无手势状态
        SetHandGestureState(handIndex, HandGestureState.None);
    }

    /// <summary>
    /// 设置指定手的手势状态
    /// </summary>
    /// <param name="handIndex">手的索引（1 表示左手，2 表示右手）</param>
    /// <param name="gestureState">手势状态</param>
    void SetHandGestureState(int handIndex, HandGestureState gestureState)
    {
        if (handIndex == 1)
        {
            InputManager_Hand.Instance.LeftHandGestureState = gestureState;
        }
        else if (handIndex == 2)
        {
            InputManager_Hand.Instance.RightHandGestureState = gestureState;
        }
        // 同时更新CurrentHandGestureState属性
        InputManager_Hand.Instance.CurrentHandGestureState = gestureState;

    }

    /// <summary>
    /// 重置手势计时器
    /// </summary>
    void ResetGestureTimers()
    {
        for (int i = 0; i < 2; i++)
        {
            pulseGrenadeStartTime[i] = -1f;
            healthRecoverStartTime[i] = -1f;
            pauseStartTime[i] = -1f;
            reloadStartTime[i] = -1f;
            pulseGrenadeTriggered[i] = false;
            healthRecoverTriggered[i] = false;
            pauseTriggered[i] = false;
            reloadTriggered[i] = false;
        }
    }

    void ResetGestureTimersForHand(int handIndex)
    {
        int i = handIndex - 1;
        if (i < 0 || i >= 2) return;

        pulseGrenadeStartTime[i] = -1f;
        healthRecoverStartTime[i] = -1f;
        pauseStartTime[i] = -1f;
        reloadStartTime[i] = -1f;
        pulseGrenadeTriggered[i] = false;
        healthRecoverTriggered[i] = false;
        pauseTriggered[i] = false;
        reloadTriggered[i] = false;
    }


}
