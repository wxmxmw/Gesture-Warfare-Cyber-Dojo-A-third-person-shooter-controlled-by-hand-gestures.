using UnityEngine;
using System.Collections;

/// <summary>
/// 导弹定点攻击系统
/// 从敌人位置向目标位置发射导弹，导弹沿曲线路径飞行并进行碰撞检测
/// </summary>
public class MissileTargetedAttack : TargetedAttackBase
{
    [Header("Missile Attack Settings")]
    [SerializeField] private float missileSpeed = 10f;          // 导弹飞行速度
    [SerializeField] private float missileLifetime = 5f;       // 导弹最大生存时间
    [SerializeField] private float curveHeight = 3f;           // 曲线飞行的高度
    [SerializeField] private float damageRadius = 1.5f;        // 爆炸伤害半径
    
    [Header("Missile Prefab Settings")]
    [SerializeField] private GameObject missilePrefab;          // 导弹预制体
    [SerializeField] private GameObject explosionEffectPrefab; // 爆炸特效预制体
    [SerializeField] private float explosionEffectDuration = 2f; // 爆炸特效持续时间
    
    [Header("Trajectory Settings")]
    [SerializeField] private AnimationCurve trajectoryCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 轨迹曲线
    [SerializeField] private bool useParabolicTrajectory = true; // 是否使用抛物线轨迹
    
    // 运行时变量
    private GameObject currentMissile;                          // 当前发射的导弹
    private Vector3 launchPosition;                            // 发射位置
    private bool missileImpacted = false;                      // 导弹是否已经爆炸
    
    protected override void OnPreparationStart()
    {
        base.OnPreparationStart();
        
        // 记录发射位置
        launchPosition = transform.position;
        
        // 显示准备阶段效果（可以是瞄准线、充能效果等）
        ShowPreparationEffect();
    }
    
    protected override void OnPreparationEnd()
    {
        base.OnPreparationEnd();
        
        // 清理准备阶段效果
        HidePreparationEffect();
    }
    
    protected override IEnumerator ExecuteSpecificAttack()
    {
        if (isInterrupted) yield break;
        
        // 发射导弹
        LaunchMissile();
        
        // 等待导弹飞行和爆炸
        yield return StartCoroutine(WaitForMissileImpact());
    }
    
    /// <summary>
    /// 发射导弹
    /// </summary>
    private void LaunchMissile()
    {
        // 创建导弹对象
        if (missilePrefab != null)
        {
            currentMissile = Instantiate(missilePrefab, launchPosition, Quaternion.identity);
        }
        else
        {
            currentMissile = CreateDefaultMissile();
        }
        
        // 设置导弹的目标和属性
        MissileProjectile missileScript = currentMissile.GetComponent<MissileProjectile>();
        if (missileScript == null)
        {
            missileScript = currentMissile.AddComponent<MissileProjectile>();
        }
        
        // 配置导弹
        missileScript.Initialize(
            launchPosition,
            targetPosition,
            missileSpeed,
            curveHeight,
            missileLifetime,
            damageRadius,
            attackDamage,
            useParabolicTrajectory,
            trajectoryCurve
        );
        
        // 订阅导弹爆炸事件
        missileScript.OnMissileImpact += OnMissileImpact;
        
        Debug.Log($"{gameObject.name} launched missile from {launchPosition} to {targetPosition}");
    }
    
    /// <summary>
    /// 等待导弹撞击
    /// </summary>
    /// <returns></returns>
    private IEnumerator WaitForMissileImpact()
    {
        missileImpacted = false;
        float elapsedTime = 0f;
        
        // 等待导弹撞击或超时
        while (!missileImpacted && elapsedTime < missileLifetime && !isInterrupted)
        {
            elapsedTime += Time.deltaTime;
            
            // 更新进度条（攻击阶段占50%-100%）
            float progress = Mathf.Min(elapsedTime / missileLifetime, 1f);
            UpdateProgressBar(0.5f + progress * 0.5f);
            
            yield return null;
        }
        
        // 如果导弹还没有爆炸且没有被中断，强制引爆
        if (!missileImpacted && !isInterrupted && currentMissile != null)
        {
            ForceImpactMissile();
        }
    }
    
    /// <summary>
    /// 导弹撞击事件处理
    /// </summary>
    /// <param name="impactPosition">撞击位置</param>
    /// <param name="hitPlayer">是否击中玩家</param>
    private void OnMissileImpact(Vector3 impactPosition, bool hitPlayer)
    {
        missileImpacted = true;
        
        // 显示爆炸效果
        ShowExplosionEffect(impactPosition);
        
        // 如果击中玩家，在基类中已经处理了伤害
        if (hitPlayer)
        {
            Debug.Log($"{gameObject.name} missile hit player at {impactPosition}!");
        }
        else
        {
            Debug.Log($"{gameObject.name} missile impacted at {impactPosition} but missed player.");
        }
    }
    
    /// <summary>
    /// 强制引爆导弹
    /// </summary>
    private void ForceImpactMissile()
    {
        if (currentMissile != null)
        {
            MissileProjectile missileScript = currentMissile.GetComponent<MissileProjectile>();
            if (missileScript != null)
            {
                missileScript.ForceImpact();
            }
        }
    }
    
    /// <summary>
    /// 创建默认导弹对象
    /// </summary>
    /// <returns>导弹游戏对象</returns>
    private GameObject CreateDefaultMissile()
    {
        GameObject missile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        missile.name = "DefaultMissile";
        missile.transform.localScale = Vector3.one * 0.3f;
        
        // 设置导弹外观
        Renderer renderer = missile.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.red;
        }
        
        // 移除默认的碰撞体，我们将使用自定义的
        Collider defaultCollider = missile.GetComponent<Collider>();
        if (defaultCollider != null)
        {
            DestroyImmediate(defaultCollider);
        }
        
        return missile;
    }
    
    /// <summary>
    /// 显示准备阶段效果
    /// </summary>
    private void ShowPreparationEffect()
    {
        // 可以在这里添加瞄准线、充能特效等
        // 例如：从敌人位置到目标位置画一条虚线
        Debug.Log($"{gameObject.name} preparing missile attack...");
    }
    
    /// <summary>
    /// 隐藏准备阶段效果
    /// </summary>
    private void HidePreparationEffect()
    {
        // 清理准备阶段的视觉效果
        Debug.Log($"{gameObject.name} missile preparation complete.");
    }
    
    /// <summary>
    /// 显示爆炸效果
    /// </summary>
    /// <param name="position">爆炸位置</param>
    private void ShowExplosionEffect(Vector3 position)
    {
        if (explosionEffectPrefab != null)
        {
            GameObject explosion = Instantiate(explosionEffectPrefab, position, Quaternion.identity);
            Destroy(explosion, explosionEffectDuration);
        }
        else
        {
            // 创建简单的爆炸效果
            CreateSimpleExplosionEffect(position);
        }
    }
    
    /// <summary>
    /// 创建简单的爆炸效果
    /// </summary>
    /// <param name="position">爆炸位置</param>
    private void CreateSimpleExplosionEffect(Vector3 position)
    {
        // 创建一个简单的爆炸视觉效果
        GameObject explosion = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        explosion.name = "ExplosionEffect";
        explosion.transform.position = position;
        explosion.transform.localScale = Vector3.one * damageRadius * 2;
        
        // 设置爆炸外观
        Renderer renderer = explosion.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(1f, 0.5f, 0f, 0.6f); // 橙色半透明
        }
        
        // 移除碰撞体
        Collider collider = explosion.GetComponent<Collider>();
        if (collider != null)
        {
            DestroyImmediate(collider);
        }
        
        // 添加缩放动画
        StartCoroutine(AnimateExplosion(explosion));
    }
    
    /// <summary>
    /// 爆炸动画
    /// </summary>
    /// <param name="explosion">爆炸对象</param>
    /// <returns></returns>
    private IEnumerator AnimateExplosion(GameObject explosion)
    {
        float duration = 0.5f;
        float elapsedTime = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one * damageRadius * 2;
        
        Renderer renderer = explosion.GetComponent<Renderer>();
        Color startColor = renderer.material.color;
        Color endColor = startColor;
        endColor.a = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            
            // 缩放动画
            explosion.transform.localScale = Vector3.Lerp(startScale, endScale, progress);
            
            // 透明度动画
            Color currentColor = Color.Lerp(startColor, endColor, progress);
            renderer.material.color = currentColor;
            
            yield return null;
        }
        
        Destroy(explosion);
    }
    
    /// <summary>
    /// 当攻击被中断时清理
    /// </summary>
    public override void InterruptAttack()
    {
        base.InterruptAttack();
        
        // 销毁当前导弹
        if (currentMissile != null)
        {
            Destroy(currentMissile);
            currentMissile = null;
        }
    }
    
    /// <summary>
    /// 设置导弹速度
    /// </summary>
    /// <param name="speed">导弹速度</param>
    public void SetMissileSpeed(float speed)
    {
        missileSpeed = speed;
    }
    
    /// <summary>
    /// 设置曲线高度
    /// </summary>
    /// <param name="height">曲线高度</param>
    public void SetCurveHeight(float height)
    {
        curveHeight = height;
    }
    
    /// <summary>
    /// 设置伤害半径
    /// </summary>
    /// <param name="radius">伤害半径</param>
    public void SetDamageRadius(float radius)
    {
        damageRadius = radius;
    }
    
    // 编辑器辅助方法
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            Vector3 start = transform.position;
            Vector3 end = usePlayerCurrentPosition && playerTransform != null ? 
                         playerTransform.position : fixedTargetPosition;
            
            // 绘制导弹轨迹
            Gizmos.color = Color.yellow;
            DrawTrajectoryGizmos(start, end);
            
            // 绘制爆炸半径
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(end, damageRadius);
        }
    }
    
    /// <summary>
    /// 绘制轨迹辅助线
    /// </summary>
    /// <param name="start">起始位置</param>
    /// <param name="end">结束位置</param>
    private void DrawTrajectoryGizmos(Vector3 start, Vector3 end)
    {
        if (useParabolicTrajectory)
        {
            // 绘制抛物线轨迹
            Vector3 previousPoint = start;
            int segments = 20;
            
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector3 currentPoint = CalculateParabolicPoint(start, end, t);
                
                Gizmos.DrawLine(previousPoint, currentPoint);
                previousPoint = currentPoint;
            }
        }
        else
        {
            // 绘制直线轨迹
            Gizmos.DrawLine(start, end);
        }
    }
    
    /// <summary>
    /// 计算抛物线上的点
    /// </summary>
    /// <param name="start">起始点</param>
    /// <param name="end">结束点</param>
    /// <param name="t">参数t（0-1）</param>
    /// <returns>抛物线上的点</returns>
    private Vector3 CalculateParabolicPoint(Vector3 start, Vector3 end, float t)
    {
        Vector3 midPoint = Vector3.Lerp(start, end, t);
        float height = trajectoryCurve.Evaluate(t) * curveHeight;
        midPoint.y += height;
        return midPoint;
    }
}
