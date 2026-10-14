using System.Collections.Generic;
using UnityEngine;

public class TestBoss5 : BossController
{
    [Header("Animation Clips")]
    [SerializeField] private AnimationClip appearAnimation;
    [SerializeField] private AnimationClip disappearAnimation;
    [SerializeField] private AnimationClip attack1Animation;
    [SerializeField] private AnimationClip attack2Animation;
    [SerializeField] private AnimationClip attack3Animation;
    [SerializeField] private AnimationClip attack4Animation;
    [SerializeField] private AnimationClip attack5Animation;
    [SerializeField] private AnimationClip idle1Animation;
    [SerializeField] private AnimationClip idle2Animation;
    [SerializeField] private AnimationClip idle3Animation;
    [SerializeField] private AnimationClip moveAnimation;
    [SerializeField] private AnimationClip deadAnimation;

    [Header("Animation Timing")]
    [SerializeField] private float appearDelay = 0f;
    [SerializeField] private float disappearDelay = 0f;
    [SerializeField] private float attackDelay = 0f;
    [SerializeField] private float deadDelay = 0f;

    public bool IsAnimationPlaying = false;

    [Header("Attack Prefabs")]
    [SerializeField] private GameObject AreaAttackPrefab; // 区域攻击预制体
    [SerializeField] private GameObject MissileAttackPrefab; // 导弹攻击预制体

    [Header("Attack Settings")]
    [SerializeField] private float attack1Damage = 5f; // Attack1伤害（按时间扣血）
    [SerializeField] private float attack2DamageToBoss = 10f; // Attack2成功时对Boss的伤害
    [SerializeField] private float attack2DamageToPlayer = 15f; // Attack2失败时对玩家的伤害

    [Header("Enemy Prefabs")]
    [SerializeField] private List<GameObject> BossSpawnEnemies; // 随boss状态生成的敌人

    // Boss生成的敌人管理
    private List<EnemyWithNewAttackSystem> bossSpawnedEnemies = new List<EnemyWithNewAttackSystem>();
    private EnemySpawner enemySpawner;

    [Header("Audio System")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip appearSound;
    [SerializeField] private AudioClip disappearSound;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip moveSound;
    [SerializeField] private AudioClip deadSound;
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 1f;

    // Boss状态跟踪
    private bool isStage2 = false; // 是否为二阶段
    private bool isStage3 = false; // 是否为三阶段

    protected override void MakeAIDecision()
    {
        // TestBoss5的AI决策逻辑由状态机处理
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

        // 获取EnemySpawner引用
        enemySpawner = FindObjectOfType<EnemySpawner>();
        if (enemySpawner == null)
        {
            Debug.LogWarning("EnemySpawner not found! Boss-spawned enemies will not be properly managed.");
        }

        // 初始化状态机并设置初始状态
        InitializeStateMachine();
        stateMachine.ChangeState(new TestBoss5AppearState());
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

    public void PlayAttack5Animation()
    {
        if (bossAnimator == null || attack5Animation == null) return;
        StartCoroutine(PlayAnimationWithDelay(attack5Animation, attackDelay));
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

    public void PlayIdle3Animation()
    {
        if (bossAnimator == null || idle3Animation == null) return;
        bossAnimator.Play(idle3Animation.name);
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
            case 2: PlayAttack1Animation(); break;
            case 3: PlayAttack2Animation(); break;
            case 4: PlayAttack3Animation(); break;
            case 5: PlayAttack4Animation(); break;
            case 6: PlayAttack5Animation(); break;
            case 7: PlayIdle1Animation(); break;
            case 8: PlayIdle2Animation(); break;
            case 9: PlayIdle3Animation(); break;
            case 10: PlayMoveAnimation(); break;
            default: break;
        }
        Debug.Log($"TestBoss5: Playing animation {index}");
    }

    // 带延迟的动画播放协程
    private System.Collections.IEnumerator PlayAnimationWithDelay(AnimationClip clip, float delay)
    {
        if (bossAnimator == null || clip == null) yield break;

        isPlayingAnimation = true;

        // 立即切换动画，避免之前的循环动画继续播放
        bossAnimator.Play(clip.name);

        // 等待延迟时间
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        // 确保动画正在播放（如果延迟期间被覆盖，重新播放）
        if (!bossAnimator.GetCurrentAnimatorStateInfo(0).IsName(clip.name))
        {
            bossAnimator.Play(clip.name);
        }

        // 等待动画播放完成
        yield return new WaitForSeconds(clip.length);

        isPlayingAnimation = false;
    }

    /// <summary>
    /// Attack3 - 创建区域攻击的动画事件
    /// </summary>
    private void AniEvent_TriggerAttack3_AreaAttack()
    {
        Debug.Log("TestBoss5: Attack3 Area Attack animation event triggered");
        PlaySound(attackSound);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && AreaAttackPrefab != null)
        {
            GameObject a = Instantiate(AreaAttackPrefab, player.transform.position + new Vector3(Random.Range(-10f, 10f), 0, 0), Quaternion.identity);
            a.GetComponent<AreaAttackProjectile>().SetEnemyPosition(this.transform.position);
            a.GetComponent<AreaAttackProjectile>().AniEvent_SetTrajectoryStartPosition(transform.position);
            a.GetComponent<AreaAttackProjectile>().AniEvent_SetTrajectoryEndPosition(player.transform.position);
        }
    }

    /// <summary>
    /// Attack3 - 召唤小兵的动画事件
    /// </summary>
    private void AniEvent_TriggerAttack3_SpawnEnemies()
    {
        Debug.Log("TestBoss5: Attack3 Spawn Enemies animation event triggered");
        
        // 检查是否有敌人预制体
        if (BossSpawnEnemies == null || BossSpawnEnemies.Count == 0)
        {
            Debug.LogWarning("TestBoss5: BossSpawnEnemies list is empty or null!");
            return;
        }

        Debug.Log($"TestBoss5: Spawning enemy group with {BossSpawnEnemies.Count} enemy types");

        // 生成所有类型的敌人
        for (int i = 0; i < BossSpawnEnemies.Count; i++)
        {
            SpawnEnemy(i);
        }

        Debug.Log($"TestBoss5: Enemy group spawn completed. Total enemies spawned: {bossSpawnedEnemies.Count}");
    }

    /// <summary>
    /// Attack4 - 导弹弹幕攻击的动画事件
    /// </summary>
    private void AniEvent_TriggerAttack4()
    {
        Debug.Log("TestBoss5: Attack4 Missile Barrage animation event triggered");
        PlaySound(attackSound);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (MissileAttackPrefab == null || player == null) return;

        Vector3 playerPosition = player.transform.position;
        Vector3 bossPosition = transform.position;

        // 计算朝向玩家的基础方向
        Vector3 directionToPlayer = (playerPosition - bossPosition).normalized;
        float baseAngle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;

        // 导弹角度偏移数组（相对于基础角度的偏移）
        float[] angleOffsets = { -20f, -10f, 0f, 10f, 20f };

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

            // 计算导弹发射位置
            Vector3 missileSpawnPosition = bossPosition + missileDirection * 2f;

            // 计算导弹的爆炸位置
            Vector3 explosionPosition = CalculateMissileExplosionPosition(missileSpawnPosition, missileDirection, playerPosition);

            // 创建导弹
            GameObject missile = Instantiate(MissileAttackPrefab, missileSpawnPosition, Quaternion.identity);
            MissileAttackProjectile missileProjectile = missile.GetComponent<MissileAttackProjectile>();

            if (missileProjectile != null)
            {
                missileProjectile.InitializeByBoss(missileSpawnPosition, explosionPosition);
            }
        }

        Debug.Log($"TestBoss5: Missile barrage attack launched - 5 missiles towards player");
    }

    /// <summary>
    /// 计算导弹的爆炸位置
    /// </summary>
    private Vector3 CalculateMissileExplosionPosition(Vector3 spawnPosition, Vector3 direction, Vector3 playerPosition)
    {
        // 计算导弹飞行到玩家Y坐标时的X坐标
        float deltaY = playerPosition.y - spawnPosition.y;
        float deltaX = direction.x * deltaY / direction.y;
        float explosionX = spawnPosition.x + deltaX;

        // 返回爆炸位置：X坐标根据轨迹计算，Y和Z坐标与玩家相同
        return new Vector3(explosionX, playerPosition.y, playerPosition.z);
    }

    /// <summary>
    /// Attack5 - 迟缓准心移动的动画事件
    /// </summary>
    private void AniEvent_TriggerAttack5()
    {
        Debug.Log("TestBoss5: Attack5 Crosshair Lag animation event triggered");
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
            Debug.LogWarning("TestBoss5: CrosshairController not found for Attack5 lag effect");
        }
    }

    /// <summary>
    /// 应用准心迟缓效果的协程
    /// </summary>
    private System.Collections.IEnumerator ApplyCrosshairLagEffect(CrosshairController crosshairController)
    {
        float lagDuration = 15f; // 迟缓效果持续时间
        float lagSpeed = 0.3f; // 迟缓移动速度（越小越慢）

        Debug.Log($"TestBoss5: Applying crosshair lag effect for {lagDuration} seconds with speed {lagSpeed}");

        // 启用准心迟缓模式（使用公共方法）
        if (crosshairController != null)
        {
            crosshairController.SetLagMode(true, lagSpeed);
        }

        // 等待持续时间
        yield return new WaitForSeconds(lagDuration);

        // 恢复正常准心移动
        if (crosshairController != null)
        {
            crosshairController.SetLagMode(false, 1f);
        }

        Debug.Log("TestBoss5: Crosshair lag effect ended, movement restored to normal");
    }

    /// <summary>
    /// 生成BossSpawnEnemies中指定序号的敌人
    /// </summary>
    private void SpawnEnemy(int index)
    {
        // 检查索引是否有效
        if (index < 0 || index >= BossSpawnEnemies.Count)
        {
            Debug.LogError($"TestBoss5: Invalid enemy index: {index}. Valid range: 0-{BossSpawnEnemies.Count - 1}");
            return;
        }

        GameObject enemyPrefab = BossSpawnEnemies[index];
        if (enemyPrefab == null)
        {
            Debug.LogError($"TestBoss5: Enemy prefab at index {index} is null!");
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

            Debug.Log($"TestBoss5: Spawned enemy {enemy.name} at {spawnPosition}. Total boss-spawned enemies: {bossSpawnedEnemies.Count}");
        }
        else
        {
            Debug.LogError($"TestBoss5: Enemy prefab at index {index} does not have EnemyWithNewAttackSystem component!");
            Destroy(enemyObject); // 清理无效的敌人对象
        }
    }

    /// <summary>
    /// 获取Attack1伤害值
    /// </summary>
    public float GetAttack1Damage()
    {
        return attack1Damage;
    }

    /// <summary>
    /// 获取Attack2对Boss的伤害值
    /// </summary>
    public float GetAttack2DamageToBoss()
    {
        return attack2DamageToBoss;
    }

    /// <summary>
    /// 获取Attack2对玩家的伤害值
    /// </summary>
    public float GetAttack2DamageToPlayer()
    {
        return attack2DamageToPlayer;
    }

    /// <summary>
    /// 切换到二阶段
    /// </summary>
    public void SwitchToStage2()
    {
        if (!isStage2 && !isStage3)
        {
            isStage2 = true;
            Debug.Log("TestBoss5 switched to Stage 2!");
        }
    }

    /// <summary>
    /// 切换到三阶段
    /// </summary>
    public void SwitchToStage3()
    {
        if (!isStage3)
        {
            isStage2 = false; // 不再使用二阶段标志
            isStage3 = true;
            Debug.Log("TestBoss5 switched to Stage 3!");
        }
    }

    /// <summary>
    /// 获取当前是否为二阶段
    /// </summary>
    public bool IsStage2()
    {
        return isStage2 && !isStage3;
    }

    /// <summary>
    /// 获取当前是否为三阶段
    /// </summary>
    public bool IsStage3()
    {
        return isStage3;
    }

    // 音效播放方法
    private void AniEvent_PlayAppearSound() { PlaySound(appearSound); }
    private void AniEvent_PlayDisappearSound() { PlaySound(disappearSound); }
    private void AniEvent_PlayAttackSound() { PlaySound(attackSound); }
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
        // 清理Boss生成的敌人
        ClearBossSpawnedEnemies();

        // 播放死亡动画
        PlayDeathAnimation();

        stateMachine.ChangeState(new TestBoss5DisappearState());

        while (isPlayingAnimation)
        {
            yield return null;
        }

        // 恢复正常准心移动
        CrosshairController crosshairController = FindObjectOfType<CrosshairController>();
        if (crosshairController != null)
        {
            crosshairController.SetLagMode(false, 1f);
        }

        // 死亡效果
        yield return new WaitForSeconds(3f);
        Destroy(gameObject);
    }

    /// <summary>
    /// 清理Boss生成的所有敌人
    /// </summary>
    private void ClearBossSpawnedEnemies()
    {
        if (bossSpawnedEnemies.Count == 0)
        {
            Debug.Log("TestBoss5: No boss-spawned enemies to clear");
            return;
        }

        Debug.Log($"TestBoss5: Clearing {bossSpawnedEnemies.Count} boss-spawned enemies");

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
                Debug.Log($"TestBoss5: Force killed boss-spawned enemy: {enemy.name}");
            }
        }

        // 清空列表
        bossSpawnedEnemies.Clear();
        Debug.Log("TestBoss5: All boss-spawned enemies have been cleared and counted for settlement");
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


