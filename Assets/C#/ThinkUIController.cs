using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;
using System;

/// <summary>
/// 思考UI控制器
/// 管理玩家思考内容的显示、动画和队列
/// </summary>
public class ThinkUIController : MonoBehaviour
{
    [Header("思考设置")]
    [SerializeField] private GameObject Think_prefab; // 思考预制体
    [SerializeField] private float displayDuration = 3f; // 思考内容显示时长
    [SerializeField] private float fadeInDuration = 0.3f; // 淡入动画时长
    [SerializeField] private float fadeOutDuration = 0.3f; // 淡出动画时长
    
    [Header("引用设置")]
    [SerializeField] private GameManager gameManager; // GameManager引用
    
    // 思考数据管理
    private Dictionary<string, ThinkData> thinkDatabase = new Dictionary<string, ThinkData>();
    private Queue<string> thinkQueue = new Queue<string>(); // 思考内容队列
    private bool isDisplaying = false; // 是否正在显示思考内容
    private GameObject currentThinkObject = null; // 当前显示的思考对象
    
    // 单例模式
    public static ThinkUIController Instance { get; private set; }
    
    private void Awake()
    {
        // 单例设置
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // 初始化思考数据库
        LoadThinkDatabase();
    }
    
    private void Start()
    {
        // 验证预制体设置
        if (Think_prefab == null)
        {
            Debug.LogError("ThinkUIController: Think_prefab 未设置！");
        }
        
        // 获取GameManager引用
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
    }
    
    /// <summary>
    /// 显示思考内容
    /// </summary>
    /// <param name="thinkId">思考内容ID</param>
    public void ShowThink(string thinkId)
    {
        if (string.IsNullOrEmpty(thinkId))
        {
            Debug.LogWarning("ThinkUIController: 思考ID为空！");
            return;
        }
        
        if (!thinkDatabase.ContainsKey(thinkId))
        {
            Debug.LogWarning($"ThinkUIController: 未找到思考ID '{thinkId}'！");
            return;
        }
        
        // 添加到队列
        thinkQueue.Enqueue(thinkId);
        
        // 如果当前没有在显示，开始显示队列中的内容
        if (!isDisplaying)
        {
            StartCoroutine(ProcessThinkQueue());
        }
    }
    
    /// <summary>
    /// 显示自定义思考内容
    /// </summary>
    /// <param name="content">思考内容</param>
    public void ShowCustomThink(string content)
    {
        ThinkData customThink = new ThinkData
        {
            id = "custom_" + Time.time,
            content = content
        };
        
        // 添加到数据库（临时）
        thinkDatabase[customThink.id] = customThink;
        
        // 显示思考内容
        ShowThink(customThink.id);
    }
    
    /// <summary>
    /// 处理思考队列
    /// </summary>
    private IEnumerator ProcessThinkQueue()
    {
        isDisplaying = true;
        
        while (thinkQueue.Count > 0)
        {
            string thinkId = thinkQueue.Dequeue();
            ThinkData thinkData = thinkDatabase[thinkId];
            
            // 检查背景是否为黑色
            if (!IsBackgroundBlack())
            {
                Debug.LogWarning("ThinkUIController: 背景不是黑色，无法显示思考内容！");
                continue;
            }
            
            // 显示思考内容
            yield return StartCoroutine(DisplayThinkCoroutine(thinkData));
        }
        
        isDisplaying = false;
    }
    
    /// <summary>
    /// 显示思考内容的协程
    /// </summary>
    private IEnumerator DisplayThinkCoroutine(ThinkData thinkData)
    {
        // 创建思考对象
        currentThinkObject = Instantiate(Think_prefab, transform);
        
        // 设置思考内容
        SetupThinkContent(currentThinkObject, thinkData);
        
        // 初始透明度设为0
        SetThinkContentAlpha(currentThinkObject, 0f);
        
        // 等待0.3秒
        yield return new WaitForSeconds(0.3f);
        
        // 淡入动画
        yield return StartCoroutine(FadeInThinkContent(currentThinkObject));
        
        // 显示指定时间
        yield return new WaitForSeconds(displayDuration);
        
        // 淡出动画
        yield return StartCoroutine(FadeOutThinkContent(currentThinkObject));
        
        // 删除思考对象
        if (currentThinkObject != null)
        {
            Destroy(currentThinkObject);
            currentThinkObject = null;
        }
    }
    
    /// <summary>
    /// 设置思考内容
    /// </summary>
    private void SetupThinkContent(GameObject thinkObject, ThinkData thinkData)
    {
        // 查找Content子物体
        Transform contentTransform = thinkObject.transform.Find("Content");
        
        if (contentTransform == null)
        {
            Debug.LogError("ThinkUIController: Think预制体结构不正确！缺少Content子物体");
            return;
        }
        
        // 设置思考内容文本
        TextMeshProUGUI contentText = contentTransform.GetComponent<TextMeshProUGUI>();
        if (contentText != null)
        {
            // 将\n转换为实际换行符
            string processedContent = thinkData.content.Replace("\\n", "\n");
            contentText.text = processedContent;
        }
        else
        {
            Debug.LogError("ThinkUIController: Content子物体缺少TextMeshProUGUI组件！");
        }
    }
    
    /// <summary>
    /// 设置思考内容透明度
    /// </summary>
    private void SetThinkContentAlpha(GameObject thinkObject, float alpha)
    {
        Transform contentTransform = thinkObject.transform.Find("Content");
        if (contentTransform != null)
        {
            TextMeshProUGUI contentText = contentTransform.GetComponent<TextMeshProUGUI>();
            if (contentText != null)
            {
                Color color = contentText.color;
                color.a = alpha;
                contentText.color = color;
            }
        }
    }
    
    /// <summary>
    /// 淡入思考内容
    /// </summary>
    private IEnumerator FadeInThinkContent(GameObject thinkObject)
    {
        Transform contentTransform = thinkObject.transform.Find("Content");
        if (contentTransform == null) yield break;
        
        TextMeshProUGUI contentText = contentTransform.GetComponent<TextMeshProUGUI>();
        if (contentText == null) yield break;
        
        Color startColor = contentText.color;
        Color targetColor = startColor;
        targetColor.a = 1f;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeInDuration;
            
            // 使用缓动函数
            float easedProgress = EaseOutQuart(progress);
            
            contentText.color = Color.Lerp(startColor, targetColor, easedProgress);
            
            yield return null;
        }
        
        // 确保最终透明度
        contentText.color = targetColor;
    }
    
    /// <summary>
    /// 淡出思考内容
    /// </summary>
    private IEnumerator FadeOutThinkContent(GameObject thinkObject)
    {
        Transform contentTransform = thinkObject.transform.Find("Content");
        if (contentTransform == null) yield break;
        
        TextMeshProUGUI contentText = contentTransform.GetComponent<TextMeshProUGUI>();
        if (contentText == null) yield break;
        
        Color startColor = contentText.color;
        Color targetColor = startColor;
        targetColor.a = 0f;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeOutDuration;
            
            // 使用缓动函数
            float easedProgress = EaseInQuart(progress);
            
            contentText.color = Color.Lerp(startColor, targetColor, easedProgress);
            
            yield return null;
        }
        
        // 确保最终透明度
        contentText.color = targetColor;
    }
    
    /// <summary>
    /// 检查背景是否为黑色
    /// </summary>
    private bool IsBackgroundBlack()
    {
        if (gameManager == null)
        {
            Debug.LogWarning("ThinkUIController: GameManager引用为空！");
            return false;
        }
        
        // 通过反射获取BackgroundTexture
        /*
        var backgroundTextureField = typeof(GameManager).GetField("BackgroundTexture", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        
        if (backgroundTextureField != null)
        {
            var backgroundTexture = backgroundTextureField.GetValue(gameManager) as UnityEngine.UI.Image;
            if (backgroundTexture != null)
            {
                // 检查颜色是否为黑色（允许一定的误差）
                Color bgColor = backgroundTexture.color;
                return bgColor.r < 0.1f && bgColor.g < 0.1f && bgColor.b < 0.1f;
            }
        }*/

        Color bgColor = gameManager.BackgroundTexture.color;
        if(bgColor.r < 0.1f && bgColor.g < 0.1f && bgColor.b < 0.1f)
            return bgColor.r < 0.1f && bgColor.g < 0.1f && bgColor.b < 0.1f;

        Debug.LogWarning("ThinkUIController: 无法获取BackgroundTexture！");
        return false;
    }
    
    /// <summary>
    /// 加载思考数据库
    /// </summary>
    private void LoadThinkDatabase()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "ThinkData.yml");
        
        if (File.Exists(filePath))
        {
            try
            {
                string yamlContent = File.ReadAllText(filePath);
                ParseThinkData(yamlContent);
                Debug.Log($"ThinkUIController: 成功加载思考数据，共 {thinkDatabase.Count} 条思考内容");
            }
            catch (Exception e)
            {
                Debug.LogError($"ThinkUIController: 加载思考数据失败 - {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("ThinkUIController: 未找到思考数据文件，将创建默认文件");
            CreateDefaultThinkData();
        }
    }
    
    /// <summary>
    /// 解析YAML格式的思考数据
    /// </summary>
    private void ParseThinkData(string yamlContent)
    {
        string[] lines = yamlContent.Split('\n');
        ThinkData currentThink = null;
        
        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                continue;
            
            if (trimmedLine.StartsWith("- id:"))
            {
                // 保存之前的思考
                if (currentThink != null)
                {
                    thinkDatabase[currentThink.id] = currentThink;
                }
                
                // 开始新思考
                currentThink = new ThinkData();
                currentThink.id = trimmedLine.Substring(6).Trim().Trim('"');
            }
            else if (trimmedLine.StartsWith("content:"))
            {
                if (currentThink != null)
                {
                    currentThink.content = trimmedLine.Substring(8).Trim().Trim('"');
                }
            }
        }
        
        // 保存最后一个思考
        if (currentThink != null)
        {
            thinkDatabase[currentThink.id] = currentThink;
        }
    }
    
    /// <summary>
    /// 创建默认思考数据文件
    /// </summary>
    private void CreateDefaultThinkData()
    {
        string defaultYaml = @"# 思考数据文件
# 格式说明：
# - id: 思考内容的唯一标识符
#   content: 思考内容文本

- id: ""thinking_1""
  content: ""我需要仔细考虑这个情况...""

- id: ""thinking_2""
  content: ""这个决定可能会影响整个战局...""

- id: ""thinking_3""
  content: ""让我分析一下敌人的弱点...""

- id: ""thinking_4""
  content: ""也许我应该改变策略...""

- id: ""thinking_5""
  content: ""时间不多了，我必须做出选择...""

- id: ""thinking_6""
  content: ""这个计划看起来可行...""

- id: ""thinking_7""
  content: ""我需要更多的信息...""

- id: ""thinking_8""
  content: ""也许有更好的方法...""
";
        
        string filePath = Path.Combine(Application.streamingAssetsPath, "ThinkData.yml");
        
        try
        {
            // 确保目录存在
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            
            // 写入文件
            File.WriteAllText(filePath, defaultYaml);
            Debug.Log("ThinkUIController: 已创建默认思考数据文件");
            
            // 重新加载
            LoadThinkDatabase();
        }
        catch (Exception e)
        {
            Debug.LogError($"ThinkUIController: 创建默认思考数据文件失败 - {e.Message}");
        }
    }
    
    /// <summary>
    /// 缓动函数 - EaseOutQuart
    /// </summary>
    private float EaseOutQuart(float t)
    {
        return 1f - Mathf.Pow(1f - t, 4f);
    }
    
    /// <summary>
    /// 缓动函数 - EaseInQuart
    /// </summary>
    private float EaseInQuart(float t)
    {
        return t * t * t * t;
    }
    
    /// <summary>
    /// 清除所有思考内容
    /// </summary>
    public void ClearAllThinks()
    {
        // 清空队列
        thinkQueue.Clear();
        
        // 停止当前显示
        if (currentThinkObject != null)
        {
            Destroy(currentThinkObject);
            currentThinkObject = null;
        }
        
        // 停止协程
        StopAllCoroutines();
        isDisplaying = false;
    }
    
    /// <summary>
    /// 获取当前队列中的思考数量
    /// </summary>
    public int GetQueueCount()
    {
        return thinkQueue.Count;
    }
    
    /// <summary>
    /// 检查思考是否存在
    /// </summary>
    public bool HasThink(string thinkId)
    {
        return thinkDatabase.ContainsKey(thinkId);
    }
    
    /// <summary>
    /// 检查是否正在显示思考内容
    /// </summary>
    public bool IsDisplayingThink()
    {
        return isDisplaying;
    }
    
    private void OnDestroy()
    {
        // 清理资源
        ClearAllThinks();
    }
}

/// <summary>
/// 思考数据结构
/// </summary>
[System.Serializable]
public class ThinkData
{
    public string id;        // 思考ID
    public string content;   // 思考内容
}
