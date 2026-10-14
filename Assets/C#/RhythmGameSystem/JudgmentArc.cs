using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 判定圆弧组件
/// 从远处向中心圆圈移动的圆弧状判定块
/// </summary>
[RequireComponent(typeof(Image))]
public class JudgmentArc : MonoBehaviour
{
    [Header("圆弧设置")]
    [SerializeField] private float arcAngle = 0f; // 圆弧角度方向
    [SerializeField] private float arcLength = 60f; // 圆弧长度（角度）
    [SerializeField] private float arcThickness = 20f;
    
    [Header("判定设置")]
    [SerializeField] private float judgmentWindow = 1f;
    [SerializeField] private float totalLifetime = 3f; // 圆弧总生存时间
    
    [Header("视觉效果")]
    [SerializeField] private AnimationCurve colorTransition = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    // 颜色现在从RhythmGameController设置
    private Color startColor = Color.white; // 开始为白色 
    private Color warningColor = Color.yellow; // 警告颜色
    private Color endColor = Color.red; // 结束为红色
    
    // 私有变量
    private Image arcImage;
    private RectTransform rectTransform;
    private RhythmGameController gameController;
    private bool isInJudgmentRange = false;
    private float totalLifeTimer = 0f;
    
    // 动画和效果
    private float remainingJudgmentTime;
    private bool isJudging = false;
    private bool hasStartedJudgment = false;
    
    private void Awake()
    {
        arcImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
    }
    
    private void Start()
    {
        SetupArc();
    }
    
    private void Update()
    {
        UpdateLifetime();
        UpdateVisualEffects();
        UpdateJudgmentLogic();
    }
    
    /// <summary>
    /// 初始化判定圆弧
    /// </summary>
    /// <param name="angle">角度方向</param>
    /// <param name="speed">移动速度（已废弃，保留兼容性）</param>
    /// <param name="judgmentTime">判定窗口时间</param>
    /// <param name="controller">游戏控制器</param>
    public void Initialize(float angle, float speed, float judgmentTime, RhythmGameController controller)
    {
        arcAngle = angle;
        judgmentWindow = judgmentTime;
        gameController = controller;
        
        // 计算总生存时间 = 准备时间 + 判定时间
        totalLifetime = 2f + judgmentTime; // 2秒准备 + 判定时间
        remainingJudgmentTime = judgmentWindow;
        totalLifeTimer = 0f;
        
        SetupArc();
    }
    
    /// <summary>
    /// 设置圆弧
    /// </summary>
    private void SetupArc()
    {
        if (arcImage == null || rectTransform == null) return;
        
        // 设置圆弧大小
        rectTransform.sizeDelta = new Vector2(arcLength, arcThickness);
        
        // 设置初始颜色
        arcImage.color = startColor;
        
        // 确保圆弧位置正确
        transform.rotation = Quaternion.Euler(0, 0, arcAngle);
        
        // 创建圆弧精灵
        if (arcImage.sprite == null)
        {
            arcImage.sprite = CreateArcSprite();
        }
        
        // 设置旋转以朝向目标
        //UpdateRotation();
    }
    
    /// <summary>
    /// 创建圆弧精灵
    /// </summary>
    /// <returns>圆弧精灵</returns>
    private Sprite CreateArcSprite()
    {
        int width = Mathf.RoundToInt(arcLength);
        int height = Mathf.RoundToInt(arcThickness);
        Texture2D texture = new Texture2D(width, height);
        
        // 创建圆弧形状
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float normalizedX = (float)x / width;
                float normalizedY = (float)y / height;
                
                // 创建圆弧形状（中间厚，两端薄）
                float distanceFromCenter = Mathf.Abs(normalizedY - 0.5f) * 2f;
                float arcCurve = Mathf.Sin(normalizedX * Mathf.PI); // 弧形
                
                float alpha = 1f - distanceFromCenter;
                alpha *= arcCurve;
                alpha = Mathf.Clamp01(alpha);
                
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }
    
    /// <summary>
    /// 更新生存时间
    /// </summary>
    private void UpdateLifetime()
    {
        if (gameController == null) return;
        
        totalLifeTimer += Time.deltaTime;
        
        // 检查是否超时
        if (totalLifeTimer >= totalLifetime)
        {
            // 超时未被判定，视为失败
            if (!hasStartedJudgment)
            {
                TriggerMissedJudgment();
            }
        }
    }
    
    /// <summary>
    /// 更新视觉效果
    /// </summary>
    private void UpdateVisualEffects()
    {
        if (arcImage == null) return;
        
        Color currentColor;
        
        if (isJudging)
        {
            // 判定阶段：根据剩余时间从白色变为红色
            float timeProgress = 1f - (remainingJudgmentTime / judgmentWindow);
            currentColor = Color.Lerp(startColor, endColor, timeProgress);
            
            // 添加紧急感的透明度变化
            float alpha = 0.7f + 0.3f * Mathf.Sin(Time.time * 10f); // 快速闪烁
            currentColor.a = alpha;
        }
        else
        {
            // 准备阶段：根据总时间进度从白色变为黄色
            float lifeProgress = totalLifeTimer / (totalLifetime - judgmentWindow);
            lifeProgress = Mathf.Clamp01(lifeProgress);
            
            if (lifeProgress < 0.5f)
            {
                // 前半段：白色到黄色
                currentColor = Color.Lerp(startColor, warningColor, lifeProgress * 2f);
            }
            else
            {
                // 后半段：黄色到红色（准备进入判定）
                currentColor = Color.Lerp(warningColor, endColor, (lifeProgress - 0.5f) * 2f);
            }
            
            currentColor.a = 0.8f; // 固定透明度
        }
        
        arcImage.color = currentColor;
    }
    
    /// <summary>
    /// 更新判定逻辑
    /// </summary>
    private void UpdateJudgmentLogic()
    {
        if (gameController == null) return;
        
        // 检查是否应该开始判定（在生存时间的最后阶段）
        float judmentStartTime = totalLifetime - judgmentWindow;
        
        if (totalLifeTimer >= judmentStartTime && !hasStartedJudgment)
        {
            StartJudgment();
        }
        
        // 始终显示判定扇形（准备阶段和判定阶段都显示）
        if (gameController.JudgmentCircle != null && arcImage != null)
        {
            float displayTime = isJudging ? remainingJudgmentTime : (totalLifetime - totalLifeTimer);
            gameController.JudgmentCircle.ShowJudgmentSector(arcAngle, displayTime, totalLifetime, isJudging, arcImage.color);
        }
        
        // 更新判定时间
        if (isJudging)
        {
            remainingJudgmentTime -= Time.deltaTime;
            
            if (remainingJudgmentTime <= 0f)
            {
                TriggerMissedJudgment();
            }
        }
    }
    
    /// <summary>
    /// 开始判定
    /// </summary>
    private void StartJudgment()
    {
        isJudging = true;
        isInJudgmentRange = true;
        hasStartedJudgment = true;
        remainingJudgmentTime = judgmentWindow;
    }
    
    /// <summary>
    /// 触发错过判定
    /// </summary>
    private void TriggerMissedJudgment()
    {
        if (gameController != null)
        {
            // 通知控制器此圆弧被错过
            // 这将触发对玩家的伤害
        }
        
        // 隐藏判定扇形
        if (gameController != null && gameController.JudgmentCircle != null)
        {
            gameController.JudgmentCircle.HideJudgmentSector();
        }
        
        // 销毁自己
        if (gameController != null)
        {
            gameController.RemoveArc(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 成功击中
    /// </summary>
    public void OnSuccessfulHit()
    {
        // 隐藏判定扇形
        if (gameController != null && gameController.JudgmentCircle != null)
        {
            gameController.JudgmentCircle.HideJudgmentSector();
        }
        
        // 播放击中效果
        StartCoroutine(PlayHitEffect());
    }
    
    /// <summary>
    /// 播放击中效果
    /// </summary>
    /// <returns></returns>
    private IEnumerator PlayHitEffect()
    {
        // 放大和淡出效果
        Vector3 originalScale = transform.localScale;
        Color originalColor = arcImage.color;
        
        float effectTime = 0.3f;
        float timer = 0f;
        
        while (timer < effectTime)
        {
            timer += Time.deltaTime;
            float t = timer / effectTime;
            
            // 放大
            transform.localScale = Vector3.Lerp(originalScale, originalScale * 1.5f, t);
            
            // 淡出
            Color color = originalColor;
            color.a = Mathf.Lerp(originalColor.a, 0f, t);
            arcImage.color = color;
            
            yield return null;
        }
        
        // 销毁
        if (gameController != null)
        {
            gameController.RemoveArc(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 设置圆弧角度范围
    /// </summary>
    /// <param name="angleRange">角度范围</param>
    public void SetArcAngleRange(float angleRange)
    {
        arcLength = angleRange;
        
        // 如果已经设置了图像，重新创建精灵
        if (arcImage != null && rectTransform != null)
        {
            arcImage.sprite = CreateArcSprite();
            rectTransform.sizeDelta = new Vector2(arcLength, arcThickness);
        }
    }
    
    /// <summary>
    /// 设置颜色
    /// </summary>
    /// <param name="start">开始颜色</param>
    /// <param name="warning">警告颜色</param>
    /// <param name="end">结束颜色</param>
    public void SetColors(Color start, Color warning, Color end)
    {
        startColor = start;
        warningColor = warning;
        endColor = end;
        
        Debug.Log($"Arc colors set - Start: {start}, Warning: {warning}, End: {end}");
    }
    
    // 公共方法
    public bool IsInJudgmentRange() => isInJudgmentRange && isJudging;
    public float GetAngle() => arcAngle;
    public float GetRemainingJudgmentTime() => remainingJudgmentTime;
    public bool IsJudging() => isJudging;
    
    // 公共属性
    public float ArcAngle => arcAngle;
    public Vector3 CurrentPosition => transform.position;
}
