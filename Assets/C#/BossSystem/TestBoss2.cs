using System.Collections.Generic;
using UnityEngine;

public class TestBoss2 : BossController
{
    [Header("Animation Clips")]
    [SerializeField] private AnimationClip appearAnimation;
    [SerializeField] private AnimationClip disappearAnimation;
    [SerializeField] private AnimationClip attackPreparationAnimation;
    [SerializeField] private AnimationClip attack1Animation;
    [SerializeField] private AnimationClip attack2Animation;
    [SerializeField] private AnimationClip attack3Animation;
    [SerializeField] private AnimationClip idle1Animation;
    [SerializeField] private AnimationClip idle2Animation;
    [SerializeField] private AnimationClip moveAnimation;
    [SerializeField] private AnimationClip deadAnimation;

    [Header("Animation Timing")]
    [SerializeField] private float appearDelay = 0f;
    [SerializeField] private float disappearDelay = 0f;
    [SerializeField] private float attackPreparationDelay = 0f;
    [SerializeField] private float attackDelay = 0f;
    [SerializeField] private float specialDelay = 0f;
    [SerializeField] private float deadDelay = 0f;

    public bool IsAnimationPlaying = false;

    [Header("Attack Prefabs")]
    [SerializeField] private GameObject AreaAttackPrefab;
    [SerializeField] private GameObject AreaAttack1Prefab; // attack1用的 大一点
    [SerializeField] private GameObject MissileAttackPrefab;
    [SerializeField] private GameObject cancelableAttackPrefab;

    [Header("Enemy Prefabs")]
    [SerializeField] private List<GameObject> BossSpawnEnemies; // 随boss状态生成的敌人
    
    // Boss生成的敌人管理
    private List<EnemyWithNewAttackSystem> bossSpawnedEnemies = new List<EnemyWithNewAttackSystem>(); // 跟踪Boss生成的敌人
    private EnemySpawner enemySpawner; // EnemySpawner引用
    
    [Header("TestBoss2 Body Parts System")]
    //[SerializeField] private List<EnemyBodyPart> bossBodyParts = new List<EnemyBodyPart>(); // Boss的身体部位列表
    private bool hasAnyBodyPartDestroyed = false; // 记录是否有部位被破坏

    [Header("Audio System")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip appearSound;
    [SerializeField] private AudioClip disappearSound;
    [SerializeField] private AudioClip attackPreparationSound;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip specialSound;
    [SerializeField] private AudioClip moveSound;
    [SerializeField] private AudioClip deadSound;
    [SerializeField] private AudioClip areaAttackSound;
    [SerializeField] private AudioClip cancelableAttackSound;
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 1f;

    private bool isState2 = false; // 是否为二阶段
    protected override void MakeAIDecision()
    {


    }

    protected override void Start()
    {
        base.Start(); // 确保调用基类的Start方法

        CalculateScreenPathPoints(); // 重新获取一遍路径点

        // 如果没有设置AudioSource，尝试获取
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // 获取EnemySpawner引用
        enemySpawner = FindObjectOfType<EnemySpawner>();
        if (enemySpawner == null)
        {
            Debug.LogWarning("EnemySpawner not found! Boss-spawned enemies will not be properly managed.");
        }

        // 初始化状态机并设置初始状态
        InitializeStateMachine();
        stateMachine.ChangeState(new TestBoss2AppearState());
        
        // 初始化身体部位系统
        InitializeBodyParts();
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

        Debug.Log("PlayDisappearAnimation");
    }

    public void PlayAttackPreparationAnimation()
    {
        if (bossAnimator == null || attackPreparationAnimation == null) return;

        StartCoroutine(PlayAnimationWithDelay(attackPreparationAnimation, attackPreparationDelay));
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

    public void PlayIdle1Animation()
    {
        if (bossAnimator == null || idle1Animation == null) return;

        // 直接播放动画片段
        bossAnimator.Play(idle1Animation.name);
        isPlayingAnimation = false;
    }

    public void PlayIdle2Animation()
    {
        if (bossAnimator == null || idle2Animation == null) return;

        // 直接播放动画片段
        bossAnimator.Play(idle2Animation.name);
        isPlayingAnimation = false;
    }

    public void PlayMoveAnimation()
    {
        if (bossAnimator == null || moveAnimation == null) return;

        // 直接播放动画片段
        bossAnimator.Play(moveAnimation.name);
        isPlayingAnimation = false;
    }

    public override void PlayAnimation(int index)
    {
        switch (index)
        {
            case 0:
                PlayAppearAnimation();
                break;
            case 1:
                PlayDisappearAnimation();
                break;
            case 2:
                PlayAttackPreparationAnimation();
                break;
            case 3:
                PlayAttack1Animation();
                break;
            case 4:
                PlayAttack2Animation();
                break;
            case 5:
                PlayAttack3Animation();
                break;
            case 6:
                PlayIdle1Animation();
                break;
            case 7:
                PlayMoveAnimation();
                break;
            case 8:
                PlayIdle2Animation();
                break;
            default:
                break;


        }
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

        if (isState2)
        {
            PlayIdle2Animation(); // 回到待机状态
        }
        else 
        {
            PlayIdle1Animation(); // 回到待机状态
        }
    }

    // 动画事件 触发一次区域攻击 类型Magic
    private void AniEvent_triggerAreaAttack()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector3 targetPos = player.transform.position + new Vector3(Random.Range(-10f, 10f), 0, 0);
            GameObject a = Instantiate(AreaAttackPrefab, targetPos, Quaternion.identity);
            AreaAttackProjectile proj = a.GetComponent<AreaAttackProjectile>();
            proj.SetEnemyPosition(this.transform.position);
            proj.AniEvent_SetTrajectoryStartPosition(new Vector3(400, 900, 0));
            proj.AniEvent_SetTrajectoryEndPosition(targetPos);
            proj.InitializeByBoss(transform.position, targetPos); // 启动攻击执行
            AniEvent_PlayAreaAttackSound();
        }
    }

    // 动画事件 触发一次可消除攻击 以boss为中心的周围随机位置处
    private void AniEvent_triggerCancelableAttack()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            GameObject attackPrefab = Instantiate(cancelableAttackPrefab, this.transform.position, Quaternion.identity);
            AttackProjectile attackProjectile = attackPrefab.GetComponent<AttackProjectile>();
            attackProjectile.InitializeByBoss(this.transform.position, this.transform.position);
        }
    }

    /// <summary>
    /// 动画事件 - 播放出现音效
    /// </summary>
    private void AniEvent_PlayAppearSound()
    {
        PlaySound(appearSound);
    }

    /// <summary>
    /// 动画事件 - 播放消失音效
    /// </summary>
    private void AniEvent_PlayDisappearSound()
    {
        PlaySound(disappearSound);
    }

    /// <summary>
    /// 动画事件 - 播放攻击准备音效
    /// </summary>
    private void AniEvent_PlayAttackPreparationSound()
    {
        PlaySound(attackPreparationSound);
    }

    /// <summary>
    /// 动画事件 - 播放攻击音效
    /// </summary>
    private void AniEvent_PlayAttackSound()
    {
        PlaySound(attackSound);
    }

    /// <summary>
    /// 动画事件 - 播放特殊技能音效
    /// </summary>
    private void AniEvent_PlaySpecialSound()
    {
        PlaySound(specialSound);
    }

    /// <summary>
    /// 动画事件 - 播放移动音效
    /// </summary>
    private void AniEvent_PlayMoveSound()
    {
        PlaySound(moveSound);
    }

    /// <summary>
    /// 动画事件 - 播放死亡音效
    /// </summary>
    private void AniEvent_PlayDeadSound()
    {
        PlaySound(deadSound);
    }

    /// <summary>
    /// 动画事件 - 播放区域攻击音效
    /// </summary>
    private void AniEvent_PlayAreaAttackSound()
    {
        PlaySound(areaAttackSound);
    }

    /// <summary>
    /// 动画事件 - 播放可取消攻击音效
    /// </summary>
    private void AniEvent_PlayCancelableAttackSound()
    {
        PlaySound(cancelableAttackSound);
    }

    /// <summary>
    /// 动画事件 - 生成敌人组合
    /// </summary>
    private void AniEvent_SpawnEnemyList() 
    {
        // 检查是否有敌人预制体
        if (BossSpawnEnemies == null || BossSpawnEnemies.Count == 0)
        {
            Debug.LogWarning("BossSpawnEnemies list is empty or null!");
            return;
        }
        
        Debug.Log($"Boss is spawning enemy group with {BossSpawnEnemies.Count} enemy types");
        
        // 生成所有类型的敌人
        for (int i = 0; i < BossSpawnEnemies.Count; i++)
        {
            SpawnEnemy(i);
        }
        
        Debug.Log($"Boss enemy group spawn completed. Total enemies spawned: {bossSpawnedEnemies.Count}");
    }

    /// <summary>
    /// 生成BossSpawnEnemies中指定序号的敌人的方法
    /// </summary>
    /// <param name="index">敌人在BossSpawnEnemies列表中的索引</param>
    private void SpawnEnemy(int index) 
    {
        // 检查索引是否有效
        if (index < 0 || index >= BossSpawnEnemies.Count)
        {
            Debug.LogError($"Invalid enemy index: {index}. Valid range: 0-{BossSpawnEnemies.Count - 1}");
            return;
        }
        
        GameObject enemyPrefab = BossSpawnEnemies[index];
        if (enemyPrefab == null)
        {
            Debug.LogError($"Enemy prefab at index {index} is null!");
            return;
        }
        
        // 在Boss周围生成敌人
        Vector3 spawnPosition = transform.position + new Vector3(
            Random.Range(-5f, 5f),
            Random.Range(-3f, 3f),
            0f
        );
        
        // 生成敌人
        GameObject enemyObject = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        EnemyWithNewAttackSystem enemy = enemyObject.GetComponent<EnemyWithNewAttackSystem>();
        
        if (enemy != null)
        {
            // 添加到Boss生成的敌人列表
            bossSpawnedEnemies.Add(enemy);
            
            // 如果有EnemySpawner，将敌人注册到系统中
            if (enemySpawner != null)
            {
                enemySpawner.RegisterBossSpawnedEnemy(enemy);
            }
            
            Debug.Log($"Boss spawned enemy {enemy.name} at {spawnPosition}. Total boss-spawned enemies: {bossSpawnedEnemies.Count}");
        }
        else
        {
            Debug.LogError($"Enemy prefab at index {index} does not have EnemyWithNewAttackSystem component!");
            Destroy(enemyObject); // 清理无效的敌人对象
        }
    }

    /// <summary>
    /// 播放指定音效的核心方法
    /// </summary>
    /// <param name="clip">要播放的音效片段</param>
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
    /// <param name="volume">音量值 (0-1)</param>
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
        // 清理Boss生成的敌人
        ClearBossSpawnedEnemies();
        
        // 播放死亡动画
        PlayDeathAnimation();

        stateMachine.ChangeState(new TestBoss2DisappearState());

        while (isPlayingAnimation)
        {
            yield return null;
        }

        // 死亡效果
        yield return new WaitForSeconds(10f);
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 清理Boss生成的所有敌人
    /// </summary>
    private void ClearBossSpawnedEnemies()
    {
        if (bossSpawnedEnemies.Count == 0)
        {
            Debug.Log("No boss-spawned enemies to clear");
            return;
        }
        
        Debug.Log($"Clearing {bossSpawnedEnemies.Count} boss-spawned enemies");
        
        // 遍历所有Boss生成的敌人
        foreach (var enemy in bossSpawnedEnemies.ToArray())
        {
            if (enemy != null && !enemy.IsDead)
            {
                // 记录敌人击杀（在敌人死亡前记录，确保计入结算）
                if (enemySpawner != null)
                {
                    enemySpawner.RecordBossEnemyKill(enemy);
                }
                
                // 强制杀死敌人
                enemy.TakeDamage(float.MaxValue);
                Debug.Log($"Force killed boss-spawned enemy: {enemy.name}");
            }
        }
        
        // 清空列表
        bossSpawnedEnemies.Clear();
        Debug.Log("All boss-spawned enemies have been cleared and counted for settlement");
    }
    
    /// <summary>
    /// 动画事件 - 生成第一个敌人
    /// </summary>
    private void AniEvent_SpawnEnemy0()
    {
        SpawnEnemy(0);
    }
    
    /// <summary>
    /// 动画事件 - 生成第二个敌人
    /// </summary>
    private void AniEvent_SpawnEnemy1()
    {
        SpawnEnemy(1);
    }
    
    /// <summary>
    /// 动画事件 - 生成第三个敌人
    /// </summary>
    private void AniEvent_SpawnEnemy2()
    {
        SpawnEnemy(2);
    }
    
    /// <summary>
    /// 获取Boss当前生成的敌人数量
    /// </summary>
    /// <returns>当前存活的Boss生成敌人数量</returns>
    public int GetBossSpawnedEnemyCount()
    {
        // 清理已经死亡的敌人引用
        bossSpawnedEnemies.RemoveAll(enemy => enemy == null || enemy.IsDead);
        return bossSpawnedEnemies.Count;
    }
    
    /// <summary>
    /// 获取Boss生成的所有敌人列表
    /// </summary>
    /// <returns>Boss生成的敌人列表的副本</returns>
    public List<EnemyWithNewAttackSystem> GetBossSpawnedEnemies()
    {
        // 清理已经死亡的敌人引用
        bossSpawnedEnemies.RemoveAll(enemy => enemy == null || enemy.IsDead);
        return new List<EnemyWithNewAttackSystem>(bossSpawnedEnemies);
    }
    
    /// <summary>
    /// 手动清理已死亡的敌人引用
    /// </summary>
    public void CleanupDeadEnemyReferences()
    {
        int originalCount = bossSpawnedEnemies.Count;
        bossSpawnedEnemies.RemoveAll(enemy => enemy == null || enemy.IsDead);
        int removedCount = originalCount - bossSpawnedEnemies.Count;
        
        if (removedCount > 0)
        {
            Debug.Log($"Cleaned up {removedCount} dead enemy references. Active boss-spawned enemies: {bossSpawnedEnemies.Count}");
        }
    }
    
    /// <summary>
    /// 初始化身体部位系统
    /// </summary>
    /*
    private void InitializeBodyParts()
    {
        // 如果Inspector中没有设置身体部位，自动查找子物体中的EnemyBodyPart组件
        if (bossBodyParts.Count == 0)
        {
            EnemyBodyPart[] bodyPartsInChildren = GetComponentsInChildren<EnemyBodyPart>();
            foreach (var bodyPart in bodyPartsInChildren)
            {
                bodyPart.isBoss = true; // 标记为Boss的身体部位
                bossBodyParts.Add(bodyPart);
            }
        }
        
        Debug.Log($"TestBoss2 initialized with {bossBodyParts.Count} body parts");
    }*/
    
    /// <summary>
    /// 检查是否有任何身体部位被破坏
    /// </summary>
    /// <returns>如果有任何部位被破坏则返回true</returns>
    public bool IsAnyBodyPartDestroyed()
    {
        // 缓存结果，避免重复计算
        if (hasAnyBodyPartDestroyed) return true;
        
        foreach (var bodyPart in bodyParts)
        {
            if (bodyPart.partObject.GetComponent<EnemyBodyPart>() != null && bodyPart.partObject.GetComponent<EnemyBodyPart>().IsPartDestroyed())
            {
                hasAnyBodyPartDestroyed = true;
                Debug.Log($"TestBoss2: Body part {bodyPart.partObject.GetComponent<EnemyBodyPart>().GetBodyPartData()?.partName} has been destroyed!");
                return true;
            }
        }
        
        return false;
    }
            
    /// <summary>
    /// 设置路径移动参数（通过反射或其他方式修改BossController的字段）
    /// </summary>
    /// <param name="screenEdgeMargin">距离屏幕边缘的间隔</param>
    /// <param name="moveSpeed">移动速度</param>
    /// <param name="moveDuration">移动持续时间</param>
    private void SetPathParameters(float screenEdgeMargin, float moveSpeed, float moveDuration)
    {
        // 这里需要访问BossController中的路径移动参数
        // 假设BossController有公共字段或属性可以修改这些参数
        
        // 使用反射获取和设置私有字段（如果需要的话）
        var bossControllerType = typeof(BossController);
        
        // 尝试设置屏幕边缘间隔
        var marginField = bossControllerType.GetField("screenEdgeMargin", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (marginField != null)
        {
            marginField.SetValue(this, screenEdgeMargin);
        }
        
        // 尝试设置移动速度
        var speedField = bossControllerType.GetField("pathMoveSpeed", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (speedField != null)
        {
            speedField.SetValue(this, moveSpeed);
        }
        
        // 尝试设置移动持续时间
        var durationField = bossControllerType.GetField("pathMoveDuration", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (durationField != null)
        {
            durationField.SetValue(this, moveDuration);
        }
        
        // 重新计算路径点
        RecalculateScreenPathPoints();
        
        Debug.Log($"TestBoss2: Path parameters set - Margin: {screenEdgeMargin}, Speed: {moveSpeed}, Duration: {moveDuration}");
    }
    
    /// <summary>
    /// 动画事件 - Attack1的攻击触发
    /// </summary>
    private void AniEvent_TriggerAttack1()
    {
        Debug.Log("TestBoss2: Attack1 triggered!");

        // Attack1: 触发区域攻击
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector3 targetPos = player.transform.position + new Vector3(Random.Range(-10f, 10f), 0, 0);
            GameObject a = Instantiate(AreaAttack1Prefab, targetPos, Quaternion.identity);
            AreaAttackProjectile proj = a.GetComponent<AreaAttackProjectile>();
            proj.SetEnemyPosition(this.transform.position);
            proj.AniEvent_SetTrajectoryStartPosition(transform.position);
            proj.AniEvent_SetTrajectoryEndPosition(targetPos);
            proj.InitializeByBoss(transform.position, targetPos); // 启动攻击执行
            AniEvent_PlayAreaAttackSound();
        }
    }
    
    /// <summary>
    /// 动画事件 - Attack2的攻击触发
    /// </summary>
    private void AniEvent_TriggerAttack2()
    {
        AniEvent_TriggerMissileAttack(true);
        Debug.Log("TestBoss2: Attack2 triggered!");
        
    }

    /// <summary>
    /// 动画事件 - 触发导弹攻击
    /// <param name="useRandomPosition">是否使用随机位置</param>
    /// </summary>
    public void AniEvent_TriggerMissileAttack(bool useRandomPosition = false) 
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (MissileAttackPrefab != null && player != null)  
        {
            int useRandom = useRandomPosition ? 1 : 0;
            GameObject a = Instantiate(MissileAttackPrefab, transform.position + useRandom * new Vector3(Random.Range(-50, 50), Random.Range(-50, 50), 0), Quaternion.identity);
            MissileAttackProjectile missileAttackProjectile = a.GetComponent<MissileAttackProjectile>();
            if (missileAttackProjectile != null) 
            {
                missileAttackProjectile.InitializeByBoss(transform.position, player.transform.position);
            }
        }
    }

    public bool GetisState2() 
    {
        return isState2;
    }

    public void SwitchToState2() 
    {
        if (!isState2)
            isState2 = true;
        else 
        {
            Debug.Log("TestBoss2 has already been State2");
        }
    }

    protected override void CalculateScreenPathPoints() 
    {
        screenPathPoints.Clear();

        if (target == null) return;

        // 获取屏幕边界
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        // 计算屏幕边界（世界坐标）
        float screenHeight = 2f * mainCamera.orthographicSize;
        float screenWidth = screenHeight * mainCamera.aspect;

        // 计算可用移动范围
        float availableWidth = screenWidth - 2f * screenEdgeMargin;
        float availableHeight = screenHeight - 2f * screenEdgeMargin;

        // 计算路径点间距
        float stepSize = availableWidth / (pathPointCount - 1);

        // 生成路径点
        if (!isState2)
        {
            float x, y, z;
            x = target.position.x;
            y = target.position.y + screenHeight / 2 - 2 * screenEdgeMargin;
            z = 0;
            Vector3 pathPoint = new Vector3(x, y, z);
            screenPathPoints.Add(pathPoint);

            x = target.position.x;
            y = target.position.y - screenHeight / 2 + 2 * screenEdgeMargin;
            pathPoint = new Vector3(x, y, z);
            screenPathPoints.Add(pathPoint);

            x = target.position.x - screenWidth / 2 + 2 * screenEdgeMargin;
            y = target.position.y;
            pathPoint = new Vector3(x, y, z);
            screenPathPoints.Add(pathPoint);

            x = target.position.x + screenWidth - 2 * screenEdgeMargin;
            y = target.position.y;
            pathPoint = new Vector3(x, y, z);
            screenPathPoints.Add(pathPoint);
        }
        else
        {
            float x, y, z;
            x = target.position.x - screenWidth / 2 + screenWidth / 2f;
            y = target.position.y - screenHeight / 2 + screenHeight / 2f;
            z = 0;
            Vector3 pathPoint = new Vector3(x, y, z);
            screenPathPoints.Add(pathPoint);

            x = target.position.x - screenWidth / 2 + (screenWidth / 2f + 2 * screenEdgeMargin) / 2;
            y = target.position.y - screenHeight / 2 + screenHeight - screenEdgeMargin;
            pathPoint = new Vector3(x, y, z);
            screenPathPoints.Add(pathPoint);

            x = target.position.x - screenWidth / 2 + 2 * screenEdgeMargin;
            y = target.position.y - screenHeight / 2 + screenHeight / 2f;
            pathPoint = new Vector3(x, y, z);
            screenPathPoints.Add(pathPoint);

            x = target.position.x - screenWidth / 2 + (screenWidth / 2f + 2 * screenEdgeMargin) / 2;
            y = target.position.y - screenHeight / 2 + screenEdgeMargin;
            pathPoint = new Vector3(x, y, z);
            screenPathPoints.Add(pathPoint);

            x = target.position.x - screenWidth / 2 + screenWidth / 2f;
            y = target.position.y - screenHeight / 2 + screenHeight / 2f;
            pathPoint = new Vector3(x, y, z);
            screenPathPoints.Add(pathPoint);

            x = target.position.x - screenWidth / 2 + (screenWidth / 2f + screenWidth - 2 * screenEdgeMargin) / 2;
            y = target.position.y - screenHeight / 2 + screenHeight - screenEdgeMargin;
            pathPoint = new Vector3(x, y, z);
            screenPathPoints.Add(pathPoint);

            x = target.position.x - screenWidth / 2 + screenWidth - 2 * screenEdgeMargin;
            y = target.position.y - screenHeight / 2 + screenHeight / 2f;
            pathPoint = new Vector3(x, y, z);
            screenPathPoints.Add(pathPoint);

            x = target.position.x - screenWidth / 2 + (screenWidth / 2f + screenWidth - 2 * screenEdgeMargin) / 2;
            y = target.position.y - screenHeight / 2 + screenEdgeMargin;
            pathPoint = new Vector3(x, y, z);
            screenPathPoints.Add(pathPoint);

        }

        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Calculated {screenPathPoints.Count} screen path points");
        }

    }
}