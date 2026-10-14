using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class TestBoss1 : BossController
{

    [Header("Animation Clips")]
    [SerializeField] private AnimationClip appearAnimation;
    [SerializeField] private AnimationClip disappearAnimation;
    [SerializeField] private AnimationClip attackPreparationAnimation;
    [SerializeField] private AnimationClip attack1Animation;
    [SerializeField] private AnimationClip attack2Animation;
    [SerializeField] private AnimationClip specialAnimation;
    [SerializeField] private AnimationClip idleAnimation;
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

    [Header("Left-Right Movement")]
    [SerializeField] private bool enableLeftRightMovement = true;
    [SerializeField] private float leftRightMoveRange = 150f;     // 左右移动幅度（屏幕单位）
    [SerializeField] private float leftRightMoveSpeed = 80f;      // 移动速度
    [SerializeField] private float leftRightSwitchInterval = 2f;  // 每次换向间隔（秒）

    private float leftRightTimer = 0f;
    private int leftRightDirection = 1; // 1=向右, -1=向左
    private Vector3 leftRightOrigin;
    private bool leftRightOriginSet = false;

    [Header("Damage Settings")]
    [SerializeField] private float damageMultiplier = 0.7f; // 攻击伤害倍率（1.0=原始，0.7=70%）

    [Header("Attack Prefabs")]
    [SerializeField] private GameObject AreaAttackPrefab;
    [SerializeField] private GameObject cancelableAttackPrefab;

    [Header("Enemy Prefabs")]
    [SerializeField] private List<GameObject> BossSpawnEnemies; // 随boss状态生成的敌人
    
    // Boss生成的敌人管理
    private List<EnemyWithNewAttackSystem> bossSpawnedEnemies = new List<EnemyWithNewAttackSystem>(); // 跟踪Boss生成的敌人
    private EnemySpawner enemySpawner; // EnemySpawner引用

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


    protected override void MakeAIDecision()
    {


    }

    protected override void Update()
    {
        base.Update();
        if (enableLeftRightMovement && leftRightOriginSet && !IsDead)
        {
            UpdateLeftRightMovement();
        }
    }

    private void UpdateLeftRightMovement()
    {
        leftRightTimer += Time.deltaTime;
        if (leftRightTimer >= leftRightSwitchInterval)
        {
            leftRightTimer = 0f;
            leftRightDirection *= -1;
        }

        float targetX = leftRightOrigin.x + leftRightDirection * leftRightMoveRange;
        Vector3 targetPos = new Vector3(targetX, transform.position.y, transform.position.z);
        transform.position = Vector3.MoveTowards(
            transform.position, targetPos, leftRightMoveSpeed * Time.deltaTime);
    }

    protected override void Start()
    {
        base.Start(); // 确保调用基类的Start方法

        // 记录Boss初始位置作为左右移动的中心点
        leftRightOrigin = transform.position;
        leftRightOriginSet = true;

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
        stateMachine.ChangeState(new TestBoss1AppearState());
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


    public void PlaySpecialAnimation()
    {
        if (bossAnimator == null || specialAnimation == null) return;

        StartCoroutine(PlayAnimationWithDelay(specialAnimation, specialDelay));
    }

    public void PlayIdleAnimation()
    {
        if (bossAnimator == null || idleAnimation == null) return;

        // 直接播放动画片段
        bossAnimator.Play(idleAnimation.name);
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
                PlaySpecialAnimation();
                break;
            case 6:
                PlayIdleAnimation();
                break;
            case 7:
                PlayMoveAnimation();
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
        PlayIdleAnimation(); // 回到待机状态
    }

    // 动画事件 触发一次区域攻击 类型Magic
    private void AniEvent_triggerAreaAttack()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            GameObject a = Instantiate(AreaAttackPrefab, player.transform.position + new Vector3(Random.Range(-10f, 10f), 0, 0), Quaternion.identity);
            a.GetComponent<AttackProjectile>().MultiplyDamage(damageMultiplier);
            a.GetComponent<AreaAttackProjectile>().SetEnemyPosition(this.transform.position);

            AniEvent_PlayAreaAttackSound();
            //a.GetComponent<AreaAttackProjectile>().AniEvent_SetTrajectoryStartPosition(transform.position);
            a.GetComponent<AreaAttackProjectile>().AniEvent_SetTrajectoryStartPosition(new Vector3(400, 900, 0));
            a.GetComponent<AreaAttackProjectile>().AniEvent_SetTrajectoryEndPosition(player.transform.position);
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
            attackProjectile.MultiplyDamage(damageMultiplier);
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

        stateMachine.ChangeState(new TestBoss1DisappearState());

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
}
