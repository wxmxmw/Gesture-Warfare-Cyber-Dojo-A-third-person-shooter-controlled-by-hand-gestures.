using UnityEngine;
using System.Collections.Generic;

public enum ArmorType
{
    WeakPoint,      // 弱点 - 150%伤害
    Light,          // 轻甲 - 100%伤害
    Medium,         // 中甲 - 80%伤害
    Heavy,          // 重甲 - 50%伤害
    Unattackable    // 不可攻击部位 - 0%伤害
}

[System.Serializable]
public class BodyPart
{
    public string partName;           // 部位名称
    public GameObject partObject;     // 部位GameObject
    public ArmorType armorType;       // 部位护甲类型
    public float damageMultiplier;    // 额外伤害倍率（用于暴击等）
    public Collider2D partCollider;   // 部位碰撞体
    public bool enableDestruction;    // 是否启用部位破坏
    public float partHealth;          // 部位独立血量
    public float maxPartHealth;       // 部位最大血量
    public int occlusionOrder;        // 遮挡顺序（从0开始，数值越大越靠前，默认0）
    
    public BodyPart(string name, GameObject obj, ArmorType armor, float multiplier = 1f, bool destruction = false, float health = 100f, int occlusion = 0)
    {
        partName = name;
        armorType = armor;
        damageMultiplier = multiplier;
        enableDestruction = destruction;
        partHealth = health;
        maxPartHealth = health;
        occlusionOrder = occlusion;
        
        if (obj != null)
            partCollider = obj.GetComponent<Collider2D>();
    }
}

[System.Serializable]
public class AnimationSequence
{
    public string sequenceName = "Default Sequence";
    public AnimationClip[] animationClips;
    public float[] delaysAfterAnimation; // 每个动画结束后延迟多少秒播放下一个
    
    public AnimationSequence(string name, AnimationClip[] clips, float[] delays)
    {
        sequenceName = name;
        animationClips = clips;
        delaysAfterAnimation = delays;
    }
}

public class Enemy : MonoBehaviour
{
    [Header("Enemy Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    
    [Header("Animation System")]
    [SerializeField] private Animator enemyAnimator;
    [SerializeField] private RuntimeAnimatorController animatorController;
    
    [Header("Animation Clips")]
    [SerializeField] private AnimationClip appearAnimation;
    [SerializeField] private AnimationClip disappearAnimation;
    [SerializeField] private AnimationClip attackPreparationAnimation;
    [SerializeField] private AnimationClip attackAnimation;
    [SerializeField] private AnimationClip specialAnimation;
    [SerializeField] private AnimationClip idleAnimation;
    [SerializeField] private AnimationClip moveAnimation;
    [SerializeField] private AnimationClip deadAnimation;
    
    [Header("Animation Sequences")]
    [SerializeField] private AnimationSequence[] animationSequences;
    [SerializeField] private bool useAnimationSequences = false;
    private int nextAnimationSequenceIndex = 0;
    
    [Header("Animation Timing")]
    [SerializeField] private float appearDelay = 0f;
    [SerializeField] private float disappearDelay = 0f;
    [SerializeField] private float attackPreparationDelay = 0f;
    [SerializeField] private float attackDelay = 0f;
    [SerializeField] private float specialDelay = 0f;
    [SerializeField] private float deadDelay = 0f;
    
    [Header("Movement")]
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private GameObject target; // 攻击目标
    
    [Header("Targeted Attack System")]
    [SerializeField] private bool enableTargetedAttacks = false;              // 是否启用定点攻击系统
    [SerializeField] private AttackSequenceType attackSequenceType = AttackSequenceType.Single; // 攻击序列类型
    [SerializeField] private List<TargetedAttackConfig> targetedAttackConfigs = new List<TargetedAttackConfig>(); // 定点攻击配置
    [SerializeField] private float attackCooldown = 5f;                       // 攻击冷却时间
    [SerializeField] private float attackTriggerRange = 10f;                  // 攻击触发范围
    [SerializeField] private bool stopMovementDuringAttack = true;            // 攻击时是否停止移动
    
    [Header("Z-Axis Movement")]
    [SerializeField] private bool isMovingInZ = true; // 是否在Z轴移动
    [SerializeField] private float zMoveSpeed = 1f; // Z轴移动速度
    [SerializeField] private float minZDistance = 5f; // 与玩家的最小Z距离
    [SerializeField] private float maxZDistance = 20f; // 与玩家的最大Z距离
    
    [Header("XOY Movement")]
    [SerializeField] private bool isMovingInXOY = true; // 是否在XOY面移动
    [SerializeField] private float xoyMoveSpeed = 1.5f; // XOY面移动速度
    [SerializeField] private MovementPattern xoyMovementPattern = MovementPattern.Random; // XOY移动模式
    
    [Header("Random Movement Settings")]
    [SerializeField] private float randomMoveRadius = 10f; // 随机移动半径
    [SerializeField] private float randomMoveInterval = 3f; // 随机移动间隔
    
    [Header("Path Movement Settings")]
    [SerializeField] private EnemyPathController pathController; // 路径控制器
    [SerializeField] private float pathFollowSpeed = 2f; // 路径跟随速度
    
    [Header("Body Parts")]
    [SerializeField] private List<BodyPart> bodyParts = new List<BodyPart>();
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject bloodEffectPrefab;
    [SerializeField] private float bloodEffectDuration = 1f;
    [SerializeField] private Color hitFlashColor = Color.red;
    [SerializeField] private float hitFlashDuration = 0.1f;
    
    // 护甲伤害倍率（基于ArmorType枚举）
    private Dictionary<ArmorType, float> armorDamageMultiplier = new Dictionary<ArmorType, float>()
    {
        { ArmorType.WeakPoint, 1.5f },    // 弱点150%伤害
        { ArmorType.Light, 1.0f },        // 轻甲100%伤害
        { ArmorType.Medium, 0.8f },       // 中甲80%伤害
        { ArmorType.Heavy, 0.5f },        // 重甲50%伤害
        { ArmorType.Unattackable, 0.0f }  // 不可攻击部位0%伤害
    };
    
    public bool IsDead { get; private set; } = false;
    
    // 定点攻击系统相关变量
    private AttackPrefabManager attackPrefabManager;          // 攻击预制体管理器
    private float lastAttackTime = 0f;                        // 上次攻击时间
    private int currentAttackSequenceIndex = 0;              // 当前攻击序列索引
    private bool isPerformingTargetedAttack = false;         // 是否正在执行定点攻击
    private GameObject currentTargetedAttackInstance;        // 当前定点攻击实例
    
    // 事件
    public System.Action<Enemy> OnEnemyDeath;
    public System.Action<Enemy, float> OnEnemyDamaged;
    public System.Action<BodyPart> OnBodyPartDestroyed;
    public System.Action<Enemy, string> OnTargetedAttackStart;    // 定点攻击开始事件
    public System.Action<Enemy, string> OnTargetedAttackComplete; // 定点攻击完成事件
    
    private float currentDistanceToTarget;
    
    // XOY移动相关变量
    private Vector3 randomTargetPosition;
    private float randomMoveTimer;
    private int currentPathIndex = 0;
    private Vector3 basicSpawnPosition;
    
    // 动画相关变量
    private bool isPlayingAnimation = false;
    private string currentAnimationState = "Idle";
    private Coroutine animationSequenceCoroutine;
    
    // 动画参数名称常量
    // private const string ANIM_PARAM_APPEAR = "Appear";
    // private const string ANIM_PARAM_DISAPPEAR = "Disappear";
    // private const string ANIM_PARAM_ATTACK_PREP = "AttackPrep";
    // private const string ANIM_PARAM_ATTACK = "Attack";
    // private const string ANIM_PARAM_SPECIAL = "Special";
    // private const string ANIM_PARAM_IDLE = "Idle";
    // private const string ANIM_PARAM_MOVE = "Move";
    // private const string ANIM_PARAM_IS_MOVING = "IsMoving";
    
    // 移动模式枚举
    public enum MovementPattern
    {
        Random,     // 随机移动
        Path        // 固定路径
    }
    
    private void Awake()
    {
        basicSpawnPosition = transform.position;
        currentHealth = maxHealth;
        InitializeBodyParts();
        SetupTarget();
        InitializeMovement();
        InitializeAnimation();
        InitializeTargetedAttackSystem();
    }
    
    private void Start()
    {
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
                bodyPartComponent.Initialize(this, bodyPart);
            }
        }
        
        // 播放出现动画
        PlayAppearAnimation();
    }
    
    private void Update()
    {
        if (!IsDead && target != null)
        {
            UpdateDistanceToTarget();
            
            // 检查是否应该停止移动（攻击期间）
            bool shouldMove = !(stopMovementDuringAttack && isPerformingTargetedAttack);
            
            if (shouldMove && isMovingInZ)
            {
                moveInZ();
            }
            
            if (shouldMove && isMovingInXOY)
            {
                moveInXOY();
            }
            
            // 检查定点攻击
            if (enableTargetedAttacks && !isPerformingTargetedAttack)
            {
                CheckTargetedAttackTrigger();
            }
        }
    }
    
    private void SetupTarget()
    {
        if (target == null)
        {
            // 自动找到玩家作为目标
            GameObject player = GameObject.FindAnyObjectByType<Player>().gameObject;
            if (player != null)
            {
                target = player;
                Debug.Log($"Enemy {name} found target: {target.name} at position");
            }
            else
            {
                Debug.LogWarning($"Enemy {name} could not find any camera as target!");
            }
        }
    }
    
    private void InitializeMovement()
    {
        // 初始化随机移动
        if (xoyMovementPattern == MovementPattern.Random)
        {
            SetNewRandomTarget();
        }
        
        // 初始化路径移动
        if (xoyMovementPattern == MovementPattern.Path && pathController != null)
        {
            currentPathIndex = 0;
        }
    }
    
    private void InitializeAnimation()
    {
        // 如果没有指定Animator，尝试获取
        if (enemyAnimator == null)
        {
            enemyAnimator = GetComponent<Animator>();
        }
        
        // 如果没有Animator，创建一个
        if (enemyAnimator == null)
        {
            enemyAnimator = gameObject.AddComponent<Animator>();
        }
        
        // 设置AnimatorController
        if (animatorController != null)
        {
            enemyAnimator.runtimeAnimatorController = animatorController;
        }
        
        // 初始化动画参数
        InitializeAnimationParameters();
    }
    
    private void InitializeAnimationParameters()
    {
        if (enemyAnimator == null) return;
        
        // 如果有待机动画，播放它
        if (idleAnimation != null)
        {
            PlayIdleAnimation();
        }
    }
    
    // 动画播放方法
    public void PlayAppearAnimation()
    {
        if (enemyAnimator == null || appearAnimation == null) return;
        
        StartCoroutine(PlayAnimationWithDelay(appearAnimation, appearDelay));
    }
    
    public void PlayDisappearAnimation()
    {
        if (enemyAnimator == null || disappearAnimation == null) return;
        
        StartCoroutine(PlayAnimationWithDelay(disappearAnimation, disappearDelay));
    }
    
    public void PlayAttackPreparationAnimation()
    {
        if (enemyAnimator == null || attackPreparationAnimation == null) return;
        
        StartCoroutine(PlayAnimationWithDelay(attackPreparationAnimation, attackPreparationDelay));
    }
    
    public void PlayAttackAnimation()
    {
        if (enemyAnimator == null || attackAnimation == null) return;
        
        StartCoroutine(PlayAnimationWithDelay(attackAnimation, attackDelay));
    }
    
    public void PlaySpecialAnimation()
    {
        if (enemyAnimator == null || specialAnimation == null) return;
        
        StartCoroutine(PlayAnimationWithDelay(specialAnimation, specialDelay));
    }
    
    public void PlayIdleAnimation()
    {
        if (enemyAnimator == null || idleAnimation == null) return;
        
        // 直接播放动画片段
        enemyAnimator.Play(idleAnimation.name);
        currentAnimationState = "Idle";
        isPlayingAnimation = false;
    }
    
    public void PlayMoveAnimation()
    {
        if (enemyAnimator == null || moveAnimation == null) return;
        
        // 直接播放动画片段
        enemyAnimator.Play(moveAnimation.name);
        currentAnimationState = "Move";
        isPlayingAnimation = false;
    }
    
    // 播放动画序列
    public void PlayAnimationSequence(int sequenceIndex)
    {
        if (!useAnimationSequences || sequenceIndex < 0 || sequenceIndex >= animationSequences.Length) return;
        
        if (animationSequenceCoroutine != null)
        {
            StopCoroutine(animationSequenceCoroutine);
        }
        
        animationSequenceCoroutine = StartCoroutine(PlayAnimationSequenceCoroutine(animationSequences[sequenceIndex]));
    }
    
    // 动画序列协程
    private System.Collections.IEnumerator PlayAnimationSequenceCoroutine(AnimationSequence sequence)
    {
        if (sequence.animationClips == null || sequence.animationClips.Length == 0) yield break;
        
        isPlayingAnimation = true;
        
        for (int i = 0; i < sequence.animationClips.Length; i++)
        {
            AnimationClip clip = sequence.animationClips[i];
            if (clip == null) continue;
            
            // 播放当前动画
            enemyAnimator.Play(clip.name);
            currentAnimationState = clip.name;

            // 等待动画播放完成
            yield return new WaitForSeconds(clip.length);
            
            // 如果还有下一个动画，等待延迟时间
            if (i < sequence.animationClips.Length - 1 && i < sequence.delaysAfterAnimation.Length)
            {
                float delay = sequence.delaysAfterAnimation[i];
                if (delay > 0f)
                {
                    yield return new WaitForSeconds(delay);
                }
            }
        }
        
        isPlayingAnimation = false;
        PlayIdleAnimation(); // 回到待机状态

        nextAnimationSequenceIndex++; // 更新nextAnimationSequenceIndex数值 后续可以改逻辑
    }
    
    // 播放单个动画片段
    private void PlayAnimationClip(AnimationClip clip)
    {
        if (enemyAnimator == null || clip == null) return;
        
        // 这里可以根据需要设置动画参数
        // 或者直接播放动画片段
        enemyAnimator.Play(clip.name);
        currentAnimationState = clip.name;
    }
    
    // 带延迟的动画播放协程
    private System.Collections.IEnumerator PlayAnimationWithDelay(AnimationClip clip, float delay)
    {
        if (enemyAnimator == null || clip == null) yield break;
        
        isPlayingAnimation = true;
        
        // 等待延迟时间
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }
        
        // 直接播放动画片段
        enemyAnimator.Play(clip.name);
        currentAnimationState = clip.name;
        
        // 等待动画播放完成
        yield return new WaitForSeconds(clip.length);
        
        isPlayingAnimation = false;
        PlayIdleAnimation(); // 回到待机状态
    }
    
    // 检查动画是否正在播放
    public bool IsAnimationPlaying()
    {
        return isPlayingAnimation;
    }
    
    // 获取当前动画状态
    public string GetCurrentAnimationState()
    {
        return currentAnimationState;
    }
    
    // 停止当前动画
    public void StopCurrentAnimation()
    {
        if (animationSequenceCoroutine != null)
        {
            StopCoroutine(animationSequenceCoroutine);
            animationSequenceCoroutine = null;
        }
        
        isPlayingAnimation = false;
        PlayIdleAnimation();
    }
    
    private void UpdateDistanceToTarget()
    {
        if (target == null) return;
        
        currentDistanceToTarget = transform.position.z - target.transform.position.z;

        // 当敌人离玩家较近时停止Z轴移动
        if (currentDistanceToTarget <= minZDistance)
        {
            isMovingInZ = false;
        }
        else if (currentDistanceToTarget >= maxZDistance)
        {
            isMovingInZ = true;
        }
    }
    
    // Z轴移动方法
    private void moveInZ()
    {
        if (target == null || !isMovingInZ) return;
        
        Vector3 directionToTarget = (target.transform.position - transform.position).normalized;
        float zDirection = Mathf.Sign(directionToTarget.z); // 获取Z轴方向
        
        // 只在Z轴方向移动
        Vector3 zMovement = Vector3.forward * zDirection * zMoveSpeed * Time.deltaTime;
        transform.position += zMovement;
        
        //Debug.Log($"Enemy {name} moving in Z direction: {zDirection}, Speed: {zMoveSpeed}");
    }
    
    // XOY面移动方法
    private void moveInXOY()
    {
        switch (xoyMovementPattern)
        {
            case MovementPattern.Random:
                MoveRandomlyInXOY();
                break;
            case MovementPattern.Path:
                MoveAlongPath();
                break;
        }
    }

    // 随机移动
    private void MoveRandomlyInXOY()
    {
        randomMoveTimer += Time.deltaTime;

        // 到达目标位置或时间到了，设置新的随机目标
        if (Vector3.Distance(transform.position, randomTargetPosition) < 0.5f || randomMoveTimer >= randomMoveInterval)
        {
            SetNewRandomTarget();
        }

        // 向随机目标移动
        Vector3 direction = (randomTargetPosition - transform.position).normalized;
        Vector3 xoyMovement = new Vector3(direction.x, direction.y, 0) * xoyMoveSpeed * Time.deltaTime;
        transform.position += xoyMovement;

        if (!isPlayingAnimation)
        {
            if (useAnimationSequences)
            {
                PlayAnimationSequence(nextAnimationSequenceIndex);
            }
            else
            {
                if (!isPlayingAnimation)
                {
                    PlayMoveAnimation();
                }
            }
        }
        
        // 旋转朝向移动方向
        /*
        if (direction.magnitude > 0.1f)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }*/
    }
    
    // 设置新的随机目标
    private void SetNewRandomTarget()
    {
        if (target == null) return;
        
        // 在玩家周围随机选择一个XOY平面上的点
        Vector2 randomCircle = Random.insideUnitCircle * randomMoveRadius;
        randomTargetPosition = new Vector3(
            target.transform.position.x + randomCircle.x,
            target.transform.position.y + randomCircle.y,
            transform.position.z // 保持当前Z位置
        );
        
        randomMoveTimer = 0f;
        //Debug.Log($"Enemy {name} new random target: {randomTargetPosition}");
    }

    // 沿路径移动
    private void MoveAlongPath()
    {
        if (pathController == null) return;

        // 获取当前路径点的世界坐标
        Vector3 currentPathPoint = pathController.GetPathPoint(currentPathIndex) - transform.position + basicSpawnPosition;
        Vector3 direction = (currentPathPoint - transform.position).normalized;

        // 向路径点移动（只在XOY平面上）
        Vector3 xoyMovement = new Vector3(direction.x, direction.y, 0) * pathFollowSpeed * Time.deltaTime;
        transform.position += xoyMovement;
        //Debug.Log(xoyMovement.x * xoyMovement.x + xoyMovement.y * xoyMovement.y);
        // 检查是否到达路径点
        float distanceToPathPoint = Vector3.Distance(
            new Vector3(transform.position.x, transform.position.y, 0),
            new Vector3(currentPathPoint.x, currentPathPoint.y, 0)
        );

        if (distanceToPathPoint < 0.5f)
        {
            // 到达当前路径点，移动到下一个
            currentPathIndex = (currentPathIndex + 1) % pathController.GetPathPointCount();
            Debug.Log($"Enemy {name} reached path point {currentPathIndex - 1}, moving to {currentPathIndex}");
        }
    }
    
    private void InitializeBodyParts()
    {
        // 如果bodyParts为空，自动扫描子对象
        if (bodyParts.Count == 0)
        {
            AutoDetectBodyParts();
        }
        
        // 确保所有部位都有碰撞体
        foreach (var bodyPart in bodyParts)
        {
            if (bodyPart.partObject != null && bodyPart.partCollider == null)
            {
                var collider = bodyPart.partObject.GetComponent<Collider2D>();
                if (collider == null)
                {
                    // 自动添加碰撞体
                    collider = bodyPart.partObject.AddComponent<CircleCollider2D>();
                }
                bodyPart.partCollider = collider;
                
                // 设置标签
                bodyPart.partObject.tag = "Enemy";
            }
        }
    }
    
    private void AutoDetectBodyParts()
    {
        // 自动检测子对象作为身体部位
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            ArmorType defaultArmor = ArmorType.Light;
            float defaultMultiplier = 1f;
            bool enableDestruction = false;
            float partHealth = 100f;
            
            // 根据名称设置默认护甲类型和伤害倍率
            string childName = child.name.ToLower();
            if (childName.Contains("head"))
            {
                defaultArmor = ArmorType.WeakPoint;
                defaultMultiplier = 2f;
                enableDestruction = true;
                partHealth = 50f;
            }
            else if (childName.Contains("chest") || childName.Contains("body"))
            {
                defaultArmor = ArmorType.Medium;
                defaultMultiplier = 1f;
                enableDestruction = true;
                partHealth = 100f;
            }
            else if (childName.Contains("arm") || childName.Contains("leg"))
            {
                defaultArmor = ArmorType.Light;
                defaultMultiplier = 0.8f;
                enableDestruction = true;
                partHealth = 80f;
            }
            
            bodyParts.Add(new BodyPart(child.name, child.gameObject, defaultArmor, defaultMultiplier, enableDestruction, partHealth));
        }
    }
    
    public void TakeDamage(float baseDamage, BodyPart hitPart = null)
    {
        if (IsDead) return;
        
        float finalDamage = 0f;
        
        if (hitPart != null)
        {
            // 计算部位伤害：基础伤害 × 护甲倍率 × 额外倍率
            float armorMultiplier = armorDamageMultiplier[hitPart.armorType];
            finalDamage = baseDamage * armorMultiplier * hitPart.damageMultiplier;
            
            // 如果部位不可攻击，直接返回
            if (armorMultiplier <= 0f)
            {
                Debug.Log($"Hit {hitPart.partName} but it's unattackable!");
                return;
            }
            
            // 部位受击闪烁
            StartCoroutine(FlashBodyPart(hitPart));
            
            // 如果部位可破坏，处理部位血量
            if (hitPart.enableDestruction)
            {
                HandleBodyPartDamage(hitPart, finalDamage);
            }
        }
        else
        {
            // 如果没有指定部位，使用默认伤害
            finalDamage = baseDamage;
        }
        
        // 敌人整体掉血
        currentHealth -= finalDamage;
        
        // 触发受伤事件
        OnEnemyDamaged?.Invoke(this, finalDamage);
        
        // 显示血液特效
        if (hitPart != null && bloodEffectPrefab != null)
        {
            ShowBloodEffect(hitPart.partObject.transform.position);
        }
        
        // 检查死亡
        if (currentHealth <= 0 && !IsDead)
        {
            Die();
        }
        
        Debug.Log($"Enemy took {finalDamage:F1} damage to {(hitPart?.partName ?? "unknown part")}. Health: {currentHealth:F1}/{maxHealth}. Distance: {currentDistanceToTarget:F1}");
    }
    
    private void HandleBodyPartDamage(BodyPart bodyPart, float damage)
    {
        if (!bodyPart.enableDestruction) return;
        
        // 部位独立血量掉血
        bodyPart.partHealth -= damage;
        
        // 检查部位是否被破坏
        if (bodyPart.partHealth <= 0)
        {
            DestroyBodyPart(bodyPart);
        }
    }
    
    private void DestroyBodyPart(BodyPart bodyPart)
    {
        // 触发部位破坏事件
        OnBodyPartDestroyed?.Invoke(bodyPart);
        
        // 获取部位组件并执行破坏逻辑
        var bodyPartComponent = bodyPart.partObject.GetComponent<EnemyBodyPart>();
        if (bodyPartComponent != null)
        {
            bodyPartComponent.OnPartDestroyed();
        }
        
        Debug.Log($"Body part {bodyPart.partName} has been destroyed!");
    }
    
    private System.Collections.IEnumerator FlashBodyPart(BodyPart bodyPart)
    {
        if (bodyPart.partObject == null) yield break;
        
        SpriteRenderer spriteRenderer = bodyPart.partObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) yield break;
        
        Color originalColor = spriteRenderer.color;
        
        // 闪烁为红色
        spriteRenderer.color = hitFlashColor;
        
        yield return new WaitForSeconds(hitFlashDuration);
        
        // 恢复原色
        spriteRenderer.color = originalColor;
    }
    
    private void ShowBloodEffect(Vector3 position)
    {
        if (bloodEffectPrefab != null)
        {
            GameObject bloodEffect = Instantiate(bloodEffectPrefab, position, Quaternion.identity);
            Destroy(bloodEffect, bloodEffectDuration);
        }
    }
    
    private void Die()
    {
        IsDead = true;
        OnEnemyDeath?.Invoke(this);
        
        // 死亡动画或效果
        StartCoroutine(DeathSequence());
    }

    private void DieButJustDie() 
    {
        IsDead = true;
        Destroy(gameObject);

    }
    
    private System.Collections.IEnumerator DeathSequence()
    {
        // 播放死亡动画
        PlayDeadAnimation();
        
        // 等待死亡动画完成
        while (isPlayingAnimation)
        {
            yield return null;
        }
        
        // 简单的死亡效果：淡出
        SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        float fadeTime = 1f;
        float elapsedTime = 0f;
        
        Color[] originalColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalColors[i] = spriteRenderers[i].color;
        }
        
        while (elapsedTime < fadeTime)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeTime);
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                Color newColor = originalColors[i];
                newColor.a = alpha;
                spriteRenderers[i].color = newColor;
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        Destroy(gameObject);
    }
    
    // 公共方法
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }
    
    public BodyPart GetBodyPartByCollider(Collider2D collider)
    {
        foreach (var bodyPart in bodyParts)
        {
            if (bodyPart.partCollider == collider)
                return bodyPart;
        }
        return null;
    }
    
    public float GetDistanceToTarget()
    {
        return currentDistanceToTarget;
    }
    
    // 编辑器辅助方法
    [ContextMenu("Setup Default Body Parts")]
    public void SetupDefaultBodyParts()
    {
        bodyParts.Clear();
        bodyParts.Add(new BodyPart("Head", null, ArmorType.WeakPoint, 2f, true, 50f));
        bodyParts.Add(new BodyPart("Chest", null, ArmorType.Medium, 1f, true, 100f));
        bodyParts.Add(new BodyPart("LeftArm", null, ArmorType.Light, 0.8f, true, 80f));
        bodyParts.Add(new BodyPart("RightArm", null, ArmorType.Light, 0.8f, true, 80f));
        bodyParts.Add(new BodyPart("LeftLeg", null, ArmorType.Light, 0.8f, true, 80f));
        bodyParts.Add(new BodyPart("RightLeg", null, ArmorType.Light, 0.8f, true, 80f));
    }
    
    [ContextMenu("Setup Animation Parameters")]
    public void SetupAnimationParameters()
    {
        if (enemyAnimator == null) return;
        
        // 这里可以添加更多动画参数设置
        Debug.Log($"Animation parameters setup for {name}");
    }
    
    [ContextMenu("Test Appear Animation")]
    public void TestAppearAnimation()
    {
        PlayAppearAnimation();
    }
    
    [ContextMenu("Test Attack Animation")]
    public void TestAttackAnimation()
    {
        PlayAttackAnimation();
    }
    
    [ContextMenu("Test Special Animation")]
    public void TestSpecialAnimation()
    {
        PlaySpecialAnimation();
    }
    
    // 添加死亡动画播放方法
    public void PlayDeadAnimation()
    {
        if (enemyAnimator == null || deadAnimation == null) return;
        
        StartCoroutine(PlayAnimationWithDelay(deadAnimation, deadDelay));
    }
    
    // 在Scene视图中显示距离信息
    private void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            // 绘制到目标的连线
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, target.transform.position);
            
            // 绘制Z轴移动范围
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(target.transform.position, minZDistance);
            Gizmos.DrawWireSphere(target.transform.position, maxZDistance);
            
            // 绘制随机移动范围
            if (xoyMovementPattern == MovementPattern.Random)
            {
            Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(target.transform.position, randomMoveRadius);
            }
        }
    }

    // 在编辑器辅助方法中添加测试死亡动画的方法
    [ContextMenu("Test Dead Animation")]
    public void TestDeadAnimation()
    {
        PlayDeadAnimation();
    }
    
    #region Targeted Attack System
    
    /// <summary>
    /// 初始化定点攻击系统
    /// </summary>
    private void InitializeTargetedAttackSystem()
    {
        if (!enableTargetedAttacks) return;
        
        // 获取或创建攻击预制体管理器
        attackPrefabManager = GetComponent<AttackPrefabManager>();
        if (attackPrefabManager == null)
        {
            attackPrefabManager = gameObject.AddComponent<AttackPrefabManager>();
        }
        
        // 初始化攻击配置
        if (targetedAttackConfigs == null || targetedAttackConfigs.Count == 0)
        {
            SetupDefaultTargetedAttacks();
        }
        
        Debug.Log($"Enemy {name} initialized targeted attack system with {targetedAttackConfigs.Count} attack configurations");
    }
    
    /// <summary>
    /// 设置默认的定点攻击配置
    /// </summary>
    private void SetupDefaultTargetedAttacks()
    {
        targetedAttackConfigs = new List<TargetedAttackConfig>
        {
            new TargetedAttackConfig
            {
                attackName = "AreaAttack",
                attackType = AttackType.Area,
                weight = 1f,
                areaSettings = new AreaAttackSettings
                {
                    overrideRadius = true,
                    attackRadius = 2f,
                    overrideIndicatorRadius = true,
                    indicatorStartRadius = 5f
                }
            },
            new TargetedAttackConfig
            {
                attackName = "MissileAttack",
                attackType = AttackType.Missile,
                weight = 1f,
                missileSettings = new MissileAttackSettings
                {
                    overrideSpeed = true,
                    missileSpeed = 8f,
                    overrideCurveHeight = true,
                    curveHeight = 4f,
                    overrideDamageRadius = true,
                    damageRadius = 2f
                }
            }
        };
    }
    
    /// <summary>
    /// 检查定点攻击触发条件
    /// </summary>
    private void CheckTargetedAttackTrigger()
    {
        // 检查冷却时间
        if (Time.time < lastAttackTime + attackCooldown)
        {
            return;
        }
        
        // 检查距离
        /*if (currentDistanceToTarget > attackTriggerRange)
        {
            return;
        }*/
        
        // 检查是否正在播放动画
        if (isPlayingAnimation)
        {
            return;
        }
        
        // 触发攻击
        TriggerTargetedAttack();
    }
    
    /// <summary>
    /// 触发定点攻击
    /// </summary>
    private void TriggerTargetedAttack()
    {
        if (targetedAttackConfigs == null || targetedAttackConfigs.Count == 0)
        {
            Debug.LogWarning($"No targeted attack configurations available for {name}");
            return;
        }
        
        // 根据攻击序列类型选择攻击
        TargetedAttackConfig selectedAttack = SelectAttackConfig();
        if (selectedAttack == null)
        {
            Debug.LogWarning($"Failed to select attack configuration for {name}");
            return;
        }
        
        // 执行攻击
        ExecuteTargetedAttack(selectedAttack);
    }
    
    /// <summary>
    /// 选择攻击配置
    /// </summary>
    /// <returns>选择的攻击配置</returns>
    private TargetedAttackConfig SelectAttackConfig()
    {
        switch (attackSequenceType)
        {
            case AttackSequenceType.Single:
                return SelectSingleAttack();
            case AttackSequenceType.Sequential:
                return SelectSequentialAttack();
            case AttackSequenceType.Random:
                return SelectRandomAttack();
            case AttackSequenceType.Weighted:
                return SelectWeightedAttack();
            default:
                return targetedAttackConfigs[0];
        }
    }
    
    /// <summary>
    /// 选择单一攻击（使用第一个配置）
    /// </summary>
    /// <returns>攻击配置</returns>
    private TargetedAttackConfig SelectSingleAttack()
    {
        return targetedAttackConfigs[0];
    }
    
    /// <summary>
    /// 选择顺序攻击
    /// </summary>
    /// <returns>攻击配置</returns>
    private TargetedAttackConfig SelectSequentialAttack()
    {
        TargetedAttackConfig config = targetedAttackConfigs[currentAttackSequenceIndex];
        currentAttackSequenceIndex = (currentAttackSequenceIndex + 1) % targetedAttackConfigs.Count;
        return config;
    }
    
    /// <summary>
    /// 选择随机攻击
    /// </summary>
    /// <returns>攻击配置</returns>
    private TargetedAttackConfig SelectRandomAttack()
    {
        int randomIndex = Random.Range(0, targetedAttackConfigs.Count);
        return targetedAttackConfigs[randomIndex];
    }
    
    /// <summary>
    /// 选择权重攻击
    /// </summary>
    /// <returns>攻击配置</returns>
    private TargetedAttackConfig SelectWeightedAttack()
    {
        float totalWeight = 0f;
        foreach (var config in targetedAttackConfigs)
        {
            totalWeight += config.weight;
        }
        
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        
        foreach (var config in targetedAttackConfigs)
        {
            currentWeight += config.weight;
            if (randomValue <= currentWeight)
            {
                return config;
            }
        }
        
        return targetedAttackConfigs[targetedAttackConfigs.Count - 1];
    }
    
    /// <summary>
    /// 执行定点攻击
    /// </summary>
    /// <param name="attackConfig">攻击配置</param>
    private void ExecuteTargetedAttack(TargetedAttackConfig attackConfig)
    {
        isPerformingTargetedAttack = true;
        lastAttackTime = Time.time;
        
        // 确定目标位置
        Vector3 targetPos = target != null ? target.transform.position : transform.position;
        
        // 触发攻击开始事件
        OnTargetedAttackStart?.Invoke(this, attackConfig.attackName);
        
        // 播放攻击准备动画
        PlayAttackPreparationAnimation();
        
        // 根据攻击类型创建攻击实例
        GameObject attackInstance = null;
        
        switch (attackConfig.attackType)
        {
            case AttackType.Area:
                attackInstance = attackPrefabManager.CreateAreaAttack(transform.position, targetPos, attackConfig.areaSettings);
                break;
            case AttackType.Missile:
                attackInstance = attackPrefabManager.CreateMissileAttack(transform.position, targetPos, attackConfig.missileSettings);
                break;
            default:
                Debug.LogWarning($"Unknown attack type: {attackConfig.attackType}");
                break;
        }
        
        if (attackInstance != null)
        {
            currentTargetedAttackInstance = attackInstance;
            
            // 订阅攻击完成事件
            TargetedAttackBase attackScript = attackInstance.GetComponent<TargetedAttackBase>();
            if (attackScript != null)
            {
                attackScript.OnAttackComplete += OnTargetedAttackCompleted;
                attackScript.OnAttackInterrupted += OnTargetedAttackCompleted;
            }
            
            Debug.Log($"Enemy {name} executed {attackConfig.attackName} targeting {targetPos}");
        }
        else
        {
            // 攻击创建失败，重置状态
            isPerformingTargetedAttack = false;
            Debug.LogError($"Failed to create attack instance for {attackConfig.attackName}");
        }
    }
    
    /// <summary>
    /// 定点攻击完成回调
    /// </summary>
    /// <param name="attack">攻击实例</param>
    private void OnTargetedAttackCompleted(AttackPattern attack)
    {
        isPerformingTargetedAttack = false;
        currentTargetedAttackInstance = null;
        
        // 触发攻击完成事件
        OnTargetedAttackComplete?.Invoke(this, attack.GetAttackName());
        
        // 播放攻击后动画或回到待机状态
        if (!isPlayingAnimation)
        {
            PlayIdleAnimation();
        }
        
        Debug.Log($"Enemy {name} completed targeted attack: {attack.GetAttackName()}");
    }
    
    /// <summary>
    /// 手动触发定点攻击（供外部调用）
    /// </summary>
    /// <param name="attackName">攻击名称</param>
    /// <param name="targetPosition">目标位置（可选）</param>
    public void TriggerTargetedAttack(string attackName, Vector3? targetPosition = null)
    {
        if (!enableTargetedAttacks || isPerformingTargetedAttack) return;
        
        TargetedAttackConfig config = targetedAttackConfigs.Find(c => c.attackName == attackName);
        if (config == null)
        {
            Debug.LogWarning($"Attack configuration '{attackName}' not found for {name}");
            return;
        }
        
        // 临时覆盖目标位置
        Vector3 originalTarget = target != null ? target.transform.position : transform.position;
        if (targetPosition.HasValue)
        {
            // 创建临时目标对象
            GameObject tempTarget = new GameObject("TempTarget");
            tempTarget.transform.position = targetPosition.Value;
            GameObject originalTargetObj = target;
            target = tempTarget;
            
            ExecuteTargetedAttack(config);
            
            // 恢复原始目标
            target = originalTargetObj;
            Destroy(tempTarget);
        }
        else
        {
            ExecuteTargetedAttack(config);
        }
    }
    
    /// <summary>
    /// 中断当前的定点攻击
    /// </summary>
    public void InterruptTargetedAttack()
    {
        if (isPerformingTargetedAttack && currentTargetedAttackInstance != null)
        {
            TargetedAttackBase attackScript = currentTargetedAttackInstance.GetComponent<TargetedAttackBase>();
            if (attackScript != null)
            {
                attackScript.InterruptAttack();
            }
        }
    }
    
    /// <summary>
    /// 设置定点攻击配置
    /// </summary>
    /// <param name="configs">攻击配置列表</param>
    public void SetTargetedAttackConfigs(List<TargetedAttackConfig> configs)
    {
        targetedAttackConfigs = configs;
    }
    
    /// <summary>
    /// 添加定点攻击配置
    /// </summary>
    /// <param name="config">攻击配置</param>
    public void AddTargetedAttackConfig(TargetedAttackConfig config)
    {
        if (targetedAttackConfigs == null)
        {
            targetedAttackConfigs = new List<TargetedAttackConfig>();
        }
        targetedAttackConfigs.Add(config);
    }
    
    /// <summary>
    /// 启用或禁用定点攻击系统
    /// </summary>
    /// <param name="enable">是否启用</param>
    public void SetTargetedAttacksEnabled(bool enable)
    {
        enableTargetedAttacks = enable;
        
        if (!enable && isPerformingTargetedAttack)
        {
            InterruptTargetedAttack();
        }
    }
    
    /// <summary>
    /// 获取当前是否正在执行定点攻击
    /// </summary>
    /// <returns>是否正在执行定点攻击</returns>
    public bool IsPerformingTargetedAttack()
    {
        return isPerformingTargetedAttack;
    }
    
    #endregion
}

/// <summary>
/// 攻击序列类型枚举
/// </summary>
public enum AttackSequenceType
{
    Single,      // 单一攻击（只使用第一个配置）
    Sequential,  // 顺序攻击（按顺序使用每个配置）
    Random,      // 随机攻击（随机选择配置）
    Weighted     // 权重攻击（根据权重随机选择）
}

/// <summary>
/// 定点攻击配置
/// </summary>
[System.Serializable]
public class TargetedAttackConfig
{
    [Header("Basic Settings")]
    public string attackName = "DefaultAttack";         // 攻击名称
    public AttackType attackType = AttackType.Area;     // 攻击类型
    public float weight = 1f;                           // 权重（用于权重选择）
    
    [Header("Type-Specific Settings")]
    public AreaAttackSettings areaSettings;             // 区域攻击设置
    public MissileAttackSettings missileSettings;       // 导弹攻击设置
    
    [Header("Description")]
    [TextArea(2, 3)]
    public string description = "";                      // 攻击描述
}