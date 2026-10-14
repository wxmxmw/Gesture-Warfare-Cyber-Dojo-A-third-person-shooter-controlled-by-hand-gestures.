using System.Collections.Generic;
using UnityEngine;

public class TestBoss4 : BossController
{
    [Header("Animation Clips")]
    [SerializeField] private AnimationClip appearAnimation;
    [SerializeField] private AnimationClip disappearAnimation;
    [SerializeField] private AnimationClip attack1Animation1; // 第一个attack1动画
    [SerializeField] private AnimationClip attack1Animation2; // 第二个attack1动画
    [SerializeField] private AnimationClip attack2Animation;
    [SerializeField] private AnimationClip attack3Animation;
    [SerializeField] private AnimationClip attack4Animation;
    [SerializeField] private AnimationClip idle1Animation;
    [SerializeField] private AnimationClip idle2Animation;
    [SerializeField] private AnimationClip moveAnimation;
    [SerializeField] private AnimationClip exposedAnimation; // 暴露状态动画
    [SerializeField] private AnimationClip deadAnimation;

    [Header("Animation Timing")]
    [SerializeField] private float appearDelay = 0f;
    [SerializeField] private float disappearDelay = 0f;
    [SerializeField] private float attackDelay = 0f;
    [SerializeField] private float exposedDelay = 0f;
    [SerializeField] private float deadDelay = 0f;

    public bool IsAnimationPlaying = false;

    [Header("Attack Prefabs")]
    [SerializeField] private GameObject AreaAttack1Prefab; // 区域攻击预制体 - Attack1
    [SerializeField] private GameObject AreaAttack2Prefab; // 区域攻击预制体 - Attack2
    [SerializeField] private GameObject CancelableAttackPrefab; // 可消除攻击预制体

    [Header("Attack Settings")]
    [SerializeField] private float attack1Damage = 15f; // Attack1伤害
    [SerializeField] private float attack2Damage = 20f; // Attack2伤害
    [SerializeField] private float attack3Damage = 25f; // Attack3伤害

    [Header("Audio System")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip appearSound;
    [SerializeField] private AudioClip disappearSound;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip exposedSound; // 暴露状态音效
    [SerializeField] private AudioClip moveSound;
    [SerializeField] private AudioClip deadSound;
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 1f;

    // Boss状态跟踪
    private bool isStage2 = false; // 是否为二阶段
    private int destroyedBodyPartsCount = 0; // 被破坏的身体部位数量
    private const int STAGE_TRANSITION_THRESHOLD = 1; // 进入暴露状态的阈值

    // 身体部位引用（用于快速访问）
    private List<BodyPart> destructibleBodyParts = new List<BodyPart>();

    protected override void MakeAIDecision()
    {
        // TestBoss4的AI决策逻辑由状态机处理
    }

    protected override void Start()
    {
        base.Start(); // 确保调用基类的Start方法

        // 如果没有设置AudioSource，尝试获取
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // 初始化身体部位系统
        InitializeBodyParts();

        // 初始化状态机并设置初始状态
        InitializeStateMachine();
        stateMachine.ChangeState(new TestBoss4AppearState());

        AdjustScreenPathPoints();
    }

    // 因为Boss4的下面的组件的相对位置有点怪，在这里调一下路径点位置
    protected void AdjustScreenPathPoints()
    {
        if (screenPathPoints.Count != 0)
        {
            for (int i = 0; i < screenPathPoints.Count; i++)
            {
                screenPathPoints[i] += new Vector3(0, 250, 0);
            }
        }
    }

    /// <summary>
    /// 初始化身体部位系统 - TestBoss4特有的可破坏身体部位结构
    /// </summary>
    protected override void InitializeBodyParts()
    {
        base.InitializeBodyParts();

        // 为所有部位添加EnemyBodyPart组件
        foreach (var bodyPart in bodyParts)
        {
            if (bodyPart.partObject != null)
            {
                var bodyPartComponent = bodyPart.partObject.GetComponent<EnemyBodyPart>();
                if (bodyPartComponent == null)
                {
                    bodyPartComponent = bodyPart.partObject.AddComponent<EnemyBodyPart>();
                }
                bodyPartComponent.isBoss = true; // 标记为Boss的身体部位
                bodyPartComponent.Initialize(this, bodyPart);

                // 收集可破坏的身体部位
                if (bodyPart.enableDestruction)
                {
                    destructibleBodyParts.Add(bodyPart);
                }
            }
        }

        Debug.Log($"TestBoss4: Initialized {destructibleBodyParts.Count} destructible body parts");
    }

    // 动画播放方法
    public void PlayAppearAnimation()
    {
        if (bossAnimator == null || appearAnimation == null) return;
        StartCoroutine(PlayAnimationWithDelay(appearAnimation, appearDelay));
    }

    public void PlayDisappearAnimation()
    {
        if (bossAnimator == null || disappearAnimation == null) return;
        StartCoroutine(PlayAnimationWithDelay(disappearAnimation, disappearDelay));
    }

    public void PlayAttack1Animation1()
    {
        if (bossAnimator == null || attack1Animation1 == null) return;
        StartCoroutine(PlayAnimationWithDelay(attack1Animation1, attackDelay));
    }

    public void PlayAttack1Animation2()
    {
        if (bossAnimator == null || attack1Animation2 == null) return;
        StartCoroutine(PlayAnimationWithDelay(attack1Animation2, attackDelay));
    }

    public void PlayAttack2Animation()
    {
        if (bossAnimator == null || attack2Animation == null) return;
        StartCoroutine(PlayAnimationWithDelay(attack2Animation, attackDelay));
    }

    public void PlayAttack3Animation()
    {
        if (bossAnimator == null || attack3Animation == null) return;
        StartCoroutine(PlayAnimationWithDelay(attack3Animation, attackDelay));
    }

    public void PlayAttack4Animation()
    {
        if (bossAnimator == null || attack4Animation == null) return;
        StartCoroutine(PlayAnimationWithDelay(attack4Animation, attackDelay));
    }

    public void PlayIdle1Animation()
    {
        if (bossAnimator == null || idle1Animation == null) return;
        bossAnimator.Play(idle1Animation.name);
        isPlayingAnimation = false;
    }

    public void PlayIdle2Animation()
    {
        if (bossAnimator == null || idle2Animation == null) return;
        bossAnimator.Play(idle2Animation.name);
        isPlayingAnimation = false;
    }

    public void PlayMoveAnimation()
    {
        if (bossAnimator == null || moveAnimation == null) return;
        bossAnimator.Play(moveAnimation.name);
        isPlayingAnimation = false;
    }

    public void PlayExposedAnimation()
    {
        if (bossAnimator == null || exposedAnimation == null) return;
        StartCoroutine(PlayAnimationWithDelay(exposedAnimation, exposedDelay));
    }

    public override void PlayAnimation(int index)
    {
        switch (index)
        {
            case 0: PlayAppearAnimation(); break;
            case 1: PlayDisappearAnimation(); break;
            case 3: PlayAttack1Animation1(); break; // 第一个attack1动画
            case 4: PlayAttack2Animation(); break;
            case 5: PlayAttack3Animation(); break;
            case 6: PlayAttack4Animation(); break;
            case 7: PlayIdle1Animation(); break;
            case 8: PlayIdle2Animation(); break;
            case 9: PlayAttack1Animation2(); break; // 第二个attack1动画
            case 10: PlayExposedAnimation(); break;
            default: break;
        }
        Debug.Log($"TestBoss4: Playing animation {index}");
    }

    // 带延迟的动画播放协程
    private System.Collections.IEnumerator PlayAnimationWithDelay(AnimationClip clip, float delay)
    {
        if (bossAnimator == null || clip == null) yield break;

        isPlayingAnimation = true;

        // 等待延迟时间
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        // 直接播放动画片段
        bossAnimator.Play(clip.name);

        // 等待动画播放完成
        yield return new WaitForSeconds(clip.length);

        isPlayingAnimation = false;
    }

    /// <summary>
    /// Attack1 - 创建区域攻击的动画事件 1-从左到右创建攻击
    /// </summary>
    private void AniEvent_TriggerAttack1_1()
    {
        Debug.Log("TestBoss4: Attack1-1 animation event triggered - Left to Right Attack");
        PlaySound(attackSound);

        StartCoroutine(CreateLeftToRightAttackSequence());
    }

    /// <summary>
    /// Attack1 - 创建区域攻击的动画事件 2-从右到左创建攻击
    /// </summary>
    private void AniEvent_TriggerAttack1_2()
    {
        Debug.Log("TestBoss4: Attack1-2 animation event triggered - Right to Left Attack");
        PlaySound(attackSound);

        StartCoroutine(CreateRightToLeftAttackSequence());
    }

    /// <summary>
    /// 从左到右创建攻击序列
    /// </summary>
    private System.Collections.IEnumerator CreateLeftToRightAttackSequence()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || AreaAttack1Prefab == null) yield break;

        // 获取屏幕信息
        Camera mainCamera = Camera.main;
        if (mainCamera == null) yield break;

        //float screenHeight = 2f * mainCamera.orthographicSize;
        //float screenWidth = screenHeight * mainCamera.aspect;

        float screenWidth = mainCamera.ScreenToWorldPoint(new Vector3(2 * Screen.width, 0, 0)).x;

        // 游戏屏幕范围是[0, 屏幕宽度]
        float screenLeft = 0f;
        float screenRight = screenWidth * 2;
        
        // 获取玩家的Y和Z坐标作为攻击线
        Vector3 playerPos = player.transform.position;
        float attackY = playerPos.y;
        float attackZ = playerPos.z;

        // 获取区域攻击的判定半径
        float attackRadius = GetAreaAttackRadius();
        float attackSpacing = attackRadius * 1.8f; // 攻击间距，稍微重叠以确保覆盖

        // 计算攻击覆盖的X轴范围（屏幕宽度的2/3）
        float attackCoverageWidth = screenWidth * 2 * (2f/3f);
        float attackStartX = screenLeft;
        float attackEndX = screenLeft + attackCoverageWidth;

        // 计算需要放置的攻击数量
        int attackCount = Mathf.FloorToInt(attackCoverageWidth / attackSpacing) + 1;
        
        Debug.Log($"TestBoss4 Attack1-1: Creating {attackCount} attacks from X={attackStartX:F2} to X={attackEndX:F2}, spacing={attackSpacing:F2}");

        // 从左到右依次创建攻击
        for (int i = 0; i < attackCount; i++)
        {
            float currentX = attackStartX + (i * attackSpacing);
            
            // 确保不超出攻击范围
            if (currentX > attackEndX) break;

            Vector3 attackPos = new Vector3(currentX, attackY, attackZ);
            CreateSingleAreaAttack(attackPos);

            // 攻击间隔时间
            yield return new WaitForSeconds(0.15f);
        }
    }

    /// <summary>
    /// 从右到左创建攻击序列
    /// </summary>
    private System.Collections.IEnumerator CreateRightToLeftAttackSequence()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || AreaAttack1Prefab == null) yield break;

        // 获取屏幕信息
        Camera mainCamera = Camera.main;
        if (mainCamera == null) yield break;

        //float screenHeight = 2f * mainCamera.orthographicSize;
        //float screenWidth = screenHeight * mainCamera.aspect;

        float screenWidth = mainCamera.ScreenToWorldPoint(new Vector3(2 * Screen.width, 0, 0)).x;

        // 游戏屏幕范围是[0, 屏幕宽度]
        float screenLeft = 0f;
        float screenRight = screenWidth * 2;
        
        
        // 获取玩家的Y和Z坐标作为攻击线
        Vector3 playerPos = player.transform.position;
        float attackY = playerPos.y;
        float attackZ = playerPos.z;

        // 获取区域攻击的判定半径
        float attackRadius = GetAreaAttackRadius();
        float attackSpacing = attackRadius * 1.8f; // 攻击间距，稍微重叠以确保覆盖

        // 计算攻击覆盖的X轴范围（屏幕宽度的2/3）
        float attackCoverageWidth = screenWidth * 2 * (2f/3f);
        float attackStartX = screenRight - attackCoverageWidth;  // 从右边开始的2/3处
        float attackEndX = screenRight;

        // 计算需要放置的攻击数量
        int attackCount = Mathf.FloorToInt(attackCoverageWidth / attackSpacing) + 1;
        
        Debug.Log($"TestBoss4 Attack1-2: Creating {attackCount} attacks from X={attackEndX:F2} to X={attackStartX:F2}, spacing={attackSpacing:F2}");

        // 从右到左依次创建攻击
        for (int i = 0; i < attackCount; i++)
        {
            float currentX = attackEndX - (i * attackSpacing);
            
            // 确保不超出攻击范围
            if (currentX < attackStartX) break;

            Vector3 attackPos = new Vector3(currentX, attackY, attackZ);
            CreateSingleAreaAttack(attackPos);

            // 攻击间隔时间
            yield return new WaitForSeconds(0.15f);
        }
    }

    /// <summary>
    /// 创建单个区域攻击
    /// </summary>
    private void CreateSingleAreaAttack(Vector3 attackPosition)
    {
        if (AreaAttack1Prefab == null) return;

        GameObject attackObj = Instantiate(AreaAttack1Prefab, attackPosition, Quaternion.identity);
        
        AreaAttackProjectile areaAttack = attackObj.GetComponent<AreaAttackProjectile>();
        if (areaAttack != null)
        {
            areaAttack.SetEnemyPosition(this.transform.position + new Vector3(0, -250, 0));
            areaAttack.AniEvent_SetTrajectoryStartPosition(this.transform.position + new Vector3(0, -250, 0));
            areaAttack.AniEvent_SetTrajectoryEndPosition(attackPosition);
            
            Debug.Log($"TestBoss4: Created area attack at position {attackPosition}");
        }
    }

    /// <summary>
    /// 获取区域攻击的判定半径
    /// </summary>
    private float GetAreaAttackRadius()
    {
        if (AreaAttack1Prefab == null) return 2f; // 默认半径

        // 尝试从预制体获取攻击半径
        AreaAttackProjectile prefabAttack = AreaAttack1Prefab.GetComponent<AreaAttackProjectile>();
        if (prefabAttack != null)
        {
            float result = prefabAttack.getAttackRadius();
            return result;
        }

        // 如果无法获取，返回默认值
        return 2f;
    }

    /// <summary>
    /// Attack2 - 创建可消除攻击的动画事件
    /// </summary>
    private void AniEvent_TriggerAttack2()
    {
        Debug.Log("TestBoss4: Attack2 animation event triggered - Destructible Attack");

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            GameObject attackPrefab = Instantiate(CancelableAttackPrefab, this.transform.position - new Vector3(0, 250, 0), Quaternion.identity);
            AttackProjectile attackProjectile = attackPrefab.GetComponent<AttackProjectile>();
            //attackProjectile.InitializeByBoss(this.transform.position + new Vector3(Random.Range(-Camera.main.ScreenToWorldPoint(new Vector3(2 * Screen.width, 0, 0)).x, Camera.main.ScreenToWorldPoint(new Vector3(2 * Screen.width, 0, 0)).x), -250, 0), this.transform.position - new Vector3(0, 250, 0));
            attackProjectile.InitializeByBoss(this.transform.position + new Vector3(0, -250, 0), this.transform.position - new Vector3(0, 250, 0));

        }
    }

    /// <summary>
    /// Attack3 - 创建区域攻击的动画事件
    /// </summary>
    private void AniEvent_TriggerAttack3()
    {
        Debug.Log("TestBoss4: Attack3 animation event triggered - Enhanced Area Attack");

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            GameObject a = Instantiate(AreaAttack2Prefab, player.transform.position + new Vector3(Random.Range(-10f, 10f), 0, 0), Quaternion.identity);
            a.GetComponent<AreaAttackProjectile>().SetEnemyPosition(this.transform.position);

            AniEvent_PlayAttackSound();
            a.GetComponent<AreaAttackProjectile>().AniEvent_SetTrajectoryStartPosition(transform.position);
            //a.GetComponent<AreaAttackProjectile>().AniEvent_SetTrajectoryStartPosition(new Vector3(400, 900, 0));
            a.GetComponent<AreaAttackProjectile>().AniEvent_SetTrajectoryEndPosition(player.transform.position);
        }

    }

    /// <summary>
    /// Attack4 - 迟缓准心移动的动画事件
    /// </summary>
    private void AniEvent_TriggerAttack4()
    {
        Debug.Log("TestBoss4: Attack4 animation event triggered - Crosshair Movement Lag Effect");
        PlaySound(attackSound);

        // 启动准心迟缓效果
        StartCrosshairLagEffect();
    }

    /// <summary>
    /// 启动准心迟缓效果
    /// </summary>
    private void StartCrosshairLagEffect()
    {
        // 查找场景中的CrosshairController
        CrosshairController crosshairController = FindObjectOfType<CrosshairController>();
        
        if (crosshairController != null)
        {
            // 启动迟缓效果协程
            StartCoroutine(ApplyCrosshairLagEffect(crosshairController));
        }
        else
        {
            Debug.LogWarning("TestBoss4: CrosshairController not found for Attack4 lag effect");
        }
    }

    /// <summary>
    /// 应用准心迟缓效果的协程
    /// </summary>
    private System.Collections.IEnumerator ApplyCrosshairLagEffect(CrosshairController crosshairController)
    {
        float lagDuration = 15f; // 迟缓效果持续时间
        float lagSpeed = 0.3f; // 迟缓移动速度（越小越慢）
        
        Debug.Log($"TestBoss4: Applying crosshair lag effect for {lagDuration} seconds with speed {lagSpeed}");

        // 启用准心迟缓模式
        EnableCrosshairLagMode(crosshairController, true, lagSpeed);

        // 等待持续时间
        yield return new WaitForSeconds(lagDuration);

        // 恢复正常准心移动
        EnableCrosshairLagMode(crosshairController, false, 1f);
        
        Debug.Log("TestBoss4: Crosshair lag effect ended, movement restored to normal");
    }

    /// <summary>
    /// 启用或禁用准心迟缓模式
    /// </summary>
    public void EnableCrosshairLagMode(CrosshairController crosshairController, bool enableLag, float lagSpeed)
    {
        // 使用反射来设置CrosshairController的迟缓模式
        // 这里假设我们需要在CrosshairController中添加相应的字段和方法
        
        var lagModeField = typeof(CrosshairController).GetField("isLagModeEnabled", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var lagSpeedField = typeof(CrosshairController).GetField("lagMoveSpeed", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (lagModeField != null)
        {
            lagModeField.SetValue(crosshairController, enableLag);
        }
        
        if (lagSpeedField != null)
        {
            lagSpeedField.SetValue(crosshairController, lagSpeed);
        }

        // 如果反射失败，尝试调用公共方法
        var lagModeMethod = typeof(CrosshairController).GetMethod("SetLagMode", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        
        if (lagModeMethod != null)
        {
            lagModeMethod.Invoke(crosshairController, new object[] { enableLag, lagSpeed });
        }
        else
        {
            Debug.LogWarning("TestBoss4: Could not find SetLagMode method in CrosshairController. Please implement lag mode support.");
        }
    }

    /// <summary>
    /// 检查是否有超过限制数量的身体部位被破坏
    /// </summary>
    public bool CheckBodyPartsDestruction()
    {
        int currentDestroyedCount = 0;
        foreach (var bodyPart in destructibleBodyParts)
        {
            if (bodyPart.partObject != null)
            {
                var bodyPartComponent = bodyPart.partObject.GetComponent<EnemyBodyPart>();
                if (bodyPartComponent != null && bodyPartComponent.IsPartDestroyed())
                {
                    currentDestroyedCount++;
                }
            }
        }

        // 检查是否新增了破坏的身体部位
        if (currentDestroyedCount > destroyedBodyPartsCount)
        {
            destroyedBodyPartsCount = currentDestroyedCount;
            Debug.Log($"TestBoss4: {destroyedBodyPartsCount} body part(s) destroyed!");

            // 检查是否达到暴露状态阈值
            if (destroyedBodyPartsCount >= STAGE_TRANSITION_THRESHOLD)
            {
                Debug.Log($"TestBoss4: More than {STAGE_TRANSITION_THRESHOLD} body parts destroyed!");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 获取被破坏的身体部位数量
    /// </summary>
    public int GetDestroyedBodyPartsCount()
    {
        return destroyedBodyPartsCount;
    }

    /// <summary>
    /// 获取剩余身体部位数量
    /// </summary>
    public int GetRemainingBodyPartsCount()
    {
        return destructibleBodyParts.Count - destroyedBodyPartsCount;
    }

    /// <summary>
    /// 检查是否应该进入二阶段
    /// </summary>
    public bool ShouldEnterStage2()
    {
        return destroyedBodyPartsCount > STAGE_TRANSITION_THRESHOLD && !isStage2;
    }

    /// <summary>
    /// 切换到二阶段
    /// </summary>
    public void SwitchToStage2()
    {
        if (!isStage2)
        {
            isStage2 = true;
            Debug.Log("TestBoss4 switched to Stage 2!");
            
            // 可以在这里添加二阶段的特殊效果
            // 例如：改变外观、增强攻击力等
        }
    }

    /// <summary>
    /// 获取当前是否为二阶段
    /// </summary>
    public bool IsStage2()
    {
        return isStage2;
    }

    // 音效播放方法
    private void AniEvent_PlayAppearSound() { PlaySound(appearSound); }
    private void AniEvent_PlayDisappearSound() { PlaySound(disappearSound); }
    private void AniEvent_PlayAttackSound() { PlaySound(attackSound); }
    private void AniEvent_PlayExposedSound() { PlaySound(exposedSound); }
    private void AniEvent_PlayMoveSound() { PlaySound(moveSound); }
    private void AniEvent_PlayDeadSound() { PlaySound(deadSound); }

    /// <summary>
    /// 播放指定音效的核心方法
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.volume = soundVolume;
            audioSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// 停止当前播放的音效
    /// </summary>
    public void StopSound()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    /// <summary>
    /// 设置音效音量
    /// </summary>
    public void SetSoundVolume(float volume)
    {
        soundVolume = Mathf.Clamp01(volume);
        if (audioSource != null)
        {
            audioSource.volume = soundVolume;
        }
    }

    protected override System.Collections.IEnumerator DeathSequence()
    {
        // 播放死亡动画
        PlayDeathAnimation();

        stateMachine.ChangeState(new TestBoss4DisappearState());

        while (isPlayingAnimation)
        {
            yield return null;
        }

        // 恢复正常准心移动
        EnableCrosshairLagMode(FindObjectOfType<CrosshairController>(), false, 1f);

        // 死亡效果
        yield return new WaitForSeconds(3f);
        Destroy(gameObject);
    }

    /// <summary>
    /// 播放死亡动画
    /// </summary>
    protected override void PlayDeathAnimation()
    {
        if (bossAnimator != null && deadAnimation != null)
        {
            StartCoroutine(PlayAnimationWithDelay(deadAnimation, deadDelay));
        }
    }

    public void AniEvent_EnableCoreDamage(int CoreIndex)
    {
        foreach(var bodypart in bodyParts)
        {
            if(bodypart.partName == ("core" + CoreIndex.ToString()).Trim()) 
            {
                bodypart.damageMultiplier = 1;
                return;
            }
        }
    }

    public void AniEvent_DisableCoreDamage(int CoreIndex) 
    {
        foreach (var bodypart in bodyParts)
        {
            if (bodypart.partName == ("core" + CoreIndex.ToString()).Trim())
            {
                bodypart.damageMultiplier = 0;
                return;
            }
        }
    }

}
