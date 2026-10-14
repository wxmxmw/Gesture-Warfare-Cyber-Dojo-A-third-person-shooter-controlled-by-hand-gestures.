using System.Collections.Generic;
using UnityEngine;

public class TestBoss3 : BossController
{
    [Header("Animation Clips")]
    [SerializeField] private AnimationClip appearAnimation;
    [SerializeField] private AnimationClip disappearAnimation;
    [SerializeField] private AnimationClip preparationAnimation;
    [SerializeField] private AnimationClip attack1Animation;
    [SerializeField] private AnimationClip attack2Animation;
    [SerializeField] private AnimationClip attack3Animation;
    [SerializeField] private AnimationClip attack4Animation;
    [SerializeField] private AnimationClip idle1Animation;
    [SerializeField] private AnimationClip idle2Animation;
    [SerializeField] private AnimationClip moveAnimation;
    [SerializeField] private AnimationClip deadAnimation;

    [Header("Animation Timing")]
    [SerializeField] private float appearDelay = 0f;
    [SerializeField] private float disappearDelay = 0f;
    [SerializeField] private float preparationDelay = 0f;
    [SerializeField] private float attackDelay = 0f;
    [SerializeField] private float deadDelay = 0f;

    public bool IsAnimationPlaying = false;

    [Header("Attack Prefabs")]
    [SerializeField] private GameObject AreaAttackPrefab; // 基础区域攻击
    [SerializeField] private GameObject LaserAttackPrefab; // 激光攻击
    [SerializeField] private GameObject MissileAttackPrefab; // 子弹预制体（弹幕攻击用）

    [Header("Attack1 Settings - 准心区域攻击")]
    [SerializeField] private float attack1DamagePerSecond = 10f; // 每秒伤害
    [SerializeField] private float[] attack1AreaSizes = {1f, 0.5f, 0.25f}; // 三次攻击的区域大小比例
    [SerializeField] private float attack1Duration = 5f; // 单次攻击持续时间

    [Header("Attack2 Settings - 连续区域攻击")]
    [SerializeField] private int attack2Count = 5; // 连续攻击次数
    [SerializeField] private float attack2Interval = 1f; // 攻击间隔

    [Header("Attack3 Settings - 激光攻击")]
    [SerializeField] private float laserDamage = 30f; // 激光伤害
    [SerializeField] private float laserWidth = 2f; // 激光宽度
    [SerializeField] private float laserDuration = 3f; // 激光持续时间

    [Header("Attack4 Settings - 弹幕攻击")]
    [SerializeField] private int bulletCount = 8; // 每次发射的子弹数量
    [SerializeField] private float bulletSpeed = 5f; // 子弹速度
    [SerializeField] private float bulletDamage = 15f; // 子弹伤害
    [SerializeField] private float bulletFireRate = 0.5f; // 发射间隔

    [Header("Audio System")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip appearSound;
    [SerializeField] private AudioClip disappearSound;
    [SerializeField] private AudioClip preparationSound;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip laserSound;
    [SerializeField] private AudioClip bulletSound;
    [SerializeField] private AudioClip moveSound;
    [SerializeField] private AudioClip deadSound;
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 1f;

    // Boss状态跟踪
    private bool isStage2 = false; // 是否为二阶段
    private int destroyedHeadCount = 0; // 被破坏的蛇头数量

    // 蛇头部位引用（用于快速访问）
    private List<BodyPart> snakeHeads = new List<BodyPart>();
    private BodyPart snakeBody;

    protected override void MakeAIDecision()
    {
        // TestBoss3的AI决策逻辑由状态机处理
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
        stateMachine.ChangeState(new TestBoss3AppearState());

        AdjustScreenPathPoints();
    }

    // 因为Boss3的下面的组件的相对位置有点怪，在这里调一下路径点位置
    protected void AdjustScreenPathPoints() 
    {
        if (screenPathPoints.Count != 0) 
        {
            for(int i = 0;i < screenPathPoints.Count;i++) 
            {

                screenPathPoints[i] += new Vector3(100, -200, 0);
            }
        }
    }

    /// <summary>
    /// 初始化身体部位系统 - TestBoss3特有的蛇头+身体结构
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

                if (bodyPart.enableDestruction) 
                {
                    snakeHeads.Add(bodyPart);
                }
            }
        }
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

    public void PlayPreparationAnimation()
    {
        if (bossAnimator == null || preparationAnimation == null) return;
        StartCoroutine(PlayAnimationWithDelay(preparationAnimation, preparationDelay));
    }

    public void PlayAttack1Animation()
    {
        if (bossAnimator == null || attack1Animation == null) return;
        StartCoroutine(PlayAnimationWithDelay(attack1Animation, attackDelay));
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

    public override void PlayAnimation(int index)
    {
        switch (index)
        {
            case 0: PlayAppearAnimation(); break;
            case 1: PlayDisappearAnimation(); break;
            case 2: PlayPreparationAnimation(); break;
            case 3: PlayAttack1Animation(); break;
            case 4: PlayAttack2Animation(); break;
            case 5: PlayAttack3Animation(); break;
            case 6: PlayAttack4Animation(); break;
            case 7: PlayIdle1Animation(); break;
            case 8: PlayIdle2Animation(); break;
            case 9: PlayMoveAnimation(); break;
            default: break;
        }
        Debug.Log("play ani" + index);
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
    /// Attack1 - 准心区域攻击的动画事件
    /// </summary>
    private void AniEvent_TriggerAttack1()
    {
        // TODO: 在这里实现Attack1的攻击逻辑
        Debug.Log("TestBoss3: Attack1 animation event triggered");
        PlaySound(attackSound);
        
        // 示例：连续三次区域攻击，区域大小递减
        StartCoroutine(ExecuteAttack1Sequence());
    }
    
    private System.Collections.IEnumerator ExecuteAttack1Sequence()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) yield break;

        for (int i = 0; i < attack1AreaSizes.Length; i++)
        {
            // 在玩家位置创建区域攻击
            Vector3 attackPos = player.transform.position;
            GameObject attackObj = Instantiate(AreaAttackPrefab, attackPos, Quaternion.identity);
            AreaAttackProjectile areaAttack = attackObj.GetComponent<AreaAttackProjectile>();
            
            if (areaAttack != null)
            {
                areaAttack.SetEnemyPosition(transform.position);
                areaAttack.InitializeByBoss(transform.position, attackPos);
                
                // 设置区域攻击的大小
                SetAreaAttackSize(areaAttack, attack1AreaSizes[i]);
            }

            PlaySound(attackSound);
            
            // 等待下次攻击
            if (i < attack1AreaSizes.Length - 1)
            {
                yield return new WaitForSeconds(1f);
            }
        }
    }

    /// <summary>
    /// Attack2 - 连续区域攻击的动画事件
    /// </summary>
    private void AniEvent_TriggerAttack2()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            GameObject a = Instantiate(AreaAttackPrefab, player.transform.position + new Vector3(Random.Range(-10f, 10f), 0, 0), Quaternion.identity);
            a.GetComponent<AreaAttackProjectile>().SetEnemyPosition(this.transform.position);

            AniEvent_PlayAreaAttackSound();
            a.GetComponent<AreaAttackProjectile>().AniEvent_SetTrajectoryStartPosition(transform.position);
            //a.GetComponent<AreaAttackProjectile>().AniEvent_SetTrajectoryStartPosition(new Vector3(400, 900, 0));
            a.GetComponent<AreaAttackProjectile>().AniEvent_SetTrajectoryEndPosition(player.transform.position);
        }
    }
    private void AniEvent_PlayAreaAttackSound()
    {
        PlaySound(bulletSound);
    }

    /// <summary>
    /// 重新设置掩体
    /// </summary>
    private void AniEvent_ReCreateCover() 
    {
        // 生成一个随机长度的掩体索引数组（长度1-6，值范围0-9，不重复）
        int[] coverIndices = GenerateRandomCoverIndices();
        
        // 调用CoverController重新创建掩体
        CoverController.Instance.ReCreateCover(coverIndices);
        
        Debug.Log($"重新设置掩体：{string.Join(", ", coverIndices)}");
    }
    
    /// <summary>
    /// 生成随机的掩体索引数组
    /// </summary>
    /// <returns>不重复的掩体索引数组，长度1-6，值范围0-9</returns>
    private int[] GenerateRandomCoverIndices()
    {
        // 随机决定掩体数量（1-6个）
        int coverCount = Random.Range(1, 7);
        
        // 创建0-9的索引池
        List<int> availableIndices = new List<int>();
        for (int i = 0; i < 10; i++)
        {
            availableIndices.Add(i);
        }
        
        // 随机选择不重复的索引
        List<int> selectedIndices = new List<int>();
        for (int i = 0; i < coverCount; i++)
        {
            int randomIndex = Random.Range(0, availableIndices.Count);
            selectedIndices.Add(availableIndices[randomIndex]);
            availableIndices.RemoveAt(randomIndex);
        }
        
        // 排序并转换为数组
        selectedIndices.Sort();
        return selectedIndices.ToArray();
    }

    /// <summary>
    /// Attack3 - 激光攻击的动画事件
    /// </summary>
    private void AniEvent_TriggerAttack3()
    {
        // 随机选择激光攻击模式
        int laserMode = Random.Range(0, 3);
        switch (laserMode)
        {
            case 0: ExecuteLaserFromLeft(); break;
            case 1: ExecuteLaserFromRight(); break;
            case 2: ExecuteLaserFromBothSides(); break;
        }
    }

    private void ExecuteLaserFromLeft()
    {
        // 从左往右的激光攻击
        // 激光从屏幕左侧开始，向右扫射
        CreateLaser(LaserMode.LeftToRight);
        PlaySound(laserSound);
    }

    private void ExecuteLaserFromRight()
    {
        // 从右往左的激光攻击
        // 激光从屏幕右侧开始，向左扫射
        CreateLaser(LaserMode.RightToLeft);
        PlaySound(laserSound);
    }

    private void ExecuteLaserFromBothSides()
    {
        // 两边往中间的激光攻击
        // 两个激光同时从两边向中间扫射
        CreateLaser(LaserMode.BothSidesToCenter);
        PlaySound(laserSound);
    }

    /// <summary>
    /// 激光攻击模式枚举
    /// </summary>
    private enum LaserMode
    {
        LeftToRight,        // 从左往右
        RightToLeft,        // 从右往左
        BothSidesToCenter   // 两边往中间
    }

    /// <summary>
    /// 创建激光攻击
    /// </summary>
    /// <param name="mode">激光模式</param>
    private void CreateLaser(LaserMode mode)
    {
        if (LaserAttackPrefab == null)
        {
            Debug.LogWarning("LaserAttackPrefab is null!");
            return;
        }

        // 获取屏幕边界信息
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        float screenHeight = 2f * mainCamera.orthographicSize;
        float screenWidth = screenHeight * mainCamera.aspect;
        Vector3 screenCenter = new Vector3(mainCamera.transform.position.x, mainCamera.transform.position.y, 0);

        switch (mode)
        {
            case LaserMode.LeftToRight:
                CreateSingleLaser(
                    screenCenter + new Vector3(-screenWidth/2 - 2f, 0f, 0f), // 激光位置（屏幕左侧外）
                    screenCenter, // 旋转原点（屏幕中心）
                    180f, // 起始角度（指向右）
                    0f,   // 结束角度（指向右）
                    laserDuration
                );
                break;

            case LaserMode.RightToLeft:
                CreateSingleLaser(
                    screenCenter + new Vector3(screenWidth/2 + 2f, 0f, 0f), // 激光位置（屏幕右侧外）
                    screenCenter, // 旋转原点（屏幕中心）
                    0f,   // 起始角度（指向左）
                    180f, // 结束角度（指向左）
                    laserDuration
                );
                break;

            case LaserMode.BothSidesToCenter:
                // 创建左侧激光
                CreateSingleLaser(
                    screenCenter + new Vector3(-screenWidth/2 - 2f, 2f, 0f), // 激光位置（屏幕左上侧外）
                    screenCenter + new Vector3(0f, 2f, 0f), // 旋转原点（屏幕中心上方）
                    180f, // 起始角度
                    90f,  // 结束角度（向中心）
                    laserDuration
                );
                
                // 创建右侧激光
                CreateSingleLaser(
                    screenCenter + new Vector3(screenWidth/2 + 2f, -2f, 0f), // 激光位置（屏幕右下侧外）
                    screenCenter + new Vector3(0f, -2f, 0f), // 旋转原点（屏幕中心下方）
                    0f,   // 起始角度
                    90f,  // 结束角度（向中心）
                    laserDuration
                );
                break;
        }
    }

    /// <summary>
    /// 创建单个激光
    /// </summary>
    /// <param name="laserPosition">激光位置</param>
    /// <param name="pivotPosition">旋转原点位置</param>
    /// <param name="startAngle">起始角度</param>
    /// <param name="endAngle">结束角度</param>
    /// <param name="duration">持续时间</param>
    private void CreateSingleLaser(Vector3 laserPosition, Vector3 pivotPosition, float startAngle, float endAngle, float duration)
    {
        // 创建激光对象
        GameObject laserObj = Instantiate(LaserAttackPrefab, laserPosition, Quaternion.identity);
        
        // 创建旋转原点对象
        GameObject pivotObj = new GameObject("LaserPivot");
        pivotObj.transform.position = pivotPosition;
        
        // 获取激光组件并配置参数
        LaserAttackProjectile laserComponent = laserObj.GetComponent<LaserAttackProjectile>();
        if (laserComponent != null)
        {
            // 设置旋转锚点（不使用父子关系，让激光组件自己处理旋转）
            laserComponent.SetRotationPivot(pivotObj.transform);
            laserComponent.SetLaserParameters(startAngle, endAngle, duration, laserDamage);
            
            // 订阅激光完成事件，用于清理旋转原点对象
            laserComponent.OnLaserCompleted += (laser) => {
                if (pivotObj != null)
                {
                    Destroy(pivotObj);
                }
            };
            
            Debug.Log($"Created laser: Position={laserPosition}, Pivot={pivotPosition}, Start={startAngle}°, End={endAngle}°, Duration={duration}s");
        }
        else
        {
            Debug.LogError("LaserAttackPrefab does not have LaserAttackProjectile component!");
            Destroy(laserObj);
            Destroy(pivotObj);
        }
    }

    /// <summary>
    /// Attack4 - 弹幕攻击的动画事件（向玩家方向发射五枚导弹）
    /// </summary>
    private void AniEvent_TriggerAttack4()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (MissileAttackPrefab == null || player == null) return;

        // 获取玩家位置
        Vector3 playerPosition = player.transform.position;
        Vector3 bossPosition = transform.position;
        
        // 计算朝向玩家的基础方向
        Vector3 directionToPlayer = (playerPosition - bossPosition).normalized;
        float baseAngle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;
        
        // 导弹角度偏移数组（相对于3号导弹的角度）
        float[] angleOffsets = { -15f, -7.5f, 0f, 7.5f, 15f };
        
        // 发射五枚导弹
        for (int i = 0; i < 5; i++)
        {
            // 计算当前导弹的发射角度
            float missileAngle = baseAngle + angleOffsets[i];
            Vector3 missileDirection = new Vector3(
                Mathf.Cos(missileAngle * Mathf.Deg2Rad),
                Mathf.Sin(missileAngle * Mathf.Deg2Rad),
                0f
            );
            
            // 计算导弹发射位置（稍微偏移以避免重叠）
            Vector3 missileSpawnPosition = bossPosition + missileDirection * 2f;
            
            // 计算导弹的爆炸位置
            // Y和Z坐标与玩家相同，X坐标根据导弹飞行轨迹计算
            Vector3 explosionPosition = CalculateMissileExplosionPosition(missileSpawnPosition, missileDirection, playerPosition);
            
            // 创建导弹
            GameObject missile = Instantiate(MissileAttackPrefab, missileSpawnPosition, Quaternion.identity);
            MissileAttackProjectile missileProjectile = missile.GetComponent<MissileAttackProjectile>();
            
            if (missileProjectile != null)
            {
                // 每枚导弹都有不同的爆炸位置
                missileProjectile.InitializeByBoss(missileSpawnPosition, explosionPosition);
            }
        }
        
        Debug.Log($"弹幕攻击：向玩家方向发射5枚导弹，玩家位置：{playerPosition}");
    }
    
    /// <summary>
    /// 计算导弹的爆炸位置
    /// </summary>
    /// <param name="spawnPosition">导弹发射位置</param>
    /// <param name="direction">导弹飞行方向</param>
    /// <param name="playerPosition">玩家位置</param>
    /// <returns>导弹爆炸位置</returns>
    private Vector3 CalculateMissileExplosionPosition(Vector3 spawnPosition, Vector3 direction, Vector3 playerPosition)
    {
        // 计算导弹飞行到玩家Y坐标时的X坐标
        // 使用线性插值：X = spawnX + direction.x * (targetY - spawnY) / direction.y
        float deltaY = playerPosition.y - spawnPosition.y;
        float deltaX = direction.x * deltaY / direction.y;
        float explosionX = spawnPosition.x + deltaX;
        
        // 返回爆炸位置：X坐标根据轨迹计算，Y和Z坐标与玩家相同
        return new Vector3(explosionX, playerPosition.y, playerPosition.z);
    }

    /// <summary>
    /// 设置区域攻击的大小
    /// </summary>
    private void SetAreaAttackSize(AreaAttackProjectile areaAttack, float sizeMultiplier)
    {
        // 通过反射设置区域攻击的大小
        var attackRadiusField = typeof(AreaAttackProjectile).GetField("attackRadius", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var warningRadiusField = typeof(AreaAttackProjectile).GetField("warningRadius", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (attackRadiusField != null)
        {
            float originalRadius = (float)attackRadiusField.GetValue(areaAttack);
            attackRadiusField.SetValue(areaAttack, originalRadius * sizeMultiplier);
        }

        if (warningRadiusField != null)
        {
            float originalWarningRadius = (float)warningRadiusField.GetValue(areaAttack);
            warningRadiusField.SetValue(areaAttack, originalWarningRadius * sizeMultiplier);
        }
    }

    // 音效播放方法
    private void AniEvent_PlayAppearSound() { PlaySound(appearSound); }
    private void AniEvent_PlayDisappearSound() { PlaySound(disappearSound); }
    private void AniEvent_PlayPreparationSound() { PlaySound(preparationSound); }
    private void AniEvent_PlayAttackSound() { PlaySound(attackSound); }
    private void AniEvent_PlayLaserSound() { PlaySound(laserSound); }
    private void AniEvent_PlayBulletSound() { PlaySound(bulletSound); }
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

    /// <summary>
    /// 检查是否有蛇头被破坏
    /// </summary>
    public bool IsAnyHeadDestroyed()
    {
        int currentDestroyedCount = 0;
        foreach (var head in snakeHeads)
        {
            if (head.partObject.GetComponent<EnemyBodyPart>() != null && head.partObject.GetComponent<EnemyBodyPart>().IsPartDestroyed())
            {
                currentDestroyedCount++;
            }
        }

        if (currentDestroyedCount > destroyedHeadCount)
        {
            destroyedHeadCount = currentDestroyedCount;
            Debug.Log($"TestBoss3: {destroyedHeadCount} snake head(s) destroyed!");
            return true;
        }

        return false;
    }

    /// <summary>
    /// 获取被破坏的蛇头数量
    /// </summary>
    public int GetDestroyedHeadCount()
    {
        return destroyedHeadCount;
    }

    /// <summary>
    /// 检查是否应该进入二阶段
    /// </summary>
    public bool ShouldEnterStage2()
    {
        return destroyedHeadCount > 0 && !isStage2;
    }

    /// <summary>
    /// 更新蛇头破坏状态并检查是否应该进入二阶段
    /// 这个方法会先调用IsAnyHeadDestroyed()来更新计数，然后检查是否应该进入二阶段
    /// </summary>
    public bool CheckAndUpdateHeadDestruction()
    {
        // 先更新破坏状态
        bool hasNewDestruction = IsAnyHeadDestroyed();
        
        // 然后检查是否应该进入二阶段
        bool shouldEnterStage2 = ShouldEnterStage2();
        
        // 返回是否应该进入二阶段（有头部被破坏且此时是一阶段）
        return hasNewDestruction && shouldEnterStage2;
    }

    /// <summary>
    /// 切换到二阶段
    /// </summary>
    public void SwitchToStage2()
    {
        if (!isStage2)
        {
            isStage2 = true;
            Debug.Log("TestBoss3 switched to Stage 2!");
        }
    }

    /// <summary>
    /// 获取当前是否为二阶段
    /// </summary>
    public bool IsStage2()
    {
        return isStage2;
    }

    /// <summary>
    /// 获取剩余蛇头数量
    /// </summary>
    public int GetRemainingHeadCount()
    {
        return snakeHeads.Count - destroyedHeadCount;
    }

    // ================================
    // Attack1 配置访问器
    // ================================
    
    /// <summary>
    /// 获取Attack1配置参数
    /// </summary>
    public (float damagePerSecond, float[] areaSizes, float duration) GetAttack1Settings()
    {
        return (attack1DamagePerSecond, attack1AreaSizes, attack1Duration);
    }

    protected override System.Collections.IEnumerator DeathSequence()
    {
        // 播放死亡动画
        PlayDeathAnimation();

        stateMachine.ChangeState(new TestBoss3DisappearState());

        while (isPlayingAnimation)
        {
            yield return null;
        }

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
}
