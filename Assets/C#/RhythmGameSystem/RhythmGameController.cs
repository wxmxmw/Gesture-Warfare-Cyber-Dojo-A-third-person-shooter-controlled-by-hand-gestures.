using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 音游风格小游戏控制器
/// 实现类似于音游"掉方块然后在限定时间段打掉"的机制
/// </summary>
public class RhythmGameController : MonoBehaviour
{
    [Header("节奏游戏设置")]
    [SerializeField] private bool isActive = false;
    [SerializeField] private Canvas gameCanvas;
    [SerializeField] private GameObject judgmentCirclePrefab;
    [SerializeField] private GameObject judgmentArcPrefab;
    
    [Header("游戏参数")]
    [SerializeField] private float circleRadius = 150f;
    [SerializeField] private float arcSpawnDistance = 400f;
    [SerializeField] private float arcMoveSpeed = 100f;
    [SerializeField] private float judgmentWindow = 1f; // 判定窗口时间
    [SerializeField] private float arcSpawnInterval = 2f; // 圆弧生成间隔
    [SerializeField] private float arcAngleRange = 60f; // 圆弧的圆心角范围
    [SerializeField] private float sectorJudgmentAngle = 30f; // 扇形判定角度范围
    
    [Header("退出条件")]
    [SerializeField] private RhythmGameExitCondition exitCondition = RhythmGameExitCondition.Timer;
    [SerializeField] private float gameDuration = 30f;
    [SerializeField] private int targetArcsToDestroy = 10;
    
    [Header("伤害设置")]
    [SerializeField] private float successDamageToEnemies = 20f;
    [SerializeField] private float failureDamageToPlayer = 10f;
    
    [Header("视觉效果")]
    [SerializeField] private Color judgmentRangeColor = new Color(1f, 1f, 0f, 0.3f);
    [SerializeField] private AnimationCurve alphaAnimationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("颜色设置")]
    [SerializeField] private Color startColor = Color.white; // 开始颜色
    [SerializeField] private Color warningColor = Color.yellow; // 警告颜色  
    [SerializeField] private Color endColor = Color.red; // 结束颜色
    
    [Header("屏幕遮罩")]
    [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.3f); // 屏幕遮罩颜色
    
    // 私有变量
    private JudgmentCircle judgmentCircle;
    private List<JudgmentArc> activeArcs = new List<JudgmentArc>();
    private Player player;
    private CrosshairController crosshair;
    private Coroutine gameCoroutine;
    private Coroutine arcSpawnCoroutine;
    
    // 视觉效果
    private GameObject screenOverlay; // 屏幕遮罩
    private float nextArcAngle; // 下一个圆弧的角度（即将生成的）
    private float previewArcAngle; // 预览圆弧的角度（再下一个）
    private bool hasNextArc = false;
    private bool hasPreviewArc = false;
    
    // 游戏状态
    private float gameTimer;
    private int arcsDestroyed;
    private bool gameRunning;
    
    // 事件
    public System.Action OnRhythmGameStarted;
    public System.Action OnRhythmGameEnded;
    public System.Action<bool> OnJudgmentResult; // true=成功, false=失败
    public System.Action OnJudgmentSuccessSound; // 判定成功音效事件
    public System.Action OnJudgmentFailureSound; // 判定失败音效事件
    
    /// <summary>
    /// 退出条件枚举
    /// </summary>
    public enum RhythmGameExitCondition
    {
        Timer,              // 持续指定时间
        ArcsDestroyed,      // 消除指定数量的判定块
        AllEnemiesDead      // 所有敌人死亡
    }
    
    private void Awake()
    {
        // 获取必要的组件引用
        player = FindObjectOfType<Player>();
        crosshair = FindObjectOfType<CrosshairController>();
        
        if (gameCanvas == null)
        {
            gameCanvas = FindObjectOfType<Canvas>();
        }
    }
    
    private void Start()
    {
        // 初始化时不激活游戏
        SetGameActive(false);
    }
    
    private void Update()
    {
        if (!gameRunning) return;
        
        // 更新游戏计时器
        gameTimer += Time.deltaTime;
        
        // 更新倒计时显示
        UpdateCountdownDisplay();
        
        // 检查退出条件
        CheckExitConditions();
        
        // 更新所有判定圆弧
        UpdateJudgmentArcs();
    }
    
    /// <summary>
    /// 启动节奏游戏
    /// </summary>
    /// <param name="condition">退出条件</param>
    /// <param name="duration">持续时间（如果条件是Timer）</param>
    /// <param name="targetCount">目标消除数量（如果条件是ArcsDestroyed）</param>
    public void StartRhythmGame(RhythmGameExitCondition condition = RhythmGameExitCondition.Timer, 
                               float duration = 30f, int targetCount = 10)
    {
        if (gameRunning)
        {
            Debug.LogWarning("Rhythm game is already running!");
            return;
        }
        
        // 设置参数
        exitCondition = condition;
        gameDuration = duration;
        targetArcsToDestroy = targetCount;
        
        // 重置游戏状态
        gameTimer = 0f;
        arcsDestroyed = 0;
        gameRunning = true;
        
        // 激活游戏
        SetGameActive(true);
        
        // 创建屏幕遮罩
        CreateScreenOverlay();
        
        // 创建判定圆圈
        CreateJudgmentCircle();
        
        // 开始生成圆弧
        arcSpawnCoroutine = StartCoroutine(SpawnArcsCoroutine());
        
        // 触发事件
        OnRhythmGameStarted?.Invoke();
        
        Debug.Log($"Rhythm Game Started! Condition: {condition}, Duration: {duration}, Target: {targetCount}");
    }
    
    /// <summary>
    /// 停止节奏游戏
    /// </summary>
    public void StopRhythmGame()
    {
        if (!gameRunning) return;
        
        gameRunning = false;
        
        // 停止圆弧生成
        if (arcSpawnCoroutine != null)
        {
            StopCoroutine(arcSpawnCoroutine);
            arcSpawnCoroutine = null;
        }
        
        // 清理所有圆弧
        ClearAllArcs();
        
        // 隐藏预览和倒计时（在销毁judgmentCircle之前）
        if (judgmentCircle != null)
        {
            judgmentCircle.HideNextJudgmentPreview();
            judgmentCircle.SetupCountdownDisplay(false); // 禁用倒计时显示
        }
        
        // 销毁判定圆圈
        if (judgmentCircle != null)
        {
            Destroy(judgmentCircle.gameObject);
            judgmentCircle = null;
        }
        
        // 销毁屏幕遮罩
        if (screenOverlay != null)
        {
            Destroy(screenOverlay);
            screenOverlay = null;
        }
        
        // 重置角度状态
        hasNextArc = false;
        hasPreviewArc = false;
        
        // 关闭游戏
        SetGameActive(false);
        
        // 触发事件
        OnRhythmGameEnded?.Invoke();
        
        Debug.Log("Rhythm Game Stopped!");
    }
    
    /// <summary>
    /// 设置游戏激活状态
    /// </summary>
    /// <param name="active">激活状态</param>
    private void SetGameActive(bool active)
    {
        isActive = active;
        // 不要禁用整个GameObject，只是标记状态
        // gameObject.SetActive(active); // 这会影响到Boss的显示！
    }
    
    /// <summary>
    /// 创建判定圆圈
    /// </summary>
    private void CreateJudgmentCircle()
    {
        if (gameCanvas == null) return;
        
        GameObject circleObj;
        
        // 如果有预制体就使用预制体，否则动态创建
        if (judgmentCirclePrefab != null)
        {
            circleObj = Instantiate(judgmentCirclePrefab, gameCanvas.transform);
            judgmentCircle = circleObj.GetComponent<JudgmentCircle>();
        }
        else
        {
            // 动态创建判定圆圈
            circleObj = CreateDynamicJudgmentCircle();
            judgmentCircle = circleObj.GetComponent<JudgmentCircle>();
        }
        
        if (judgmentCircle != null)
        {
            judgmentCircle.Initialize(circleRadius, judgmentRangeColor);
            // 设置扇形判定角度
            judgmentCircle.SetSectorAngle(sectorJudgmentAngle);
            // 启用倒计时显示
            judgmentCircle.SetupCountdownDisplay(true);
            // 播放进入动画
            StartCoroutine(PlayEntranceAnimation());
        }
    }
    
    /// <summary>
    /// 动态创建判定圆圈
    /// </summary>
    /// <returns>圆圈GameObject</returns>
    private GameObject CreateDynamicJudgmentCircle()
    {
        GameObject circleObj = new GameObject("JudgmentCircle");
        circleObj.transform.SetParent(gameCanvas.transform, false);
        
        // 添加RectTransform组件
        RectTransform rectTransform = circleObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(circleRadius * 2, circleRadius * 2);
        
        // 添加Image组件
        Image circleImage = circleObj.AddComponent<Image>();
        circleImage.sprite = CreateCircleSprite(256);
        circleImage.color = new Color(1f, 1f, 1f, 0.8f);
        circleImage.raycastTarget = false;
        
        // 添加JudgmentCircle组件
        JudgmentCircle judgmentCircleComponent = circleObj.AddComponent<JudgmentCircle>();
        
        Debug.Log("Dynamically created JudgmentCircle");
        return circleObj;
    }
    
    /// <summary>
    /// 生成判定圆弧的协程
    /// </summary>
    /// <returns></returns>
    private IEnumerator SpawnArcsCoroutine()
    {
        // 预生成前两个圆弧的角度
        PrepareInitialArcs();
        
        while (gameRunning)
        {
            SpawnJudgmentArc();
            yield return new WaitForSeconds(arcSpawnInterval);
            
            // 准备下一个圆弧
            AdvanceArcQueue();
        }
    }
    
    /// <summary>
    /// 生成判定圆弧
    /// </summary>
    private void SpawnJudgmentArc()
    {
        if (gameCanvas == null || judgmentCircle == null) return;
        
        // 使用预设的角度
        float angle = hasNextArc ? nextArcAngle : Random.Range(0f, 360f);
        
        GameObject arcObj;
        
        // 如果有预制体就使用预制体，否则动态创建
        if (judgmentArcPrefab != null)
        {
            arcObj = Instantiate(judgmentArcPrefab, gameCanvas.transform);
            arcObj.transform.position = judgmentCircle.transform.position; // 直接在圆圈位置
            arcObj.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        else
        {
            // 动态创建判定圆弧（直接在圆圈位置）
            arcObj = CreateDynamicJudgmentArc(judgmentCircle.transform.position, angle);
        }
        
        JudgmentArc arc = arcObj.GetComponent<JudgmentArc>();
        if (arc != null)
        {
            arc.Initialize(angle, arcMoveSpeed, judgmentWindow, this);
            // 设置圆心角和颜色
            arc.SetArcAngleRange(arcAngleRange);
            arc.SetColors(startColor, warningColor, endColor);
            activeArcs.Add(arc);
        }
    }
    
    /// <summary>
    /// 动态创建判定圆弧
    /// </summary>
    /// <param name="position">生成位置</param>
    /// <param name="angle">角度</param>
    /// <returns>圆弧GameObject</returns>
    private GameObject CreateDynamicJudgmentArc(Vector3 position, float angle)
    {
        GameObject arcObj = new GameObject("JudgmentArc");
        arcObj.transform.SetParent(gameCanvas.transform, false);
        
        // 圆弧应该显示在圆圈位置，而不是从远处移动
        if (judgmentCircle != null)
        {
            arcObj.transform.position = judgmentCircle.transform.position;
        }
        else
        {
            arcObj.transform.position = position;
        }
        
        // 设置旋转角度
        arcObj.transform.rotation = Quaternion.Euler(0, 0, angle);
        
        // 添加RectTransform组件 - 使用和圆圈相同的大小
        RectTransform rectTransform = arcObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(circleRadius * 2, circleRadius * 2);
        
        // 添加Image组件
        Image arcImage = arcObj.AddComponent<Image>();
        arcImage.sprite = CreateArrowSprite(256); // 使用箭头精灵
        arcImage.color = startColor; // 使用设置的开始颜色
        arcImage.raycastTarget = false;
        
        // 添加JudgmentArc组件
        JudgmentArc judgmentArcComponent = arcObj.AddComponent<JudgmentArc>();
        
        Debug.Log($"Dynamically created JudgmentArc at circle position with angle range: {arcAngleRange}");
        return arcObj;
    }
    
    /// <summary>
    /// 更新所有判定圆弧
    /// </summary>
    private void UpdateJudgmentArcs()
    {
        for (int i = activeArcs.Count - 1; i >= 0; i--)
        {
            if (activeArcs[i] == null)
            {
                activeArcs.RemoveAt(i);
                continue;
            }
            
            // 检查圆弧是否需要判定
            if (activeArcs[i].IsInJudgmentRange())
            {
                PerformJudgment(activeArcs[i]);
            }
        }
    }
    
    /// <summary>
    /// 执行判定
    /// </summary>
    /// <param name="arc">判定圆弧</param>
    private void PerformJudgment(JudgmentArc arc)
    {
        if (arc == null || crosshair == null) return;
        
        // 检查玩家准心是否在扇形范围内
        bool isInSector = IsPlayerAimInSector(arc.GetAngle());
        
        if (isInSector)
        {
            // 判定成功
            OnJudgmentSuccess(arc);
        }
        else
        {
            // 判定失败
            OnJudgmentFailure(arc);
        }
        
        // 移除圆弧
        RemoveArc(arc);
    }
    
    /// <summary>
    /// 判定成功处理
    /// </summary>
    /// <param name="arc">圆弧</param>
    private void OnJudgmentSuccess(JudgmentArc arc)
    {        
        // 播放成功效果
        PlaySuccessEffect();
        
        // 对所有敌人造成伤害
        DamageAllEnemies(successDamageToEnemies);
        
        // 增加消除计数
        arcsDestroyed++;
        
        // 触发事件
        OnJudgmentResult?.Invoke(true);
        OnJudgmentSuccessSound?.Invoke(); // 触发成功音效事件
    }
    
    /// <summary>
    /// 判定失败处理
    /// </summary>
    /// <param name="arc">圆弧</param>
    private void OnJudgmentFailure(JudgmentArc arc)
    {
        Debug.Log("Judgment Failure!");
        
        // 播放失败效果
        PlayFailureEffect();
        
        // 对玩家造成伤害
        if (player != null)
        {
            player.TakeDamage(failureDamageToPlayer);
        }
        
        // 触发事件
        OnJudgmentResult?.Invoke(false);
        OnJudgmentFailureSound?.Invoke(); // 触发失败音效事件
    }
    
    /// <summary>
    /// 检查玩家准心是否在扇形范围内
    /// </summary>
    /// <param name="arcAngle">圆弧角度</param>
    /// <returns></returns>
    private bool IsPlayerAimInSector(float arcAngle)
    {
        if (crosshair == null || judgmentCircle == null) return false;
        
        // 获取准心位置（世界坐标）
        Vector3 crosshairWorldPos = crosshair.transform.position;
        Vector3 circleWorldPos = judgmentCircle.transform.position;
        
        // 转换为屏幕坐标进行计算（更准确）
        Camera cam = Camera.main;
        if (cam == null) return false;
        
        Vector2 crosshairScreenPos = cam.WorldToScreenPoint(crosshairWorldPos);
        Vector2 circleScreenPos = cam.WorldToScreenPoint(circleWorldPos);
        
        // 计算从圆心到准心的向量
        Vector2 toAim = crosshairScreenPos - circleScreenPos;
        
        // 检查距离（准心是否靠近圆圈）
        float distance = toAim.magnitude;
        float maxDistance = circleRadius * 2f; // 允许的最大距离
        
        if (distance > maxDistance)
        {
            Debug.Log($"准心距离圆圈太远: {distance} > {maxDistance}");
            return false;
        }
        
        // 计算角度
        float aimAngle = Mathf.Atan2(toAim.y, toAim.x) * Mathf.Rad2Deg;
        if (aimAngle < 0) aimAngle += 360f;
        
        // 计算角度差
        float angleDiff = Mathf.Abs(Mathf.DeltaAngle(aimAngle, arcAngle));
        
        bool inRange = angleDiff <= sectorJudgmentAngle * 0.5f;
                
        return inRange;
    }
    
    /// <summary>
    /// 对所有敌人造成伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    private void DamageAllEnemies(float damage)
    {
        // 查找所有敌人
        EnemyWithNewAttackSystem[] enemies = FindObjectsOfType<EnemyWithNewAttackSystem>();
        foreach (var enemy in enemies)
        {
            if (!enemy.IsDead)
            {
                enemy.TakeDamage(damage);
            }
        }
        
        // 也检查Boss
        BossController[] bosses = FindObjectsOfType<BossController>();
        foreach (var boss in bosses)
        {
            if (!boss.IsDead)
            {
                boss.TakeDamage(damage);
            }
        }
        
    }
    
    /// <summary>
    /// 移除圆弧
    /// </summary>
    /// <param name="arc">要移除的圆弧</param>
    public void RemoveArc(JudgmentArc arc)
    {
        if (arc != null)
        {
            activeArcs.Remove(arc);
            Destroy(arc.gameObject);
        }
    }
    
    /// <summary>
    /// 清理所有圆弧
    /// </summary>
    private void ClearAllArcs()
    {
        foreach (var arc in activeArcs)
        {
            if (arc != null)
            {
                Destroy(arc.gameObject);
            }
        }
        activeArcs.Clear();
    }
    
    /// <summary>
    /// 检查退出条件
    /// </summary>
    private void CheckExitConditions()
    {
        bool shouldExit = false;
        
        switch (exitCondition)
        {
            case RhythmGameExitCondition.Timer:
                shouldExit = gameTimer >= gameDuration;
                break;
                
            case RhythmGameExitCondition.ArcsDestroyed:
                shouldExit = arcsDestroyed >= targetArcsToDestroy;
                break;
                
            case RhythmGameExitCondition.AllEnemiesDead:
                shouldExit = AreAllEnemiesDead();
                break;
        }
        
        if (shouldExit)
        {
            StopRhythmGame();
        }
    }
    
    /// <summary>
    /// 检查所有敌人是否都死亡
    /// </summary>
    /// <returns></returns>
    private bool AreAllEnemiesDead()
    {
        // 检查普通敌人
        EnemyWithNewAttackSystem[] enemies = FindObjectsOfType<EnemyWithNewAttackSystem>();
        foreach (var enemy in enemies)
        {
            if (!enemy.IsDead) return false;
        }
        
        // 检查Boss
        BossController[] bosses = FindObjectsOfType<BossController>();
        foreach (var boss in bosses)
        {
            if (!boss.IsDead) return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 设置预制体引用
    /// </summary>
    /// <param name="circlePrefab">判定圆圈预制体</param>
    /// <param name="arcPrefab">判定圆弧预制体</param>
    /// <param name="canvas">游戏画布</param>
    public void SetPrefabs(GameObject circlePrefab, GameObject arcPrefab, Canvas canvas)
    {
        judgmentCirclePrefab = circlePrefab;
        judgmentArcPrefab = arcPrefab;
        gameCanvas = canvas;
    }
    
    /// <summary>
    /// 创建圆形边框精灵
    /// </summary>
    /// <param name="size">尺寸</param>
    /// <returns>圆形边框精灵</returns>
    private Sprite CreateCircleSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size);
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float outerRadius = size * 0.45f;
        float innerRadius = size * 0.40f; // 创建边框效果
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);
                
                // 只在边框范围内绘制
                if (distance >= innerRadius && distance <= outerRadius)
                {
                    // 边框渐变效果
                    float alpha = 1f - Mathf.Abs(distance - (innerRadius + outerRadius) * 0.5f) / ((outerRadius - innerRadius) * 0.5f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(alpha)));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
    
    /// <summary>
    /// 创建箭头精灵（用于指示判定范围）
    /// </summary>
    /// <param name="size">尺寸</param>
    /// <returns>箭头精灵</returns>
    private Sprite CreateArrowSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size);
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        
        // 箭头参数
        float arrowLength = size * 0.35f; // 箭头长度
        float arrowWidth = size * 0.15f;  // 箭头宽度
        float headLength = size * 0.12f;  // 箭头头部长度
        float headWidth = size * 0.25f;   // 箭头头部宽度
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2 pos = new Vector2(x, y);
                Vector2 localPos = pos - center;
                
                // 旋转到水平向右方向（0度）
                float distance = localPos.x + arrowLength * 0.5f; // 距离箭头起点的距离
                float width = Mathf.Abs(localPos.y);
                
                bool inArrow = false;
                float alpha = 0f;
                
                if (distance >= 0 && distance <= arrowLength)
                {
                    if (distance <= arrowLength - headLength)
                    {
                        // 箭头杆部分
                        if (width <= arrowWidth * 0.5f)
                        {
                            inArrow = true;
                            alpha = 1f - width / (arrowWidth * 0.5f) * 0.3f; // 边缘稍淡
                        }
                    }
                    else
                    {
                        // 箭头头部
                        float headProgress = (distance - (arrowLength - headLength)) / headLength;
                        float headMaxWidth = headWidth * 0.5f * (1f - headProgress);
                        
                        if (width <= headMaxWidth)
                        {
                            inArrow = true;
                            alpha = 1f - width / headMaxWidth * 0.2f; // 边缘稍淡
                        }
                    }
                }
                
                if (inArrow)
                {
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
    
    /// <summary>
    /// 创建屏幕遮罩
    /// </summary>
    private void CreateScreenOverlay()
    {
        if (gameCanvas == null) return;
        
        screenOverlay = new GameObject("ScreenOverlay");
        screenOverlay.transform.SetParent(gameCanvas.transform, false);
        
        // 设置为最底层，不遮挡其他UI
        screenOverlay.transform.SetAsFirstSibling();
        
        // 添加RectTransform - 覆盖整个屏幕
        RectTransform rectTransform = screenOverlay.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        
        // 添加Image组件
        Image overlayImage = screenOverlay.AddComponent<Image>();
        overlayImage.color = overlayColor;
        overlayImage.raycastTarget = false;
        
        Debug.Log("Created screen overlay for rhythm game");
    }
    
    /// <summary>
    /// 准备初始的两个圆弧角度
    /// </summary>
    private void PrepareInitialArcs()
    {
        // 准备即将生成的圆弧角度
        nextArcAngle = Random.Range(0f, 360f);
        hasNextArc = true;
        
        // 准备预览圆弧角度（再下一个）
        previewArcAngle = Random.Range(0f, 360f);
        hasPreviewArc = true;
        
        // 显示预览（显示再下一个圆弧的位置）
        if (judgmentCircle != null)
        {
            judgmentCircle.ShowNextJudgmentPreview(previewArcAngle);
        }
        
    }
    
    /// <summary>
    /// 推进圆弧队列（生成完圆弧后调用）
    /// </summary>
    private void AdvanceArcQueue()
    {
        // 将预览角度变为下一个生成角度
        nextArcAngle = previewArcAngle;
        hasNextArc = true;
        
        // 生成新的预览角度
        previewArcAngle = Random.Range(0f, 360f);
        hasPreviewArc = true;
        
        // 更新预览显示
        if (judgmentCircle != null)
        {
            judgmentCircle.ShowNextJudgmentPreview(previewArcAngle);
        }
        
    }
    
    /// <summary>
    /// 更新倒计时显示
    /// </summary>
    private void UpdateCountdownDisplay()
    {
        if (judgmentCircle == null) return;
        
        float remainingTime = 0f;
        string displayText = "";
        
        switch (exitCondition)
        {
            case RhythmGameExitCondition.Timer:
                remainingTime = gameDuration - gameTimer;
                if (remainingTime > 0f)
                {
                    displayText = Mathf.Ceil(remainingTime).ToString();
                }
                else
                {
                    displayText = "0";
                }
                break;
                
            case RhythmGameExitCondition.ArcsDestroyed:
                int remainingArcs = targetArcsToDestroy - arcsDestroyed;
                displayText = remainingArcs.ToString();
                break;
                
            case RhythmGameExitCondition.AllEnemiesDead:
                displayText = "♦"; // 特殊符号表示消灭所有敌人模式
                break;
        }
        
        judgmentCircle.UpdateCountdownText(displayText);
    }
    
    /// <summary>
    /// 播放进入动画
    /// </summary>
    /// <returns></returns>
    private IEnumerator PlayEntranceAnimation()
    {
        if (judgmentCircle == null) yield break;
        
        // 初始隐藏所有UI元素
        Transform circleTransform = judgmentCircle.transform;
        circleTransform.localScale = Vector3.zero;
        
        // 隐藏倒计时和扇形
        judgmentCircle.SetCountdownVisible(false);
        judgmentCircle.HideJudgmentSector();
        
        float animDuration = 1f;
        float timer = 0f;
        
        // 第一阶段：圆圈从点逐渐展开 (0-0.4s)
        while (timer < animDuration * 0.4f)
        {
            timer += Time.deltaTime;
            float t = timer / (animDuration * 0.4f);
            float ease = Mathf.SmoothStep(0f, 1f, t); // 平滑曲线
            
            // 圆圈缩放动画
            circleTransform.localScale = Vector3.one * ease;
            
            yield return null;
        }
        
        // 第二阶段：显示倒计时 (0.4-0.7s)
        judgmentCircle.SetCountdownVisible(true);
        
        // 第三阶段：显示预览范围 (0.7-1.0s)
        yield return new WaitForSeconds(0.3f);
        
        // 完成动画，开始正常游戏逻辑
        Debug.Log("Entrance animation completed");
    }
    
    /// <summary>
    /// 播放成功判定效果
    /// </summary>
    private void PlaySuccessEffect()
    {
        if (crosshair == null || gameCanvas == null) return;
        
        StartCoroutine(CreateRippleEffect());
    }
    
    /// <summary>
    /// 创建波纹效果
    /// </summary>
    /// <returns></returns>
    private IEnumerator CreateRippleEffect()
    {
        // 在准心位置创建波纹
        Vector3 crosshairPos = crosshair.transform.position;
        
        // 转换为屏幕坐标
        Camera cam = Camera.main;
        if (cam == null) yield break;
        
        Vector2 screenPos = cam.WorldToScreenPoint(crosshairPos);
        
        // 创建波纹GameObject
        GameObject ripple = new GameObject("SuccessRipple");
        ripple.transform.SetParent(gameCanvas.transform, false);
        
        // 设置位置
        RectTransform rippleRect = ripple.AddComponent<RectTransform>();
        rippleRect.position = screenPos;
        rippleRect.sizeDelta = new Vector2(50f, 50f);
        
        // 添加Image组件
        Image rippleImage = ripple.AddComponent<Image>();
        rippleImage.sprite = CreateRippleSprite();
        rippleImage.color = new Color(0f, 1f, 0f, 0.8f); // 绿色
        rippleImage.raycastTarget = false;
        
        // 波纹扩散动画
        float duration = 0.5f;
        float timer = 0f;
        Vector3 startScale = Vector3.one * 0.5f;
        Vector3 endScale = Vector3.one * 3f;
        Color startColor = rippleImage.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            
            // 缩放和淡出
            ripple.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            rippleImage.color = Color.Lerp(startColor, endColor, t);
            
            yield return null;
        }
        
        // 销毁波纹
        Destroy(ripple);
    }
    
    /// <summary>
    /// 播放失败判定效果
    /// </summary>
    private void PlayFailureEffect()
    {
        if (judgmentCircle == null || gameCanvas == null) return;
        
        StartCoroutine(CreateFailureBullet());
    }
    
    /// <summary>
    /// 创建失败子弹效果
    /// </summary>
    /// <returns></returns>
    private IEnumerator CreateFailureBullet()
    {
        // 动态寻找玩家血条UI位置
        Vector2 playerHealthPos = FindPlayerHealthBarPosition();
        Vector2 circlePos = judgmentCircle.transform.position;
        
        // 创建子弹GameObject
        GameObject bullet = new GameObject("FailureBullet");
        bullet.transform.SetParent(gameCanvas.transform, false);
        
        // 设置起始位置
        RectTransform bulletRect = bullet.AddComponent<RectTransform>();
        bulletRect.position = circlePos;
        bulletRect.sizeDelta = new Vector2(20f, 20f);
        
        // 添加Image组件
        Image bulletImage = bullet.AddComponent<Image>();
        bulletImage.sprite = CreateBulletSprite();
        bulletImage.raycastTarget = false;
        
        // 五彩斑斓效果 - 不断变化颜色
        float duration = 0.8f;
        float timer = 0f;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            
            // 曲线移动到血条位置
            Vector2 currentPos = Vector2.Lerp(circlePos, playerHealthPos, EaseOutQuad(t));
            bulletRect.position = currentPos;
            
            // 五彩颜色变化
            float hue = (Time.time * 3f) % 1f; // 快速变化色调
            bulletImage.color = Color.HSVToRGB(hue, 1f, 1f);
            
            // 旋转效果
            bullet.transform.Rotate(0, 0, 360f * Time.deltaTime * 2f);
            
            yield return null;
        }
        
        // 碰撞效果（简单的闪烁）
        yield return StartCoroutine(CreateImpactFlash(playerHealthPos));
        
        // 销毁子弹
        Destroy(bullet);
    }
    
    /// <summary>
    /// 创建碰撞闪光效果
    /// </summary>
    /// <param name="position">碰撞位置</param>
    /// <returns></returns>
    private IEnumerator CreateImpactFlash(Vector2 position)
    {
        GameObject flash = new GameObject("ImpactFlash");
        flash.transform.SetParent(gameCanvas.transform, false);
        
        RectTransform flashRect = flash.AddComponent<RectTransform>();
        flashRect.position = position;
        flashRect.sizeDelta = new Vector2(80f, 80f);
        
        Image flashImage = flash.AddComponent<Image>();
        flashImage.sprite = CreateRippleSprite();
        flashImage.color = Color.red;
        flashImage.raycastTarget = false;
        
        // 快速闪烁
        for (int i = 0; i < 3; i++)
        {
            flashImage.color = Color.red;
            yield return new WaitForSeconds(0.05f);
            flashImage.color = Color.clear;
            yield return new WaitForSeconds(0.05f);
        }
        
        Destroy(flash);
    }
    
    /// <summary>
    /// 创建波纹精灵
    /// </summary>
    /// <returns></returns>
    private Sprite CreateRippleSprite()
    {
        int size = 64;
        Texture2D texture = new Texture2D(size, size);
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float outerRadius = size * 0.45f;
        float innerRadius = size * 0.35f;
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);
                
                if (distance >= innerRadius && distance <= outerRadius)
                {
                    float alpha = 1f - Mathf.Abs(distance - (innerRadius + outerRadius) * 0.5f) / ((outerRadius - innerRadius) * 0.5f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
    
    /// <summary>
    /// 创建子弹精灵
    /// </summary>
    /// <returns></returns>
    private Sprite CreateBulletSprite()
    {
        int size = 32;
        Texture2D texture = new Texture2D(size, size);
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.4f;
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);
                
                if (distance <= radius)
                {
                    float alpha = 1f - (distance / radius) * 0.3f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
    
    /// <summary>
    /// 寻找玩家血条位置
    /// </summary>
    /// <returns>血条的屏幕位置</returns>
    private Vector2 FindPlayerHealthBarPosition()
    {
        // 默认位置（屏幕左上角）
        Vector2 defaultPos = new Vector2(Screen.width - 50f, Screen.height / 2);
        
        if (gameCanvas == null) return defaultPos;
        
        // 在Canvas下寻找名为PlayerHPSlider的物体
        Transform playerHPSlider = gameCanvas.transform.Find("PlayerHPSlider");
        
        if (playerHPSlider == null)
        {
            // 如果找不到，尝试递归搜索整个Canvas
            playerHPSlider = FindChildWithName(gameCanvas.transform, "PlayerHPSlider");
        }
        
        if (playerHPSlider != null)
        {
            // 检查是否有HPSlider组件
            HPSilder hpSlider = playerHPSlider.GetComponent<HPSilder>();
            if (hpSlider != null)
            {
                // 获取血条的世界位置并转换为屏幕位置
                RectTransform hpRect = playerHPSlider.GetComponent<RectTransform>();
                if (hpRect != null)
                {
                    // 使用RectTransform的position（已经是屏幕坐标）
                    return hpRect.position;
                }
            }
        }
        
        Debug.LogWarning("Could not find PlayerHPSlider with HPSlider component, using default position");
        return defaultPos;
    }
    
    /// <summary>
    /// 递归搜索子物体
    /// </summary>
    /// <param name="parent">父物体</param>
    /// <param name="name">要搜索的名称</param>
    /// <returns>找到的Transform，如果没找到返回null</returns>
    private Transform FindChildWithName(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name) return child;
            
            // 递归搜索子物体
            Transform result = FindChildWithName(child, name);
            if (result != null) return result;
        }
        
        return null;
    }
    
    /// <summary>
    /// 缓出二次方缓动函数
    /// </summary>
    /// <param name="t">时间参数(0-1)</param>
    /// <returns></returns>
    private float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }
    
    // 公共属性
    public bool IsActive => isActive;
    public bool IsGameRunning => gameRunning;
    public float GameTimer => gameTimer;
    public int ArcsDestroyed => arcsDestroyed;
    public JudgmentCircle JudgmentCircle => judgmentCircle;
    public float ArcAngleRange => arcAngleRange;
    public float SectorJudgmentAngle => sectorJudgmentAngle;
    public Color StartColor => startColor;
    public Color WarningColor => warningColor;
    public Color EndColor => endColor;
}
