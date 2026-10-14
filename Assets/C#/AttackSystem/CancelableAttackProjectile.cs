using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// 可消除攻击类型
public enum CancelableAttackType
{
    normal, // 原位置 一个
    random_position,// 原位置附近的随机位置 一个
    boss_fixed_3 // 固定位置设置三个
}

/// <summary>
/// 可消除攻击物体
/// 在指定位置生成标靶，玩家需要按顺序击中标靶来阻止攻击
/// </summary>
public class CancelableAttackProjectile : AttackProjectile
{
    [Header("Cancelable Attack Settings")]
    [SerializeField] private int targetCount = 3;                  // 标靶数量
    [SerializeField] private float targetSpread = 5f;              // 标靶分布范围
    [SerializeField] private float attackDuration = 10f;           // 攻击持续时间
    [SerializeField] private float targetHitRadius = 0.5f;         // 标靶击中半径
    
    [Header("Target Placement")]
    [SerializeField] public CancelableAttackType type = CancelableAttackType.normal; // 攻击类型 用于决定怎么放置 放几个标靶 
    
    [Header("Beam Effect Settings")]
    [SerializeField] private bool enableBeamEffect = true;       // 是否启用光束效果
    [SerializeField] private float beamDuration = 1f;            // 光束持续时间
    [SerializeField] private Color beamColor = Color.red;        // 光束颜色
    [SerializeField] private float beamWidth = 0.1f;            // 光束宽度
    [SerializeField] private GameObject beamEffectPrefab;        // 光束特效预制体

    [Header("Visual Settings")]
    [SerializeField] private GameObject targetPrefab;             // 标靶预制体
    [SerializeField] private Color[] targetColors = { Color.red, Color.yellow, Color.green }; // 标靶颜色（按顺序）
    [SerializeField] private float targetScale = 1f;              // 标靶缩放
    
    [Header("Final Attack Settings")]
    [SerializeField] private float finalAttackRadius = 3f;        // 最终攻击半径
    [SerializeField] private GameObject attackEffectPrefab;       // 攻击特效预制体
    
    [Header("Target Animation Settings")]
    [SerializeField] private bool enableTargetAnimations = true;           // 是否启用标靶动画
    [SerializeField] private float targetAppearDuration = 0.5f;            // 标靶出现动画持续时间
    [SerializeField] private float targetDisappearDuration = 0.5f;         // 标靶消失动画持续时间
    [SerializeField] private Vector3 targetAppearStartScale = Vector3.zero; // 标靶出现起始缩放
    [SerializeField] private float targetAppearRotationSpeed = 360f;       // 标靶出现旋转速度
    [SerializeField] private GameObject targetDisappearEffectPrefab;       // 标靶消失特效预制体
    
    // 运行时变量
    private List<AttackTarget> targets = new List<AttackTarget>();
    private int currentTargetIndex = 0;
    private bool isAttackCanceled = false;
    private float attackStartTime;
    
    protected override IEnumerator ExecuteAttack()
    {
        attackStartTime = Time.time;
        
        // 创建标靶
        CreateTargets();
        
        // 等待玩家击中标靶或时间耗尽
        yield return StartCoroutine(WaitForTargetsOrTimeout());
        
        // 检查攻击是否被取消
        if (!isAttackCanceled && !isAttackInterrupted)
        {
            // 执行最终攻击
            yield return StartCoroutine(ExecuteFinalAttack());
        }
        else if (isAttackCanceled)
        {
            // 攻击被成功取消
            ShowCancelationEffect();
        }
        
        // 清理标靶
        CleanupTargets();
        
        // 完成攻击
        CompleteAttack();
    }
    
    /// <summary>
    /// 创建标靶
    /// </summary>
    private void CreateTargets()
    {
        switch(type) 
        {
            case CancelableAttackType.normal:
                targetCount = 1;
                break;
            case CancelableAttackType.random_position:
                targetCount = 1;
                break;
            case CancelableAttackType.boss_fixed_3:
                targetCount = 3;
                break;
            default:
                break;
        }

        Vector3[] positions = GetTargetPositions();
        
        for (int i = 0; i < targetCount && i < positions.Length; i++)
        {
            GameObject targetObj = CreateTarget(positions[i], i);
            AttackTarget target = targetObj.GetComponent<AttackTarget>();
            
            if (target != null)
            {
                target.Initialize(this, i, targetColors[i % targetColors.Length]);
                target.OnTargetHit += OnTargetHit;
                
                // 配置动画参数
                ConfigureTargetAnimation(target);
                
                targets.Add(target);
            }
        }
        
        if (targets.Count > 0) 
        {
            for (int i = 0; i < targets.Count; i++) 
            {
                targets[i].SetActive(false);
            }
        }

        // 激活第一个标靶
        if (targets.Count > 0)
        {
            targets[0].SetActive(true);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Created {targets.Count} attack targets");
        }
    }
    
    /// <summary>
    /// 获取标靶位置
    /// </summary>
    /// <returns>标靶位置数组</returns>
    private Vector3[] GetTargetPositions()
    {
        /*
        if (!useRandomPlacement && fixedTargetPositions != null && fixedTargetPositions.Length > 0)
        {
            return fixedTargetPositions;
        }
        
        // 随机生成标靶位置
        Vector3[] positions = new Vector3[targetCount];
        
        for (int i = 0; i < targetCount; i++)
        {
            // 在目标位置周围随机分布
            Vector2 randomOffset = Random.insideUnitCircle * targetSpread;
            positions[i] = targetPosition + new Vector3(randomOffset.x, randomOffset.y, 0);
        }
        
        return positions;
        */

        Vector3[] positions = new Vector3[targetCount];

        switch (type) 
        {
            case CancelableAttackType.normal:// 一个 原位置
                positions[0] = this.transform.position;
                break;
            case CancelableAttackType.random_position:
                for (int i = 0; i < targetCount; i++)
                {
                    // 在目标位置周围随机分布
                    Vector2 randomOffset = Random.insideUnitCircle * targetSpread;
                    positions[i] = targetPosition + new Vector3(randomOffset.x, randomOffset.y, 0);
                }
                break;
            case CancelableAttackType.boss_fixed_3:
                positions[0] = new Vector3(600, 300, 0);
                positions[1] = new Vector3(200, 300, 0);
                positions[2] = new Vector3(400, 500, 0);
                break;
            default:
                positions[0] = this.transform.position;
                break;

        }
        return positions;
    }

    /// <summary>
    /// 创建单个标靶
    /// </summary>
    /// <param name="position">标靶位置</param>
    /// <param name="index">标靶索引</param>
    /// <returns>标靶对象</returns>
    private GameObject CreateTarget(Vector3 position, int index)
    {
        GameObject targetObj;
        
        if (targetPrefab != null)
        {
            targetObj = Instantiate(targetPrefab, position, Quaternion.identity);
        }
        else
        {
            targetObj = CreateDefaultTarget(position, index);
        }
        
        // 添加AttackTarget组件
        AttackTarget target = targetObj.GetComponent<AttackTarget>();
        if (target == null)
        {
            target = targetObj.AddComponent<AttackTarget>();
        }
        
        // 设置缩放
        targetObj.transform.localScale = Vector3.one * targetScale;
        
        return targetObj;
    }
    
    /// <summary>
    /// 创建默认标靶
    /// </summary>
    /// <param name="position">位置</param>
    /// <param name="index">索引</param>
    /// <returns>标靶对象</returns>
    private GameObject CreateDefaultTarget(Vector3 position, int index)
    {
        GameObject targetObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        targetObj.name = $"AttackTarget_{index}";
        targetObj.transform.position = position;
        targetObj.transform.localScale = new Vector3(1f, 0.1f, 1f); // 扁平圆柱体
        
        // 设置颜色
        Renderer renderer = targetObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = targetColors[index % targetColors.Length];
            renderer.material = material;
        }
        
        // 添加碰撞体
        Collider collider = targetObj.GetComponent<Collider>();
        if (collider == null)
        {
            SphereCollider sphereCollider = targetObj.AddComponent<SphereCollider>();
            sphereCollider.radius = targetHitRadius;
        }
        
        return targetObj;
    }
    
    /// <summary>
    /// 等待标靶被击中或超时
    /// </summary>
    /// <returns></returns>
    private IEnumerator WaitForTargetsOrTimeout()
    {
        while (!isAttackCanceled && !isAttackInterrupted)
        {
            // 检查超时
            if (Time.time - attackStartTime >= attackDuration)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"[{gameObject.name}] Attack timeout, executing final attack");
                }
                break;
            }
            
            yield return null;
        }
    }
    
    /// <summary>
    /// 标靶被击中的回调
    /// </summary>
    /// <param name="target">被击中的标靶</param>
    /// <param name="targetIndex">标靶索引</param>
    private void OnTargetHit(AttackTarget target, int targetIndex)
    {
        if (targetIndex != currentTargetIndex)
        {
            // 击中了错误的标靶，可能重置或给予惩罚
            if (enableDebugLogs)
            {
                Debug.Log($"[{gameObject.name}] Wrong target hit: {targetIndex}, expected: {currentTargetIndex}");
            }
            return;
        }
        
        // 击中了正确的标靶
        target.OnHit();
        currentTargetIndex++;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Target {targetIndex} hit, progress: {currentTargetIndex}/{targetCount}");
        }
        
        // 检查是否所有标靶都被击中
        if (currentTargetIndex >= targetCount)
        {
            // 攻击被成功取消
            isAttackCanceled = true;
            
            if (enableDebugLogs)
            {
                Debug.Log($"[{gameObject.name}] All targets hit, attack canceled!");
            }
        }
        else
        {
            // 激活下一个标靶
            if (currentTargetIndex < targets.Count)
            {
                targets[currentTargetIndex].SetActive(true);
            }
        }
    }
    
    /// <summary>
    /// 执行最终攻击
    /// </summary>
    /// <returns></returns>
    private IEnumerator ExecuteFinalAttack()
    {
        // 显示光束效果
        if (enableBeamEffect)
        {
            yield return StartCoroutine(ShowBeamEffects());
        }

        // 显示攻击特效
        ShowFinalAttackEffect();
        
        // 对玩家造成伤害
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.GetComponent<Player>().TakeDamage(damage);
        }
        
        yield return new WaitForSeconds(0.5f);
    }
    
    /// <summary>
    /// 显示最终攻击特效
    /// </summary>
    private void ShowFinalAttackEffect()
    {
        if (attackEffectPrefab != null)
        {
            GameObject effect = Instantiate(attackEffectPrefab, targetPosition, Quaternion.identity);
            Destroy(effect, 2f);
        }
        else
        {
            StartCoroutine(CreateSimpleFinalAttackEffect());
        }
    }
    
    /// <summary>
    /// 创建简单的最终攻击特效
    /// </summary>
    /// <returns></returns>
    private IEnumerator CreateSimpleFinalAttackEffect()
    {
        GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        effect.name = "FinalAttackEffect";
        effect.transform.position = targetPosition;
        
        // 移除碰撞体
        Collider collider = effect.GetComponent<Collider>();
        if (collider != null)
        {
            DestroyImmediate(collider);
        }
        
        // 设置材质
        Renderer renderer = effect.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.red;
        }
        
        // 扩散动画
        float duration = 0.5f;
        float elapsedTime = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one * finalAttackRadius * 2;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            
            effect.transform.localScale = Vector3.Lerp(startScale, endScale, progress);
            
            yield return null;
        }
        
        Destroy(effect);
    }
    
    /// <summary>
    /// 显示攻击取消特效
    /// </summary>
    private void ShowCancelationEffect()
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Attack successfully canceled by player!");
        }
        
        // 可以添加取消特效，比如绿色光芒等
        StartCoroutine(CreateCancelationEffect());
    }
    
    /// <summary>
    /// 创建取消特效
    /// </summary>
    /// <returns></returns>
    private IEnumerator CreateCancelationEffect()
    {
        GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        effect.name = "CancelationEffect";
        effect.transform.position = targetPosition;
        
        // 移除碰撞体
        Collider collider = effect.GetComponent<Collider>();
        if (collider != null)
        {
            DestroyImmediate(collider);
        }
        
        // 设置绿色材质
        Renderer renderer = effect.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.green;
        }
        
        // 闪烁效果
        for (int i = 0; i < 3; i++)
        {
            effect.SetActive(false);
            yield return new WaitForSeconds(0.1f);
            effect.SetActive(true);
            yield return new WaitForSeconds(0.1f);
        }
        
        Destroy(effect);
    }
    
    /// <summary>
    /// 清理所有标靶
    /// </summary>
    private void CleanupTargets()
    {
        foreach (var target in targets)
        {
            if (target != null)
            {
                Destroy(target.gameObject);
            }
        }
        targets.Clear();
    }
    
    /// <summary>
    /// 中断攻击时清理
    /// </summary>
    public override void InterruptAttack()
    {
        CleanupTargets();
        base.InterruptAttack();
    }
    
    /// <summary>
    /// 销毁时清理
    /// </summary>
    protected override void OnDestroy()
    {
        CleanupTargets();
        base.OnDestroy();
    }

        /// <summary>
    /// 显示光束效果
    /// </summary>
    /// <returns></returns>
    private IEnumerator ShowBeamEffects()
    {
        // 获取玩家位置
        Vector3 playerPosition = GetPlayerPosition();
        
        // 为每个激活的标靶创建光束
        foreach (var target in targets)
        {
            if (target != null && target.IsActive())
            {
                CreateBeamEffect(target.transform.position, playerPosition);
            }
        }
        
        // 等待光束动画完成
        yield return new WaitForSeconds(beamDuration);
    }
    
    /// <summary>
    /// 创建光束效果
    /// </summary>
    /// <param name="startPos">起始位置</param>
    /// <param name="endPos">结束位置</param>
    private void CreateBeamEffect(Vector3 startPos, Vector3 endPos)
    {
        GameObject beamObj;
        
        if (beamEffectPrefab != null)
        {
            beamObj = Instantiate(beamEffectPrefab, startPos, Quaternion.identity);
        }
        else
        {
            beamObj = CreateDefaultBeamEffect(startPos);
        }
        
        // 获取或添加BeamEffect组件
        BeamEffect beamEffect = beamObj.GetComponent<BeamEffect>();
        if (beamEffect == null)
        {
            beamEffect = beamObj.AddComponent<BeamEffect>();
        }
        
        // 设置光束参数
        beamEffect.SetBeamParameters(beamDuration, Vector3.Distance(startPos, endPos), beamColor, beamWidth);
        
        // 启动光束效果
        beamEffect.StartBeam(startPos, endPos);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Beam effect created from {startPos} to {endPos}");
        }
    }
    
    /// <summary>
    /// 创建默认光束效果
    /// </summary>
    /// <param name="position">位置</param>
    /// <returns>光束对象</returns>
    private GameObject CreateDefaultBeamEffect(Vector3 position)
    {
        GameObject beamObj = new GameObject("BeamEffect");
        beamObj.transform.position = position;
        
        return beamObj;
    }
    
    /// <summary>
    /// 获取玩家位置
    /// </summary>
    /// <returns>玩家位置</returns>
    private Vector3 GetPlayerPosition()
    {
        // 尝试找到玩家
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            return player.transform.position;
        }
        
        // 如果找不到玩家，使用默认位置
        return Vector3.zero;
    }
    
    // 公共方法
    public bool IsAttackCanceled() => isAttackCanceled;
    public int GetCurrentTargetIndex() => currentTargetIndex;
    public int GetTotalTargetCount() => targetCount;
    public float GetRemainingTime() => Mathf.Max(0, attackDuration - (Time.time - attackStartTime));
    public float GetAttackDuration() => attackDuration;
    public float GetElapsedTime() => Time.time - attackStartTime;
    
    /// <summary>
    /// 配置标靶动画参数
    /// </summary>
    /// <param name="target">标靶对象</param>
    private void ConfigureTargetAnimation(AttackTarget target)
    {
        if (target == null) return;
        
        // 设置动画启用状态
        target.SetAnimationEnabled(enableTargetAnimations, enableTargetAnimations);
        
        // 设置动画持续时间
        target.SetAnimationDurations(targetAppearDuration, targetDisappearDuration);
        
        // 设置出现动画参数
        target.SetAppearAnimationSettings(targetAppearStartScale, targetAppearRotationSpeed);
        
        // 设置消失特效预制体
        if (targetDisappearEffectPrefab != null)
        {
            target.SetDisappearEffectPrefab(targetDisappearEffectPrefab);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Configured animation for target {target.GetTargetIndex()}");
        }
    }
    
    // 公共设置方法
    public void SetTargetCount(int count) => targetCount = count;
    public void SetTargetSpread(float spread) => targetSpread = spread;
    public void SetAttackDuration(float duration) => attackDuration = duration;
    public void SetFinalAttackRadius(float radius) => finalAttackRadius = radius;
    
    /// <summary>
    /// 设置标靶动画参数
    /// </summary>
    /// <param name="enableAnimations">是否启用动画</param>
    /// <param name="appearDuration">出现动画持续时间</param>
    /// <param name="disappearDuration">消失动画持续时间</param>
    public void SetTargetAnimationSettings(bool enableAnimations, float appearDuration, float disappearDuration)
    {
        enableTargetAnimations = enableAnimations;
        targetAppearDuration = appearDuration;
        targetDisappearDuration = disappearDuration;
    }
    
    /// <summary>
    /// 设置标靶出现动画参数
    /// </summary>
    /// <param name="startScale">起始缩放</param>
    /// <param name="rotationSpeed">旋转速度</param>
    public void SetTargetAppearSettings(Vector3 startScale, float rotationSpeed)
    {
        targetAppearStartScale = startScale;
        targetAppearRotationSpeed = rotationSpeed;
    }
    
    /// <summary>
    /// 设置标靶消失特效预制体
    /// </summary>
    /// <param name="effectPrefab">特效预制体</param>
    public void SetTargetDisappearEffectPrefab(GameObject effectPrefab)
    {
        targetDisappearEffectPrefab = effectPrefab;
    }
}
