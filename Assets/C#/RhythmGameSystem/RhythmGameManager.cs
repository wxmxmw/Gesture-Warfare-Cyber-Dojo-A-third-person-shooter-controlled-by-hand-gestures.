using UnityEngine;

/// <summary>
/// 音游管理器
/// 负责管理音游系统的全局设置和预制体引用
/// </summary>
public class RhythmGameManager : MonoBehaviour
{
    [Header("预制体引用")]
    [SerializeField] private GameObject judgmentCirclePrefab;
    [SerializeField] private GameObject judgmentArcPrefab;
    [SerializeField] private Canvas gameCanvas;
    
    [Header("全局设置")]
    [SerializeField] private bool debugMode = false;
    
    // 单例
    private static RhythmGameManager instance;
    public static RhythmGameManager Instance => instance;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 创建音游控制器
    /// </summary>
    /// <param name="parent">父物体</param>
    /// <returns>音游控制器</returns>
    public RhythmGameController CreateRhythmGameController(Transform parent = null)
    {
        GameObject controllerObj = new GameObject("RhythmGameController");
        
        if (parent != null)
        {
            controllerObj.transform.SetParent(parent);
        }
        
        RhythmGameController controller = controllerObj.AddComponent<RhythmGameController>();
        
        // 设置预制体引用
        SetupControllerReferences(controller);
        
        return controller;
    }
    
    /// <summary>
    /// 设置控制器引用
    /// </summary>
    /// <param name="controller">控制器</param>
    private void SetupControllerReferences(RhythmGameController controller)
    {
        // 通过反射或者公共方法设置预制体引用
        // 由于RhythmGameController的字段是private，我们需要添加公共方法
        if (judgmentCirclePrefab != null && judgmentArcPrefab != null)
        {
            controller.SetPrefabs(judgmentCirclePrefab, judgmentArcPrefab, gameCanvas);
        }
    }
    
    // 属性
    public GameObject JudgmentCirclePrefab => judgmentCirclePrefab;
    public GameObject JudgmentArcPrefab => judgmentArcPrefab;
    public Canvas GameCanvas => gameCanvas;
    public bool DebugMode => debugMode;
}
