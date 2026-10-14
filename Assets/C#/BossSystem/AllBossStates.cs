using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TestBoss 的出现状态
public class TestBossAppearState : IBossState 
{
    private float AppearDuration = 10f;
    private float AppearTimer;

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss {bossController.name} entered Appear State");
        AppearTimer = AppearDuration;

        // 播放出现动画
        if (bossController is TestBoss testBoss)
        {
            testBoss.PlayAnimation(0); // PlayAppearAnimation()
        }
    }

    public void UpdateState(BossController bossController)
    {
        AppearTimer -= Time.deltaTime;

        if (AppearTimer <= 0f)
        {
            bossController.GetComponent<BossStateMachine>().ChangeState(new TestBossPreparationState());
        }
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss {bossController.name} exited Appear State");
    }

}

// TestBoss 的静止状态
public class TestBossIdleState : IBossState
{
    private float idleDuration = 10f;
    private float idleTimer;

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss {bossController.name} entered Idle State");
        idleTimer = idleDuration;

        // 播放待机动画
        if (bossController is TestBoss testBoss)
        {
            testBoss.PlayAnimation(5); // PlayIdleAnimation()

            bossController.StartScreenPathMove(); // 随机选择路径点

        }
    }

    public void UpdateState(BossController bossController)
    {
        idleTimer -= Time.deltaTime;

        if (idleTimer <= 0f)
        {
            bossController.GetComponent<BossStateMachine>().ChangeState(new TestBossPreparationState());
        }
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss {bossController.name} exited Idle State");
    }
}

// TestBoss 的准备状态
public class TestBossPreparationState : IBossState
{
    private bool isAnimationFinished = false;
    private float damageTakenInThisState = 0f;
    private float lastHealth;

    private float Duration = 10f;
    private float Timer;


    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss {bossController.name} entered Preparation State");

        Timer = Duration;
        isAnimationFinished = false;
        damageTakenInThisState = 0f;
        lastHealth = bossController.CurrentHealth;

        // 播放准备动画
        if (bossController is TestBoss testBoss)
        {
            testBoss.PlayAnimation(2); // PlayAttackPreparationAnimation()

            bossController.StartScreenPathMove(); // 随机选择路径点

        }
    }

    public void UpdateState(BossController bossController)
    {
        Timer -= Time.deltaTime;

        // 检查动画是否播放完成
        if (Timer <= 0f && !isAnimationFinished && bossController is TestBoss testBoss)
        {
            if (testBoss.IsAnimationPlaying == false)
            {
                isAnimationFinished = true;
                OnPreparationFinished(bossController);
            }
        }

        // 检查血量变化
        float currentHealth = bossController.CurrentHealth;
        float damageTaken = lastHealth - currentHealth;
        if (damageTaken > 20f)
        {
            damageTakenInThisState += damageTaken;
            lastHealth = currentHealth;

            // 检查是否应该重新进入静止状态
            CheckDamageThreshold(bossController);
        }
    }

    private void CheckDamageThreshold(BossController bossController)
    {
        float threshold = bossController.HealthPercentage > 0.5f ? 50f : 100f; // 阶段一50，阶段二100

        if (damageTakenInThisState >= threshold)
        {
            Debug.Log($"TestBoss took {damageTakenInThisState} damage in preparation state, returning to idle");
            bossController.GetComponent<BossStateMachine>().ChangeState(new TestBossIdleState());
        }
    }

    private void OnPreparationFinished(BossController bossController)
    {
        Debug.Log("TestBoss preparation animation finished");

        // 50%概率进入攻击状态，50%概率进入特殊状态
        if (Random.Range(0f, 1f) < 0.5f)
        {
            bossController.GetComponent<BossStateMachine>().ChangeState(new TestBossAttackState());
        }
        else
        {
            bossController.GetComponent<BossStateMachine>().ChangeState(new TestBossSpecialState());
        }
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss {bossController.name} exited Preparation State");
    }
}

// TestBoss 的攻击状态
public class TestBossAttackState : IBossState
{
    private bool isAnimationFinished = false;
    private bool hasDealtDamage = false;

    private float Duration = 10f;
    private float Timer;

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss {bossController.name} entered Attack State");

        Timer = Duration;
        isAnimationFinished = false;
        hasDealtDamage = false;

        // 播放攻击动画
        if (bossController is TestBoss testBoss)
        {
            testBoss.PlayAnimation(3); // PlayAttackAnimation()
        }
    }

    public void UpdateState(BossController bossController)
    {
        Timer -= Time.deltaTime;
        // 检查动画是否播放完成
        if (Timer <= 0f && !isAnimationFinished && bossController is TestBoss testBoss)
        {
            if (testBoss.IsAnimationPlaying == false)
            {
                isAnimationFinished = true;
                OnAttackFinished(bossController);
            }
        }
    }

    private void OnAttackFinished(BossController bossController)
    {
        Debug.Log("TestBoss attack animation finished");

        // 对玩家造成10点伤害
        if (!hasDealtDamage)
        {
            DealDamageToPlayer(bossController, 10f);
            hasDealtDamage = true;
        }

        // 攻击结束后重新进入静止状态
        bossController.GetComponent<BossStateMachine>().ChangeState(new TestBossIdleState());
    }

    private void DealDamageToPlayer(BossController bossController, float damage)
    {
        Debug.Log($"TestBoss dealt {damage} damage to player");

        // 示例：查找玩家并造成伤害
        Player player = bossController.FindPlayer();
        if (player != null)
        {
            player.TakeDamage(damage);
        }
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss {bossController.name} exited Attack State");
    }
}

// TestBoss 的特殊状态
public class TestBossSpecialState : IBossState
{
    private float Duration = 10f;
    private float Timer;

    private bool isAnimationFinished = false;
    private float damageTakenInThisState = 0f;
    private float lastHealth;
    private bool hasDealtDamage = false;

    public void EnterState(BossController bossController)
    {
        Timer = Duration;

        Debug.Log($"TestBoss {bossController.name} entered Special State");
        isAnimationFinished = false;
        damageTakenInThisState = 0f;
        lastHealth = bossController.CurrentHealth;
        hasDealtDamage = false;

        // 播放特殊动画
        if (bossController is TestBoss testBoss)
        {
            testBoss.PlayAnimation(4); // PlaySpecialAnimation()
        }
    }

    public void UpdateState(BossController bossController)
    {
        Timer -= Time.deltaTime;

        // 检查血量变化
        float currentHealth = bossController.CurrentHealth;
        float damageTaken = lastHealth - currentHealth;
        if (damageTaken > 20f)
        {
            damageTakenInThisState += damageTaken;
            lastHealth = currentHealth;
        }

        // 检查动画是否播放完成
        if (Timer <= 0f && !isAnimationFinished && bossController is TestBoss testBoss)
        {
            if (testBoss.IsAnimationPlaying == false)
            {
                isAnimationFinished = true;
                OnSpecialFinished(bossController);
            }
        }
        
    }

    private void OnSpecialFinished(BossController bossController)
    {
        Debug.Log("TestBoss special animation finished");

        // 检查玩家是否造成了50点伤害
        if (damageTakenInThisState < 50f)
        {
            // 玩家没有造成足够伤害，对玩家造成20点伤害
            if (!hasDealtDamage)
            {
                DealDamageToPlayer(bossController, 20f);
                hasDealtDamage = true;
            }
        }

        // 特殊状态结束后重新进入准备状态
        bossController.GetComponent<BossStateMachine>().ChangeState(new TestBossPreparationState());
    }

    private void DealDamageToPlayer(BossController bossController, float damage)
    {
        Debug.Log($"TestBoss dealt {damage} damage to player in special state");

        // 示例：查找玩家并造成伤害
        Player player = bossController.FindPlayer();
        if (player != null)
        {
            player.TakeDamage(damage);
        }
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss {bossController.name} exited Special State");
    }
}

// TestBoss 的消失状态
public class TestBossDisappearState : IBossState
{
    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss {bossController.name} entered Disappear State");

        // 播放消失动画
        if (bossController is TestBoss testBoss)
        {
            testBoss.PlayAnimation(1); // PlayDisappearAnimation()
        }
    }

    public void UpdateState(BossController bossController)
    {
        // 检查动画是否播放完成
        /*
        if (bossController is TestBoss testBoss)
        {
            if (testBoss.IsAnimationPlaying == false)
            {
                OnDisappearFinished(bossController);
            }
        }
        */
    }

    private void OnDisappearFinished(BossController bossController)
    {
        Debug.Log("TestBoss disappear animation finished");

        // 动画播放完成后boss消失
        bossController.DestroyBoss();
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss {bossController.name} exited Disappear State");
    }
}

// TestBoss1 的状态实现
// TestBoss1 的出现状态
public class TestBoss1AppearState : IBossState
{
    private float AppearDuration = 3f; // 出现动画的持续时间
    private float AppearTimer;

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss {bossController.name} entered Appear State");
        AppearTimer = AppearDuration;

        // 播放出现动画
        if (bossController is TestBoss1 testBoss)
        {
            //testBoss.PlayAnimation(0); // PlayAppearAnimation()
        }
    }

    public void UpdateState(BossController bossController)
    {
        AppearTimer -= Time.deltaTime;

        if (AppearTimer <= 0f)
        {
            bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss1PreparationState());
        }
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss {bossController.name} exited Appear State");
    }

}

// TestBoss1 的静止状态
public class TestBoss1IdleState : IBossState
{
    private float idleDuration = 3f;
    private float idleTimer;

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss {bossController.name} entered Idle State");
        idleTimer = idleDuration;

        // 播放待机动画
        if (bossController is TestBoss1 testBoss)
        {
            testBoss.PlayAnimation(6); // PlayIdleAnimation()

            bossController.StartScreenPathMove(); // 随机选择路径点

        }
    }

    public void UpdateState(BossController bossController)
    {
        idleTimer -= Time.deltaTime;

        if (idleTimer <= 0f)
        {
            bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss1PreparationState());
        }
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss {bossController.name} exited Idle State");
    }
}

// TestBoss1 的准备状态
public class TestBoss1PreparationState : IBossState
{
    private bool isAnimationFinished = false;

    private float Duration = 2.5f;
    private float Timer;
    
    // 阶段和状态跟踪
    private static bool hasEnteredStage2 = false;  // 是否已进入stage2
    private static int attackCountInStage2 = 0;    // stage2中的攻击次数计数
    private static bool isFirstStage2Transition = false; // 是否是从stage1到stage2的首次转换

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss {bossController.name} entered Preparation State");

        Timer = Duration;
        isAnimationFinished = false;
        
        // 检查是否进入stage2
        CheckStageTransition(bossController);

        // 播放准备动画
        if (bossController is TestBoss1 testBoss)
        {
            testBoss.PlayAnimation(2); // PlayAttackPreparationAnimation()

            bossController.StartScreenPathMove(); // 随机选择路径点

        }
    }

    public void UpdateState(BossController bossController)
    {
        Timer -= Time.deltaTime;

        // 检查动画是否播放完成
        if (Timer <= 0f && !isAnimationFinished && bossController is TestBoss1 testBoss)
        {
            if (testBoss.IsAnimationPlaying == false)
            {
                isAnimationFinished = true;
                OnPreparationFinished(bossController);
            }
        }

    }

    private void OnPreparationFinished(BossController bossController)
    {
        Debug.Log("TestBoss preparation animation finished");

        // 根据阶段选择下一个状态
        if (IsInStage2(bossController))
        {
            // Stage2逻辑
            if (isFirstStage2Transition)
            {
                // 首次进入stage2，直接进入SpecialState
                Debug.Log("First time entering stage2, going to SpecialState");
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss1SpecialState());
                isFirstStage2Transition = false;
            }
            else
            {
                // 检查是否需要进入SpecialState（每2次attack后进入1次SpecialState）
                if (attackCountInStage2 >= 2)
                {
                    Debug.Log("Stage2: 2 attacks completed, going to SpecialState");
                    bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss1SpecialState());
                    attackCountInStage2 = 0; // 重置计数
                }
                else
                {
                    // 随机选择attack1或attack2
                    if (Random.Range(0f, 1f) < 0.5f)
                    {
                        Debug.Log("Stage2: Going to Attack1");
                        bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss1Attack1State());
                    }
                    else
                    {
                        Debug.Log("Stage2: Going to Attack2");
                        bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss1Attack2State());
                    }
                    attackCountInStage2++; // 增加攻击计数
                }
            }
        }
        else
        {
            // Stage1逻辑：1/3概率attack1，2/3概率attack2
            if (Random.Range(0f, 1f) < 1f/3f)
            {
                Debug.Log("Stage1: Going to Attack1 (1/3 probability)");
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss1Attack1State());
            }
            else
            {
                Debug.Log("Stage1: Going to Attack2 (2/3 probability)");
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss1Attack2State());
            }
        }
    }

    /// <summary>
    /// 检查阶段转换
    /// </summary>
    /// <param name="bossController">Boss控制器</param>
    private void CheckStageTransition(BossController bossController)
    {
        if (!hasEnteredStage2 && IsInStage2(bossController))
        {
            hasEnteredStage2 = true;
            isFirstStage2Transition = true;
            attackCountInStage2 = 0; // 重置stage2攻击计数
            Debug.Log("Boss entered Stage2! First transition detected.");
        }
    }
    
    /// <summary>
    /// 检查是否在stage2（血量低于1/3）
    /// </summary>
    /// <param name="bossController">Boss控制器</param>
    /// <returns>是否在stage2</returns>
    private bool IsInStage2(BossController bossController)
    {
        return bossController.HealthPercentage <= 2f/3f;
    }
    
    /// <summary>
    /// 重置阶段状态（用于Boss重生或重新开始）
    /// </summary>
    public static void ResetStageState()
    {
        hasEnteredStage2 = false;
        attackCountInStage2 = 0;
        isFirstStage2Transition = false;
        Debug.Log("Boss stage state reset.");
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss {bossController.name} exited Preparation State");
    }
}

// TestBoss1 的攻击状态
public class TestBoss1Attack1State : IBossState
{
    private bool isAnimationFinished = false;

    private float Duration = 4f;
    private float Timer;

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss {bossController.name} entered Attack State");

        Timer = Duration;
        isAnimationFinished = false;

        // 播放攻击动画
        if (bossController is TestBoss1 testBoss)
        {
            testBoss.PlayAnimation(3); // PlayAttack1Animation()
        }
    }

    public void UpdateState(BossController bossController)
    {
        Timer -= Time.deltaTime;
        // 检查动画是否播放完成
        if (Timer <= 0f && !isAnimationFinished && bossController is TestBoss1 testBoss)
        {
            if (testBoss.IsAnimationPlaying == false)
            {
                isAnimationFinished = true;
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss1IdleState());

            }
        }
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss {bossController.name} exited Attack State");
    }
}

// TestBoss1 的攻击状态
public class TestBoss1Attack2State : IBossState
{
    private float Duration = 3f;
    private float Timer;

    private bool isAnimationFinished = false;
    private float lastHealth;

    public void EnterState(BossController bossController)
    {
        Timer = Duration;

        Debug.Log($"TestBoss {bossController.name} entered Special State");
        isAnimationFinished = false;
        lastHealth = bossController.CurrentHealth;

        // 播放特殊动画
        if (bossController is TestBoss1 testBoss)
        {
            testBoss.PlayAnimation(4); // PlayAttack2Animation()
        }
    }

    public void UpdateState(BossController bossController)
    {
        Timer -= Time.deltaTime;

        // 检查血量变化
        float currentHealth = bossController.CurrentHealth;
        float damageTaken = lastHealth - currentHealth;
        if (damageTaken > 20f)
        {
            lastHealth = currentHealth;
        }

        // 检查动画是否播放完成
        if (Timer <= 0f && !isAnimationFinished && bossController is TestBoss1 testBoss)
        {
            if (testBoss.IsAnimationPlaying == false)
            {
                isAnimationFinished = true;
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss1PreparationState());
            }
        }

    }

    public void ExitState(BossController bossController)
    {
        bossController.StartScreenPathMove(); // 随机选择路径点

        Debug.Log($"TestBoss {bossController.name} exited Special State");
    }
}

// TestBoss1 的特殊状态 - 直接集成音游机制
public class TestBoss1SpecialState : IBossState
{
    [Header("音游设置")]
    private float rhythmGameDuration = 15f; // 音游持续时间
    private int targetArcsToDestroy = 10;    // 需要消除的判定块数量
    
    private RhythmGameController rhythmGameController;
    private bool rhythmGameStarted = false;
    private bool rhythmGameCompleted = false;
    private float stateTimer = 0f;

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss {bossController.name} entered Special State with Rhythm Game");
        
        // 播放特殊动画
        if (bossController is TestBoss1 testBoss1)
        {
            testBoss1.PlayAnimation(5); // 播放特殊动画
        }

        bossController.StartScreenPathMove(); // 随机选择路径点

        // 启动音游机制
        StartRhythmGame(bossController);
    }

    public void UpdateState(BossController bossController)
    {
        stateTimer += Time.deltaTime;
        
        // 检查音游是否完成
        if (rhythmGameCompleted || (rhythmGameController != null && !rhythmGameController.IsGameRunning))
        {
            // 音游结束，退出特殊状态
            Debug.Log("Rhythm Game completed, exiting Special State");
            bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss1PreparationState());
            return;
        }
        
        // 安全检查：如果音游运行时间过长，强制结束
        if (stateTimer > rhythmGameDuration + 5f)
        {
            Debug.LogWarning("Rhythm Game running too long, force ending");
            bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss1PreparationState());
        }
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss {bossController.name} exited Special State");
        
        // 清理音游
        CleanupRhythmGame();
    }
    
    /// <summary>
    /// 启动音游机制
    /// </summary>
    /// <param name="bossController">Boss控制器</param>
    private void StartRhythmGame(BossController bossController)
    {
        if (rhythmGameStarted) return;
        
        // 获取或创建音游控制器
        rhythmGameController = FindOrCreateRhythmGameController(bossController);
        
        if (rhythmGameController != null)
        {
            // 订阅音游结束事件
            rhythmGameController.OnRhythmGameEnded += OnRhythmGameCompleted;
            
            // 启动音游
            rhythmGameController.StartRhythmGame(
                RhythmGameController.RhythmGameExitCondition.Timer,
                rhythmGameDuration,
                targetArcsToDestroy
            );
            
            rhythmGameStarted = true;
            Debug.Log($"Rhythm Game started in Special State - Duration: {rhythmGameDuration}s, Target: {targetArcsToDestroy}");
        }
        else
        {
            Debug.LogError("Failed to create RhythmGameController in Special State!");
            rhythmGameCompleted = true; // 强制结束状态
        }
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
            controller = UnityEngine.Object.FindObjectOfType<RhythmGameController>();
        }
        
        if (controller == null)
        {
            // 创建新的音游控制器
            GameObject rhythmGameObj = new GameObject("RhythmGameController_Boss");
            controller = rhythmGameObj.AddComponent<RhythmGameController>();
            
            // 设置父物体为Boss（可选）
            rhythmGameObj.transform.SetParent(bossController.transform);
            
            Debug.Log("Created new RhythmGameController for Boss Special State");
        }
        
        return controller;
    }
    
    /// <summary>
    /// 音游完成回调
    /// </summary>
    private void OnRhythmGameCompleted()
    {
        Debug.Log("Rhythm Game completed in Special State!");
        rhythmGameCompleted = true;
    }
    
    /// <summary>
    /// 清理音游资源
    /// </summary>
    private void CleanupRhythmGame()
    {
        if (rhythmGameController != null)
        {
            // 取消事件订阅
            rhythmGameController.OnRhythmGameEnded -= OnRhythmGameCompleted;
            
            // 如果音游还在运行，停止它
            if (rhythmGameController.IsGameRunning)
            {
                rhythmGameController.StopRhythmGame();
            }
        }
        
        rhythmGameStarted = false;
        rhythmGameCompleted = false;
        stateTimer = 0f;
    }
}

// TestBoss1 的消失状态
public class TestBoss1DisappearState : IBossState
{
    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss {bossController.name} entered Disappear State");

        // 播放消失动画
        if (bossController is TestBoss testBoss)
        {
            testBoss.PlayAnimation(1); // PlayDisappearAnimation()
        }
    }

    public void UpdateState(BossController bossController)
    {

    }

    private void OnDisappearFinished(BossController bossController)
    {
        Debug.Log("TestBoss disappear animation finished");

        // 动画播放完成后boss消失
        bossController.DestroyBoss();
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss {bossController.name} exited Disappear State");
    }
}


// TestBoss2的状态类实现
// TestBoss2的出现状态
public class TestBoss2AppearState : IBossState
{
    private float appearDuration = 3f;
    private float appearTimer;

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss2 {bossController.name} entered Appear State");
        appearTimer = appearDuration;

        // 播放出现动画
        if (bossController is TestBoss2 testBoss2)
        {
            testBoss2.PlayAnimation(0); // PlayAppearAnimation()
        }
    }

    public void UpdateState(BossController bossController)
    {
        appearTimer -= Time.deltaTime;

        if (appearTimer <= 0f)
        {
            bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss2Attack1State());
        }
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss2 {bossController.name} exited Appear State");
    }
}

// TestBoss2的静止状态1
public class TestBoss2Idle1State : IBossState
{
    private float idleDuration = 1.5f;
    private float idleTimer;

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss2 {bossController.name} entered Idle1 State");
        idleTimer = idleDuration;

        // 播放待机动画
        if (bossController is TestBoss2 testBoss2)
        {
            testBoss2.PlayAnimation(6); // PlayIdleAnimation()
            testBoss2.StartScreenPathMove(-2);
        }
    }

    public void UpdateState(BossController bossController)
    {
        idleTimer -= Time.deltaTime;
        
        // 检查部位破坏状态或血量条件
        if (bossController is TestBoss2 testBoss2)
        {
            bool shouldSwitchToState2 = false;
            string switchReason = "";
            
            // 检查部位破坏条件
            if (testBoss2.IsAnyBodyPartDestroyed() && !testBoss2.GetisState2())
            {
                shouldSwitchToState2 = true;
                switchReason = "Body part destroyed";
            }
            // 检查血量条件（血量低于70%且还在一阶段）
            else if (bossController.HealthPercentage < 0.7f && !testBoss2.GetisState2())
            {
                shouldSwitchToState2 = true;
                switchReason = $"Health below 50% (current: {bossController.HealthPercentage:P1})";
            }
            
            if (shouldSwitchToState2)
            {
                Debug.Log($"TestBoss2: {switchReason}, switching to Attack3");
                testBoss2.SwitchToState2();//  切到二阶段
                testBoss2.RecalculateScreenPathPoints();// 重画路径点
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss2Attack3State());
                return;
            }
        }

        if (idleTimer <= 0f)
        {
            if (Random.Range(0f, 1f) < 0.5f)
            {
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss2Attack1State());
            }
            else 
            {
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss2Attack2State());
            }
        }
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss2 {bossController.name} exited Idle1 State");
    }
}

// TestBoss2的攻击状态1
public class TestBoss2Attack1State : IBossState
{
    private bool isAnimationFinished = false;
    private float duration = 3f;
    private float timer;

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss2 {bossController.name} entered Attack1 State");

        timer = duration;
        isAnimationFinished = false;

        // 播放攻击1动画
        if (bossController is TestBoss2 testBoss2)
        {
            testBoss2.PlayAnimation(3); // PlayAttack1Animation()
            testBoss2.StartScreenPathMove(-2);

        }
    }

    public void UpdateState(BossController bossController)
    {
        timer -= Time.deltaTime;

        // 检查部位破坏状态或血量条件
        if (bossController is TestBoss2 testBoss2)
        {
            bool shouldSwitchToState2 = false;
            string switchReason = "";

            // 检查部位破坏条件
            if (testBoss2.IsAnyBodyPartDestroyed() && !testBoss2.GetisState2())
            {
                shouldSwitchToState2 = true;
                switchReason = "Body part destroyed";
            }
            // 检查血量条件（血量低于70%且还在一阶段）
            else if (bossController.HealthPercentage < 0.7f && !testBoss2.GetisState2())
            {
                shouldSwitchToState2 = true;
                switchReason = $"Health below 50% (current: {bossController.HealthPercentage:P1})";
            }

            if (shouldSwitchToState2)
            {
                Debug.Log($"TestBoss2: {switchReason}, switching to Attack3");
                testBoss2.SwitchToState2();//  切到二阶段
                testBoss2.RecalculateScreenPathPoints();// 重画路径点
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss2Attack3State());
                return;
            }
        }

        // 检查动画是否播放完成
        if (timer <= 0f && !isAnimationFinished && bossController is TestBoss2 testBoss)
        {
            if (testBoss.IsAnimationPlaying == false)
            {
                isAnimationFinished = true;
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss2Idle1State());
            }
        }
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss2 {bossController.name} exited Attack1 State");
    }
}

// TestBoss2的静止状态2
public class TestBoss2Idle2State : IBossState
{
    private float idleDuration = 0.8f;
    private float idleTimer;

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss2 {bossController.name} entered Idle2 State");
        idleTimer = idleDuration;

        // 播放待机动画
        if (bossController is TestBoss2 testBoss2)
        {
            testBoss2.PlayAnimation(6); // PlayIdleAnimation()
        }
    }

    public void UpdateState(BossController bossController)
    {
        idleTimer -= Time.deltaTime;
       

        if (idleTimer <= 0f)
        {
            if(Random.Range(0f, 1f) < 0.5f) 
            {
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss2Attack2State());
            }
            else 
            {
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss2Attack3State());
            }
        }
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss2 {bossController.name} exited Idle2 State");
    }
}

// TestBoss2的攻击状态2
public class TestBoss2Attack2State : IBossState
{
    private bool isAnimationFinished = false;
    private float duration = 2.5f;
    private float timer;

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss2 {bossController.name} entered Attack2 State");

        timer = duration;
        isAnimationFinished = false;

        // 播放攻击2动画
        if (bossController is TestBoss2 testBoss2)
        {
            testBoss2.PlayAnimation(4); // PlayAttack2Animation()
        }
    }

    public void UpdateState(BossController bossController)
    {
        timer -= Time.deltaTime;

        // 检查部位破坏状态或血量条件
        if (bossController is TestBoss2 testBoss2)
        {
            bool shouldSwitchToState2 = false;
            string switchReason = "";

            // 检查部位破坏条件
            if (testBoss2.IsAnyBodyPartDestroyed() && !testBoss2.GetisState2())
            {
                shouldSwitchToState2 = true;
                switchReason = "Body part destroyed";
            }
            // 检查血量条件（血量低于70%且还在一阶段）
            else if (bossController.HealthPercentage < 0.7f && !testBoss2.GetisState2())
            {
                shouldSwitchToState2 = true;
                switchReason = $"Health below 50% (current: {bossController.HealthPercentage:P1})";
            }

            if (shouldSwitchToState2)
            {
                Debug.Log($"TestBoss2: {switchReason}, switching to Attack3");
                testBoss2.SwitchToState2();//  切到二阶段
                testBoss2.RecalculateScreenPathPoints();// 重画路径点
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss2Attack3State());
                return;
            }
        }

        // 检查动画是否播放完成
        if (timer <= 0f && !isAnimationFinished && bossController is TestBoss2 testBoss)
        {
            if (testBoss.IsAnimationPlaying == false)
            {
                isAnimationFinished = true;
                if (testBoss.GetisState2())
                {
                    bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss2Idle2State());
                }
                else 
                {
                    bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss2Idle1State());
                }
            }
        }
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss2 {bossController.name} exited Attack2 State");
    }
}

// TestBoss2的攻击状态3 - 音游攻击（更高难度）
public class TestBoss2Attack3State : IBossState
{
    [Header("音游设置 - TestBoss2")]
    private float rhythmGameDuration = 10f; // 比TestBoss1更短的时间（15s -> 10s）
    private int targetArcsToDestroy = 8;    // 需要消除的判定块数量
    
    private RhythmGameController rhythmGameController;
    private bool rhythmGameStarted = false;
    private bool rhythmGameCompleted = false;
    private float stateTimer = 0f;

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss2 {bossController.name} entered Attack3 State with Rhythm Game (HARD MODE)");
        
        // 播放特殊动画
        if (bossController is TestBoss2 testBoss2)
        {
            testBoss2.PlayAnimation(5); // 播放特殊动画
        }

        // 启动音游机制
        StartRhythmGame(bossController);
    }

    public void UpdateState(BossController bossController)
    {
        stateTimer += Time.deltaTime;
        
        // 检查音游是否完成
        if (rhythmGameCompleted || (rhythmGameController != null && !rhythmGameController.IsGameRunning))
        {
            // 音游结束，回到idle1状态继续循环
            Debug.Log("TestBoss2 Rhythm Game completed, back to Idle2 State");
            bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss2Idle2State());
            return;
        }
        
        // 安全检查：如果音游运行时间过长，强制结束
        if (stateTimer > rhythmGameDuration + 5f)
        {
            Debug.LogWarning("TestBoss2 Rhythm Game running too long, force ending");
            bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss2Idle2State());
        }
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss2 {bossController.name} exited Attack3 State");
        
        // 清理音游
        CleanupRhythmGame();
    }
    
    /// <summary>
    /// 启动音游机制（TestBoss2的高难度版本）
    /// </summary>
    /// <param name="bossController">Boss控制器</param>
    private void StartRhythmGame(BossController bossController)
    {
        if (rhythmGameStarted) return;
        
        // 获取或创建音游控制器
        rhythmGameController = FindOrCreateRhythmGameController(bossController);
        
        if (rhythmGameController != null)
        {
            // 订阅音游结束事件
            rhythmGameController.OnRhythmGameEnded += OnRhythmGameCompleted;
            
            // 配置TestBoss2的高难度音游参数
            ConfigureHardModeRhythmGame();
            
            // 启动音游
            rhythmGameController.StartRhythmGame(
                RhythmGameController.RhythmGameExitCondition.Timer,
                rhythmGameDuration,
                targetArcsToDestroy
            );
            
            rhythmGameStarted = true;
            Debug.Log($"TestBoss2 Hard Mode Rhythm Game started - Duration: {rhythmGameDuration}s, Target: {targetArcsToDestroy}");
        }
        else
        {
            Debug.LogError("Failed to create RhythmGameController for TestBoss2!");
            rhythmGameCompleted = true; // 强制结束状态
        }
    }
    
    /// <summary>
    /// 配置TestBoss2的高难度音游参数
    /// </summary>
    private void ConfigureHardModeRhythmGame()
    {
        if (rhythmGameController == null) return;
        
        // 使用反射设置更高难度的参数
        // 更大的判定范围、更高的失败伤害
        
        // 增加失败伤害：从10f增加到20f
        var failureDamageField = typeof(RhythmGameController).GetField("failureDamageToPlayer", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (failureDamageField != null)
        {
            failureDamageField.SetValue(rhythmGameController, 20f);
        }
        
        // 增大判定窗口使玩家更容易成功（更大判定范围）
        var judgmentWindowField = typeof(RhythmGameController).GetField("judgmentWindow", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (judgmentWindowField != null)
        {
            judgmentWindowField.SetValue(rhythmGameController, 1.5f);
        }
        
        // 增加扇形判定角度范围：从30度增加到45度
        var sectorAngleField = typeof(RhythmGameController).GetField("sectorJudgmentAngle", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (sectorAngleField != null)
        {
            sectorAngleField.SetValue(rhythmGameController, 45f);
        }
        
        Debug.Log("TestBoss2 Hard Mode parameters configured: Higher failure damage, larger judgment range");
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
            controller = UnityEngine.Object.FindObjectOfType<RhythmGameController>();
        }
        
        if (controller == null)
        {
            // 创建新的音游控制器
            GameObject rhythmGameObj = new GameObject("RhythmGameController_TestBoss2");
            controller = rhythmGameObj.AddComponent<RhythmGameController>();
            
            // 设置父物体为Boss（可选）
            rhythmGameObj.transform.SetParent(bossController.transform);
            
            Debug.Log("Created new RhythmGameController for TestBoss2 Attack3 State");
        }
        
        return controller;
    }
    
    /// <summary>
    /// 音游完成回调
    /// </summary>
    private void OnRhythmGameCompleted()
    {
        Debug.Log("TestBoss2 Rhythm Game completed!");
        rhythmGameCompleted = true;
    }
    
    /// <summary>
    /// 清理音游资源
    /// </summary>
    private void CleanupRhythmGame()
    {
        if (rhythmGameController != null)
        {
            // 取消事件订阅
            rhythmGameController.OnRhythmGameEnded -= OnRhythmGameCompleted;
            
            // 如果音游还在运行，停止它
            if (rhythmGameController.IsGameRunning)
            {
                rhythmGameController.StopRhythmGame();
            }
        }
        
        rhythmGameStarted = false;
        rhythmGameCompleted = false;
        stateTimer = 0f;
    }
}

// TestBoss2的消失状态
public class TestBoss2DisappearState : IBossState
{
    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss2 {bossController.name} entered Disappear State");

        // 播放消失动画
        if (bossController is TestBoss2 testBoss2)
        {
            testBoss2.PlayAnimation(1); // PlayDisappearAnimation()
        }
    }

    public void UpdateState(BossController bossController)
    {
        // 消失状态的更新逻辑
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss2 {bossController.name} exited Disappear State");
    }
}

// ================================
// TestBoss3 的状态实现
// ================================

// TestBoss3的出现状态
public class TestBoss3AppearState : IBossState
{
    private float appearDuration = 3f;
    private float appearTimer;

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss3 {bossController.name} entered Appear State");
        appearTimer = appearDuration;

        // 播放出现动画
        if (bossController is TestBoss3 testBoss3)
        {
            testBoss3.PlayAnimation(0); // PlayAppearAnimation()
        }
    }

    public void UpdateState(BossController bossController)
    {
        appearTimer -= Time.deltaTime;

        if (appearTimer <= 0f)
        {
            // 出现状态结束后直接进入Attack1状态
            bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss3Attack1State());
        }
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss3 {bossController.name} exited Appear State");
    }
}

// TestBoss3的静止状态1（一阶段）
public class TestBoss3Idle1State : IBossState
{
    private float idleDuration = 2f;
    private float idleTimer;

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss3 {bossController.name} entered Idle1 State");
        idleTimer = idleDuration;

        // 播放待机动画
        if (bossController is TestBoss3 testBoss3)
        {
            testBoss3.PlayAnimation(7); // PlayIdle1Animation()
            bossController.StartScreenPathMove(); // 随机选择路径点
        }
    }

    public void UpdateState(BossController bossController)
    {
        idleTimer -= Time.deltaTime;

        // 检查是否有蛇头被破坏，如果有则进入二阶段
        if (bossController is TestBoss3 testBoss3)
        {
            if (testBoss3.CheckAndUpdateHeadDestruction())
            {
                Debug.Log("TestBoss3: Snake head destroyed, entering Stage 2 with Attack4");
                testBoss3.SwitchToStage2();
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss3Attack4State());
                return;
            }
        }

        if (idleTimer <= 0f)
        {
            // Idle1状态结束后重新进入Attack1状态
            bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss3Attack1State());
        }
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss3 {bossController.name} exited Idle1 State");
    }
}

// TestBoss3的静止状态2（二阶段）
public class TestBoss3Idle2State : IBossState
{
    private float idleDuration = 1.5f;
    private float idleTimer;

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss3 {bossController.name} entered Idle2 State (Stage 2)");
        idleTimer = idleDuration;

        // 播放待机动画
        if (bossController is TestBoss3 testBoss3)
        {
            testBoss3.PlayAnimation(8); // PlayIdle2Animation()
            bossController.StartScreenPathMove(); // 随机选择路径点
        }
    }

    public void UpdateState(BossController bossController)
    {
        idleTimer -= Time.deltaTime;

        if (idleTimer <= 0f)
        {
            // 二阶段：1/3概率进入attack1、attack2、attack4状态
            float randomValue = Random.Range(0f, 1f);
            
            if (randomValue < 1f/3f)
            {
                Debug.Log("TestBoss3 Stage2: Going to Attack1");
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss3Attack1State());
            }
            else if (randomValue < 2f/3f)
            {
                Debug.Log("TestBoss3 Stage2: Going to Attack2");
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss3Attack2State());
            }
            else
            {
                Debug.Log("TestBoss3 Stage2: Going to Attack4");
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss3Attack4State());
            }
        }
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss3 {bossController.name} exited Idle2 State");
    }
}

// TestBoss3的准备状态
public class TestBoss3PreparationState : IBossState
{
    private bool isAnimationFinished = false;
    private float duration = 2f;
    private float timer;
    private string nextAttackState = ""; // 记录下一个攻击状态

    public TestBoss3PreparationState(string nextState = "")
    {
        nextAttackState = nextState;
    }

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss3 {bossController.name} entered Preparation State (Next: {nextAttackState})");

        timer = duration;
        isAnimationFinished = false;

        // 播放准备动画
        if (bossController is TestBoss3 testBoss3)
        {
            testBoss3.PlayAnimation(2); // PlayPreparationAnimation()
            bossController.StartScreenPathMove(); // 随机选择路径点
        }
    }

    public void UpdateState(BossController bossController)
    {
        timer -= Time.deltaTime;

        // 检查是否有蛇头被破坏，如果有则进入二阶段
        if (bossController is TestBoss3 testBoss3)
        {
            if (testBoss3.CheckAndUpdateHeadDestruction())
            {
                Debug.Log("TestBoss3: Snake head destroyed during preparation, entering Stage 2 with Attack4");
                testBoss3.SwitchToStage2();
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss3Attack4State());
                return;
            }
        }

        // 检查动画是否播放完成
        if (timer <= 0f && !isAnimationFinished && bossController is TestBoss3 testBoss)
        {
            if (testBoss.IsAnimationPlaying == false)
            {
                isAnimationFinished = true;
                OnPreparationFinished(bossController);
            }
        }
    }

    private void OnPreparationFinished(BossController bossController)
    {
        Debug.Log($"TestBoss3 preparation animation finished, transitioning to {nextAttackState}");
        
        // 根据nextAttackState决定下一个状态
        switch (nextAttackState)
        {
            case "Attack2":
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss3Attack2State());
                break;
            case "Attack3":
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss3Attack3State());
                break;
            default:
                Debug.LogWarning($"TestBoss3: Unknown next attack state: {nextAttackState}");
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss3Idle1State());
                break;
        }
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss3 {bossController.name} exited Preparation State");
    }
}

// TestBoss3的攻击状态1 - 准心区域攻击
public class TestBoss3Attack1State : IBossState
{
    private bool isAnimationFinished = false;
    private float duration = 9f;
    private float timer;

    private float timer_stage1;
    private float timer_stage2;
    private float timer_stage3;

    private float duration_stage1 = 3f;
    private float duration_stage2 = 3f;
    private float duration_stage3 = 3f;

    public enum TestBoss3Attack1State_AttackingStage
    {
        Stage0,
        Stage1,
        Stage2,
        Stage3
    }

    private TestBoss3Attack1State_AttackingStage AttackingStage = TestBoss3Attack1State_AttackingStage.Stage0;
    
    // 安全区域定义
    private List<CrossHairPosition> safePositions_Stage0 = new List<CrossHairPosition>();
    private List<CrossHairPosition> safePositions_Stage1 = new List<CrossHairPosition>();
    private List<CrossHairPosition> safePositions_Stage2 = new List<CrossHairPosition>();
    
    // 伤害计时器
    private float damageTimer = 0f;
    private float damageInterval = 0.1f; // 每0.1秒检查一次伤害
    private float damageAmount = 5f; // 每次伤害量
    
    // 视觉效果相关
    private Canvas gameCanvas;
    private GameObject[] dangerZoneObjects = new GameObject[8]; // 8个危险区域显示对象
    private CrosshairController crosshairController;

    enum CrossHairPosition
    {
        NorthByEast,
        EastByNorth,
        EastBySouth,
        SouthByEast,
        SouthByWest,
        WestBySouth,
        WestByNorth,
        NorthByWest
    }

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss3 {bossController.name} entered Attack1 State");

        timer = duration;
        isAnimationFinished = false;
        
        // 初始化阶段计时器
        timer_stage1 = duration_stage1;
        timer_stage2 = duration_stage2;
        timer_stage3 = duration_stage3;
        
        // 初始化伤害计时器
        damageTimer = 0f;
        
        // 初始化安全区域
        InitializeSafePositions();
        
        // 初始化视觉效果系统
        InitializeVisualEffects();

        // 播放攻击1动画
        if (bossController is TestBoss3 testBoss3)
        {
            testBoss3.PlayAnimation(3); // PlayAttack1Animation()
        }
    }

    public void UpdateState(BossController bossController)
    {
        timer -= Time.deltaTime;

        // 检查是否有蛇头被破坏，如果有则进入二阶段
        if (bossController is TestBoss3 testBoss3)
        {
            if (testBoss3.CheckAndUpdateHeadDestruction())
            {
                Debug.Log("TestBoss3: Snake head destroyed during Attack1, entering Stage 2 with Attack4");
                testBoss3.SwitchToStage2();
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss3Attack4State());
                return;
            }
        }

        // 检查动画是否播放完成
        if (timer <= 0f && !isAnimationFinished && bossController is TestBoss3 testBoss)
        {
            if (testBoss.IsAnimationPlaying == false)
            {
                isAnimationFinished = true;
                OnAttack1Finished(bossController);
            }
        }

        // update里加伤害判断逻辑 
        CrossHairPosition direction = CheckCrosshairPosition();
        
        // 更新伤害计时器
        damageTimer += Time.deltaTime;
        
        switch (AttackingStage) 
        {
            case TestBoss3Attack1State_AttackingStage.Stage0:
                // 执行第一次判断 1/2屏幕区域安全 选4个连块的CrossHairPosition
                timer_stage1 -= Time.deltaTime;
                
                // 如果direction在被选中的之中，不掉血，若不在，玩家每0.1秒掉血
                if (!safePositions_Stage0.Contains(direction))
                {
                    if (damageTimer >= damageInterval)
                    {
                        DealDamageToPlayer(bossController, damageAmount);
                        CreateDamageRippleEffect(bossController); // 创建伤害波纹效果
                        damageTimer = 0f;
                    }
                }
                else
                {
                    // 在安全区域，重置伤害计时器
                    damageTimer = 0f;
                }
                
                // 更新危险区域显示
                UpdateDangerZoneDisplay(safePositions_Stage0);
                
                // 同时更新对应的timer，当到达对应的duration后AttackingStage升级
                if (timer_stage1 <= 0f)
                {
                    AttackingStage = TestBoss3Attack1State_AttackingStage.Stage1;
                    Debug.Log("TestBoss3 Attack1: Stage0 -> Stage1");
                }
                break;
                
            case TestBoss3Attack1State_AttackingStage.Stage1:
                // 执行第二次判断 1/4屏幕区域安全
                timer_stage2 -= Time.deltaTime;
                
                // 逻辑同上
                if (!safePositions_Stage1.Contains(direction))
                {
                    if (damageTimer >= damageInterval)
                    {
                        DealDamageToPlayer(bossController, damageAmount);
                        CreateDamageRippleEffect(bossController); // 创建伤害波纹效果
                        damageTimer = 0f;
                    }
                }
                else
                {
                    damageTimer = 0f;
                }
                
                // 更新危险区域显示
                UpdateDangerZoneDisplay(safePositions_Stage1);
                
                if (timer_stage2 <= 0f)
                {
                    AttackingStage = TestBoss3Attack1State_AttackingStage.Stage2;
                    Debug.Log("TestBoss3 Attack1: Stage1 -> Stage2");
                }
                break;
                
            case TestBoss3Attack1State_AttackingStage.Stage2:
                // 执行第三次判断 1/8屏幕区域安全
                timer_stage3 -= Time.deltaTime;
                
                // 逻辑同上
                if (!safePositions_Stage2.Contains(direction))
                {
                    if (damageTimer >= damageInterval)
                    {
                        DealDamageToPlayer(bossController, damageAmount);
                        CreateDamageRippleEffect(bossController); // 创建伤害波纹效果
                        damageTimer = 0f;
                    }
                }
                else
                {
                    damageTimer = 0f;
                }
                
                // 更新危险区域显示
                UpdateDangerZoneDisplay(safePositions_Stage2);
                
                if (timer_stage3 <= 0f)
                {
                    AttackingStage = TestBoss3Attack1State_AttackingStage.Stage3;
                    Debug.Log("TestBoss3 Attack1: Stage2 -> Stage3");
                }
                break;
                
            case TestBoss3Attack1State_AttackingStage.Stage3:
                // 能进到这里证明三次判断做完了，直接切状态
                OnAttack1Finished(bossController);
                break;
                
            default:
                break;
        }

        // 加上美术效果（判定范围 判定结果
    }

    private void OnAttack1Finished(BossController bossController)
    {
        Debug.Log("TestBoss3 Attack1 finished");

        if (bossController is TestBoss3 testBoss3)
        {
            if (testBoss3.IsStage2())
            {
                // 二阶段：攻击结束后回到Idle2状态
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss3Idle2State());
            }
            else
            {
                // 一阶段：Attack1 -> Preparation -> Attack2
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss3PreparationState("Attack2"));
            }
        }
    }

    /// <summary>
    /// 初始化各阶段的安全区域
    /// </summary>
    private void InitializeSafePositions()
    {
        // Stage0: 1/2屏幕区域安全 - 选择4个连续的方向（半个屏幕）
        safePositions_Stage0.Clear();
        int startPos = Random.Range(0, 8); // 随机选择起始位置
        CrossHairPosition[] allPositions = System.Enum.GetValues(typeof(CrossHairPosition)) as CrossHairPosition[];
        
        for (int i = 0; i < 4; i++)
        {
            safePositions_Stage0.Add(allPositions[(startPos + i) % 8]);
        }
        
        // Stage1: 1/4屏幕区域安全 - 从Stage0的安全区域中选择2个连续的方向
        safePositions_Stage1.Clear();
        int stage1Start = Random.Range(0, 3); // 在Stage0的4个位置中选择起始位置
        for (int i = 0; i < 2; i++)
        {
            safePositions_Stage1.Add(safePositions_Stage0[(stage1Start + i) % safePositions_Stage0.Count]);
        }
        
        // Stage2: 1/8屏幕区域安全 - 从Stage1的安全区域中选择1个方向
        safePositions_Stage2.Clear();
        int stage2Index = Random.Range(0, safePositions_Stage1.Count);
        safePositions_Stage2.Add(safePositions_Stage1[stage2Index]);
        
        //Debug.Log($"TestBoss3 Attack1 Safe Positions - Stage0: {string.Join(", ", safePositions_Stage0)}, Stage1: {string.Join(", ", safePositions_Stage1)}, Stage2: {string.Join(", ", safePositions_Stage2)}");
    }
    
    /// <summary>
    /// 对玩家造成伤害
    /// </summary>
    private void DealDamageToPlayer(BossController bossController, float damage)
    {
        Player player = bossController.FindPlayer();
        if (player != null)
        {
            player.TakeDamage(damage);
            //Debug.Log($"TestBoss3 Attack1: Player took {damage} damage for being in unsafe area");
        }
    }
    
    /// <summary>
    /// 初始化视觉效果系统
    /// </summary>
    private void InitializeVisualEffects()
    {
        // 查找或创建Canvas
        gameCanvas = GameObject.FindObjectOfType<Canvas>();
        if (gameCanvas == null)
        {
            GameObject canvasGO = new GameObject("TestBoss3_AttackCanvas");
            gameCanvas = canvasGO.AddComponent<Canvas>();
            gameCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            gameCanvas.sortingOrder = 100; // 确保在最上层显示
            
            // 添加CanvasScaler
            UnityEngine.UI.CanvasScaler scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }
        
        // 获取准心控制器
        crosshairController = GameObject.FindObjectOfType<CrosshairController>();
        
        // 创建8个危险区域显示对象
        CreateDangerZoneObjects();
        
        Debug.Log("TestBoss3 Attack1: Visual effects system initialized");
    }
    
    /// <summary>
    /// 创建危险区域显示对象
    /// </summary>
    private void CreateDangerZoneObjects()
    {
        CrossHairPosition[] allPositions = System.Enum.GetValues(typeof(CrossHairPosition)) as CrossHairPosition[];
        
        for (int i = 0; i < 8; i++)
        {
            GameObject dangerZone = new GameObject($"DangerZone_{allPositions[i]}");
            dangerZone.transform.SetParent(gameCanvas.transform, false);
            
            // 添加RectTransform
            RectTransform rectTransform = dangerZone.AddComponent<RectTransform>();
            
            // 设置为全屏大小
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            // 添加Image组件
            UnityEngine.UI.Image image = dangerZone.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(1f, 0f, 0f, 0.3f); // 半透明红色
            image.raycastTarget = false;
            
            // 添加Mask组件来创建扇形
            UnityEngine.UI.Mask mask = dangerZone.AddComponent<UnityEngine.UI.Mask>();
            mask.showMaskGraphic = false;
            
            // 创建扇形遮罩
            GameObject sectorMask = CreateSectorMask(allPositions[i], rectTransform);
            
            // 初始时隐藏
            dangerZone.SetActive(false);
            
            dangerZoneObjects[i] = dangerZone;
        }
    }
    
    /// <summary>
    /// 创建扇形遮罩
    /// </summary>
    private GameObject CreateSectorMask(CrossHairPosition position, RectTransform parent)
    {
        GameObject sectorMask = new GameObject($"SectorMask_{position}");
        sectorMask.transform.SetParent(parent, false);
        
        RectTransform maskRect = sectorMask.AddComponent<RectTransform>();
        maskRect.anchorMin = new Vector2(0.5f, 0.5f);
        maskRect.anchorMax = new Vector2(0.5f, 0.5f);
        maskRect.sizeDelta = new Vector2(2000f, 2000f); // 足够大的尺寸
        maskRect.anchoredPosition = Vector2.zero;
        
        // 添加Image组件作为遮罩
        UnityEngine.UI.Image maskImage = sectorMask.AddComponent<UnityEngine.UI.Image>();
        maskImage.sprite = CreateSectorSprite(position);
        maskImage.color = Color.white;
        
        return sectorMask;
    }
    
    /// <summary>
    /// 创建扇形精灵
    /// </summary>
    private Sprite CreateSectorSprite(CrossHairPosition position)
    {
        int size = 512;
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float startAngle = GetAngleForPosition(position) - 22.5f;
        float endAngle = startAngle + 45f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y);
                Vector2 dir = pos - center;
                
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                if (angle < 0) angle += 360f;
                
                // 检查是否在扇形范围内
                bool inSector = IsAngleInRange(angle, startAngle, endAngle);
                
                pixels[y * size + x] = inSector ? Color.white : Color.clear;
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
    
    /// <summary>
    /// 获取CrossHairPosition对应的角度
    /// </summary>
    private float GetAngleForPosition(CrossHairPosition position)
    {
        switch (position)
        {
            case CrossHairPosition.EastByNorth: return 0f;
            case CrossHairPosition.NorthByEast: return 45f;
            case CrossHairPosition.NorthByWest: return 90f;
            case CrossHairPosition.WestByNorth: return 135f;
            case CrossHairPosition.WestBySouth: return 180f;
            case CrossHairPosition.SouthByWest: return 225f;
            case CrossHairPosition.SouthByEast: return 270f;
            case CrossHairPosition.EastBySouth: return 315f;
            default: return 0f;
        }
    }
    
    /// <summary>
    /// 检查角度是否在范围内
    /// </summary>
    private bool IsAngleInRange(float angle, float startAngle, float endAngle)
    {
        // 处理角度跨越0度的情况
        if (endAngle > 360f)
        {
            return angle >= startAngle || angle <= (endAngle - 360f);
        }
        else if (startAngle < 0f)
        {
            return angle >= (startAngle + 360f) || angle <= endAngle;
        }
        else
        {
            return angle >= startAngle && angle <= endAngle;
        }
    }
    
    /// <summary>
    /// 更新危险区域显示
    /// </summary>
    private void UpdateDangerZoneDisplay(List<CrossHairPosition> safePositions)
    {
        CrossHairPosition[] allPositions = System.Enum.GetValues(typeof(CrossHairPosition)) as CrossHairPosition[];
        
        for (int i = 0; i < 8; i++)
        {
            if (dangerZoneObjects[i] != null)
            {
                // 如果不在安全区域中，显示为危险区域
                bool isDangerous = !safePositions.Contains(allPositions[i]);
                dangerZoneObjects[i].SetActive(isDangerous);
            }
        }
    }
    
    /// <summary>
    /// 创建伤害波纹效果
    /// </summary>
    private void CreateDamageRippleEffect(BossController bossController)
    {
        if (crosshairController == null || gameCanvas == null || bossController == null) return;
        
        // 通过BossController启动协程
        bossController.StartCoroutine(CreateDamageRipple());
    }
    
    /// <summary>
    /// 创建伤害波纹协程
    /// </summary>
    private System.Collections.IEnumerator CreateDamageRipple()
    {
        // 获取准心位置
        Vector3 crosshairWorldPos = crosshairController.transform.position;
        
        // 转换为屏幕坐标
        Camera cam = Camera.main;
        if (cam == null) yield break;
        
        Vector2 screenPos = cam.WorldToScreenPoint(crosshairWorldPos);
        
        // 创建波纹GameObject
        GameObject ripple = new GameObject("DamageRipple");
        ripple.transform.SetParent(gameCanvas.transform, false);
        
        // 设置位置
        RectTransform rippleRect = ripple.AddComponent<RectTransform>();
        rippleRect.position = screenPos;
        rippleRect.sizeDelta = new Vector2(100f, 100f);
        
        // 添加Image组件
        UnityEngine.UI.Image rippleImage = ripple.AddComponent<UnityEngine.UI.Image>();
        rippleImage.sprite = CreateRippleSprite();
        rippleImage.color = new Color(1f, 0f, 0f, 0.8f); // 红色伤害波纹
        rippleImage.raycastTarget = false;
        
        // 波纹扩散动画
        float duration = 0.6f;
        float timer = 0f;
        Vector3 startScale = Vector3.one * 0.3f;
        Vector3 endScale = Vector3.one * 2.5f;
        Color startColor = rippleImage.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            
            // 缩放和淡出
            ripple.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            rippleImage.color = Color.Lerp(startColor, endColor, t);
            
            yield return null;
        }
        
        // 销毁波纹
        if (ripple != null)
        {
            GameObject.Destroy(ripple);
        }
    }
    
    /// <summary>
    /// 创建波纹精灵
    /// </summary>
    private Sprite CreateRippleSprite()
    {
        int size = 128;
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float maxRadius = size * 0.4f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);
                
                // 创建环形波纹
                float ringWidth = 8f;
                float alpha = 0f;
                
                if (distance <= maxRadius)
                {
                    float ringPos = (distance / maxRadius) * 4f; // 4个环
                    float ringFraction = ringPos - Mathf.Floor(ringPos);
                    
                    if (ringFraction < 0.3f) // 环的宽度
                    {
                        alpha = 1f - (distance / maxRadius); // 外围渐淡
                    }
                }
                
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
    
    /// <summary>
    /// 清理视觉效果
    /// </summary>
    private void CleanupVisualEffects()
    {
        // 销毁所有危险区域对象
        for (int i = 0; i < dangerZoneObjects.Length; i++)
        {
            if (dangerZoneObjects[i] != null)
            {
                GameObject.Destroy(dangerZoneObjects[i]);
                dangerZoneObjects[i] = null;
            }
        }
        
        Debug.Log("TestBoss3 Attack1: Visual effects cleaned up");
    }

    private CrossHairPosition CheckCrosshairPosition() 
    {
        // 获取准心位置 
        CrosshairController CrosshairObject = GameObject.FindObjectOfType<CrosshairController>();
        if (CrosshairObject == null)
        {
            Debug.LogWarning("CrosshairController not found!");
            return CrossHairPosition.EastByNorth; // 默认值
        }
        
        Vector3 crosshairWorldPos = CrosshairObject.transform.position;
        // 跟摄像头中心比判断位置返回 
        Vector3 cameraPos = Camera.main.transform.position;

        float screenHeight = 2f * Camera.main.orthographicSize;
        float screenWidth = screenHeight * Camera.main.aspect;

        Vector3 crosshairDirection = crosshairWorldPos - cameraPos;
        // 按照角度和八个CrossHairPosition对应
        float angle = Mathf.Atan2(crosshairDirection.y, crosshairDirection.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        // 根据角度范围判断方向
        if (angle >= 337.5f || angle < 22.5f)
            return CrossHairPosition.EastByNorth;
        else if (angle >= 22.5f && angle < 67.5f)
            return CrossHairPosition.NorthByEast;
        else if (angle >= 67.5f && angle < 112.5f)
            return CrossHairPosition.NorthByWest;
        else if (angle >= 112.5f && angle < 157.5f)
            return CrossHairPosition.WestByNorth;
        else if (angle >= 157.5f && angle < 202.5f)
            return CrossHairPosition.WestBySouth;
        else if (angle >= 202.5f && angle < 247.5f)
            return CrossHairPosition.SouthByWest;
        else if (angle >= 247.5f && angle < 292.5f)
            return CrossHairPosition.SouthByEast;
        else // angle >= 292.5f && angle < 337.5f
            return CrossHairPosition.EastBySouth;
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss3 {bossController.name} exited Attack1 State");
        
        // 清理视觉效果
        CleanupVisualEffects();
    }
}

// TestBoss3的攻击状态2 - 连续区域攻击
public class TestBoss3Attack2State : IBossState
{
    private bool isAnimationFinished = false;
    private float duration = 5f;
    private float timer;

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss3 {bossController.name} entered Attack2 State (Continuous Area Attack)");

        timer = duration;
        isAnimationFinished = false;

        // 播放攻击2动画
        if (bossController is TestBoss3 testBoss3)
        {
            testBoss3.PlayAnimation(4); // PlayAttack2Animation()
        }
    }

    public void UpdateState(BossController bossController)
    {
        timer -= Time.deltaTime;

        // 检查是否有蛇头被破坏，如果有则进入二阶段
        if (bossController is TestBoss3 testBoss3)
        {
            if (testBoss3.CheckAndUpdateHeadDestruction())
            {
                Debug.Log("TestBoss3: Snake head destroyed during Attack2, entering Stage 2 with Attack4");
                testBoss3.SwitchToStage2();
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss3Attack4State());
                return;
            }
        }

        // 检查动画是否播放完成
        if (timer <= 0 && !isAnimationFinished && bossController is TestBoss3 testBoss)
        {
            if (testBoss.IsAnimationPlaying == false)
            {
                isAnimationFinished = true;
                OnAttack2Finished(bossController);
            }
            // 如果动画还在播放但时间超时，强制结束（安全机制）
            else if (timer <= -2f) // 给2秒的缓冲时间
            {
                Debug.LogWarning("TestBoss3 Attack2: Animation timeout, forcing finish");
                isAnimationFinished = true;
                OnAttack2Finished(bossController);
            }
        }
    }

    private void OnAttack2Finished(BossController bossController)
    {
        Debug.Log("TestBoss3 Attack2 finished");

        if (bossController is TestBoss3 testBoss3)
        {
            if (testBoss3.IsStage2())
            {
                // 二阶段：攻击结束后回到Idle2状态
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss3Idle2State());
            }
            else
            {
                // 一阶段：Attack2 -> Preparation -> Attack3
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss3PreparationState("Attack3"));
            }
        }
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss3 {bossController.name} exited Attack2 State");
    }
}

// TestBoss3的攻击状态3 - 激光攻击 妈的有bug打不出伤害
public class TestBoss3Attack3State : IBossState
{
    private bool isAnimationFinished = false;
    private float duration = 8f;
    private float timer;

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss3 {bossController.name} entered Attack3 State (Laser Attack)");

        timer = duration;
        isAnimationFinished = false;

        // 播放攻击3动画
        if (bossController is TestBoss3 testBoss3)
        {
            testBoss3.PlayAnimation(5); // PlayAttack3Animation()
        }
    }

    public void UpdateState(BossController bossController)
    {
        timer -= Time.deltaTime;

        // 检查是否有蛇头被破坏，如果有则进入二阶段
        if (bossController is TestBoss3 testBoss3)
        {
            if (testBoss3.CheckAndUpdateHeadDestruction())
            {
                Debug.Log("TestBoss3: Snake head destroyed during Attack3, entering Stage 2 with Attack4");
                testBoss3.SwitchToStage2();
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss3Attack4State());
                return;
            }
        }

        // 检查动画是否播放完成 - 修复逻辑
        if (timer <= 0 && !isAnimationFinished && bossController is TestBoss3 testBoss)
        {
            if (testBoss.IsAnimationPlaying == false)
            {
                isAnimationFinished = true;
                OnAttack3Finished(bossController);
            }
            // 如果动画还在播放但时间超时，强制结束（安全机制）
            else if (timer <= -2f) // 给2秒的缓冲时间
            {
                Debug.LogWarning("TestBoss3 Attack3: Animation timeout, forcing finish");
                isAnimationFinished = true;
                OnAttack3Finished(bossController);
            }
        }
    }

    private void OnAttack3Finished(BossController bossController)
    {
        Debug.Log("TestBoss3 Attack3 finished");

        // 一阶段：Attack3结束后进入Idle1状态
        bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss3Idle1State());
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss3 {bossController.name} exited Attack3 State");
    }
}

// TestBoss3的攻击状态4 - 弹幕攻击（二阶段专用）
public class TestBoss3Attack4State : IBossState
{
    private bool isAnimationFinished = false;
    private float duration = 8f; // 弹幕攻击持续时间较长
    private float timer;

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss3 {bossController.name} entered Attack4 State (Bullet Hell Attack - Stage 2)");

        timer = duration;
        isAnimationFinished = false;

        // 播放攻击4动画
        if (bossController is TestBoss3 testBoss3)
        {
            testBoss3.PlayAnimation(6); // PlayAttack4Animation()
        }
    }

    public void UpdateState(BossController bossController)
    {
        timer -= Time.deltaTime;

        // 检查动画是否播放完成 - 修复逻辑
        if (!isAnimationFinished && bossController is TestBoss3 testBoss)
        {
            // 优先检查动画是否播放完成，而不是依赖timer
            if (testBoss.IsAnimationPlaying == false)
            {
                isAnimationFinished = true;
                OnAttack4Finished(bossController);
            }
            // 如果动画还在播放但时间超时，强制结束（安全机制）
            else if (timer <= -2f) // 给2秒的缓冲时间
            {
                Debug.LogWarning("TestBoss3 Attack4: Animation timeout, forcing finish");
                isAnimationFinished = true;
                OnAttack4Finished(bossController);
            }
        }
    }

    private void OnAttack4Finished(BossController bossController)
    {
        Debug.Log("TestBoss3 Attack4 (Bullet Hell) finished");

        // Attack4结束后进入Idle2状态
        bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss3Idle2State());
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss3 {bossController.name} exited Attack4 State");
    }
}

// TestBoss3的消失状态
public class TestBoss3DisappearState : IBossState
{
    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss3 {bossController.name} entered Disappear State");

        // 播放消失动画
        if (bossController is TestBoss3 testBoss3)
        {
            testBoss3.PlayAnimation(1); // PlayDisappearAnimation()
        }
    }

    public void UpdateState(BossController bossController)
    {
        // 消失状态的更新逻辑
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss3 {bossController.name} exited Disappear State");
    }
}

// ================================
// TestBoss4 的状态实现
// ================================

// TestBoss4的出现状态
public class TestBoss4AppearState : IBossState
{
    private float appearDuration = 3f;
    private float appearTimer;

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss4 {bossController.name} entered Appear State");
        appearTimer = appearDuration;

        // 播放出现动画
        if (bossController is TestBoss4 testBoss4)
        {
            testBoss4.PlayAnimation(0); // PlayAppearAnimation()
        }
    }

    public void UpdateState(BossController bossController)
    {
        appearTimer -= Time.deltaTime;

        if (appearTimer <= 0f)
        {
            // 出现状态结束后进入Attack4状态
            bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss4Attack4State());
        }
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss4 {bossController.name} exited Appear State");
    }
}

// TestBoss4的静止状态1（一阶段）
public class TestBoss4Idle1State : IBossState
{
    private float idleDuration = 3.5f;
    private float idleTimer;

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss4 {bossController.name} entered Idle1 State (Stage 1)");
        idleTimer = idleDuration;

        // 播放待机动画
        if (bossController is TestBoss4 testBoss4)
        {
            testBoss4.PlayAnimation(7); // PlayIdle1Animation()
            bossController.StartScreenPathMove(); // 随机选择路径点
        }
    }

    public void UpdateState(BossController bossController)
    {
        idleTimer -= Time.deltaTime;

        // 检查是否有超过三个身体部位被破坏，如果有则进入暴露状态
        if (bossController is TestBoss4 testBoss4)
        {
            if (testBoss4.CheckBodyPartsDestruction())
            {
                Debug.Log("TestBoss4: More than 3 body parts destroyed, entering Exposed State");
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss4ExposedState());
                return;
            }
        }

        if (idleTimer <= 0f)
        {
            // Idle1状态结束后随机进入Attack1、Attack2或Attack3状态
            float randomValue = Random.Range(0f, 3f);
            
            if (randomValue < 1f)
            {
                Debug.Log("TestBoss4 Stage1: Going to Attack1");
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss4Attack1State());
            }
            else if (randomValue < 2f)
            {
                Debug.Log("TestBoss4 Stage1: Going to Attack2");
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss4Attack2State());
            }
            else
            {
                Debug.Log("TestBoss4 Stage1: Going to Attack3");
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss4Attack3State());
            }
        }
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss4 {bossController.name} exited Idle1 State");
    }
}

// TestBoss4的静止状态2（二阶段）
public class TestBoss4Idle2State : IBossState
{
    private float idleDuration = 3f;
    private float idleTimer;
    private static int attackSequence = 0; // 0=Attack1, 1=Attack2, 2=Attack3, 3=Attack4

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss4 {bossController.name} entered Idle2 State (Stage 2)");
        idleTimer = idleDuration;

        // 播放待机动画
        if (bossController is TestBoss4 testBoss4)
        {
            testBoss4.PlayAnimation(8); // PlayIdle2Animation()
            bossController.StartScreenPathMove(); // 随机选择路径点
        }
    }

    public void UpdateState(BossController bossController)
    {
        idleTimer -= Time.deltaTime;

        // 检查是否有超过三个身体部位被破坏，如果有则进入暴露状态
        if (bossController is TestBoss4 testBoss4)
        {
            if (testBoss4.CheckBodyPartsDestruction())
            {
                Debug.Log("TestBoss4: More than 3 body parts destroyed in Stage 2, entering Exposed State");
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss4ExposedState());
                return;
            }
        }

        if (idleTimer <= 0f)
        {
            // 二阶段：按顺序进入 Attack1 → Attack2 → Attack3 → Attack4
            switch (attackSequence)
            {
                case 0:
                    Debug.Log("TestBoss4 Stage2: Going to Attack1 (sequence)");
                    bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss4Attack1State());
                    attackSequence = 1;
                    break;
                case 1:
                    Debug.Log("TestBoss4 Stage2: Going to Attack2 (sequence)");
                    bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss4Attack2State());
                    attackSequence = 2;
                    break;
                case 2:
                    Debug.Log("TestBoss4 Stage2: Going to Attack3 (sequence)");
                    bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss4Attack3State());
                    attackSequence = 3;
                    break;
                case 3:
                    Debug.Log("TestBoss4 Stage2: Going to Attack4 (sequence)");
                    bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss4Attack4State());
                    attackSequence = 0; // 重置序列
                    break;
                default:
                    bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss4Attack1State());
                    attackSequence = 0; 
                    break;
            }
        }
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss4 {bossController.name} exited Idle2 State");
    }

    /// <summary>
    /// 重置攻击序列（用于Boss重生或重新开始）
    /// </summary>
    public static void ResetAttackSequence()
    {
        attackSequence = 0;
        Debug.Log("TestBoss4 attack sequence reset.");
    }
}

// TestBoss4的攻击状态1 - 随机播放两个attack1动画之一，创建区域攻击
public class TestBoss4Attack1State : IBossState
{
    private bool isAnimationFinished = false;
    private float duration = 4f;
    private float timer;
    private bool hasSelectedAnimation = false;

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss4 {bossController.name} entered Attack1 State");

        timer = duration;
        isAnimationFinished = false;
        hasSelectedAnimation = false;

        // 随机播放两个attack1动画中的一个
        if (bossController is TestBoss4 testBoss4)
        {
            // 随机选择播放动画3或动画9（假设有两个attack1动画）
            int animationIndex = Random.Range(0, 2) == 0 ? 3 : 9;
            testBoss4.PlayAnimation(animationIndex);
            hasSelectedAnimation = true;
            
            Debug.Log($"TestBoss4 Attack1: Playing animation {animationIndex}");
        }
    }

    public void UpdateState(BossController bossController)
    {
        timer -= Time.deltaTime;

        // 检查是否有超过三个身体部位被破坏
        if (bossController is TestBoss4 testBoss4)
        {
            if (testBoss4.CheckBodyPartsDestruction())
            {
                Debug.Log("TestBoss4: More than 3 body parts destroyed during Attack1, entering Exposed State");
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss4ExposedState());
                return;
            }
        }

        // 检查动画是否播放完成
        if (timer <= 0f && !isAnimationFinished && bossController is TestBoss4 testBoss)
        {
            if (testBoss.IsAnimationPlaying == false)
            {
                isAnimationFinished = true;
                OnAttack1Finished(bossController);
            }
        }
    }

    private void OnAttack1Finished(BossController bossController)
    {
        Debug.Log("TestBoss4 Attack1 finished");

        if (bossController is TestBoss4 testBoss4)
        {
            if (testBoss4.IsStage2())
            {
                // 二阶段：回到Idle2状态
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss4Idle2State());
            }
            else
            {
                // 一阶段：回到Idle1状态
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss4Idle1State());
            }
        }
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss4 {bossController.name} exited Attack1 State");
    }
}

// TestBoss4的攻击状态2 - 通过动画事件创建可消除攻击
public class TestBoss4Attack2State : IBossState
{
    private bool isAnimationFinished = false;
    private float duration = 3f;
    private float timer;

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss4 {bossController.name} entered Attack2 State (Destructible Attack)");

        timer = duration;
        isAnimationFinished = false;

        // 播放攻击2动画
        if (bossController is TestBoss4 testBoss4)
        {
            testBoss4.PlayAnimation(4); // PlayAttack2Animation()
        }
    }

    public void UpdateState(BossController bossController)
    {
        timer -= Time.deltaTime;

        // 检查是否有超过三个身体部位被破坏
        if (bossController is TestBoss4 testBoss4)
        {
            if (testBoss4.CheckBodyPartsDestruction())
            {
                Debug.Log("TestBoss4: More than 3 body parts destroyed during Attack2, entering Exposed State");
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss4ExposedState());
                return;
            }
        }

        // 检查动画是否播放完成
        if (timer <= 0f && !isAnimationFinished && bossController is TestBoss4 testBoss)
        {
            if (testBoss.IsAnimationPlaying == false)
            {
                isAnimationFinished = true;
                OnAttack2Finished(bossController);
            }
        }
    }

    private void OnAttack2Finished(BossController bossController)
    {
        Debug.Log("TestBoss4 Attack2 finished");

        if (bossController is TestBoss4 testBoss4)
        {
            if (testBoss4.IsStage2())
            {
                // 二阶段：回到Idle2状态
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss4Idle2State());
            }
            else
            {
                // 一阶段：回到Idle1状态
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss4Idle1State());
            }
        }
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss4 {bossController.name} exited Attack2 State");
    }
}

// TestBoss4的攻击状态3 - 通过动画事件创建区域攻击
public class TestBoss4Attack3State : IBossState
{
    private bool isAnimationFinished = false;
    private float duration = 3f;
    private float timer;

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss4 {bossController.name} entered Attack3 State (Area Attack)");

        timer = duration;
        isAnimationFinished = false;

        // 播放攻击3动画
        if (bossController is TestBoss4 testBoss4)
        {
            testBoss4.PlayAnimation(5); // PlayAttack3Animation()
        }
    }

    public void UpdateState(BossController bossController)
    {
        timer -= Time.deltaTime;

        // 检查是否有超过三个身体部位被破坏
        if (bossController is TestBoss4 testBoss4)
        {
            if (testBoss4.CheckBodyPartsDestruction())
            {
                Debug.Log("TestBoss4: More than 3 body parts destroyed during Attack3, entering Exposed State");
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss4ExposedState());
                return;
            }
        }

        // 检查动画是否播放完成
        if (timer <= 0f && !isAnimationFinished && bossController is TestBoss4 testBoss)
        {
            if (testBoss.IsAnimationPlaying == false)
            {
                isAnimationFinished = true;
                OnAttack3Finished(bossController);
            }
        }
    }

    private void OnAttack3Finished(BossController bossController)
    {
        Debug.Log("TestBoss4 Attack3 finished");

        if (bossController is TestBoss4 testBoss4)
        {
            if (testBoss4.IsStage2())
            {
                // 二阶段：回到Idle2状态
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss4Idle2State());
            }
            else
            {
                // 一阶段：回到Idle1状态
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss4Idle1State());
            }
        }
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss4 {bossController.name} exited Attack3 State");
    }
}

// TestBoss4的攻击状态4 - 迟缓准心移动
public class TestBoss4Attack4State : IBossState
{
    private bool isAnimationFinished = false;
    private float duration = 3f;
    private float timer;

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss4 {bossController.name} entered Attack4 State (Reserved)");

        timer = duration;
        isAnimationFinished = false;

        // 播放攻击4动画
        if (bossController is TestBoss4 testBoss4)
        {
            testBoss4.PlayAnimation(6); // PlayAttack4Animation()
        }
    }

    public void UpdateState(BossController bossController)
    {
        timer -= Time.deltaTime;

        // 检查是否有超过三个身体部位被破坏
        if (bossController is TestBoss4 testBoss4)
        {
            if (testBoss4.CheckBodyPartsDestruction())
            {
                Debug.Log("TestBoss4: More than 3 body parts destroyed during Attack4, entering Exposed State");
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss4ExposedState());
                return;
            }
        }

        // 检查动画是否播放完成
        if (timer <= 0f && !isAnimationFinished && bossController is TestBoss4 testBoss)
        {
            if (testBoss.IsAnimationPlaying == false)
            {
                isAnimationFinished = true;
                OnAttack4Finished(bossController);
            }
        }
    }

    private void OnAttack4Finished(BossController bossController)
    {
        Debug.Log("TestBoss4 Attack4 finished");

        if (bossController is TestBoss4 testBoss4)
        {
            if (testBoss4.IsStage2())
            {
                // 二阶段：回到Idle2状态
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss4Idle2State());
            }
            else
            {
                // 一阶段：Attack4结束后进入Idle1状态
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss4Idle1State());
            }
        }
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss4 {bossController.name} exited Attack4 State");
    }
}

// TestBoss4的暴露状态 - 当超过三个身体部位被破坏时进入
public class TestBoss4ExposedState : IBossState
{
    private float exposedDuration = 6f; // 暴露状态持续时间
    private float exposedTimer;
    private bool isAnimationFinished = false;
    private bool hasTriggeredStageTransition = false;

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss4 {bossController.name} entered Exposed State - Vulnerable to massive damage!");
        exposedTimer = exposedDuration;
        isAnimationFinished = false;
        hasTriggeredStageTransition = false;

        // 播放暴露状态动画
        if (bossController is TestBoss4 testBoss4)
        {
            testBoss4.PlayAnimation(10); // PlayExposedAnimation()
            
            Debug.Log("TestBoss4 is now exposed and vulnerable - deal massive damage!");
        }
    }

    public void UpdateState(BossController bossController)
    {
        exposedTimer -= Time.deltaTime;

        // 检查动画是否播放完成
        if (bossController is TestBoss4 testBoss4)
        {
            if (!isAnimationFinished && !testBoss4.IsAnimationPlaying)
            {
                isAnimationFinished = true;
            }
        }

        // 暴露状态结束
        if (exposedTimer <= 0f)
        {
            OnExposedFinished(bossController);
        }
    }

    private void OnExposedFinished(BossController bossController)
    {
        Debug.Log("TestBoss4 Exposed State finished - transitioning to Stage 2");

        if (bossController is TestBoss4 testBoss4 && !hasTriggeredStageTransition)
        {
            hasTriggeredStageTransition = true;
            
            // 暴露状态结束后，切换到二阶段
            testBoss4.SwitchToStage2();
            
            // 进入Attack4状态，然后进入Idle2状态
            bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss4Attack4State());
        }
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss4 {bossController.name} exited Exposed State");
        
        // 恢复Boss的正常状态
        if (bossController is TestBoss4 testBoss4)
        {
            // 可以在这里添加恢复正常防御力等逻辑
        }
    }
}

// TestBoss4的消失状态
public class TestBoss4DisappearState : IBossState
{
    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss4 {bossController.name} entered Disappear State");

        // 播放消失动画
        if (bossController is TestBoss4 testBoss4)
        {
            testBoss4.PlayAnimation(1); // PlayDisappearAnimation()
        }
    }

    public void UpdateState(BossController bossController)
    {
        // 检查动画是否播放完成
        if (bossController is TestBoss4 testBoss4)
        {
            if (!testBoss4.IsAnimationPlaying)
            {
                // 恢复正常准心移动
                testBoss4.EnableCrosshairLagMode(UnityEngine.GameObject.FindObjectOfType<CrosshairController>(), false, 1f);

                OnDisappearFinished(bossController);
            }
        }
    }

    private void OnDisappearFinished(BossController bossController)
    {
        Debug.Log("TestBoss4 disappear animation finished");

        // 动画播放完成后boss消失
        bossController.DestroyBoss();
        
        // 重置静态状态
        TestBoss4Idle2State.ResetAttackSequence();
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss4 {bossController.name} exited Disappear State");
    }
}

// ================================
// TestBoss5 的状态实现
// ================================

// TestBoss5的出现状态
public class TestBoss5AppearState : IBossState
{
    private float appearDuration = 3f;
    private float appearTimer;

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss5 {bossController.name} entered Appear State");
        appearTimer = appearDuration;

        // 播放出现动画
        if (bossController is TestBoss5 testBoss5)
        {
            testBoss5.PlayAnimation(0); // PlayAppearAnimation()
        }
    }

    public void UpdateState(BossController bossController)
    {
        appearTimer -= Time.deltaTime;

        if (appearTimer <= 0f)
        {
            // 出现状态结束后进入Idle1状态
            bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss5Idle1State());
        }
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss5 {bossController.name} exited Appear State");
    }
}

// TestBoss5的静止状态1（一阶段）
public class TestBoss5Idle1State : IBossState
{
    private float idleDuration = 3f;
    private float idleTimer;

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss5 {bossController.name} entered Idle1 State (Stage 1)");
        idleTimer = idleDuration;

        // 播放待机动画
        if (bossController is TestBoss5 testBoss5)
        {
            testBoss5.PlayAnimation(7); // PlayIdle1Animation()
            bossController.StartScreenPathMove(); // 随机选择路径点
        }
    }

    public void UpdateState(BossController bossController)
    {
        idleTimer -= Time.deltaTime;

        // 检查血量是否小于2/3，如果是则进入二阶段
        if (bossController is TestBoss5 testBoss5)
        {
            if (bossController.HealthPercentage < 2f/3f && !testBoss5.IsStage2() && !testBoss5.IsStage3())
            {
                Debug.Log("TestBoss5: Health below 2/3, entering Stage 2");
                testBoss5.SwitchToStage2();
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss5Idle2State());
                return;
            }
        }

        if (idleTimer <= 0f)
        {
            // Idle1状态结束后随机选择Attack1或Attack2
            if (Random.Range(0f, 1f) < 0.5f)
            {
                Debug.Log("TestBoss5 Stage1: Going to Attack1");
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss5Attack1State());
            }
            else
            {
                Debug.Log("TestBoss5 Stage1: Going to Attack2");
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss5Attack2State());
            }
        }
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss5 {bossController.name} exited Idle1 State");
    }
}

// TestBoss5的攻击状态1 - 区域限制机制（连续触发三次）
public class TestBoss5Attack1State : IBossState
{
    private bool isAnimationFinished = false;
    private float duration = 10f;
    private float timer;

    // 三次攻击的阶段
    private int currentAttackPhase = 0; // 0, 1, 2 表示三次攻击
    private float phaseDuration = 3f;
    private float phaseTimer = 0f;
    private float phaseInterval = 0.5f; // 每次攻击之间的间隔

    // 区域限制相关
    private List<CrossHairPosition> safePositions = new List<CrossHairPosition>();
    private float damageTimer = 0f;
    private float damageInterval = 0.1f;
    
    // 视觉效果相关
    private Canvas gameCanvas;
    private GameObject[] dangerZoneObjects = new GameObject[8];
    private CrosshairController crosshairController;

    enum CrossHairPosition
    {
        NorthByEast,
        EastByNorth,
        EastBySouth,
        SouthByEast,
        SouthByWest,
        WestBySouth,
        WestByNorth,
        NorthByWest
    }

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss5 {bossController.name} entered Attack1 State (Area Restriction - 3 Phases)");

        timer = duration;
        isAnimationFinished = false;
        currentAttackPhase = 0;
        phaseTimer = 0f;
        damageTimer = 0f;

        // 初始化视觉效果系统
        InitializeVisualEffects();

        // 播放攻击1动画
        if (bossController is TestBoss5 testBoss5)
        {
            // 确保动画状态正确重置
            testBoss5.IsAnimationPlaying = false;
            testBoss5.PlayAnimation(2); // PlayAttack1Animation()
            bossController.StartScreenPathMove();
        }
    }

    public void UpdateState(BossController bossController)
    {
        timer -= Time.deltaTime;
        phaseTimer += Time.deltaTime;
        damageTimer += Time.deltaTime;

        // 检查是否完成所有三次攻击
        if (currentAttackPhase >= 3)
        {
            if (!isAnimationFinished && bossController is TestBoss5 testBoss)
            {
                if (testBoss.IsAnimationPlaying == false)
                {
                    isAnimationFinished = true;
                    OnAttack1Finished(bossController);
                }
            }
            return;
        }

        // 检查当前阶段是否应该开始或结束
        if (currentAttackPhase < 3)
        {
            // 如果阶段时间到了，进入下一阶段或攻击阶段
            if (phaseTimer >= phaseDuration)
            {
                // 完成当前阶段的攻击，准备进入下一阶段
                currentAttackPhase++;
                phaseTimer = 0f;
                damageTimer = 0f;

                if (currentAttackPhase < 3)
                {
                    // 重新初始化安全区域（为下一次攻击）
                    InitializeSafePositions();
                    Debug.Log($"TestBoss5 Attack1: Starting phase {currentAttackPhase + 1}");
                }
                else
                {
                    Debug.Log("TestBoss5 Attack1: All 3 phases completed");
                    // 清理视觉效果
                    CleanupVisualEffects();
                }
            }
            else
            {
                // 执行当前阶段的伤害判断
                CrossHairPosition direction = CheckCrosshairPosition();

                // 如果不在安全区域，按时间扣血
                if (!safePositions.Contains(direction))
                {
                    if (damageTimer >= damageInterval)
                    {
                        DealDamageToPlayer(bossController);
                        CreateDamageRippleEffect(bossController); // 创建伤害波纹效果
                        damageTimer = 0f;
                    }
                }
                else
                {
                    // 在安全区域，重置伤害计时器
                    damageTimer = 0f;
                }

                // 更新危险区域显示
                UpdateDangerZoneDisplay();
            }
        }

        // 安全检查：如果时间过长，强制结束
        if (timer <= -2f && !isAnimationFinished)
        {
            Debug.LogWarning("TestBoss5 Attack1: Timeout, forcing finish");
            isAnimationFinished = true;
            CleanupVisualEffects();
            OnAttack1Finished(bossController);
        }
    }

    private void OnAttack1Finished(BossController bossController)
    {
        Debug.Log("TestBoss5 Attack1 finished");

        if (bossController is TestBoss5 testBoss5)
        {
            if (testBoss5.IsStage3())
            {
                // 三阶段：Attack1结束后进入Attack2
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss5Attack2State());
            }
            else
            {
                // 一阶段：Attack1结束后进入Attack2
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss5Attack2State());
            }
        }
    }

    /// <summary>
    /// 初始化安全区域（随机选择4个连续的方向作为安全区域）
    /// </summary>
    private void InitializeSafePositions()
    {
        safePositions.Clear();
        int startPos = Random.Range(0, 8);
        CrossHairPosition[] allPositions = System.Enum.GetValues(typeof(CrossHairPosition)) as CrossHairPosition[];

        for (int i = 0; i < 4; i++)
        {
            safePositions.Add(allPositions[(startPos + i) % 8]);
        }
    }

    /// <summary>
    /// 对玩家造成伤害
    /// </summary>
    private void DealDamageToPlayer(BossController bossController)
    {
        if (bossController is TestBoss5 testBoss5)
        {
            float damage = testBoss5.GetAttack1Damage();
            Player player = bossController.FindPlayer();
            if (player != null)
            {
                player.TakeDamage(damage);
            }
        }
    }

    /// <summary>
    /// 初始化视觉效果系统
    /// </summary>
    private void InitializeVisualEffects()
    {
        // 初始化安全区域
        InitializeSafePositions();

        // 查找或创建Canvas
        gameCanvas = GameObject.FindObjectOfType<Canvas>();
        if (gameCanvas == null)
        {
            GameObject canvasGO = new GameObject("TestBoss5_AttackCanvas");
            gameCanvas = canvasGO.AddComponent<Canvas>();
            gameCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            gameCanvas.sortingOrder = 100; // 确保在最上层显示

            UnityEngine.UI.CanvasScaler scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        // 获取准心控制器
        crosshairController = GameObject.FindObjectOfType<CrosshairController>();

        // 创建8个危险区域显示对象
        CreateDangerZoneObjects();

        Debug.Log("TestBoss5 Attack1: Visual effects system initialized");
    }

    /// <summary>
    /// 创建危险区域显示对象
    /// </summary>
    private void CreateDangerZoneObjects()
    {
        CrossHairPosition[] allPositions = System.Enum.GetValues(typeof(CrossHairPosition)) as CrossHairPosition[];

        for (int i = 0; i < 8; i++)
        {
            GameObject dangerZone = new GameObject($"DangerZone_{allPositions[i]}");
            dangerZone.transform.SetParent(gameCanvas.transform, false);

            // 添加RectTransform
            RectTransform rectTransform = dangerZone.AddComponent<RectTransform>();

            // 设置为全屏大小
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            // 添加Image组件
            UnityEngine.UI.Image image = dangerZone.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(1f, 0f, 0f, 0.3f); // 半透明红色
            image.raycastTarget = false;

            // 添加Mask组件来创建扇形
            UnityEngine.UI.Mask mask = dangerZone.AddComponent<UnityEngine.UI.Mask>();
            mask.showMaskGraphic = false;

            // 创建扇形遮罩
            GameObject sectorMask = CreateSectorMask(allPositions[i], rectTransform);

            // 初始时隐藏
            dangerZone.SetActive(false);

            dangerZoneObjects[i] = dangerZone;
        }
    }

    /// <summary>
    /// 创建扇形遮罩
    /// </summary>
    private GameObject CreateSectorMask(CrossHairPosition position, RectTransform parent)
    {
        GameObject sectorMask = new GameObject($"SectorMask_{position}");
        sectorMask.transform.SetParent(parent, false);

        RectTransform maskRect = sectorMask.AddComponent<RectTransform>();
        maskRect.anchorMin = new Vector2(0.5f, 0.5f);
        maskRect.anchorMax = new Vector2(0.5f, 0.5f);
        maskRect.sizeDelta = new Vector2(2000f, 2000f); // 足够大的尺寸
        maskRect.anchoredPosition = Vector2.zero;

        // 添加Image组件作为遮罩
        UnityEngine.UI.Image maskImage = sectorMask.AddComponent<UnityEngine.UI.Image>();
        maskImage.sprite = CreateSectorSprite(position);
        maskImage.color = Color.white;

        return sectorMask;
    }

    /// <summary>
    /// 创建扇形精灵
    /// </summary>
    private Sprite CreateSectorSprite(CrossHairPosition position)
    {
        int size = 512;
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float startAngle = GetAngleForPosition(position) - 22.5f;
        float endAngle = startAngle + 45f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y);
                Vector2 dir = pos - center;

                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                if (angle < 0) angle += 360f;

                // 检查是否在扇形范围内
                bool inSector = IsAngleInRange(angle, startAngle, endAngle);

                pixels[y * size + x] = inSector ? Color.white : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    /// <summary>
    /// 获取CrossHairPosition对应的角度
    /// </summary>
    private float GetAngleForPosition(CrossHairPosition position)
    {
        switch (position)
        {
            case CrossHairPosition.EastByNorth: return 0f;
            case CrossHairPosition.NorthByEast: return 45f;
            case CrossHairPosition.NorthByWest: return 90f;
            case CrossHairPosition.WestByNorth: return 135f;
            case CrossHairPosition.WestBySouth: return 180f;
            case CrossHairPosition.SouthByWest: return 225f;
            case CrossHairPosition.SouthByEast: return 270f;
            case CrossHairPosition.EastBySouth: return 315f;
            default: return 0f;
        }
    }

    /// <summary>
    /// 检查角度是否在范围内
    /// </summary>
    private bool IsAngleInRange(float angle, float startAngle, float endAngle)
    {
        // 处理角度跨越0度的情况
        if (endAngle > 360f)
        {
            return angle >= startAngle || angle <= (endAngle - 360f);
        }
        else if (startAngle < 0f)
        {
            return angle >= (startAngle + 360f) || angle <= endAngle;
        }
        else
        {
            return angle >= startAngle && angle <= endAngle;
        }
    }

    /// <summary>
    /// 更新危险区域显示
    /// </summary>
    private void UpdateDangerZoneDisplay()
    {
        CrossHairPosition[] allPositions = System.Enum.GetValues(typeof(CrossHairPosition)) as CrossHairPosition[];

        for (int i = 0; i < 8; i++)
        {
            if (dangerZoneObjects[i] != null)
            {
                // 如果不在安全区域中，显示为危险区域
                bool isDangerous = !safePositions.Contains(allPositions[i]);
                dangerZoneObjects[i].SetActive(isDangerous);
            }
        }
    }

    /// <summary>
    /// 检查准心位置
    /// </summary>
    private CrossHairPosition CheckCrosshairPosition()
    {
        CrosshairController CrosshairObject = GameObject.FindObjectOfType<CrosshairController>();
        if (CrosshairObject == null)
        {
            return CrossHairPosition.EastByNorth;
        }

        Vector3 crosshairWorldPos = CrosshairObject.transform.position;
        Vector3 cameraPos = Camera.main.transform.position;

        Vector3 crosshairDirection = crosshairWorldPos - cameraPos;
        float angle = Mathf.Atan2(crosshairDirection.y, crosshairDirection.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        // 根据角度范围判断方向
        if (angle >= 337.5f || angle < 22.5f)
            return CrossHairPosition.EastByNorth;
        else if (angle >= 22.5f && angle < 67.5f)
            return CrossHairPosition.NorthByEast;
        else if (angle >= 67.5f && angle < 112.5f)
            return CrossHairPosition.NorthByWest;
        else if (angle >= 112.5f && angle < 157.5f)
            return CrossHairPosition.WestByNorth;
        else if (angle >= 157.5f && angle < 202.5f)
            return CrossHairPosition.WestBySouth;
        else if (angle >= 202.5f && angle < 247.5f)
            return CrossHairPosition.SouthByWest;
        else if (angle >= 247.5f && angle < 292.5f)
            return CrossHairPosition.SouthByEast;
        else
            return CrossHairPosition.EastBySouth;
    }

    /// <summary>
    /// 创建伤害波纹效果
    /// </summary>
    private void CreateDamageRippleEffect(BossController bossController)
    {
        if (crosshairController == null || gameCanvas == null || bossController == null) return;

        // 通过BossController启动协程
        bossController.StartCoroutine(CreateDamageRipple());
    }

    /// <summary>
    /// 创建伤害波纹协程
    /// </summary>
    private System.Collections.IEnumerator CreateDamageRipple()
    {
        // 获取准心位置
        Vector3 crosshairWorldPos = crosshairController.transform.position;

        // 转换为屏幕坐标
        Camera cam = Camera.main;
        if (cam == null) yield break;

        Vector2 screenPos = cam.WorldToScreenPoint(crosshairWorldPos);

        // 创建波纹GameObject
        GameObject ripple = new GameObject("DamageRipple");
        ripple.transform.SetParent(gameCanvas.transform, false);

        // 设置位置
        RectTransform rippleRect = ripple.AddComponent<RectTransform>();
        rippleRect.position = screenPos;
        rippleRect.sizeDelta = new Vector2(100f, 100f);

        // 添加Image组件
        UnityEngine.UI.Image rippleImage = ripple.AddComponent<UnityEngine.UI.Image>();
        rippleImage.sprite = CreateRippleSprite();
        rippleImage.color = new Color(1f, 0f, 0f, 0.8f); // 红色伤害波纹
        rippleImage.raycastTarget = false;

        // 波纹扩散动画
        float duration = 0.6f;
        float timer = 0f;
        Vector3 startScale = Vector3.one * 0.3f;
        Vector3 endScale = Vector3.one * 2.5f;
        Color startColor = rippleImage.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            // 缩放和淡出
            ripple.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            rippleImage.color = Color.Lerp(startColor, endColor, t);

            yield return null;
        }

        // 销毁波纹
        if (ripple != null)
        {
            GameObject.Destroy(ripple);
        }
    }

    /// <summary>
    /// 创建波纹精灵
    /// </summary>
    private Sprite CreateRippleSprite()
    {
        int size = 128;
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float maxRadius = size * 0.4f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);

                // 创建环形波纹
                float ringWidth = 8f;
                float alpha = 0f;

                if (distance <= maxRadius)
                {
                    float ringPos = (distance / maxRadius) * 4f; // 4个环
                    float ringFraction = ringPos - Mathf.Floor(ringPos);

                    if (ringFraction < 0.3f) // 环的宽度
                    {
                        alpha = 1f - (distance / maxRadius); // 外围渐淡
                    }
                }

                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    /// <summary>
    /// 清理视觉效果
    /// </summary>
    private void CleanupVisualEffects()
    {
        // 销毁所有危险区域对象
        for (int i = 0; i < dangerZoneObjects.Length; i++)
        {
            if (dangerZoneObjects[i] != null)
            {
                GameObject.Destroy(dangerZoneObjects[i]);
                dangerZoneObjects[i] = null;
            }
        }

        Debug.Log("TestBoss5 Attack1: Visual effects cleaned up");
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss5 {bossController.name} exited Attack1 State");
        CleanupVisualEffects();
    }
}

// TestBoss5的攻击状态2 - 音游机制
public class TestBoss5Attack2State : IBossState
{
    private float rhythmGameDuration = 12f; // 音游持续时间
    private int targetArcsToDestroy = 8;    // 需要消除的判定块数量

    private RhythmGameController rhythmGameController;
    private bool rhythmGameStarted = false;
    private bool rhythmGameCompleted = false;
    private float stateTimer = 0f;
    private bool isAnimationFinished = false;
    private float duration = 15f;
    private float timer;

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss5 {bossController.name} entered Attack2 State (Rhythm Game)");

        timer = duration;
        isAnimationFinished = false;
        rhythmGameStarted = false;
        rhythmGameCompleted = false;
        stateTimer = 0f;

        // 播放攻击2动画
        if (bossController is TestBoss5 testBoss5)
        {
            // 确保动画状态正确重置
            testBoss5.IsAnimationPlaying = false;
            testBoss5.PlayAnimation(3); // PlayAttack2Animation()
            bossController.StartScreenPathMove();
        }

        // 启动音游机制
        StartRhythmGame(bossController);
    }

    public void UpdateState(BossController bossController)
    {
        timer -= Time.deltaTime;
        stateTimer += Time.deltaTime;

        // 检查音游是否完成
        if (rhythmGameCompleted || (rhythmGameController != null && !rhythmGameController.IsGameRunning))
        {
            // 音游结束，检查结果并处理伤害
            if (!rhythmGameCompleted)
            {
                OnRhythmGameCompleted(bossController);
            }

            // 检查动画是否播放完成
            if (bossController is TestBoss5 testBoss)
            {
                if (!isAnimationFinished && testBoss.IsAnimationPlaying == false)
                {
                    isAnimationFinished = true;
                    OnAttack2Finished(bossController);
                }
            }

            // 如果时间过长，强制结束
            if (timer <= -2f && !isAnimationFinished)
            {
                Debug.LogWarning("TestBoss5 Attack2: Timeout, forcing finish");
                isAnimationFinished = true;
                OnAttack2Finished(bossController);
            }
        }

        // 安全检查：如果音游运行时间过长，强制结束
        if (stateTimer > rhythmGameDuration + 5f && !rhythmGameCompleted)
        {
            Debug.LogWarning("TestBoss5 Attack2: Rhythm Game running too long, force ending");
            OnRhythmGameCompleted(bossController);
            rhythmGameCompleted = true;
        }
    }

    private void OnAttack2Finished(BossController bossController)
    {
        Debug.Log("TestBoss5 Attack2 finished");

        if (bossController is TestBoss5 testBoss5)
        {
            if (testBoss5.IsStage3())
            {
                // 三阶段：Attack2结束后回到Idle3状态
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss5Idle3State());
            }
            else
            {
                // 一阶段：Attack2结束后回到Idle1状态
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss5Idle1State());
            }
        }
    }

    /// <summary>
    /// 启动音游机制
    /// </summary>
    private void StartRhythmGame(BossController bossController)
    {
        if (rhythmGameStarted) return;

        rhythmGameController = FindOrCreateRhythmGameController(bossController);

        if (rhythmGameController != null)
        {
            // 订阅音游结束事件
            rhythmGameController.OnRhythmGameEnded += () => OnRhythmGameCompleted(bossController);

            // 启动音游
            rhythmGameController.StartRhythmGame(
                RhythmGameController.RhythmGameExitCondition.Timer,
                rhythmGameDuration,
                targetArcsToDestroy
            );

            rhythmGameStarted = true;
            Debug.Log($"TestBoss5 Attack2: Rhythm Game started - Duration: {rhythmGameDuration}s, Target: {targetArcsToDestroy}");
        }
        else
        {
            Debug.LogError("TestBoss5 Attack2: Failed to create RhythmGameController!");
            rhythmGameCompleted = true;
        }
    }

    /// <summary>
    /// 查找或创建音游控制器
    /// </summary>
    private RhythmGameController FindOrCreateRhythmGameController(BossController bossController)
    {
        RhythmGameController controller = bossController.GetComponent<RhythmGameController>();

        if (controller == null)
        {
            controller = UnityEngine.Object.FindObjectOfType<RhythmGameController>();
        }

        if (controller == null)
        {
            GameObject rhythmGameObj = new GameObject("RhythmGameController_TestBoss5");
            controller = rhythmGameObj.AddComponent<RhythmGameController>();
            rhythmGameObj.transform.SetParent(bossController.transform);
            Debug.Log("TestBoss5 Attack2: Created new RhythmGameController");
        }

        return controller;
    }

    /// <summary>
    /// 音游完成回调（处理伤害）
    /// </summary>
    private void OnRhythmGameCompleted(BossController bossController)
    {
        if (rhythmGameCompleted) return;
        rhythmGameCompleted = true;

        Debug.Log("TestBoss5 Attack2: Rhythm Game completed");

        // 检查音游结果并处理伤害
        if (rhythmGameController != null && bossController is TestBoss5 testBoss5)
        {
            // 这里可以根据音游的完成情况来判断成功或失败
            // 假设检查是否有足够的判定块被消除
            // 实际实现可能需要根据RhythmGameController的具体API调整
            bool success = true; // 默认成功，实际应该检查音游结果

            if (success)
            {
                // 判定成功，伤害Boss
                float damage = testBoss5.GetAttack2DamageToBoss();
                bossController.TakeDamage(damage);
                Debug.Log($"TestBoss5 Attack2: Player succeeded! Boss took {damage} damage.");
            }
            else
            {
                // 判定失败，伤害玩家
                float damage = testBoss5.GetAttack2DamageToPlayer();
                Player player = bossController.FindPlayer();
                if (player != null)
                {
                    player.TakeDamage(damage);
                    Debug.Log($"TestBoss5 Attack2: Player failed! Player took {damage} damage.");
                }
            }
        }
    }

    /// <summary>
    /// 清理音游资源
    /// </summary>
    private void CleanupRhythmGame()
    {
        if (rhythmGameController != null)
        {
            rhythmGameController.OnRhythmGameEnded -= () => OnRhythmGameCompleted(null);

            if (rhythmGameController.IsGameRunning)
            {
                rhythmGameController.StopRhythmGame();
            }
        }

        rhythmGameStarted = false;
        rhythmGameCompleted = false;
        stateTimer = 0f;
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss5 {bossController.name} exited Attack2 State");
        CleanupRhythmGame();
    }
}

// TestBoss5的静止状态2（二阶段）
public class TestBoss5Idle2State : IBossState
{
    private float idleDuration = 3f;
    private float idleTimer;

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss5 {bossController.name} entered Idle2 State (Stage 2)");
        idleTimer = idleDuration;

        // 播放待机动画
        if (bossController is TestBoss5 testBoss5)
        {
            testBoss5.PlayAnimation(8); // PlayIdle2Animation()
            bossController.StartScreenPathMove(); // 随机选择路径点
        }
    }

    public void UpdateState(BossController bossController)
    {
        idleTimer -= Time.deltaTime;

        // 检查血量是否小于1/3，如果是则进入三阶段
        if (bossController is TestBoss5 testBoss5)
        {
            if (bossController.HealthPercentage < 1f/3f && !testBoss5.IsStage3())
            {
                Debug.Log("TestBoss5: Health below 1/3, entering Stage 3");
                testBoss5.SwitchToStage3();
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss5Attack5State());
                return;
            }
        }

        if (idleTimer <= 0f)
        {
            // Idle2状态结束后进入Attack3状态
            bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss5Attack3State());
        }
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss5 {bossController.name} exited Idle2 State");
    }
}

// TestBoss5的攻击状态3 - 区域攻击+召唤小兵
public class TestBoss5Attack3State : IBossState
{
    private bool isAnimationFinished = false;
    private float duration = 5f;
    private float timer;

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss5 {bossController.name} entered Attack3 State (Area Attack + Spawn Enemies)");

        timer = duration;
        isAnimationFinished = false;

        // 播放攻击3动画
        if (bossController is TestBoss5 testBoss5)
        {
            testBoss5.PlayAnimation(4); // PlayAttack3Animation()
            bossController.StartScreenPathMove();
        }
    }

    public void UpdateState(BossController bossController)
    {
        timer -= Time.deltaTime;

        // 检查动画是否播放完成
        if (timer <= 0f && !isAnimationFinished && bossController is TestBoss5 testBoss)
        {
            if (testBoss.IsAnimationPlaying == false)
            {
                isAnimationFinished = true;
                OnAttack3Finished(bossController);
            }
        }
    }

    private void OnAttack3Finished(BossController bossController)
    {
        Debug.Log("TestBoss5 Attack3 finished");

        // Attack3结束后进入Attack4状态
        bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss5Attack4State());
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss5 {bossController.name} exited Attack3 State");
    }
}

// TestBoss5的攻击状态4 - 导弹弹幕攻击
public class TestBoss5Attack4State : IBossState
{
    private bool isAnimationFinished = false;
    private float duration = 5f;
    private float timer;

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss5 {bossController.name} entered Attack4 State (Missile Barrage)");

        timer = duration;
        isAnimationFinished = false;

        // 播放攻击4动画
        if (bossController is TestBoss5 testBoss5)
        {
            testBoss5.PlayAnimation(5); // PlayAttack4Animation()
            bossController.StartScreenPathMove();
        }
    }

    public void UpdateState(BossController bossController)
    {
        timer -= Time.deltaTime;

        // 检查动画是否播放完成
        if (timer <= 0f && !isAnimationFinished && bossController is TestBoss5 testBoss)
        {
            if (testBoss.IsAnimationPlaying == false)
            {
                isAnimationFinished = true;
                OnAttack4Finished(bossController);
            }
        }
    }

    private void OnAttack4Finished(BossController bossController)
    {
        Debug.Log("TestBoss5 Attack4 finished");

        // Attack4结束后，检查是否应该进入三阶段
        if (bossController is TestBoss5 testBoss5)
        {
            if (bossController.HealthPercentage < 1f/3f && !testBoss5.IsStage3())
            {
                Debug.Log("TestBoss5: Health below 1/3 after Attack4, entering Stage 3");
                testBoss5.SwitchToStage3();
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss5Attack5State());
            }
            else
            {
                // 正常回到Idle2状态
                bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss5Idle2State());
            }
        }
        else
        {
            // 正常回到Idle2状态
            bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss5Idle2State());
        }
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss5 {bossController.name} exited Attack4 State");
    }
}

// TestBoss5的静止状态3（三阶段）
public class TestBoss5Idle3State : IBossState
{
    private float idleDuration = 3f;
    private float idleTimer;

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss5 {bossController.name} entered Idle3 State (Stage 3)");
        idleTimer = idleDuration;

        // 播放待机动画
        if (bossController is TestBoss5 testBoss5)
        {
            testBoss5.PlayAnimation(9); // PlayIdle3Animation()
            bossController.StartScreenPathMove(); // 随机选择路径点
        }
    }

    public void UpdateState(BossController bossController)
    {
        idleTimer -= Time.deltaTime;

        if (idleTimer <= 0f)
        {
            // Idle3状态结束后进入Attack5状态
            bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss5Attack5State());
        }
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss5 {bossController.name} exited Idle3 State");
    }
}

// TestBoss5的攻击状态5 - 迟滞准心移动
public class TestBoss5Attack5State : IBossState
{
    private bool isAnimationFinished = false;
    private float duration = 5f;
    private float timer;
    private bool lagEffectStarted = false;

    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss5 {bossController.name} entered Attack5 State (Crosshair Lag)");

        timer = duration;
        isAnimationFinished = false;
        lagEffectStarted = false;

        // 播放攻击5动画
        if (bossController is TestBoss5 testBoss5)
        {
            // 确保动画状态正确重置
            testBoss5.IsAnimationPlaying = false;
            testBoss5.PlayAnimation(6); // PlayAttack5Animation()
            bossController.StartScreenPathMove();
        }
    }

    public void UpdateState(BossController bossController)
    {
        timer -= Time.deltaTime;

        // 动画事件会触发准心迟滞效果，这里只检查动画是否完成
        if (timer <= 0f && !isAnimationFinished && bossController is TestBoss5 testBoss)
        {
            if (testBoss.IsAnimationPlaying == false)
            {
                isAnimationFinished = true;
                OnAttack5Finished(bossController);
            }
        }
    }

    private void OnAttack5Finished(BossController bossController)
    {
        Debug.Log("TestBoss5 Attack5 finished");

        // Attack5结束后进入Attack1状态
        bossController.GetComponent<BossStateMachine>().ChangeState(new TestBoss5Attack1State());
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss5 {bossController.name} exited Attack5 State");
    }
}

// TestBoss5的消失状态
public class TestBoss5DisappearState : IBossState
{
    public void EnterState(BossController bossController)
    {
        Debug.Log($"TestBoss5 {bossController.name} entered Disappear State");

        // 播放消失动画
        if (bossController is TestBoss5 testBoss5)
        {
            testBoss5.PlayAnimation(1); // PlayDisappearAnimation()
        }
    }

    public void UpdateState(BossController bossController)
    {
        // 检查动画是否播放完成
        if (bossController is TestBoss5 testBoss5)
        {
            if (!testBoss5.IsAnimationPlaying)
            {
                // 恢复正常准心移动
                CrosshairController crosshairController = UnityEngine.GameObject.FindObjectOfType<CrosshairController>();
                if (crosshairController != null)
                {
                    crosshairController.SetLagMode(false, 1f);
                }

                OnDisappearFinished(bossController);
            }
        }
    }

    private void OnDisappearFinished(BossController bossController)
    {
        Debug.Log("TestBoss5 disappear animation finished");

        // 动画播放完成后boss消失
        bossController.DestroyBoss();
    }

    public void ExitState(BossController bossController)
    {
        Debug.Log($"TestBoss5 {bossController.name} exited Disappear State");
    }
}