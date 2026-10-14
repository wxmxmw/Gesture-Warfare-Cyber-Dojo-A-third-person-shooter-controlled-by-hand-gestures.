using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 导弹攻击效果类型
/// </summary>
public enum MissileAttackEffectType
{
    Normal,         // 普通导弹攻击（原有的单导弹效果）
    MultipleBullet  // 多导弹攻击（生成多个导弹，播放准备动画后逐个发射）
}

/// <summary>
/// 导弹攻击物体
/// 从起始位置沿曲线飞向目标位置，到达时进行爆炸伤害
/// </summary>
public class MissileAttackProjectile : AttackProjectile
{
    [Header("Missile Attack Effect")]
    [SerializeField] private MissileAttackEffectType attackEffectType = MissileAttackEffectType.Normal; // 攻击效果类型
    
    [Header("Missile Settings")]
    [SerializeField] private float missileSpeed = 10f;              // 导弹飞行速度
    [SerializeField] private float curveHeight = 3f;               // 曲线高度
    [SerializeField] private float explosionRadius = 2f;           // 爆炸半径
    [SerializeField] private bool useParabolicTrajectory = true;   // 是否使用抛物线轨迹
    
    [Header("Multiple Bullet Settings")]
    [SerializeField] private int bulletCount = 3;                  // 多导弹模式下的导弹数量
    [SerializeField] private float bulletSpacing = 10f;             // 导弹之间的间距
    [SerializeField] private float preparationAnimationTime = 1f;  // 准备动画时间
    [SerializeField] private float launchInterval = 0.3f;          // 导弹发射间隔
    [SerializeField] private string preparationAnimationName = "Prepare"; // 准备动画名称
    
    [Header("Visual Settings")]
    [SerializeField] private GameObject missilePrefab;             // 导弹预制体（可选）
    [SerializeField] private GameObject explosionEffectPrefab;     // 爆炸特效预制体（可选）
    [SerializeField] private bool showTrail = true;               // 是否显示拖尾
    [SerializeField] private AnimationCurve trajectoryCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 轨迹曲线

    [SerializeField] private bool isMissilePointAtX = false;
    
    /// <summary>
    /// 计算导弹朝向（根据isMissilePointAtX设置）
    /// </summary>
    /// <param name="direction">飞行方向向量</param>
    /// <returns>旋转四元数</returns>
    private Quaternion CalculateMissileRotation(Vector3 direction)
    {
        if (isMissilePointAtX)
        {
            // X方向朝向飞行方向，只在Z轴旋转
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            return Quaternion.Euler(0, 0, angle);
        }
        else
        {
            // 原有的旋转方式
            return Quaternion.LookRotation(Vector3.forward, direction);
        }
    }
    
    // 运行时变量 - Normal模式
    private GameObject missileVisual;
    private TrailRenderer trailRenderer;
    private float journeyTime = 0f;
    private float totalJourneyTime;
    private bool hasReachedTarget = false;
    
    // 运行时变量 - MultipleBullet模式
    private List<GameObject> spawnedMissiles = new List<GameObject>(); // 生成的导弹列表
    private List<Animator> missileAnimators = new List<Animator>();    // 导弹动画控制器列表
    
    // 爆炸效果管理
    private GameObject currentExplosionEffect;
    private Coroutine explosionCoroutine;
    
    protected override IEnumerator ExecuteAttack()
    {
        // 根据攻击效果类型执行不同的攻击逻辑
        switch (attackEffectType)
        {
            case MissileAttackEffectType.Normal:
                yield return StartCoroutine(ExecuteNormalAttack());
                break;
            case MissileAttackEffectType.MultipleBullet:
                yield return StartCoroutine(ExecuteMultipleBulletAttack());
                break;
        }
        
        // 完成攻击
        CompleteAttack();
    }
    
    /// <summary>
    /// 执行普通导弹攻击（原有的攻击方式）
    /// </summary>
    private IEnumerator ExecuteNormalAttack()
    {
        // 创建导弹视觉
        CreateMissileVisual();
        
        // 计算飞行时间
        CalculateJourneyTime();
        
        // 开始飞行
        yield return StartCoroutine(FlyToTarget());
        
        // 到达目标，执行爆炸
        if (!isAttackInterrupted)
        {
            yield return StartCoroutine(ExplodeAtTarget());
        }
    }
    
    /// <summary>
    /// 执行多导弹攻击
    /// </summary>
    private IEnumerator ExecuteMultipleBulletAttack()
    {
        // 1. 在默认位置生成指定数量的导弹
        SpawnMultipleMissiles();
        
        // 2. 播放所有导弹的准备动画
        yield return StartCoroutine(PlayPreparationAnimations());
        
        // 3. 逐个发射导弹到目标位置
        yield return StartCoroutine(LaunchMissilesSequentially());
    }
    
    /// <summary>
    /// 生成多个导弹预制体
    /// </summary>
    private void SpawnMultipleMissiles()
    {
        spawnedMissiles.Clear();
        missileAnimators.Clear();
        
        // 计算导弹排列的起始位置（以transform.position为中心）
        float totalWidth = (bulletCount - 1) * bulletSpacing;
        Vector3 startOffset = Vector3.left * (totalWidth / 2f);
        
        for (int i = 0; i < bulletCount; i++)
        {
            Vector3 spawnPosition = transform.position + startOffset + Vector3.right * (i * bulletSpacing);
            
            // 计算初始朝向角度
            float initialAngle;
            if (bulletCount == 1)
            {
                // 只有一个导弹时，朝向0°（中间）
                initialAngle = 90f;
            }
            else
            {
                // 多个导弹时，均匀分布在-90°到90°之间
                initialAngle = 45f + (90f / (bulletCount - 1)) * i;
            }
            Quaternion initialRotation = Quaternion.Euler(0, 0, initialAngle);
            
            GameObject missile;
            if (missilePrefab != null)
            {
                missile = Instantiate(missilePrefab, spawnPosition, initialRotation);
            }
            else
            {
                missile = CreateDefaultMissile();
                missile.transform.position = spawnPosition;
                missile.transform.rotation = initialRotation;
            }
            
            spawnedMissiles.Add(missile);
            
            // 获取动画控制器
            Animator animator = missile.GetComponent<Animator>();
            if (animator != null)
            {
                missileAnimators.Add(animator);
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"[{gameObject.name}] 生成导弹 {i + 1}/{bulletCount} 在位置: {spawnPosition}, 初始角度: {initialAngle}°");
            }
        }
    }
    
    /// <summary>
    /// 播放所有导弹的准备动画
    /// </summary>
    private IEnumerator PlayPreparationAnimations()
    {
        // 触发所有导弹的准备动画
        foreach (Animator animator in missileAnimators)
        {
            if (animator != null)
            {
                animator.Play(preparationAnimationName);
            }
        }
        
        // 等待准备动画完成
        yield return new WaitForSeconds(preparationAnimationTime);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] 所有导弹准备动画完成");
        }
    }
    
    /// <summary>
    /// 逐个发射导弹
    /// </summary>
    private IEnumerator LaunchMissilesSequentially()
    {
        for (int i = 0; i < spawnedMissiles.Count; i++)
        {
            if (isAttackInterrupted)
            {
                break;
            }
            
            GameObject missile = spawnedMissiles[i];
            if (missile != null)
            {
                // 启动单个导弹的发射协程
                StartCoroutine(LaunchSingleMissile(missile, i));
                
                // 等待发射间隔
                yield return new WaitForSeconds(launchInterval);
            }
        }
        
        // 等待所有导弹完成飞行（粗略估计）
        float maxFlightTime = Vector3.Distance(transform.position, targetPosition) / missileSpeed + 1f;
        yield return new WaitForSeconds(maxFlightTime);
    }
    
    /// <summary>
    /// 发射单个导弹
    /// </summary>
    private IEnumerator LaunchSingleMissile(GameObject missile, int index)
    {
        if (missile == null) yield break;
        
        Vector3 startPos = missile.transform.position;
        float flightTime = 0f;
        float totalTime = Vector3.Distance(startPos, targetPosition) / missileSpeed;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] 发射导弹 {index + 1}: {startPos} -> {targetPosition}");
        }
        
        // 导弹飞行过程
        while (flightTime < totalTime && !isAttackInterrupted)
        {
            flightTime += Time.deltaTime;
            float progress = flightTime / totalTime;
            
            if (missile != null)
            {
                // 直线飞向目标
                missile.transform.position = Vector3.Lerp(startPos, targetPosition, progress);
                
                // 更新朝向
                Vector3 direction = (targetPosition - missile.transform.position).normalized;
                if (direction != Vector3.zero)
                {
                    missile.transform.rotation = CalculateMissileRotation(direction);
                }
            }
            
            yield return null;
        }
        
        // 到达目标位置
        if (missile != null && !isAttackInterrupted)
        {
            missile.transform.position = targetPosition;
            
            // 生成爆炸效果
            if (explosionEffectPrefab != null)
            {
                GameObject explosion = Instantiate(explosionEffectPrefab, targetPosition, Quaternion.identity);
                Destroy(explosion, 2f);
            }
            
            // 执行伤害检测
            DetectAndDamagePlayer(targetPosition, explosionRadius);
            
            // 销毁导弹
            Destroy(missile);
            
            if (enableDebugLogs)
            {
                Debug.Log($"[{gameObject.name}] 导弹 {index + 1} 到达目标并爆炸");
            }
        }
    }
    
    /// <summary>
    /// 创建导弹视觉效果
    /// </summary>
    private void CreateMissileVisual()
    {
        if (missilePrefab != null)
        {
            missileVisual = Instantiate(missilePrefab, startPosition, Quaternion.identity);
        }
        else
        {
            missileVisual = CreateDefaultMissile();
        }
        
        // 设置拖尾效果
        if (showTrail)
        {
            SetupTrail();
        }
        
        // 设置初始朝向
        Vector3 direction = (targetPosition - startPosition).normalized;
        if (direction != Vector3.zero)
        {
            if (isMissilePointAtX)
            {
                // X方向朝向飞行方向，只在Z轴旋转
                missileVisual.transform.rotation = CalculateMissileRotation(direction);
            }
            else 
            {
                // 原有的旋转方式
                missileVisual.transform.rotation *= Quaternion.Euler(new Vector3(0, 0, Mathf.Atan2(direction.y, direction.x)/ Mathf.Deg2Rad - 90));
            }
        }
        
    }
    
    /// <summary>
    /// 创建默认导弹外观
    /// </summary>
    /// <returns>导弹对象</returns>
    private GameObject CreateDefaultMissile()
    {
        GameObject missile = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        missile.name = "DefaultMissile";
        missile.transform.position = startPosition;
        missile.transform.localScale = new Vector3(0.3f, 0.5f, 0.3f);
        
        // 设置材质
        Renderer renderer = missile.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.red;
        }
        
        // 移除碰撞体
        Collider collider = missile.GetComponent<Collider>();
        if (collider != null)
        {
            DestroyImmediate(collider);
        }
        
        return missile;
    }
    
    /// <summary>
    /// 设置拖尾效果
    /// </summary>
    private void SetupTrail()
    {
        trailRenderer = missileVisual.GetComponent<TrailRenderer>();
        if (trailRenderer == null)
        {
            trailRenderer = missileVisual.AddComponent<TrailRenderer>();
        }
        
        trailRenderer.time = 0.5f;
        trailRenderer.startWidth = 0.2f;
        trailRenderer.endWidth = 0.05f;
        trailRenderer.material = CreateTrailMaterial();
        trailRenderer.sortingOrder = 1;
    }
    
    /// <summary>
    /// 创建拖尾材质
    /// </summary>
    /// <returns>拖尾材质</returns>
    private Material CreateTrailMaterial()
    {
        Material material = new Material(Shader.Find("Sprites/Default"));
        material.color = new Color(1f, 0.5f, 0f, 0.8f); // 橙色半透明
        return material;
    }
    
    /// <summary>
    /// 计算飞行时间
    /// </summary>
    private void CalculateJourneyTime()
    {
        float distance = Vector3.Distance(startPosition, targetPosition);
        totalJourneyTime = distance / missileSpeed;
        
    }
    
    /// <summary>
    /// 飞向目标
    /// </summary>
    /// <returns></returns>
    private IEnumerator FlyToTarget()
    {
        journeyTime = 0f;
        
        while (journeyTime < totalJourneyTime && !isAttackInterrupted)
        {
            journeyTime += Time.deltaTime;
            float progress = journeyTime / totalJourneyTime;
            
            // 计算当前位置
            Vector3 currentPosition = CalculatePosition(progress);
            
            // 更新导弹位置
            if (missileVisual != null)
            {
                missileVisual.transform.position = currentPosition;
                
                // 更新朝向
                Vector3 direction = (CalculatePosition(progress + 0.01f) - currentPosition).normalized;
                if (direction != Vector3.zero)
                {
                    if (isMissilePointAtX)
                    {
                        // X方向朝向飞行方向，只在Z轴旋转
                        missileVisual.transform.rotation = CalculateMissileRotation(direction);
                    }
                    else
                    {
                        // 原有的旋转方式（保持原有逻辑，不更新旋转）
                        // 如果需要也可以在这里更新
                    }
                }
            }
            
            yield return null;
        }
        
        // 确保到达目标位置
        if (!isAttackInterrupted && missileVisual != null)
        {
            missileVisual.transform.position = targetPosition;
            hasReachedTarget = true;
        }
    }
    
    /// <summary>
    /// 根据进度计算位置
    /// </summary>
    /// <param name="progress">进度（0-1）</param>
    /// <returns>计算出的位置</returns>
    private Vector3 CalculatePosition(float progress)
    {
        progress = Mathf.Clamp01(progress);
        
        if (useParabolicTrajectory)
        {
            // 抛物线轨迹
            Vector3 linearPosition = Vector3.Lerp(startPosition, targetPosition, progress);
            float heightOffset = trajectoryCurve.Evaluate(progress) * curveHeight;
            linearPosition.y += heightOffset;
            return linearPosition;
        }
        else
        {
            // 直线轨迹
            return Vector3.Lerp(startPosition, targetPosition, progress);
        }
    }
    
    /// <summary>
    /// 在目标位置爆炸
    /// </summary>
    /// <returns></returns>
    private IEnumerator ExplodeAtTarget()
    {
        // 隐藏导弹
        if (missileVisual != null)
        {
            missileVisual.SetActive(false);
        }
        
        // 显示爆炸特效
        ShowExplosionEffect();
        
        // 进行范围伤害检测
        bool hitPlayer = DetectAndDamagePlayer(targetPosition, explosionRadius);
                
        // 等待爆炸效果播放
        yield return new WaitForSeconds(0.5f);
    }
    
    /// <summary>
    /// 显示爆炸特效
    /// </summary>
    private void ShowExplosionEffect()
    {
        if (explosionEffectPrefab != null)
        {
            currentExplosionEffect = Instantiate(explosionEffectPrefab, targetPosition, Quaternion.identity);
            Destroy(currentExplosionEffect, 2f);
        }
        else
        {
            // 创建简单爆炸效果
            explosionCoroutine = StartCoroutine(CreateSimpleExplosion());
        }
    }
    
    /// <summary>
    /// 创建简单爆炸效果
    /// </summary>
    /// <returns></returns>
    private IEnumerator CreateSimpleExplosion()
    {
        currentExplosionEffect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        currentExplosionEffect.name = "ExplosionEffect";
        currentExplosionEffect.transform.position = targetPosition;
        
        // 移除碰撞体
        Collider collider = currentExplosionEffect.GetComponent<Collider>();
        if (collider != null)
        {
            DestroyImmediate(collider);
        }
        
        // 设置材质
        Renderer renderer = currentExplosionEffect.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(1f, 0.5f, 0f, 0.8f); // 橙色
        }
        
        // 缩放动画
        float duration = 0.5f;
        float elapsedTime = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one * explosionRadius * 2;
        
        while (elapsedTime < duration && currentExplosionEffect != null && !isAttackInterrupted)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            
            if (currentExplosionEffect != null)
            {
                currentExplosionEffect.transform.localScale = Vector3.Lerp(startScale, endScale, progress);
                
                // 透明度动画
                if (renderer != null)
                {
                    Color color = renderer.material.color;
                    color.a = Mathf.Lerp(0.8f, 0f, progress);
                    renderer.material.color = color;
                }
            }
            
            yield return null;
        }
        
        // 清理爆炸效果
        if (currentExplosionEffect != null)
        {
            Destroy(currentExplosionEffect);
            currentExplosionEffect = null;
        }
        
        explosionCoroutine = null;
    }
    
    /// <summary>
    /// 清理导弹视觉
    /// </summary>
    private void CleanupMissile()
    {
        // 清理Normal模式的导弹
        if (missileVisual != null)
        {
            Destroy(missileVisual);
            missileVisual = null;
        }
        
        // 清理MultipleBullet模式的所有导弹
        if (spawnedMissiles != null)
        {
            foreach (GameObject missile in spawnedMissiles)
            {
                if (missile != null)
                {
                    Destroy(missile);
                }
            }
            spawnedMissiles.Clear();
        }
        
        if (missileAnimators != null)
        {
            missileAnimators.Clear();
        }
    }
    
    /// <summary>
    /// 清理爆炸效果
    /// </summary>
    private void CleanupExplosion()
    {
        // 停止爆炸协程
        if (explosionCoroutine != null)
        {
            StopCoroutine(explosionCoroutine);
            explosionCoroutine = null;
        }
        
        // 销毁爆炸效果对象
        if (currentExplosionEffect != null)
        {
            Destroy(currentExplosionEffect);
            currentExplosionEffect = null;
        }
    }
    
    /// <summary>
    /// 中断攻击时清理
    /// </summary>
    public override void InterruptAttack()
    {
        CleanupMissile();
        CleanupExplosion();
        base.InterruptAttack();
    }
    
    /// <summary>
    /// 销毁时清理
    /// </summary>
    protected override void OnDestroy()
    {
        CleanupMissile();
        CleanupExplosion();
        base.OnDestroy();
    }
    
    // 公共设置方法
    public void SetMissileSpeed(float speed) => missileSpeed = speed;
    public void SetCurveHeight(float height) => curveHeight = height;
    public void SetExplosionRadius(float radius) => explosionRadius = radius;
    public void SetUseParabolicTrajectory(bool useParabolic) => useParabolicTrajectory = useParabolic;
    
    /// <summary>
    /// 设置导弹攻击效果类型
    /// </summary>
    /// <param name="effectType">攻击效果类型</param>
    public void SetAttackEffectType(MissileAttackEffectType effectType)
    {
        attackEffectType = effectType;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] 设置导弹攻击效果类型: {effectType}");
        }
    }
    
    /// <summary>
    /// 获取当前攻击效果类型
    /// </summary>
    public MissileAttackEffectType GetAttackEffectType()
    {
        return attackEffectType;
    }
    
    /// <summary>
    /// 设置多导弹模式参数
    /// </summary>
    /// <param name="count">导弹数量</param>
    /// <param name="spacing">导弹间距</param>
    /// <param name="interval">发射间隔</param>
    public void SetMultipleBulletSettings(int count, float spacing, float interval)
    {
        bulletCount = Mathf.Max(1, count);
        bulletSpacing = spacing;
        launchInterval = interval;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] 设置多导弹参数: 数量={bulletCount}, 间距={bulletSpacing}, 间隔={launchInterval}");
        }
    }
    
    /// <summary>
    /// 设置准备动画参数
    /// </summary>
    /// <param name="animationName">动画名称</param>
    /// <param name="animationTime">动画时间</param>
    public void SetPreparationAnimation(string animationName, float animationTime)
    {
        preparationAnimationName = animationName;
        preparationAnimationTime = animationTime;
    }
    
    // 编辑器辅助
    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && !hasReachedTarget)
        {
            // 显示轨迹
            Gizmos.color = Color.yellow;
            DrawTrajectoryGizmos();
        }
        
        // 显示爆炸范围
        Gizmos.color = Color.red;
        Vector3 explosionCenter = Application.isPlaying ? targetPosition : transform.position;
        Gizmos.DrawWireSphere(explosionCenter, explosionRadius);
    }
    
    /// <summary>
    /// 绘制轨迹辅助线
    /// </summary>
    private void DrawTrajectoryGizmos()
    {
        if (useParabolicTrajectory)
        {
            Vector3 previousPoint = startPosition;
            int segments = 20;
            
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector3 currentPoint = CalculatePosition(t);
                
                Gizmos.DrawLine(previousPoint, currentPoint);
                previousPoint = currentPoint;
            }
        }
        else
        {
            Gizmos.DrawLine(startPosition, targetPosition);
        }
    }
}

