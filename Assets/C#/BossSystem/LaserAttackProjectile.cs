using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 激光攻击投射物组件
/// 用于TestBoss3的Attack3激光攻击
/// </summary>
[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class LaserAttackProjectile : MonoBehaviour
{
    [Header("激光伤害设置")]
    [SerializeField] private float damage = 30f;
    //[SerializeField] private LayerMask playerLayerMask = -1;
    
    [Header("激光旋转设置")]
    [SerializeField] private Transform rotationPivot; // 旋转原点，如果为null则使用自身
    [SerializeField] private float startAngle = 0f; // 起始角度
    [SerializeField] private float endAngle = 180f; // 结束角度
    [SerializeField] private float rotationSpeed = 90f; // 旋转速度（度/秒）
    [SerializeField] private float rotationDuration = 3f; // 旋转持续时间
    [SerializeField] private bool useFixedDuration = true; // 是否使用固定持续时间而非速度
    
    [Header("激光生命周期")]
    [SerializeField] private float warmUpTime = 0.5f; // 预热时间（激光出现但不造成伤害）
    [SerializeField] private float coolDownTime = 0.5f; // 冷却时间（激光消失前的时间）
    [SerializeField] private bool autoDestroy = true; // 是否自动销毁
    
    [Header("视觉效果")]
    [SerializeField] private Color warmUpColor = Color.yellow; // 预热时的颜色
    [SerializeField] private Color activeColor = Color.red; // 激活时的颜色
    [SerializeField] private Color coolDownColor = Color.gray; // 冷却时的颜色
    [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // 透明度曲线
    
    [Header("调试显示")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private float gizmoLength = 5f; // 调试线条长度
    [SerializeField] private Color startAngleColor = Color.green;
    [SerializeField] private Color endAngleColor = Color.red;
    
    // 组件引用
    private SpriteRenderer spriteRenderer;
    private Collider2D laserCollider;
    
    // 状态变量
    private bool isActive = false;
    private bool canDamage = false;
    private float currentAngle;
    private float rotationTimer = 0f;
    private float totalLifeTime;
    private float lifeTimer = 0f;
    private float initialDistanceFromPivot; // 激光与锚点的初始距离
    
    // 伤害跟踪 - 基于时间的伤害检测
    private Dictionary<Collider2D, float> lastDamageTime = new Dictionary<Collider2D, float>();
    private float damageCooldown = 0.5f; // 0.5秒伤害冷却时间
    
    // 原始颜色
    private Color originalColor;
    
    // 事件
    public System.Action<LaserAttackProjectile> OnLaserCompleted;
    public System.Action<LaserAttackProjectile, Collider2D> OnPlayerHit;

    private void Awake()
    {
        // 获取组件引用
        spriteRenderer = GetComponent<SpriteRenderer>();
        laserCollider = GetComponent<Collider2D>();
        
        // 保存原始颜色
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        // 如果没有设置旋转原点，使用自身
        if (rotationPivot == null)
        {
            rotationPivot = transform;
        }
        
        // 计算激光与锚点的初始距离
        if (rotationPivot != transform)
        {
            initialDistanceFromPivot = Vector3.Distance(transform.position, rotationPivot.position);
        }
        else
        {
            initialDistanceFromPivot = 0f; // 自身旋转时距离为0
        }
        
        // 计算总生命时间
        totalLifeTime = warmUpTime + rotationDuration + coolDownTime;
        
        // 初始设置
        currentAngle = startAngle;
        SetRotation(currentAngle);
        
        // 初始状态设置为不可见
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 0f;
            spriteRenderer.color = color;
        }
        
        // 初始禁用碰撞器
        if (laserCollider != null)
        {
            laserCollider.enabled = false;
        }
    }

    private void Start()
    {
        // 开始激光序列
        StartLaser();
    }

    private void Update()
    {
        if (!isActive) return;
        
        lifeTimer += Time.deltaTime;
        
        // 更新激光状态
        UpdateLaserState();
        
        // 更新旋转
        UpdateRotation();
        
        // 更新视觉效果
        UpdateVisualEffects();
        
        // 检查是否完成
        if (lifeTimer >= totalLifeTime)
        {
            CompleteLaser();
        }
    }

    /// <summary>
    /// 启动激光
    /// </summary>
    public void StartLaser()
    {
        isActive = true;
        lifeTimer = 0f;
        rotationTimer = 0f;
        currentAngle = startAngle;
        lastDamageTime.Clear();
        
        SetRotation(currentAngle);
        
        Debug.Log($"Laser started: Start={startAngle}°, End={endAngle}°, Duration={rotationDuration}s");
    }

    /// <summary>
    /// 停止激光
    /// </summary>
    public void StopLaser()
    {
        isActive = false;
        canDamage = false;
        
        if (laserCollider != null)
        {
            laserCollider.enabled = false;
        }
    }

    /// <summary>
    /// 完成激光攻击
    /// </summary>
    private void CompleteLaser()
    {
        StopLaser();
        
        OnLaserCompleted?.Invoke(this);
        
        if (autoDestroy)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 更新激光状态
    /// </summary>
    private void UpdateLaserState()
    {
        if (lifeTimer <= warmUpTime)
        {
            // 预热阶段
            canDamage = false;
            if (laserCollider != null)
            {
                laserCollider.enabled = false;
            }
        }
        else if (lifeTimer <= warmUpTime + rotationDuration)
        {
            // 激活阶段
            canDamage = true;
            if (laserCollider != null)
            {
                laserCollider.enabled = true;
            }
        }
        else
        {
            // 冷却阶段
            canDamage = false;
            if (laserCollider != null)
            {
                laserCollider.enabled = false;
            }
        }
    }

    /// <summary>
    /// 更新旋转
    /// </summary>
    private void UpdateRotation()
    {
        if (lifeTimer > warmUpTime && lifeTimer <= warmUpTime + rotationDuration)
        {
            rotationTimer += Time.deltaTime;
            
            // 计算旋转进度
            float progress = rotationTimer / rotationDuration;
            progress = Mathf.Clamp01(progress);
            
            // 计算当前角度
            currentAngle = Mathf.Lerp(startAngle, endAngle, progress);
            
            // 应用旋转
            SetRotation(currentAngle);
        }
    }

    /// <summary>
    /// 设置旋转角度
    /// </summary>
    /// <param name="angle">角度</param>
    private void SetRotation(float angle)
    {
        if (rotationPivot != null)
        {
            // 如果旋转原点是自身，直接旋转
            if (rotationPivot == transform)
            {
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
            else
            {
                // 如果旋转原点是其他物体，围绕该点旋转
                // 将角度转换为弧度
                float angleRad = angle * Mathf.Deg2Rad;
                
                // 使用预先计算的距离计算新的位置
                Vector3 newPosition = rotationPivot.position + new Vector3(
                    Mathf.Cos(angleRad) * initialDistanceFromPivot,
                    Mathf.Sin(angleRad) * initialDistanceFromPivot,
                    0f
                );
                
                // 设置位置和旋转
                transform.position = newPosition;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }
    }

    /// <summary>
    /// 更新视觉效果
    /// </summary>
    private void UpdateVisualEffects()
    {
        if (spriteRenderer == null) return;
        
        Color targetColor;
        float alpha = 1f;
        
        // 根据阶段设置颜色
        if (lifeTimer <= warmUpTime)
        {
            // 预热阶段
            targetColor = warmUpColor;
            float progress = lifeTimer / warmUpTime;
            alpha = alphaCurve.Evaluate(progress) * 0.7f; // 预热时透明度较低
        }
        else if (lifeTimer <= warmUpTime + rotationDuration)
        {
            // 激活阶段
            targetColor = activeColor;
            alpha = 1f;
        }
        else
        {
            // 冷却阶段
            targetColor = coolDownColor;
            float progress = (lifeTimer - warmUpTime - rotationDuration) / coolDownTime;
            alpha = alphaCurve.Evaluate(1f - progress) * 0.5f; // 冷却时逐渐透明
        }
        
        // 应用颜色
        targetColor.a = alpha;
        spriteRenderer.color = targetColor;
    }

    /// <summary>
    /// 碰撞检测
    /// </summary>
    /// <param name="other">碰撞的对象</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        //if (!canDamage) return;
        
        // 检查是否是玩家
        if (other.CompareTag("Player"))
        {
            Debug.Log("laser is player");
            // 检查是否已经在0.5s内对该目标造成伤害
            float currentTime = Time.time;
            if (!lastDamageTime.ContainsKey(other) || currentTime - lastDamageTime[other] >= damageCooldown)
            {
                Debug.Log("laser is player and can damage");
                // 更新伤害时间
                lastDamageTime[other] = currentTime;
                
                // 对玩家造成伤害
                Player player = other.GetComponent<Player>();
                if (player != null && !player.IsDead)
                {
                    player.TakeDamage(damage);
                    
                    OnPlayerHit?.Invoke(this, other);
                    
                    Debug.Log($"Laser hit player for {damage} damage!");
                }
            }
        }
    }

    /// <summary>
    /// 设置激光参数
    /// </summary>
    /// <param name="startAngle">起始角度</param>
    /// <param name="endAngle">结束角度</param>
    /// <param name="duration">旋转持续时间</param>
    /// <param name="damage">伤害值</param>
    public void SetLaserParameters(float startAngle, float endAngle, float duration, float damage = -1f)
    {
        this.startAngle = startAngle;
        this.endAngle = endAngle;
        this.rotationDuration = duration;
        
        if (damage > 0f)
        {
            this.damage = damage;
        }
        
        // 重新计算总生命时间
        totalLifeTime = warmUpTime + rotationDuration + coolDownTime;
        
        Debug.Log($"Laser parameters set: Start={startAngle}°, End={endAngle}°, Duration={duration}s, Damage={this.damage}");
    }

    /// <summary>
    /// 设置旋转原点
    /// </summary>
    /// <param name="pivot">旋转原点</param>
    public void SetRotationPivot(Transform pivot)
    {
        rotationPivot = pivot;
        
        // 重新计算距离
        if (rotationPivot != null && rotationPivot != transform)
        {
            initialDistanceFromPivot = Vector3.Distance(transform.position, rotationPivot.position);
        }
        else
        {
            initialDistanceFromPivot = 0f;
        }
    }

    /// <summary>
    /// 获取当前旋转进度
    /// </summary>
    /// <returns>旋转进度（0-1）</returns>
    public float GetRotationProgress()
    {
        if (rotationDuration <= 0f) return 1f;
        return Mathf.Clamp01(rotationTimer / rotationDuration);
    }

    /// <summary>
    /// 获取当前角度
    /// </summary>
    /// <returns>当前角度</returns>
    public float GetCurrentAngle()
    {
        return currentAngle;
    }

    /// <summary>
    /// 调试绘制
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        Vector3 pivotPos = (rotationPivot != null) ? rotationPivot.position : transform.position;
        
        // 绘制旋转原点
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(pivotPos, 0.2f);
        
        // 绘制起始角度线
        Gizmos.color = startAngleColor;
        Vector3 startDir = new Vector3(Mathf.Cos(startAngle * Mathf.Deg2Rad), Mathf.Sin(startAngle * Mathf.Deg2Rad), 0f);
        Gizmos.DrawLine(pivotPos, pivotPos + startDir * gizmoLength);
        
        // 绘制结束角度线
        Gizmos.color = endAngleColor;
        Vector3 endDir = new Vector3(Mathf.Cos(endAngle * Mathf.Deg2Rad), Mathf.Sin(endAngle * Mathf.Deg2Rad), 0f);
        Gizmos.DrawLine(pivotPos, pivotPos + endDir * gizmoLength);
        
        // 绘制旋转弧线
        Gizmos.color = Color.yellow;
        float arcAngle = Mathf.Abs(endAngle - startAngle);
        int segments = Mathf.Max(8, (int)(arcAngle / 10f));
        
        for (int i = 0; i < segments; i++)
        {
            float t1 = (float)i / segments;
            float t2 = (float)(i + 1) / segments;
            
            float angle1 = Mathf.Lerp(startAngle, endAngle, t1);
            float angle2 = Mathf.Lerp(startAngle, endAngle, t2);
            
            Vector3 dir1 = new Vector3(Mathf.Cos(angle1 * Mathf.Deg2Rad), Mathf.Sin(angle1 * Mathf.Deg2Rad), 0f);
            Vector3 dir2 = new Vector3(Mathf.Cos(angle2 * Mathf.Deg2Rad), Mathf.Sin(angle2 * Mathf.Deg2Rad), 0f);
            
            Gizmos.DrawLine(pivotPos + dir1 * gizmoLength * 0.8f, pivotPos + dir2 * gizmoLength * 0.8f);
        }
        
        // 绘制当前角度（运行时）
        if (Application.isPlaying && isActive)
        {
            Gizmos.color = Color.cyan;
            Vector3 currentDir = new Vector3(Mathf.Cos(currentAngle * Mathf.Deg2Rad), Mathf.Sin(currentAngle * Mathf.Deg2Rad), 0f);
            Gizmos.DrawLine(pivotPos, pivotPos + currentDir * gizmoLength * 1.2f);
        }
    }

    /// <summary>
    /// 编辑器中的调试信息
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;
        
        // 显示更详细的信息
        Vector3 pivotPos = (rotationPivot != null) ? rotationPivot.position : transform.position;
        
        // 绘制激光范围
        if (spriteRenderer != null)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, spriteRenderer.bounds.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器中验证参数
    /// </summary>
    private void OnValidate()
    {
        // 确保角度在合理范围内
        startAngle = startAngle % 360f;
        endAngle = endAngle % 360f;
        
        // 确保持续时间为正数
        rotationDuration = Mathf.Max(0.1f, rotationDuration);
        warmUpTime = Mathf.Max(0f, warmUpTime);
        coolDownTime = Mathf.Max(0f, coolDownTime);
        
        // 确保伤害为正数
        damage = Mathf.Max(0f, damage);
        
        // 重新计算总生命时间
        totalLifeTime = warmUpTime + rotationDuration + coolDownTime;
    }
#endif
}
