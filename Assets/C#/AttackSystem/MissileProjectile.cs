using UnityEngine;
using System.Collections;

/// <summary>
/// 导弹投射物组件
/// 处理导弹的飞行、轨迹计算和碰撞检测
/// </summary>
public class MissileProjectile : MonoBehaviour
{
    [Header("Missile Properties")]
    [SerializeField] private float speed = 10f;               // 飞行速度
    [SerializeField] private float lifetime = 5f;             // 生存时间
    [SerializeField] private float curveHeight = 3f;          // 曲线高度
    [SerializeField] private float damageRadius = 1.5f;       // 伤害半径
    [SerializeField] private float damage = 50f;              // 伤害值
    [SerializeField] private bool useParabolic = true;        // 是否使用抛物线轨迹
    
    [Header("Detection Settings")]
    [SerializeField] private LayerMask playerLayerMask = -1;  // 玩家层级掩码
    [SerializeField] private float detectionRadius = 0.5f;    // 检测半径
    
    [Header("Visual Settings")]
    [SerializeField] private bool showTrail = true;           // 是否显示拖尾
    [SerializeField] private float trailLifetime = 0.5f;      // 拖尾持续时间
    
    // 运行时变量
    private Vector3 startPosition;                            // 起始位置
    private Vector3 targetPosition;                           // 目标位置
    private float journeyTime = 0f;                          // 飞行时间
    private float totalJourneyTime;                          // 总飞行时间
    private AnimationCurve trajectoryCurve;                  // 轨迹曲线
    private bool hasImpacted = false;                        // 是否已经撞击
    private TrailRenderer trailRenderer;                     // 拖尾渲染器
    private Collider2D missileCollider;                      // 导弹碰撞体
    
    // 事件
    public System.Action<Vector3, bool> OnMissileImpact;     // 撞击事件（位置，是否击中玩家）
    
    // 爆炸效果管理
    private GameObject currentExplosionEffect;
    
    private void Awake()
    {
        // 设置碰撞体
        SetupCollider();
        
        // 设置拖尾效果
        if (showTrail)
        {
            SetupTrail();
        }
    }
    
    private void Start()
    {
        // 开始生命周期倒计时
        StartCoroutine(LifetimeCountdown());
    }
    
    private void Update()
    {
        if (!hasImpacted)
        {
            // 更新导弹位置
            UpdatePosition();
            
            // 检测碰撞
            CheckCollisions();
        }
    }
    
    /// <summary>
    /// 初始化导弹
    /// </summary>
    /// <param name="start">起始位置</param>
    /// <param name="target">目标位置</param>
    /// <param name="missileSpeed">飞行速度</param>
    /// <param name="heightCurve">曲线高度</param>
    /// <param name="maxLifetime">最大生存时间</param>
    /// <param name="explosionRadius">爆炸半径</param>
    /// <param name="missileDamage">导弹伤害</param>
    /// <param name="useParabolicTrajectory">是否使用抛物线轨迹</param>
    /// <param name="curve">轨迹动画曲线</param>
    public void Initialize(Vector3 start, Vector3 target, float missileSpeed, float heightCurve, 
                          float maxLifetime, float explosionRadius, float missileDamage,
                          bool useParabolicTrajectory, AnimationCurve curve)
    {
        startPosition = start;
        targetPosition = target;
        speed = missileSpeed;
        curveHeight = heightCurve;
        lifetime = maxLifetime;
        damageRadius = explosionRadius;
        damage = missileDamage;
        useParabolic = useParabolicTrajectory;
        trajectoryCurve = curve ?? AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        // 计算总飞行时间
        float distance = Vector3.Distance(startPosition, targetPosition);
        totalJourneyTime = distance / speed;
        
        // 设置初始位置
        transform.position = startPosition;
        
        // 设置初始朝向
        Vector3 direction = (targetPosition - startPosition).normalized;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
        
        Debug.Log($"Missile initialized: {startPosition} -> {targetPosition}, Speed: {speed}, Time: {totalJourneyTime}");
    }
    
    /// <summary>
    /// 更新导弹位置
    /// </summary>
    private void UpdatePosition()
    {
        journeyTime += Time.deltaTime;
        float journeyProgress = journeyTime / totalJourneyTime;
        
        if (journeyProgress >= 1f)
        {
            // 到达目标位置
            transform.position = targetPosition;
            Impact(targetPosition);
            return;
        }
        
        // 根据轨迹类型计算新位置
        Vector3 newPosition;
        if (useParabolic)
        {
            newPosition = CalculateParabolicPosition(journeyProgress);
        }
        else
        {
            newPosition = Vector3.Lerp(startPosition, targetPosition, journeyProgress);
        }
        
        // 更新朝向
        Vector3 direction = (newPosition - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
        
        transform.position = newPosition;
    }
    
    /// <summary>
    /// 计算抛物线位置
    /// </summary>
    /// <param name="t">进度参数（0-1）</param>
    /// <returns>计算出的位置</returns>
    private Vector3 CalculateParabolicPosition(float t)
    {
        // 基础线性插值
        Vector3 linearPosition = Vector3.Lerp(startPosition, targetPosition, t);
        
        // 添加高度曲线
        float heightOffset = trajectoryCurve.Evaluate(t) * curveHeight;
        linearPosition.y += heightOffset;
        
        return linearPosition;
    }
    
    /// <summary>
    /// 检测碰撞
    /// </summary>
    private void CheckCollisions()
    {
        // 使用OverlapCircle检测碰撞
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, detectionRadius, playerLayerMask);
        
        foreach (var collider in colliders)
        {
            if (collider.CompareTag("Player"))
            {
                // 撞击到玩家
                Impact(transform.position, true);
                return;
            }
        }
        
        // 也可以添加与地面或其他障碍物的碰撞检测
        // 这里暂时省略，可以根据需要添加
    }
    
    /// <summary>
    /// 导弹撞击
    /// </summary>
    /// <param name="impactPosition">撞击位置</param>
    /// <param name="hitPlayer">是否击中玩家</param>
    private void Impact(Vector3 impactPosition, bool hitPlayer = false)
    {
        if (hasImpacted) return;
        
        hasImpacted = true;
        
        // 执行范围伤害检测
        if (hitPlayer || damageRadius > 0)
        {
            bool playerHit = PerformAreaDamage(impactPosition);
            hitPlayer = hitPlayer || playerHit;
        }
        
        // 触发撞击事件
        OnMissileImpact?.Invoke(impactPosition, hitPlayer);
        
        // 销毁导弹
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 执行范围伤害
    /// </summary>
    /// <param name="center">爆炸中心</param>
    /// <returns>是否击中玩家</returns>
    private bool PerformAreaDamage(Vector3 center)
    {
        bool hitPlayer = false;
        
        // 检测爆炸范围内的所有玩家
        Collider2D[] colliders = Physics2D.OverlapCircleAll(center, damageRadius, playerLayerMask);
        
        foreach (var collider in colliders)
        {
            if (collider.CompareTag("Player"))
            {
                Player player = collider.GetComponent<Player>();
                if (player != null && !player.IsDead)
                {
                    // 计算距离衰减的伤害
                    float distance = Vector3.Distance(center, collider.transform.position);
                    float damageMultiplier = 1f - (distance / damageRadius);
                    float finalDamage = damage * damageMultiplier;
                    
                    player.TakeDamage(finalDamage);
                    hitPlayer = true;
                    
                    Debug.Log($"Missile hit player with {finalDamage} damage (distance: {distance}, multiplier: {damageMultiplier})");
                }
            }
        }
        
        return hitPlayer;
    }
    
    /// <summary>
    /// 强制撞击（生命周期结束时调用）
    /// </summary>
    public void ForceImpact()
    {
        if (!hasImpacted)
        {
            Impact(transform.position);
        }
    }
    
    /// <summary>
    /// 清理爆炸效果
    /// </summary>
    private void CleanupExplosion()
    {
        if (currentExplosionEffect != null)
        {
            Destroy(currentExplosionEffect);
            currentExplosionEffect = null;
        }
    }
    
    /// <summary>
    /// 中断导弹并清理所有效果
    /// </summary>
    public void InterruptMissile()
    {
        if (!hasImpacted)
        {
            hasImpacted = true;
            CleanupExplosion();
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 生命周期倒计时
    /// </summary>
    /// <returns></returns>
    private IEnumerator LifetimeCountdown()
    {
        yield return new WaitForSeconds(lifetime);
        
        if (!hasImpacted)
        {
            Debug.Log("Missile reached lifetime limit, forcing impact");
            ForceImpact();
        }
    }
    
    /// <summary>
    /// 设置碰撞体
    /// </summary>
    private void SetupCollider()
    {
        // 添加触发器碰撞体用于检测
        missileCollider = gameObject.GetComponent<Collider2D>();
        if (missileCollider == null)
        {
            CircleCollider2D circleCollider = gameObject.AddComponent<CircleCollider2D>();
            circleCollider.radius = detectionRadius;
            circleCollider.isTrigger = true;
            missileCollider = circleCollider;
        }
    }
    
    /// <summary>
    /// 设置拖尾效果
    /// </summary>
    private void SetupTrail()
    {
        trailRenderer = gameObject.GetComponent<TrailRenderer>();
        if (trailRenderer == null)
        {
            trailRenderer = gameObject.AddComponent<TrailRenderer>();
        }
        
        // 配置拖尾
        trailRenderer.time = trailLifetime;
        trailRenderer.startWidth = 0.3f;
        trailRenderer.endWidth = 0.1f;
        trailRenderer.material = CreateTrailMaterial();
        trailRenderer.sortingOrder = 1;
    }
    
    /// <summary>
    /// 创建拖尾材质
    /// </summary>
    /// <returns>拖尾材质</returns>
    private Material CreateTrailMaterial()
    {
        // 创建简单的拖尾材质
        Material trailMaterial = new Material(Shader.Find("Sprites/Default"));
        trailMaterial.color = new Color(1f, 0.5f, 0f, 0.8f); // 橙色半透明
        return trailMaterial;
    }
    
    /// <summary>
    /// 触发器进入事件
    /// </summary>
    /// <param name="other">其他碰撞体</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasImpacted) return;
        
        if (other.CompareTag("Player"))
        {
            Impact(transform.position, true);
        }
        // 可以添加与其他物体的碰撞逻辑
    }
    
    // 编辑器辅助方法
    private void OnDrawGizmosSelected()
    {
        // 显示检测半径
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        // 显示爆炸半径
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
        
        // 显示飞行轨迹（如果已初始化）
        if (Application.isPlaying && !hasImpacted)
        {
            Gizmos.color = Color.green;
            if (useParabolic && totalJourneyTime > 0)
            {
                // 绘制剩余轨迹
                Vector3 currentPos = transform.position;
                for (int i = 0; i < 10; i++)
                {
                    float futureProgress = (journeyTime + i * 0.1f) / totalJourneyTime;
                    if (futureProgress > 1f) break;
                    
                    Vector3 futurePos = CalculateParabolicPosition(futureProgress);
                    Gizmos.DrawLine(currentPos, futurePos);
                    currentPos = futurePos;
                }
            }
            else
            {
                Gizmos.DrawLine(transform.position, targetPosition);
            }
        }
    }
}
