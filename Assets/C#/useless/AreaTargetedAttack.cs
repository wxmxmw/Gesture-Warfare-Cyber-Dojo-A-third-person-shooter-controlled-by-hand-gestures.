using UnityEngine;
using System.Collections;

/// <summary>
/// 范围定点攻击系统
/// 在目标位置生成黑色小圆（攻击范围）和红色大圆，红圆缩小到与黑圆一样大时进行攻击
/// </summary>
public class AreaTargetedAttack : TargetedAttackBase
{
    [Header("Area Attack Visual Settings")]
    [SerializeField] private float attackRadius = 20f;           // 实际攻击范围（黑圆半径）
    [SerializeField] private float indicatorStartRadius = 50f;   // 指示器初始半径（红圆初始大小）
    [SerializeField] private Color attackAreaColor = Color.black;      // 攻击范围颜色（黑圆）
    [SerializeField] private Color warningIndicatorColor = Color.red;  // 警告指示器颜色（红圆）
    
    [Header("Area Attack Prefab Settings")]
    [SerializeField] private GameObject attackAreaPrefab;       // 攻击范围预制体（如果不为空则使用预制体）
    [SerializeField] private GameObject warningIndicatorPrefab; // 警告指示器预制体（如果不为空则使用预制体）
    
    // 运行时创建的视觉对象
    private GameObject attackAreaVisual;                        // 攻击范围视觉对象（黑圆）
    private GameObject warningIndicatorVisual;                  // 警告指示器视觉对象（红圆）
    private SpriteRenderer attackAreaRenderer;                 // 攻击范围渲染器
    private SpriteRenderer warningIndicatorRenderer;           // 警告指示器渲染器
    
    protected override void OnPreparationStart()
    {
        base.OnPreparationStart();
        Debug.Log("11111111");
        CreateAreaVisuals();
    }
    
    protected override void UpdatePreparationProgress(float progress)
    {
        base.UpdatePreparationProgress(progress);
        UpdateWarningIndicatorSize(progress);
    }
    
    protected override void OnPreparationEnd()
    {
        base.OnPreparationEnd();
        // 不在这里清理，在攻击完成后清理
    }
    
    protected override IEnumerator ExecuteSpecificAttack()
    {
        float elapsedTime = 0f;
        
        // 攻击执行阶段：红圆继续缩小
        while (elapsedTime < attackExecutionTime && !isInterrupted)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / attackExecutionTime;
            
            // 更新红圆大小（从当前大小缩小到攻击范围大小）
            UpdateWarningIndicatorSizeDuringAttack(progress);
            
            // 更新总进度条（攻击阶段占50%-100%）
            UpdateProgressBar(0.5f + progress * 0.5f);
            
            yield return null;
        }
        
        if (!isInterrupted)
        {
            // 执行攻击：检测攻击范围内的玩家
            bool hitPlayer = DetectAndDamagePlayer(targetPosition, attackRadius);
            
            // 显示攻击效果
            ShowAttackEffect(hitPlayer);
            
            // 等待一小段时间展示攻击效果
            yield return new WaitForSeconds(0.3f);
        }
        
        // 清理视觉效果
        CleanupAreaVisuals();
    }
    
    /// <summary>
    /// 创建区域攻击的视觉效果
    /// </summary>
    private void CreateAreaVisuals()
    {
        // 创建攻击范围视觉（黑圆）
        if (attackAreaPrefab != null)
        {
            attackAreaVisual = Instantiate(attackAreaPrefab, targetPosition, Quaternion.identity);
        }
        else
        {
            attackAreaVisual = CreateCircleVisual("AttackArea", targetPosition, attackRadius, attackAreaColor);
        }
        
        attackAreaRenderer = attackAreaVisual.GetComponent<SpriteRenderer>();
        if (attackAreaRenderer != null)
        {
            attackAreaRenderer.sortingOrder = 1; // 确保在下层
        }
        
        // 创建警告指示器视觉（红圆）
        if (warningIndicatorPrefab != null)
        {
            warningIndicatorVisual = Instantiate(warningIndicatorPrefab, targetPosition, Quaternion.identity);
        }
        else
        {
            warningIndicatorVisual = CreateCircleVisual("WarningIndicator", targetPosition, indicatorStartRadius, warningIndicatorColor);
        }
        
        warningIndicatorRenderer = warningIndicatorVisual.GetComponent<SpriteRenderer>();
        if (warningIndicatorRenderer != null)
        {
            warningIndicatorRenderer.sortingOrder = 2; // 确保在上层
            // 设置为半透明
            Color color = warningIndicatorRenderer.color;
            color.a = 0.6f;
            warningIndicatorRenderer.color = color;
        }
    }
    
    /// <summary>
    /// 创建圆形视觉对象
    /// </summary>
    /// <param name="name">对象名称</param>
    /// <param name="position">位置</param>
    /// <param name="radius">半径</param>
    /// <param name="color">颜色</param>
    /// <returns>创建的游戏对象</returns>
    private GameObject CreateCircleVisual(string name, Vector3 position, float radius, Color color)
    {
        GameObject circleObj = new GameObject(name);
        circleObj.transform.position = position;
        
        // 添加SpriteRenderer组件
        SpriteRenderer renderer = circleObj.AddComponent<SpriteRenderer>();
        
        // 创建圆形精灵
        Sprite circleSprite = CreateCircleSprite(radius, color);
        renderer.sprite = circleSprite;
        renderer.color = color;
        
        return circleObj;
    }
    
    /// <summary>
    /// 创建圆形精灵
    /// </summary>
    /// <param name="radius">半径</param>
    /// <param name="color">颜色</param>
    /// <returns>圆形精灵</returns>
    private Sprite CreateCircleSprite(float radius, Color color)
    {
        int diameter = Mathf.CeilToInt(radius * 2 * 32); // 32像素每单位
        diameter = Mathf.Max(diameter, 32); // 最小32像素
        
        Texture2D texture = new Texture2D(diameter, diameter);
        Vector2 center = new Vector2(diameter / 2f, diameter / 2f);
        float textureRadius = diameter / 2f - 1;
        
        // 绘制圆形
        for (int x = 0; x < diameter; x++)
        {
            for (int y = 0; y < diameter; y++)
            {
                Vector2 point = new Vector2(x, y);
                float distance = Vector2.Distance(point, center);
                
                if (distance <= textureRadius)
                {
                    // 圆形内部设置为指定颜色，外部设置为透明
                    if (distance >= textureRadius - 2) // 边缘
                    {
                        texture.SetPixel(x, y, color);
                    }
                    else
                    {
                        Color fillColor = color;
                        fillColor.a = 0.3f; // 内部半透明
                        texture.SetPixel(x, y, fillColor);
                    }
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, diameter, diameter), new Vector2(0.5f, 0.5f), 32);
    }
    
    /// <summary>
    /// 在准备阶段更新警告指示器大小
    /// </summary>
    /// <param name="progress">准备进度（0-1）</param>
    private void UpdateWarningIndicatorSize(float progress)
    {
        if (warningIndicatorVisual == null) return;
        
        // 在准备阶段，红圆从初始大小缩小到比攻击范围稍大
        float currentRadius = Mathf.Lerp(indicatorStartRadius, attackRadius * 1.2f, progress);
        float scale = currentRadius / (indicatorStartRadius);
        
        warningIndicatorVisual.transform.localScale = Vector3.one * scale;
    }
    
    /// <summary>
    /// 在攻击阶段更新警告指示器大小
    /// </summary>
    /// <param name="progress">攻击进度（0-1）</param>
    private void UpdateWarningIndicatorSizeDuringAttack(float progress)
    {
        if (warningIndicatorVisual == null) return;
        
        // 在攻击阶段，红圆从1.2倍攻击范围缩小到攻击范围大小
        float startRadius = attackRadius * 1.2f;
        float currentRadius = Mathf.Lerp(startRadius, attackRadius, progress);
        float scale = currentRadius / indicatorStartRadius;
        
        warningIndicatorVisual.transform.localScale = Vector3.one * scale;
    }
    
    /// <summary>
    /// 显示攻击效果
    /// </summary>
    /// <param name="hitPlayer">是否击中玩家</param>
    private void ShowAttackEffect(bool hitPlayer)
    {
        if (attackAreaRenderer != null)
        {
            // 攻击时让黑圆闪烁
            StartCoroutine(FlashAttackArea());
        }
        
        // 可以在这里添加更多视觉效果，如粒子系统、震屏等
        if (hitPlayer)
        {
            Debug.Log($"{gameObject.name} area attack hit player!");
            // 添加击中特效
        }
        else
        {
            Debug.Log($"{gameObject.name} area attack missed!");
        }
    }
    
    /// <summary>
    /// 攻击区域闪烁效果
    /// </summary>
    /// <returns></returns>
    private IEnumerator FlashAttackArea()
    {
        if (attackAreaRenderer == null) yield break;
        
        Color originalColor = attackAreaRenderer.color;
        Color flashColor = Color.white;
        
        // 闪烁3次
        for (int i = 0; i < 3; i++)
        {
            attackAreaRenderer.color = flashColor;
            yield return new WaitForSeconds(0.1f);
            attackAreaRenderer.color = originalColor;
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    /// <summary>
    /// 清理区域攻击的视觉效果
    /// </summary>
    private void CleanupAreaVisuals()
    {
        if (attackAreaVisual != null)
        {
            Destroy(attackAreaVisual);
            attackAreaVisual = null;
        }
        
        if (warningIndicatorVisual != null)
        {
            Destroy(warningIndicatorVisual);
            warningIndicatorVisual = null;
        }
    }
    
    /// <summary>
    /// 当攻击被中断时清理视觉效果
    /// </summary>
    public override void InterruptAttack()
    {
        base.InterruptAttack();
        CleanupAreaVisuals();
    }
    
    /// <summary>
    /// 设置攻击半径
    /// </summary>
    /// <param name="radius">攻击半径</param>
    public void SetAttackRadius(float radius)
    {
        attackRadius = radius;
    }
    
    /// <summary>
    /// 设置指示器初始半径
    /// </summary>
    /// <param name="radius">初始半径</param>
    public void SetIndicatorStartRadius(float radius)
    {
        indicatorStartRadius = radius;
    }
    
    // 编辑器辅助方法
    private void OnDrawGizmosSelected()
    {
        // 在Scene视图中显示攻击范围
        if (!Application.isPlaying)
        {
            Vector3 center = usePlayerCurrentPosition && playerTransform != null ? 
                            playerTransform.position : fixedTargetPosition;
            
            Gizmos.color = attackAreaColor;
            Gizmos.DrawWireSphere(center, attackRadius);
            
            Gizmos.color = warningIndicatorColor;
            Gizmos.DrawWireSphere(center, indicatorStartRadius);
        }
        else if (targetPosition != Vector3.zero)
        {
            Gizmos.color = attackAreaColor;
            Gizmos.DrawWireSphere(targetPosition, attackRadius);
        }
    }
}
