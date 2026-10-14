using UnityEngine;
using System.Collections;

/// <summary>
/// 开始按钮组件
/// 可以被射击触发，开始游戏
/// </summary>
public class StartButton : MonoBehaviour
{
    [Header("Button Settings")]
    [SerializeField] private bool isActivated = false;
    [SerializeField] private float activationDelay = 0.5f; // 激活延迟时间
    
    [Header("Position Settings")]
    [SerializeField] private bool autoCenterOnScreen = true; // 自动居中到屏幕
    [SerializeField] private Vector3 customPosition = Vector3.zero; // 自定义位置（当autoCenterOnScreen为false时使用）
    [SerializeField] private float distanceFromCamera = 10f; // 距离摄像机的距离
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject buttonModel; // 按钮3D模型
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private Color activatedColor = Color.green;
    [SerializeField] private float flashDuration = 0.2f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip activationSound;
    [SerializeField] private AudioClip hoverSound;
    
    // 组件引用
    private Collider2D buttonCollider;
    private AudioSource audioSource;
    private Renderer buttonRenderer;
    private GameManager gameManager;
    private Camera playerCamera;
    
    // 状态变量
    private bool isHovered = false;
    private bool canBeActivated = true;
    
    // 事件
    public System.Action<StartButton> OnButtonActivated;
    
    private void Awake()
    {
        // 获取组件引用
        buttonCollider = GetComponent<Collider2D>();
        audioSource = GetComponent<AudioSource>();
        buttonRenderer = GetComponent<Renderer>();
        
        // 获取摄像机引用
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = FindObjectOfType<Camera>();
        }
        
        // 如果没有指定按钮模型，使用自身
        if (buttonModel == null)
        {
            buttonModel = gameObject;
        }
        
        // 获取游戏管理器
        gameManager = FindObjectOfType<GameManager>();
        
        // 设置初始状态
        SetupButton();
    }
    
    private void Start()
    {
        // 确保按钮在游戏开始时是激活状态
        if (buttonCollider != null)
        {
            buttonCollider.enabled = true;
        }
        
        // 设置按钮位置
        SetButtonPosition();
    }
    
    /// <summary>
    /// 设置按钮位置
    /// </summary>
    private void SetButtonPosition()
    {
        if (playerCamera == null) return;
        
        Vector3 targetPosition;
        
        if (autoCenterOnScreen)
        {
            // 自动居中到屏幕中央
            targetPosition = CalculateScreenCenterPosition();
        }
        else
        {
            // 使用自定义位置
            targetPosition = customPosition;
        }
        
        transform.position = targetPosition;
        
        Debug.Log($"Start button positioned at: {targetPosition}");
    }
    
    /// <summary>
    /// 计算屏幕中央位置
    /// </summary>
    /// <returns>屏幕中央的世界坐标</returns>
    private Vector3 CalculateScreenCenterPosition()
    {
        if (playerCamera == null) return transform.position;
        
        // 获取屏幕中央的屏幕坐标
        Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0);
        
        // 转换为世界坐标
        Vector3 worldPosition = playerCamera.ScreenToWorldPoint(screenCenter);

        worldPosition.z = 0;

        return screenCenter;
    }
    
    /// <summary>
    /// 设置按钮初始状态
    /// </summary>
    private void SetupButton()
    {
        if (buttonRenderer != null)
        {
            buttonRenderer.material.color = normalColor;
        }
        
        // 确保碰撞体是触发器
        if (buttonCollider != null)
        {
            buttonCollider.isTrigger = true;
        }
    }
    
    /// <summary>
    /// 当被射击时调用
    /// </summary>
    public void OnShot()
    {
        if (!canBeActivated || isActivated) return;
        
        Debug.Log("Start button was shot!");

        gameObject.AddComponent<Rigidbody2D>();
        gameObject.GetComponent<Rigidbody2D>().useAutoMass = true;
        gameObject.GetComponent<Rigidbody2D>().gravityScale = 15f;
        gameObject.GetComponent<Rigidbody2D>().angularDrag = 0f;

        // 播放激活音效
        PlaySound(activationSound);

        SpeakUIController.Instance.ShowTalk("game_start");
        SpeakUIController.Instance.ShowTalk("enemy_spawn");

        // 播放激活效果
        StartCoroutine(ActivationSequence());
    }
    
    /// <summary>
    /// 激活序列
    /// </summary>
    private IEnumerator ActivationSequence()
    {
        isActivated = true;
        canBeActivated = false;
        
        // 播放闪烁效果
        yield return StartCoroutine(FlashEffect(activatedColor));
        
        // 延迟后触发游戏开始
        yield return new WaitForSeconds(activationDelay);
        
        // 触发游戏开始事件
        OnButtonActivated?.Invoke(this);
        
        // 通知游戏管理器开始游戏
        if (gameManager != null)
        {
            gameManager.StartGame();
        }
        
        // 隐藏按钮
        HideButton();
    }
    
    /// <summary>
    /// 闪烁效果
    /// </summary>
    private IEnumerator FlashEffect(Color flashColor)
    {
        if (buttonRenderer == null) yield break;
        
        Color originalColor = buttonRenderer.material.color;
        
        // 闪烁几次
        for (int i = 0; i < 3; i++)
        {
            buttonRenderer.material.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            buttonRenderer.material.color = originalColor;
            yield return new WaitForSeconds(flashDuration);
        }
        
        // 最终颜色
        buttonRenderer.material.color = flashColor;
    }
    
    /// <summary>
    /// 播放音效
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    /// <summary>
    /// 隐藏按钮
    /// </summary>
    private void HideButton()
    {
        if (buttonModel != null)
        {
            buttonModel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 显示按钮
    /// </summary>
    public void ShowButton()
    {
        if (buttonModel != null)
        {
            buttonModel.SetActive(true);
        }
        
        isActivated = false;
        canBeActivated = true;
        SetupButton();
        
        // 重新设置位置
        SetButtonPosition();
    }
    
    /// <summary>
    /// 重新计算并设置按钮位置（用于屏幕尺寸变化时）
    /// </summary>
    public void RefreshPosition()
    {
        SetButtonPosition();
    }
    
    // 碰撞检测（用于鼠标悬停效果）
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerAttack") && !isHovered && !isActivated)
        {
            OnHoverEnter();
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("PlayerAttack") && isHovered && !isActivated)
        {
            OnHoverExit();
        }
    }
    
    /// <summary>
    /// 鼠标悬停进入
    /// </summary>
    private void OnHoverEnter()
    {
        isHovered = true;
        
        if (buttonRenderer != null)
        {
            buttonRenderer.material.color = hoverColor;
        }
        
        PlaySound(hoverSound);
    }
    
    /// <summary>
    /// 鼠标悬停退出
    /// </summary>
    private void OnHoverExit()
    {
        isHovered = false;
        
        if (buttonRenderer != null)
        {
            buttonRenderer.material.color = normalColor;
        }
    }
    
    // 公共访问器
    public bool IsActivated => isActivated;
    public bool CanBeActivated => canBeActivated;
    
    // 编辑器辅助方法
    [ContextMenu("Refresh Position")]
    public void EditorRefreshPosition()
    {
        SetButtonPosition();
    }
    
    [ContextMenu("Test Screen Center Position")]
    public void TestScreenCenterPosition()
    {
        Vector3 centerPos = CalculateScreenCenterPosition();
        Debug.Log($"Screen center position: {centerPos}");
        transform.position = centerPos;
    }
}
