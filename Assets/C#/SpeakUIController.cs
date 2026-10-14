using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.IO;
using System;

/// <summary>
/// 对话UI控制器
/// 管理对话内容的显示、动画和布局
/// </summary>
public class SpeakUIController : MonoBehaviour
{
    [Header("对话设置")]
    [SerializeField] private GameObject Talk_Prefab; // 对话预制体
    [SerializeField] private float talkSpacing = 80f; // 对话间距
    [SerializeField] private float maxTalkCount = 5; // 最大同时显示对话数量
    [SerializeField] private float autoHideDelay = 3f; // 自动隐藏延迟时间
    
    [Header("动画设置")]
    [SerializeField] private float appearAnimationDuration = 0.5f; // 出现动画时长
    [SerializeField] private float disappearAnimationDuration = 0.5f; // 消失动画时长
    
    [Header("头像设置")]
    [SerializeField] private Sprite defaultAvatar; // 默认头像
    [SerializeField] private AvatarData[] avatarDatabase; // 头像数据库
    
    // 对话数据管理
    private Dictionary<string, TalkData> talkDatabase = new Dictionary<string, TalkData>();
    private Queue<GameObject> Active_TalkUI = new Queue<GameObject>();
    private List<GameObject> allTalkObjects = new List<GameObject>();
    
    // 动画控制
    private bool isAnimating = false;
    
    // 单例模式
    public static SpeakUIController Instance { get; private set; }
    
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
        
        // 初始化对话数据库
        LoadTalkDatabase();
    }
    
    private void Start()
    {
        // 验证预制体设置
        if (Talk_Prefab == null)
        {
            Debug.LogError("SpeakUIController: Talk_Prefab 未设置！");
        }
    }
    
    /// <summary>
    /// 显示对话
    /// </summary>
    /// <param name="talkId">对话ID</param>
    /// <param name="autoHide">是否自动隐藏</param>
    public void ShowTalk(string talkId, bool autoHide = true)
    {
        if (string.IsNullOrEmpty(talkId))
        {
            Debug.LogWarning("SpeakUIController: 对话ID为空！");
            return;
        }
        
        if (!talkDatabase.ContainsKey(talkId))
        {
            Debug.LogWarning($"SpeakUIController: 未找到对话ID '{talkId}'！");
            return;
        }
        
        TalkData talkData = talkDatabase[talkId];
        StartCoroutine(CreateAndShowTalk(talkData, autoHide));
    }
    
    /// <summary>
    /// 显示对话（支持自定义显示时长）
    /// </summary>
    /// <param name="talkId">对话ID</param>
    /// <param name="customDuration">自定义显示时长</param>
    /// <param name="autoHide">是否自动隐藏</param>
    public void ShowTalk(string talkId, float customDuration, bool autoHide = true)
    {
        if (string.IsNullOrEmpty(talkId))
        {
            Debug.LogWarning("SpeakUIController: 对话ID为空！");
            return;
        }
        
        if (!talkDatabase.ContainsKey(talkId))
        {
            Debug.LogWarning($"SpeakUIController: 未找到对话ID '{talkId}'！");
            return;
        }
        
        TalkData talkData = talkDatabase[talkId];
        StartCoroutine(CreateAndShowTalkWithCustomDuration(talkData, customDuration, autoHide));
    }
    
    /// <summary>
    /// 显示自定义对话
    /// </summary>
    /// <param name="speaker">说话人</param>
    /// <param name="content">对话内容</param>
    /// <param name="autoHide">是否自动隐藏</param>
    public void ShowCustomTalk(string speaker, string content, bool autoHide = true)
    {
        TalkData customTalk = new TalkData
        {
            id = "custom_" + Time.time,
            speaker = speaker,
            content = content
        };
        
        StartCoroutine(CreateAndShowTalk(customTalk, autoHide));
    }
    
    /// <summary>
    /// 显示自定义对话（支持自定义显示时长）
    /// </summary>
    /// <param name="speaker">说话人</param>
    /// <param name="content">对话内容</param>
    /// <param name="customDuration">自定义显示时长</param>
    /// <param name="autoHide">是否自动隐藏</param>
    public void ShowCustomTalk(string speaker, string content, float customDuration, bool autoHide = true)
    {
        TalkData customTalk = new TalkData
        {
            id = "custom_" + Time.time,
            speaker = speaker,
            content = content
        };
        
        StartCoroutine(CreateAndShowTalkWithCustomDuration(customTalk, customDuration, autoHide));
    }
    
    /// <summary>
    /// 创建并显示对话
    /// </summary>
    private IEnumerator CreateAndShowTalk(TalkData talkData, bool autoHide)
    {
        // 等待动画完成
        while (isAnimating)
        {
            yield return null;
        }
        
        // 检查最大对话数量
        if (Active_TalkUI.Count >= maxTalkCount)
        {
            // 移除最旧的对话
            GameObject oldestTalk = Active_TalkUI.Dequeue();
            if (oldestTalk != null)
            {
                StartCoroutine(HideTalk(oldestTalk));
            }
        }
        
        // 创建对话对象
        GameObject talkObject = Instantiate(Talk_Prefab, transform);
        allTalkObjects.Add(talkObject);
        Active_TalkUI.Enqueue(talkObject);
        
        // 设置对话内容
        SetupTalkContent(talkObject, talkData);
        
        // 调整对话位置
        AdjustTalkPositions();
        
        // 播放出现动画
        yield return StartCoroutine(PlayAppearAnimation(talkObject));
        
        // 自动隐藏
        if (autoHide)
        {
            yield return new WaitForSeconds(autoHideDelay);
            yield return StartCoroutine(HideTalk(talkObject));
        }
    }
    
    /// <summary>
    /// 创建并显示对话（支持自定义显示时长）
    /// </summary>
    private IEnumerator CreateAndShowTalkWithCustomDuration(TalkData talkData, float customDuration, bool autoHide)
    {
        // 等待动画完成
        while (isAnimating)
        {
            yield return null;
        }
        
        // 检查最大对话数量
        if (Active_TalkUI.Count >= maxTalkCount)
        {
            // 移除最旧的对话
            GameObject oldestTalk = Active_TalkUI.Dequeue();
            if (oldestTalk != null)
            {
                StartCoroutine(HideTalk(oldestTalk));
            }
        }
        
        // 创建对话对象
        GameObject talkObject = Instantiate(Talk_Prefab, transform);
        allTalkObjects.Add(talkObject);
        Active_TalkUI.Enqueue(talkObject);
        
        // 设置对话内容
        SetupTalkContent(talkObject, talkData);
        
        // 调整对话位置
        AdjustTalkPositions();
        
        // 播放出现动画
        yield return StartCoroutine(PlayAppearAnimation(talkObject));
        
        // 自动隐藏（使用自定义时长）
        if (autoHide)
        {
            yield return new WaitForSeconds(customDuration);
            yield return StartCoroutine(HideTalk(talkObject));
        }
    }
    
    /// <summary>
    /// 设置对话内容
    /// </summary>
    private void SetupTalkContent(GameObject talkObject, TalkData talkData)
    {
        // 查找子物体
        Transform headerTransform = talkObject.transform.Find("Header");
        Transform contentTransform = talkObject.transform.Find("Content");
        
        if (headerTransform == null || contentTransform == null)
        {
            Debug.LogError("SpeakUIController: Talk预制体结构不正确！缺少Header或Content子物体");
            return;
        }
        
        // 设置头像
        UnityEngine.UI.Image headerImage = headerTransform.GetComponent<UnityEngine.UI.Image>();
        if (headerImage != null)
        {
            Sprite avatar = GetAvatarForSpeaker(talkData.speaker);
            headerImage.sprite = avatar;
        }
        
        // 设置对话内容
        TextMeshProUGUI contentText = contentTransform.GetComponent<TextMeshProUGUI>();
        if (contentText != null)
        {
            contentText.text = talkData.content;
        }
        else
        {
            Debug.LogError("SpeakUIController: Content子物体缺少TextMeshProUGUI组件！");
        }
    }
    
    /// <summary>
    /// 获取说话人对应的头像
    /// </summary>
    private Sprite GetAvatarForSpeaker(string speaker)
    {
        if (avatarDatabase != null)
        {
            foreach (var avatarData in avatarDatabase)
            {
                if (avatarData.speakerName == speaker)
                {
                    return avatarData.avatarSprite;
                }
            }
        }
        
        return defaultAvatar;
    }
    
    /// <summary>
    /// 调整对话位置
    /// </summary>
    private void AdjustTalkPositions()
    {
        GameObject[] activeTalks = Active_TalkUI.ToArray();
        
        for (int i = 0; i < activeTalks.Length; i++)
        {
            if (activeTalks[i] != null)
            {
                RectTransform rectTransform = activeTalks[i].GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    // 从下往上排列，每个对话间隔talkSpacing
                    float yPosition = i * talkSpacing;
                    rectTransform.anchoredPosition = new Vector2(0, yPosition);
                }
            }
        }
    }
    
    /// <summary>
    /// 播放出现动画（使用Animator）
    /// </summary>
    private IEnumerator PlayAppearAnimation(GameObject talkObject)
    {
        isAnimating = true;
        
        // 获取Animator组件
        Animator animator = talkObject.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("SpeakUIController: Talk预制体缺少Animator组件！");
            isAnimating = false;
            yield break;
        }
        
        // 播放出现动画
        animator.Play("SpeakUI_appear");
        
        // 等待动画完成
        yield return new WaitForSeconds(appearAnimationDuration);
        
        isAnimating = false;
    }
    
    /// <summary>
    /// 隐藏对话（使用Animator）
    /// </summary>
    private IEnumerator HideTalk(GameObject talkObject)
    {
        if (talkObject == null) yield break;
        
        isAnimating = true;
        
        // 从队列中移除
        Queue<GameObject> newQueue = new Queue<GameObject>();
        while (Active_TalkUI.Count > 0)
        {
            GameObject talk = Active_TalkUI.Dequeue();
            if (talk != talkObject)
            {
                newQueue.Enqueue(talk);
            }
        }
        Active_TalkUI = newQueue;
        
        // 获取Animator组件
        Animator animator = talkObject.GetComponent<Animator>();
        if (animator != null)
        {
            // 播放消失动画
            animator.Play("SpeakUI_disappear");
            
            // 等待动画完成
            yield return new WaitForSeconds(disappearAnimationDuration);
        }
        else
        {
            Debug.LogWarning("SpeakUIController: Talk预制体缺少Animator组件，使用默认等待时间");
            yield return new WaitForSeconds(disappearAnimationDuration);
        }
        
        // 调整剩余对话位置
        AdjustTalkPositions();
        
        // 销毁对象
        allTalkObjects.Remove(talkObject);
        Destroy(talkObject);
        
        isAnimating = false;
    }
    
    /// <summary>
    /// 加载对话数据库
    /// </summary>
    private void LoadTalkDatabase()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "TalkData.yml");
        
        if (File.Exists(filePath))
        {
            try
            {
                string yamlContent = File.ReadAllText(filePath);
                ParseTalkData(yamlContent);
                Debug.Log($"SpeakUIController: 成功加载对话数据，共 {talkDatabase.Count} 条对话");
            }
            catch (Exception e)
            {
                Debug.LogError($"SpeakUIController: 加载对话数据失败 - {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("SpeakUIController: 未找到对话数据文件，将创建默认文件");
            CreateDefaultTalkData();
        }
    }
    
    /// <summary>
    /// 解析YAML格式的对话数据
    /// </summary>
    private void ParseTalkData(string yamlContent)
    {
        string[] lines = yamlContent.Split('\n');
        TalkData currentTalk = null;
        
        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                continue;
            
            if (trimmedLine.StartsWith("- id:"))
            {
                // 保存之前的对话
                if (currentTalk != null)
                {
                    talkDatabase[currentTalk.id] = currentTalk;
                }
                
                // 开始新对话
                currentTalk = new TalkData();
                currentTalk.id = trimmedLine.Substring(6).Trim().Trim('"');
            }
            else if (trimmedLine.StartsWith("speaker:"))
            {
                if (currentTalk != null)
                {
                    currentTalk.speaker = trimmedLine.Substring(8).Trim().Trim('"');
                }
            }
            else if (trimmedLine.StartsWith("content:"))
            {
                if (currentTalk != null)
                {
                    currentTalk.content = trimmedLine.Substring(8).Trim().Trim('"');
                }
            }
        }
        
        // 保存最后一个对话
        if (currentTalk != null)
        {
            talkDatabase[currentTalk.id] = currentTalk;
        }
    }
    
    /// <summary>
    /// 创建默认对话数据文件
    /// </summary>
    private void CreateDefaultTalkData()
    {
        string defaultYaml = @"# 对话数据文件
# 格式说明：
# - id: 对话的唯一标识符
#   speaker: 说话人名称
#   content: 对话内容

- id: ""welcome""
  speaker: ""系统""
  content: ""欢迎来到手势战争！""

- id: ""game_start""
  speaker: ""指挥官""
  content: ""准备战斗，士兵！""

- id: ""enemy_spawn""
  speaker: ""系统""
  content: ""敌人出现了！""

- id: ""boss_warning""
  speaker: ""指挥官""
  content: ""警告！强大的Boss正在接近！""

- id: ""victory""
  speaker: ""指挥官""
  content: ""干得好！任务完成！""
";
        
        string filePath = Path.Combine(Application.streamingAssetsPath, "TalkData.yml");
        
        try
        {
            // 确保目录存在
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            
            // 写入文件
            File.WriteAllText(filePath, defaultYaml);
            Debug.Log("SpeakUIController: 已创建默认对话数据文件");
            
            // 重新加载
            LoadTalkDatabase();
        }
        catch (Exception e)
        {
            Debug.LogError($"SpeakUIController: 创建默认对话数据文件失败 - {e.Message}");
        }
    }
    
    /// <summary>
    /// 清除所有对话
    /// </summary>
    public void ClearAllTalks()
    {
        StartCoroutine(ClearAllTalksCoroutine());
    }
    
    private IEnumerator ClearAllTalksCoroutine()
    {
        while (Active_TalkUI.Count > 0)
        {
            GameObject talk = Active_TalkUI.Dequeue();
            if (talk != null)
            {
                yield return StartCoroutine(HideTalk(talk));
            }
        }
    }
    
    /// <summary>
    /// 获取当前活跃对话数量
    /// </summary>
    public int GetActiveTalkCount()
    {
        return Active_TalkUI.Count;
    }
    
    /// <summary>
    /// 检查对话是否存在
    /// </summary>
    public bool HasTalk(string talkId)
    {
        return talkDatabase.ContainsKey(talkId);
    }
    
    private void OnDestroy()
    {
        // 清理资源
        ClearAllTalks();
    }
}

/// <summary>
/// 对话数据结构
/// </summary>
[System.Serializable]
public class TalkData
{
    public string id;        // 对话ID
    public string speaker;   // 说话人
    public string content;   // 对话内容
}

/// <summary>
/// 头像数据结构
/// </summary>
[System.Serializable]
public class AvatarData
{
    public string speakerName;   // 说话人名称
    public Sprite avatarSprite;  // 头像图片
}
