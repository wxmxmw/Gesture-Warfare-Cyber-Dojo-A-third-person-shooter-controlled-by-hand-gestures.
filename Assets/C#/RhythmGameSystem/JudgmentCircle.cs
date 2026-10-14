using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 判定圆圈组件
/// 用于显示音游中的中心大圆圈，并提供判定范围的视觉反馈
/// </summary>
[RequireComponent(typeof(Image))]
public class JudgmentCircle : MonoBehaviour
{
    [Header("圆圈设置")]
    [SerializeField] private float radius = 150f;
    [SerializeField] private Color circleColor = Color.white;
    [SerializeField] private float circleAlpha = 0.8f;
    
    [Header("判定范围设置")]
    [SerializeField] private bool showJudgmentSector = true;
    [SerializeField] private float sectorAngle = 30f; // 扇形角度
    [SerializeField] private Color sectorColor = new Color(1f, 1f, 0f, 0.3f);
    [SerializeField] private GameObject sectorIndicatorPrefab;
    
    // 私有变量
    private Image circleImage;
    private RectTransform rectTransform;
    private GameObject currentSectorIndicator;
    private Image sectorImage;
    
    // 下一个判定预览
    private GameObject nextJudgmentPreview;
    private Image nextPreviewImage;
    
    // 倒计时显示
    private GameObject countdownObject;
    private TextMeshProUGUI countdownText;
    
    // 平滑过渡相关
    private bool isTransitioning = false;
    private Coroutine transitionCoroutine;
    
    // 动画相关
    private float pulseSpeed = 2f;
    private float pulseIntensity = 0.2f;
    private float originalScale;
    
    private void Awake()
    {
        circleImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale.x;
    }
    
    private void Start()
    {
        SetupCircle();
    }
    
    private void Update()
    {
        // 不需要脉冲动画，保持固定大小
        // UpdatePulseAnimation();
    }
    
    /// <summary>
    /// 初始化判定圆圈
    /// </summary>
    /// <param name="circleRadius">圆圈半径</param>
    /// <param name="judgmentColor">判定范围颜色</param>
    public void Initialize(float circleRadius, Color judgmentColor)
    {
        radius = circleRadius;
        sectorColor = judgmentColor;
        SetupCircle();
    }
    
    /// <summary>
    /// 设置圆圈
    /// </summary>
    private void SetupCircle()
    {
        if (circleImage == null || rectTransform == null) return;
        
        // 设置圆圈大小
        rectTransform.sizeDelta = new Vector2(radius * 2, radius * 2);
        
        // 设置圆圈颜色和透明度
        Color color = circleColor;
        color.a = circleAlpha;
        circleImage.color = color;
        
        // 确保使用圆形精灵
        if (circleImage.sprite == null)
        {
            // 创建一个简单的圆形纹理
            circleImage.sprite = CreateCircleSprite();
        }
        
        // 设置为圆形
        circleImage.type = Image.Type.Simple;
        circleImage.preserveAspect = true;
    }
    
    /// <summary>
    /// 创建圆形精灵
    /// </summary>
    /// <returns>圆形精灵</returns>
    private Sprite CreateCircleSprite()
    {
        int size = 128;
        Texture2D texture = new Texture2D(size, size);
        
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radiusPixels = size * 0.4f;
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);
                
                if (distance <= radiusPixels)
                {
                    // 在圆圈边缘创建渐变效果  
                    float alpha = 1f - Mathf.Clamp01((distance - radiusPixels + 10f) / 10f);
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
    /// 显示判定扇形（带颜色变化和平滑过渡）
    /// </summary>
    /// <param name="angle">扇形朝向角度</param>
    /// <param name="remainingTime">剩余判定时间</param>
    /// <param name="totalTime">总判定时间</param>
    /// <param name="isJudging">是否在判定阶段</param>
    /// <param name="arcColor">圆弧当前颜色（用于同步扇形颜色）</param>
    public void ShowJudgmentSector(float angle, float remainingTime, float totalTime, bool isJudging, Color arcColor)
    {
        if (!showJudgmentSector) return;
        
        // 创建或更新扇形指示器
        if (currentSectorIndicator == null)
        {
            CreateSectorIndicator();
        }
        
        if (currentSectorIndicator != null)
        {
            // 检查是否需要平滑过渡到新角度
            float currentAngle = currentSectorIndicator.transform.eulerAngles.z;
            if (currentAngle < 0) currentAngle += 360f;
            
            float targetAngle = angle;
            if (targetAngle < 0) targetAngle += 360f;
            
            float angleDifference = Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle));
            
            // 如果角度差异大于5度且当前没在过渡，启动平滑过渡
            if (angleDifference > 5f && !isTransitioning)
            {
                StartSectorTransition(targetAngle);
            }
            else if (!isTransitioning)
            {
                // 小幅度变化，直接设置
                currentSectorIndicator.transform.rotation = Quaternion.Euler(0, 0, angle);
            }
            
            // 根据圆弧颜色和状态调整扇形显示
            if (sectorImage != null)
            {
                Color sectorDisplayColor = arcColor;
                
                if (isJudging)
                {
                    // 判定阶段：使用圆弧的颜色，但透明度较低，并且闪烁
                    float flashAlpha = 0.15f + 0.1f * Mathf.Sin(Time.time * 8f); // 闪烁效果
                    sectorDisplayColor.a = flashAlpha;
                }
                else
                {
                    // 准备阶段：根据时间比例调整透明度
                    float timeRatio = remainingTime / totalTime;
                    sectorDisplayColor.a = Mathf.Lerp(0.05f, 0.2f, 1f - timeRatio); // 时间越少，越明显
                }
                
                sectorImage.color = sectorDisplayColor;
            }
            
            currentSectorIndicator.SetActive(true);
        }
    }
    
    /// <summary>
    /// 隐藏判定扇形
    /// </summary>
    public void HideJudgmentSector()
    {
        if (currentSectorIndicator != null)
        {
            currentSectorIndicator.SetActive(false);
        }
    }
    
    /// <summary>
    /// 显示下一个判定范围预览
    /// </summary>
    /// <param name="angle">预览角度</param>
    public void ShowNextJudgmentPreview(float angle)
    {
        // 创建或更新预览指示器
        if (nextJudgmentPreview == null)
        {
            CreateNextJudgmentPreview();
        }
        
        if (nextJudgmentPreview != null)
        {
            // 设置预览位置和角度
            nextJudgmentPreview.transform.rotation = Quaternion.Euler(0, 0, angle);
            
            // 设置预览颜色
            if (nextPreviewImage != null)
            {
                Color previewColor = Color.grey;
                previewColor.a = 0.5f; 
                nextPreviewImage.color = previewColor;
            }
            
            nextJudgmentPreview.SetActive(true);
        }
    }
    
    /// <summary>
    /// 隐藏下一个判定预览
    /// </summary>
    public void HideNextJudgmentPreview()
    {
        if (nextJudgmentPreview != null)
        {
            nextJudgmentPreview.SetActive(false);
        }
    }
    
    /// <summary>
    /// 创建扇形指示器
    /// </summary>
    private void CreateSectorIndicator()
    {
        // 创建扇形GameObject
        currentSectorIndicator = new GameObject("SectorIndicator");
        currentSectorIndicator.transform.SetParent(transform, false);
        
        // 添加Image组件
        sectorImage = currentSectorIndicator.AddComponent<Image>();
        sectorImage.sprite = CreateSectorSprite();
        sectorImage.color = sectorColor;
        sectorImage.raycastTarget = false;
        
        // 设置RectTransform - 使用更大的尺寸确保覆盖整个屏幕
        RectTransform sectorRect = currentSectorIndicator.GetComponent<RectTransform>();
        sectorRect.sizeDelta = new Vector2(radius * 6, radius * 6); // 适当调整大小，确保扇形从圆圈边缘开始
        sectorRect.anchoredPosition = Vector2.zero;
    }
    
    /// <summary>
    /// 创建扇形精灵（从圆心延伸到边缘的完整扇形）
    /// </summary>
    /// <returns>扇形精灵</returns>
    private Sprite CreateSectorSprite()
    {
        int size = 512; // 使用更高分辨率
        Texture2D texture = new Texture2D(size, size);
        
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float startAngle = -sectorAngle * 0.5f;
        float endAngle = sectorAngle * 0.5f;
        float maxRadius = size * 0.5f; // 扇形延伸到纹理边缘
        float minRadius = size * 0f; // 从圆圈内边缘开始，与圆圈边框内侧对齐
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2 pos = new Vector2(x, y);
                Vector2 direction = (pos - center);
                float distance = direction.magnitude;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                
                // 将角度转换到-180到180范围
                if (angle < -180f) angle += 360f;
                if (angle > 180f) angle -= 360f;
                
                bool inSector = angle >= startAngle && angle <= endAngle;
                bool inRadius = distance >= minRadius && distance <= maxRadius;
                
                if (inSector && inRadius)
                {
                    // 创建从圆圈边缘向外的渐变效果
                    float distanceRatio = (distance - minRadius) / (maxRadius - minRadius);
                    
                    // 从圆圈边缘开始较强，向外逐渐减弱
                    float alpha = Mathf.Lerp(0.8f, 0.2f, distanceRatio * distanceRatio); // 使用平方渐变，更自然
                    
                    // 角度边缘也有渐变
                    float angleFromCenter = Mathf.Abs(angle);
                    if (angleFromCenter > sectorAngle * 0.35f)
                    {
                        float edgeFactor = (angleFromCenter - sectorAngle * 0.35f) / (sectorAngle * 0.15f);
                        alpha *= 1f - Mathf.Clamp01(edgeFactor);
                    }
                    
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
    /// 更新脉冲动画（已禁用）
    /// </summary>
    private void UpdatePulseAnimation()
    {
        // 脉冲动画已禁用，保持固定大小
        rectTransform.localScale = Vector3.one * originalScale;
    }
    
    /// <summary>
    /// 设置脉冲参数（已禁用脉冲动画）
    /// </summary>
    /// <param name="speed">脉冲速度</param>
    /// <param name="intensity">脉冲强度</param>
    public void SetPulseParameters(float speed, float intensity)
    {
        pulseSpeed = speed;
        pulseIntensity = intensity;
        // 脉冲动画已禁用，保持固定大小
    }
    
    /// <summary>
    /// 设置圆圈颜色
    /// </summary>
    /// <param name="color">颜色</param>
    public void SetCircleColor(Color color)
    {
        circleColor = color;
        if (circleImage != null)
        {
            Color newColor = circleColor;
            newColor.a = circleAlpha;
            circleImage.color = newColor;
        }
    }
    
    /// <summary>
    /// 设置扇形颜色
    /// </summary>
    /// <param name="color">颜色</param>
    public void SetSectorColor(Color color)
    {
        sectorColor = color;
        if (sectorImage != null)
        {
            sectorImage.color = color;
        }
    }
    
    /// <summary>
    /// 创建下一个判定预览指示器
    /// </summary>
    private void CreateNextJudgmentPreview()
    {
        if (transform == null) return;
        
        nextJudgmentPreview = new GameObject("NextJudgmentPreview");
        nextJudgmentPreview.transform.SetParent(transform, false);
        
        // 添加Image组件
        nextPreviewImage = nextJudgmentPreview.AddComponent<Image>();
        nextPreviewImage.sprite = CreateSectorSprite();
        nextPreviewImage.color = new Color(1f, 1f, 1f, 0.15f);
        nextPreviewImage.raycastTarget = false;
        
        // 设置RectTransform
        RectTransform previewRect = nextJudgmentPreview.GetComponent<RectTransform>();
        previewRect.sizeDelta = new Vector2(radius * 6, radius * 6);
        previewRect.anchoredPosition = Vector2.zero;
        
        Debug.Log("Created next judgment preview indicator");
    }
    
    /// <summary>
    /// 设置倒计时显示
    /// </summary>
    /// <param name="enabled">是否启用倒计时显示</param>
    public void SetupCountdownDisplay(bool enabled)
    {
        if (enabled)
        {
            CreateCountdownDisplay();
        }
        else
        {
            if (countdownObject != null)
            {
                Destroy(countdownObject);
                countdownObject = null;
                countdownText = null;
            }
        }
    }
    
    /// <summary>
    /// 创建倒计时显示
    /// </summary>
    private void CreateCountdownDisplay()
    {
        if (transform == null) return;
        
        countdownObject = new GameObject("CountdownDisplay");
        countdownObject.transform.SetParent(transform, false);
        
        // 添加TextMeshProUGUI组件
        countdownText = countdownObject.AddComponent<TextMeshProUGUI>();
        
        // 设置文本属性
        countdownText.text = "";
        countdownText.fontSize = 48f;
        countdownText.fontStyle = FontStyles.Bold | FontStyles.Italic; // 粗体 + 倾斜
        countdownText.color = Color.white;
        countdownText.alignment = TextAlignmentOptions.Center;
        
        // 启用自适应大小
        countdownText.enableAutoSizing = true;
        countdownText.fontSizeMin = 12f;
        countdownText.fontSizeMax = 100f;
        
        // 设置RectTransform - 不超过圆圈大小，居中
        RectTransform textRect = countdownObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        
        // 文本框大小为圆圈直径的70%，留一些边距
        float textBoxSize = radius * 1.4f; // 圆圈直径的70%
        textRect.sizeDelta = new Vector2(textBoxSize, textBoxSize);
        
        Debug.Log($"Created countdown display with size: {textBoxSize}");
    }
    
    /// <summary>
    /// 更新倒计时文本
    /// </summary>
    /// <param name="text">要显示的文本</param>
    public void UpdateCountdownText(string text)
    {
        if (countdownText != null)
        {
            countdownText.text = text;
        }
    }
    
    /// <summary>
    /// 设置倒计时可见性
    /// </summary>
    /// <param name="visible">是否可见</param>
    public void SetCountdownVisible(bool visible)
    {
        if (countdownObject != null)
        {
            countdownObject.SetActive(visible);
        }
    }
    
    /// <summary>
    /// 设置扇形角度
    /// </summary>
    /// <param name="angle">扇形角度</param>
    public void SetSectorAngle(float angle)
    {
        sectorAngle = angle;
        
        // 如果已经创建了扇形，重新创建精灵
        if (currentSectorIndicator != null && sectorImage != null)
        {
            sectorImage.sprite = CreateSectorSprite();
        }
        
        // 如果已经创建了预览，也重新创建精灵
        if (nextJudgmentPreview != null && nextPreviewImage != null)
        {
            nextPreviewImage.sprite = CreateSectorSprite();
        }
    }
    
    /// <summary>
    /// 开始扇形平滑过渡
    /// </summary>
    /// <param name="targetAngle">目标角度</param>
    private void StartSectorTransition(float targetAngle)
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
        
        transitionCoroutine = StartCoroutine(SmoothSectorTransition(targetAngle));
    }
    
    /// <summary>
    /// 扇形平滑过渡协程
    /// </summary>
    /// <param name="targetAngle">目标角度</param>
    /// <returns></returns>
    private System.Collections.IEnumerator SmoothSectorTransition(float targetAngle)
    {
        if (currentSectorIndicator == null) yield break;
        
        isTransitioning = true;
        
        float startAngle = currentSectorIndicator.transform.eulerAngles.z;
        float duration = 0.1f; // 0.1秒过渡时间
        float timer = 0f;
        
        // 确保使用最短路径旋转
        while (targetAngle - startAngle > 180f) targetAngle -= 360f;
        while (startAngle - targetAngle > 180f) startAngle -= 360f;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            
            // 使用平滑插值
            float currentAngle = Mathf.LerpAngle(startAngle, targetAngle, Mathf.SmoothStep(0f, 1f, t));
            currentSectorIndicator.transform.rotation = Quaternion.Euler(0, 0, currentAngle);
            
            yield return null;
        }
        
        // 确保最终角度准确
        currentSectorIndicator.transform.rotation = Quaternion.Euler(0, 0, targetAngle);
        
        isTransitioning = false;
        transitionCoroutine = null;
    }
    
    // 公共属性
    public float Radius => radius;
    public Vector3 WorldPosition => transform.position;
    public bool IsSectorVisible => currentSectorIndicator != null && currentSectorIndicator.activeInHierarchy;
    public bool IsCountdownActive => countdownText != null && countdownObject != null && countdownObject.activeInHierarchy;
}
