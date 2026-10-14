using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BGM播放系统管理器
/// 功能：背景音乐播放、音量控制、场景切换、淡入淡出效果
/// </summary>
public class BGMManager : MonoBehaviour
{
    [Header("BGM设置")]
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private AudioClip menuBGM;          // 菜单背景音乐
    [SerializeField] private AudioClip battleBGM;        // 战斗背景音乐
    [SerializeField] private AudioClip bossBGM;          // Boss战背景音乐
    [SerializeField] private AudioClip victoryBGM;       // 胜利音乐
    [SerializeField] private AudioClip gameOverBGM;      // 游戏结束音乐
    
    [Header("音量控制")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 0.7f;  // 主音量
    [Range(0f, 1f)]
    [SerializeField] private float bgmVolume = 0.8f;     // BGM音量
    
    [Header("淡入淡出设置")]
    [SerializeField] private float fadeInDuration = 2f;   // 淡入时间
    [SerializeField] private float fadeOutDuration = 1f;  // 淡出时间
    [SerializeField] private bool smoothTransition = true; // 平滑过渡
    
    [Header("播放设置")]
    [SerializeField] private bool playOnStart = true;     // 开始时自动播放
    [SerializeField] private bool loopBGM = true;         // 循环播放
    
    // 私有变量
    private static BGMManager instance;
    private Coroutine fadeCoroutine;
    private AudioClip currentBGM;
    private bool isPaused = false;
    private float targetVolume;
    
    // BGM类型枚举
    public enum BGMType
    {
        Menu,       // 菜单音乐
        Battle,     // 战斗音乐
        Boss,       // Boss战音乐
        Victory,    // 胜利音乐
        GameOver    // 游戏结束音乐
    }
    
    // 单例属性
    public static BGMManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<BGMManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("BGMManager");
                    instance = go.AddComponent<BGMManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }
    
    #region Unity生命周期
    
    private void Awake()
    {
        // 确保只有一个实例存在
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeBGMManager();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        //if (playOnStart && battleBGM != null)
        //{
        //    PlayBGM(BGMType.Battle);
        //}
    }
    
    #endregion
    
    #region 初始化
    
    /// <summary>
    /// 初始化BGM管理器
    /// </summary>
    private void InitializeBGMManager()
    {
        // 创建AudioSource组件（如果不存在）
        if (bgmAudioSource == null)
        {
            bgmAudioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // 配置AudioSource
        bgmAudioSource.loop = loopBGM;
        bgmAudioSource.playOnAwake = false;
        bgmAudioSource.volume = 0f; // 初始音量为0，用于淡入效果
        
        // 计算目标音量
        targetVolume = masterVolume * bgmVolume;
        
        Debug.Log("BGM管理器初始化完成");
    }
    
    #endregion
    
    #region 公共接口
    
    /// <summary>
    /// 播放指定类型的BGM
    /// </summary>
    /// <param name="bgmType">BGM类型</param>
    /// <param name="forceRestart">是否强制重新播放（即使是相同的BGM）</param>
    public void PlayBGM(BGMType bgmType, bool forceRestart = false)
    {
        AudioClip newBGM = GetBGMClip(bgmType);
        
        if (newBGM == null)
        {
            Debug.LogWarning($"未找到 {bgmType} 类型的BGM音频文件");
            return;
        }
        
        // 如果是相同的BGM且不强制重播，则不执行
        if (currentBGM == newBGM && !forceRestart && bgmAudioSource.isPlaying)
        {
            Debug.Log($"BGM {bgmType} 已在播放中");
            return;
        }
        
        StartCoroutine(SwitchBGMCoroutine(newBGM));
        Debug.Log($"开始播放BGM: {bgmType}");

        UpdateVolume();
    }
    
    /// <summary>
    /// 停止BGM播放
    /// </summary>
    /// <param name="fadeOut">是否使用淡出效果</param>
    public void StopBGM(bool fadeOut = true)
    {
        if (fadeOut)
        {
            StartCoroutine(FadeOutAndStop());
        }
        else
        {
            bgmAudioSource.Stop();
            currentBGM = null;
        }
        
        Debug.Log("BGM已停止播放");
    }
    
    /// <summary>
    /// 暂停BGM
    /// </summary>
    public void PauseBGM()
    {
        if (bgmAudioSource.isPlaying)
        {
            bgmAudioSource.Pause();
            isPaused = true;
            Debug.Log("BGM已暂停");
        }
    }
    
    /// <summary>
    /// 恢复BGM播放
    /// </summary>
    public void ResumeBGM()
    {
        if (isPaused)
        {
            bgmAudioSource.UnPause();
            isPaused = false;
            Debug.Log("BGM已恢复播放");
        }
    }
    
    /// <summary>
    /// 设置主音量
    /// </summary>
    /// <param name="volume">音量值 (0-1)</param>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateVolume();
        Debug.Log($"主音量设置为: {masterVolume:F2}");
    }
    
    /// <summary>
    /// 设置BGM音量
    /// </summary>
    /// <param name="volume">音量值 (0-1)</param>
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        UpdateVolume();
        Debug.Log($"BGM音量设置为: {bgmVolume:F2}");
    }
    
    /// <summary>
    /// 获取当前是否正在播放BGM
    /// </summary>
    public bool IsPlaying()
    {
        return bgmAudioSource.isPlaying;
    }
    
    /// <summary>
    /// 获取当前是否暂停
    /// </summary>
    public bool IsPaused()
    {
        return isPaused;
    }
    
    #endregion
    
    #region 私有方法
    
    /// <summary>
    /// 根据BGM类型获取对应的音频文件
    /// </summary>
    /// <param name="bgmType">BGM类型</param>
    /// <returns>音频文件</returns>
    private AudioClip GetBGMClip(BGMType bgmType)
    {
        switch (bgmType)
        {
            case BGMType.Menu: return menuBGM;
            case BGMType.Battle: return battleBGM;
            case BGMType.Boss: return bossBGM;
            case BGMType.Victory: return victoryBGM;
            case BGMType.GameOver: return gameOverBGM;
            default: return null;
        }
    }
    
    /// <summary>
    /// 更新音量
    /// </summary>
    private void UpdateVolume()
    {
        targetVolume = masterVolume * bgmVolume;
        if (!smoothTransition)
        {
            bgmAudioSource.volume = targetVolume;
        }
    }
    
    /// <summary>
    /// BGM切换协程
    /// </summary>
    /// <param name="newBGM">新的BGM音频文件</param>
    private IEnumerator SwitchBGMCoroutine(AudioClip newBGM)
    {
        // 停止之前的淡入淡出协程
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        
        // 如果当前有BGM在播放，先淡出
        if (currentBGM != null && bgmAudioSource.isPlaying)
        {
            yield return StartCoroutine(FadeOut());
        }
        
        // 切换到新的BGM
        currentBGM = newBGM;
        bgmAudioSource.clip = newBGM;
        bgmAudioSource.Play();
        
        // 淡入新的BGM
        yield return StartCoroutine(FadeIn());
    }
    
    /// <summary>
    /// 淡入效果协程
    /// </summary>
    private IEnumerator FadeIn()
    {
        float currentTime = 0;
        float startVolume = 0f;
        
        bgmAudioSource.volume = startVolume;
        
        while (currentTime < fadeInDuration)
        {
            currentTime += Time.deltaTime;
            bgmAudioSource.volume = Mathf.Lerp(startVolume, targetVolume, currentTime / fadeInDuration);
            yield return null;
        }
        
        bgmAudioSource.volume = targetVolume;
    }
    
    /// <summary>
    /// 淡出效果协程
    /// </summary>
    private IEnumerator FadeOut()
    {
        float currentTime = 0;
        float startVolume = bgmAudioSource.volume;
        
        while (currentTime < fadeOutDuration)
        {
            currentTime += Time.deltaTime;
            bgmAudioSource.volume = Mathf.Lerp(startVolume, 0f, currentTime / fadeOutDuration);
            yield return null;
        }
        
        bgmAudioSource.volume = 0f;
    }
    
    /// <summary>
    /// 淡出并停止播放协程
    /// </summary>
    private IEnumerator FadeOutAndStop()
    {
        yield return StartCoroutine(FadeOut());
        bgmAudioSource.Stop();
        currentBGM = null;
    }
    
    #endregion
    
    #region 编辑器工具方法
    
#if UNITY_EDITOR
    [ContextMenu("测试播放战斗BGM")]
    private void TestPlayBattleBGM()
    {
        PlayBGM(BGMType.Battle);
    }
    
    [ContextMenu("测试播放Boss BGM")]
    private void TestPlayBossBGM()
    {
        PlayBGM(BGMType.Boss);
    }
    
    [ContextMenu("测试停止BGM")]
    private void TestStopBGM()
    {
        StopBGM();
    }
#endif
    
    #endregion
}
