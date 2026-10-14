using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/*
public class crosshairTarget : MonoBehaviour
{
    // 相机引用
    [SerializeField] private Camera gameCamera;

    // 伤害值
    public int damage = 10;

    // 射击范围
    [SerializeField] private float shootingRange = 200f;

    // 敌人层级遮罩
    [SerializeField] private LayerMask enemyLayerMask = -1;

    // 是否可以射击
    private bool canShoot = true;

    void Start()
    {
        // 获取相机引用
        gameCamera = Camera.main ?? FindObjectOfType<Camera>();
    }

    // 当射击手势触发时调用此方法
    public void ActivateCollider()
    {
        if (canShoot)
        {
            // 使用射线检测进行射击
            PerformRaycastShoot();
        }
    }

    // 执行射线检测射击
    private void PerformRaycastShoot()
    {
        // 获取准星的世界位置
        Vector3 crosshairWorldPos = transform.position;

        // 计算从相机到准星的射线
        Ray shootRay = CalculateShootRay(crosshairWorldPos);

        Debug.Log(shootRay.origin);
        Debug.Log(crosshairWorldPos - gameCamera.transform.position);
        Debug.DrawRay(shootRay.origin, crosshairWorldPos - gameCamera.transform.position, Color.blue,5f);

        // 进行射线检测
        // RaycastHit2D hit = Physics2D.Raycast(shootRay.origin, crosshairWorldPos - gameCamera.transform.position, shootingRange, enemyLayerMask);
        /// 我操这里怎么是2d？？？？？？？？？？
        RaycastHit2D hit = Physics2D.Raycast(crosshairWorldPos - gameCamera.transform.position, crosshairWorldPos - gameCamera.transform.position, shootingRange, enemyLayerMask);

        if (hit.collider != null)
        {
            // 射中目标
            HandleHit(hit);
        }
        else
        {
            // 未射中
            Debug.Log("Shot missed!");
        }

        // 短暂延迟后允许下次射击
        StartCoroutine(ShootCooldown(0.1f));
    }

    // 计算射击射线
    private Ray CalculateShootRay(Vector3 targetWorldPos)
    {
        if (gameCamera.orthographic)
        {
            // 正交相机：从相机位置射向目标位置
            Vector3 direction = (targetWorldPos - gameCamera.transform.position);
            return new Ray(gameCamera.transform.position, direction);
        }
        else
        {
            // 透视相机：从相机位置射向目标位置
            Vector3 direction = (targetWorldPos - gameCamera.transform.position);
            return new Ray(gameCamera.transform.position, direction);
        }
    }

    // 处理击中目标
    private void HandleHit(RaycastHit2D hit)
    {
        // 检查是否是开始按钮
        StartButton startButton = hit.collider.GetComponent<StartButton>();
        if (startButton != null && !startButton.IsActivated)
        {
            startButton.OnShot();
            return;
        }

        EnemyBodyPart bodyPart = hit.collider.GetComponent<EnemyBodyPart>();
        if (bodyPart != null && !bodyPart.isBoss)
        {
            EnemyWithNewAttackSystem enemy = bodyPart.GetParentEnemy().GetComponent<EnemyWithNewAttackSystem>();
            BodyPart hitBodyPart = bodyPart.GetBodyPartData();

            if (enemy != null && !enemy.IsDead)
            {
                // 对敌人造成伤害
                enemy.TakeDamage(damage, hitBodyPart);

                Debug.Log($"Hit {enemy.name} in {hitBodyPart.partName} for {damage} damage!");
            }
        }
        else if (bodyPart != null && bodyPart.isBoss)
        {
            HandleBossHit(bodyPart, hit);
        }
    }

    private void HandleBossHit(EnemyBodyPart bodyPart, RaycastHit2D hit)
    {
        // 获取身体部位数据
        BodyPart hitBodyPart = bodyPart.GetBodyPartData();
        if (hitBodyPart != null)
        {
            // 通过身体部位找到父级Boss
            var parentEntity = bodyPart.GetParentEnemy();
            if (parentEntity != null)
            {
                // 尝试获取BossController组件
                BossController boss = parentEntity.GetComponent<BossController>();
                if (boss != null && !boss.IsDead)
                {
                    // 对Boss造成部位伤害
                    boss.TakeDamage(damage, hitBodyPart);

                    Debug.Log($"Hit Boss {boss.name} in {hitBodyPart.partName} for {damage} damage!");
                }
                else
                {
                    Debug.LogWarning($"Boss component not found or Boss is dead on {parentEntity.name}");
                }
            }
            else
            {
                Debug.LogWarning($"Parent entity not found for body part {bodyPart.name}");
            }
        }
        else
        {
            Debug.LogWarning($"Body part data not found for {bodyPart.name}");
        }
    }

    // 射击冷却协程
    private IEnumerator ShootCooldown(float delay)
    {
        canShoot = false;
        yield return new WaitForSeconds(delay);
        canShoot = true;
    }
}
*/

public class crosshairTarget : MonoBehaviour
{
    // 碰撞体组件引用
    private Collider2D targetCollider;

    // 伤害值
    public int damage = 10;

    // 是否可以造成伤害
    private bool canDealDamage = false;

    // ==========================================
    // 【弹道特效】
    // ==========================================
    [Header("视觉表现")]
    [Tooltip("拖入挂载了 TrajectoryLine 脚本的物体")]
    public TrajectoryLine trajectoryEffect;

    // ==========================================
    // 【连击与评价系统】
    // ==========================================
    [Header("连击反馈设置")]
    [Tooltip("连击允许的最大间隔时间（秒），超过则连击中断")]
    public float comboResetTime = 2.0f;

    // 记录连续命中次数 (int)
    private int consecutiveHits = 0;

    // 记录距离上次命中的时间 (float)
    private float timeSinceLastHit = 0f;

    // ==========================================
    // 【UI 设置】
    // ==========================================
    [Header("UI 设置")]
    [Tooltip("拖入屏幕上的 Text 文本物体")]
    public Text comboTextUI;

    [Tooltip("文字在屏幕上停留的时间")]
    public float textDisplayTime = 1.5f;

    void Start()
    {
        // 获取碰撞体组件
        targetCollider = GetComponent<Collider2D>();

        // 确保碰撞体是触发器
        if (targetCollider != null)
        {
            targetCollider.isTrigger = true;
            // 初始状态下禁用碰撞体
            targetCollider.enabled = false;
        }

        // 游戏开始时，清空屏幕上的字
        if (comboTextUI != null)
        {
            comboTextUI.text = "";
        }
    }

    void Update()
    {
        // 只要连击数大于0，就开始计时
        if (consecutiveHits > 0)
        {
            timeSinceLastHit += Time.deltaTime; // 累加时间

            // 如果距离上次命中的时间超过了允许的最大间隔，连击清零
            if (timeSinceLastHit > comboResetTime)
            {
                consecutiveHits = 0;
            }
        }
    }

    // 当射击手势触发时调用此方法 (开火)
    public void ActivateCollider()
    {
        if (targetCollider != null)
        {
            targetCollider.enabled = true;
            canDealDamage = true;

            // 调用画线脚本显示弹道
            if (trajectoryEffect != null)
            {
                trajectoryEffect.ShowShootLine();
            }

            // 检查并消耗霰弹弹药
            if (PowerUpManager.Instance != null && PowerUpManager.Instance.HasShotgunEffect())
            {
                PowerUpManager.Instance.ConsumeShotgunAmmo();
            }

            // 短暂延迟后自动禁用碰撞体（模拟单次射击）
            StartCoroutine(DeactivateColliderAfterDelay(0.1f));
        }
    }

    // 延迟禁用碰撞体的协程
    private IEnumerator DeactivateColliderAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        targetCollider.enabled = false;
        canDealDamage = false;
    }

    // 碰撞检测
    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("triggerEnter!!!");

        if (!canDealDamage) return;

        // 检查是否是开始按钮
        StartButton startButton = other.GetComponent<StartButton>();
        if (startButton != null && !startButton.IsActivated)
        {
            startButton.OnShot();
            return;
        }

        EnemyBodyPart bodyPart = other.GetComponent<EnemyBodyPart>();
        if (bodyPart != null && !bodyPart.isBoss)
        {
            EnemyWithNewAttackSystem enemy = bodyPart.GetParentEnemy().GetComponent<EnemyWithNewAttackSystem>();
            BodyPart hitBodyPart = bodyPart.GetBodyPartData();

            if (enemy != null && !enemy.IsDead)
            {
                // 对敌人造成伤害
                enemy.TakeDamage(damage, hitBodyPart);
                Debug.Log($"Hit {enemy.name} in {hitBodyPart.partName} for {damage} damage!");

                // 打中普通敌人，记录一次成功命中！
                RegisterSuccessfulHit();
            }
        }
        else if (bodyPart != null && bodyPart.isBoss)
        {
            HandleBossHit(bodyPart, other);
        }

        /*
        // 获取所有在触发器范围内的碰撞体
        List<Collider2D> overlappingColliders = GetOverlappingColliders();

        // 从所有碰撞体中选择目标
        EnemyBodyPart targetBodyPart = SelectTargetBodyPart(overlappingColliders);

        if (targetBodyPart != null)
        {
            // 对选中的目标造成伤害
            ApplyDamageToTarget(targetBodyPart);
        }
        不会写这一部分 操
        */
    }

    /// <summary>
    /// 获取所有与触发器重叠的碰撞体
    /// </summary>
    private List<Collider2D> GetOverlappingColliders()
    {
        List<Collider2D> overlappingColliders = new List<Collider2D>();

        // 获取触发器碰撞体
        Collider2D triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider == null) return overlappingColliders;

        // 使用OverlapCollider获取所有重叠的碰撞体
        ContactFilter2D contactFilter = new ContactFilter2D();
        contactFilter.useTriggers = true;
        contactFilter.useLayerMask = true;
        contactFilter.layerMask = -1; // 所有层级

        triggerCollider.OverlapCollider(contactFilter, overlappingColliders);

        if (overlappingColliders.Count == 0)
            Debug.Log("overlappingColliders == 0");

        return overlappingColliders;
    }

    /// <summary>
    /// 根据遮挡顺序和距离选择目标身体部位
    /// </summary>
    private EnemyBodyPart SelectTargetBodyPart(List<Collider2D> colliders)
    {
        List<EnemyBodyPart> validBodyParts = new List<EnemyBodyPart>();

        // 筛选出有效的EnemyBodyPart
        foreach (Collider2D collider in colliders)
        {
            EnemyBodyPart bodyPart = collider.GetComponent<EnemyBodyPart>();
            if (bodyPart != null && !bodyPart.IsPartDestroyed())
            {
                validBodyParts.Add(bodyPart);
            }
        }

        if (validBodyParts.Count == 0) return null;

        // 按遮挡顺序分组
        Dictionary<int, List<EnemyBodyPart>> occlusionGroups = new Dictionary<int, List<EnemyBodyPart>>();

        foreach (EnemyBodyPart bodyPart in validBodyParts)
        {
            int occlusionOrder = bodyPart.GetBodyPartData().occlusionOrder;

            if (!occlusionGroups.ContainsKey(occlusionOrder))
            {
                occlusionGroups[occlusionOrder] = new List<EnemyBodyPart>();
            }

            occlusionGroups[occlusionOrder].Add(bodyPart);
        }

        // 找到最高遮挡顺序
        int highestOcclusionOrder = int.MinValue;
        foreach (int order in occlusionGroups.Keys)
        {
            if (order > highestOcclusionOrder)
            {
                highestOcclusionOrder = order;
            }
        }

        // 获取最高遮挡顺序的身体部位列表
        List<EnemyBodyPart> highestOcclusionGroup = occlusionGroups[highestOcclusionOrder];

        // 在最高遮挡顺序组中选择距离最近的身体部位
        EnemyBodyPart closestBodyPart = null;
        float closestDistance = float.MaxValue;
        Vector3 crosshairCenter = transform.position;

        foreach (EnemyBodyPart bodyPart in highestOcclusionGroup)
        {
            float distance = Vector3.Distance(crosshairCenter, bodyPart.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestBodyPart = bodyPart;
            }
        }

        return closestBodyPart;
    }

    /// <summary>
    /// 对目标身体部位造成伤害
    /// </summary>
    private void ApplyDamageToTarget(EnemyBodyPart targetBodyPart)
    {
        if (targetBodyPart.isBoss)
        {
            HandleBossHit(targetBodyPart, targetBodyPart.GetComponent<Collider2D>());
        }
        else
        {
            EnemyWithNewAttackSystem enemy = targetBodyPart.GetParentEnemy().GetComponent<EnemyWithNewAttackSystem>();
            BodyPart hitBodyPart = targetBodyPart.GetBodyPartData();

            if (enemy != null && !enemy.IsDead)
            {
                // 对敌人造成伤害
                enemy.TakeDamage(damage, hitBodyPart);

                Debug.Log($"Hit {enemy.name} in {hitBodyPart.partName} for {damage} damage! (Occlusion Order: {hitBodyPart.occlusionOrder})");
            }
        }
    }

    private void HandleBossHit(EnemyBodyPart bodyPart, Collider2D other)
    {
        // 获取身体部位数据
        BodyPart hitBodyPart = bodyPart.GetBodyPartData();
        if (hitBodyPart != null)
        {
            // 通过身体部位找到父级Boss
            var parentEntity = bodyPart.GetParentEnemy();
            if (parentEntity != null)
            {
                // 尝试获取BossController组件
                BossController boss = parentEntity.GetComponent<BossController>();
                if (boss != null && !boss.IsDead)
                {
                    // 对Boss造成部位伤害
                    boss.TakeDamage(damage, hitBodyPart);
                    Debug.Log($"Hit Boss {boss.name} in {hitBodyPart.partName} for {damage} damage!");

                    // 打中Boss，同样记录一次成功命中！
                    RegisterSuccessfulHit();
                }
                else
                {
                    Debug.LogWarning($"Boss component not found or Boss is dead on {parentEntity.name}");
                }
            }
            else
            {
                Debug.LogWarning($"Parent entity not found for body part {bodyPart.name}");
            }
        }
        else
        {
            Debug.LogWarning($"Body part data not found for {bodyPart.name}");
        }
    }

    // ==========================================
    // 【处理连击和评价UI的核心方法】
    // ==========================================
    private void RegisterSuccessfulHit()
    {
        consecutiveHits++;       // 连击数 +1
        timeSinceLastHit = 0f;   // 每次打中重置超时计时器

        // 根据当前的连击数，给予不同的评价反馈
        if (consecutiveHits == 3)
        {
            ShowComboUI("GOOD! (3连击)");
        }
        else if (consecutiveHits == 5)
        {
            ShowComboUI("GREAT!! (5连击)");
        }
        else if (consecutiveHits >= 10 && consecutiveHits % 5 == 0)
        {
            // 达到 10、15、20 连击时触发
            ShowComboUI($"EXCELLENT!!! ({consecutiveHits}连击)");
        }
    }

    // 把文字推送到屏幕上的方法
    private void ShowComboUI(string message)
    {
        if (comboTextUI != null)
        {
            comboTextUI.text = message; // 修改屏幕上的字

            // 每次出新字时，重新开始倒计时（防止字还没看完就消失）
            StopCoroutine("HideTextRoutine");
            StartCoroutine("HideTextRoutine");
        }
        else
        {
            Debug.Log("当前连击：" + message + " (未绑定UI，仅在控制台显示)");
        }
    }

    // 几秒钟后自动把字隐藏的协程
    private IEnumerator HideTextRoutine()
    {
        yield return new WaitForSeconds(textDisplayTime);
        if (comboTextUI != null)
        {
            comboTextUI.text = ""; // 清空文字
        }
    }
}