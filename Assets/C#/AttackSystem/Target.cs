using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro; // 添加TextMeshPro引用

public class Target : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private int targetOrder = 0;
    [SerializeField] private float timeLimit = 3f;
    [SerializeField] private float health = 100f;
    [SerializeField] private bool isDestroyed = false;
    
    [Header("Visual Elements")]
    [SerializeField] private SpriteRenderer targetSprite;
    [SerializeField] private TextMeshPro orderText; // 改为TextMeshPro
    
    private CancelableAttack parentAttack;
    private TargetEliminationSystem eliminationSystem;
    private float currentTime;
    private bool isActive = true;
    
    private void Awake()
    {
        Debug.Log($"Target {targetOrder} created at position {transform.position}");
        CreateTargetUI();
    }
    
    private void CreateTargetUI()
    {
        // 创建3D Text显示顺序数字
        GameObject textGO = new GameObject("OrderText3D");
        textGO.transform.SetParent(transform);
        textGO.transform.localPosition = -Vector3.forward * 1f; // 在标靶前方显示
        
        orderText = textGO.AddComponent<TextMeshPro>();
        orderText.text = targetOrder.ToString();
        orderText.fontSize = 5f;
        orderText.color = Color.black;
        orderText.alignment = TextAlignmentOptions.Center;
        
        // 让文字始终面向相机
        textGO.AddComponent<Billboard>();
        
        Debug.Log($"Target {targetOrder} UI created with 3D Text");
    }
    
    public void Initialize(CancelableAttack attack, TargetEliminationSystem system, int order)
    {
        parentAttack = attack;
        eliminationSystem = system;
        targetOrder = order;
        currentTime = timeLimit;
        isActive = true;
        
        // 更新3D Text显示
        if (orderText != null)
        {
            orderText.text = targetOrder.ToString();
        }
        
        Debug.Log($"Target {targetOrder} initialized for attack {attack.name}");
    }
    
    private void Update()
    {
        if (!isActive || isDestroyed) return;
        
        // 更新倒计时
        currentTime -= Time.deltaTime;
        
        // 检查时间是否用完
        if (currentTime <= 0)
        {
            OnTimeExpired();
        }
    }
    
    public void TakeDamage(float damage)
    {
        if (isDestroyed || !isActive) return;
        
        health -= damage;
        Debug.Log($"Target {targetOrder} took {damage} damage. Health: {health}");
        
        // 检查是否被破坏
        if (health <= 0)
        {
            OnDestroyed();
        }
    }
    
    private void OnDestroyed()
    {
        if (isDestroyed) return; // 防止重复调用
        
        isDestroyed = true;
        isActive = false;
        
        Debug.Log($"Target {targetOrder} destroyed, notifying elimination system");
        
        // 通知消除系统
        if (eliminationSystem != null)
        {
            eliminationSystem.OnTargetDestroyed(this);
        }
        else
        {
            Debug.LogWarning($"Target {targetOrder} eliminationSystem is null!");
        }
        
        // 播放破坏效果
        StartCoroutine(DestroyEffect());
    }
    
    private void OnTimeExpired()
    {
        if (!isActive) return; // 防止重复调用
        
        isActive = false;
        
        Debug.Log($"Target {targetOrder} time expired, notifying elimination system");
        
        // 通知消除系统时间到了
        if (eliminationSystem != null)
        {
            eliminationSystem.OnTargetTimeExpired(this);
        }
        else
        {
            Debug.LogWarning($"Target {targetOrder} eliminationSystem is null!");
        }
        
        // 时间到期后也要销毁标靶
        StartCoroutine(DestroyEffect());
    }
    
    private IEnumerator DestroyEffect()
    {
        // 简单的破坏效果：淡出
        if (targetSprite != null)
        {
            float fadeTime = 0.5f;
            float elapsedTime = 0f;
            Color originalColor = targetSprite.color;
            
            while (elapsedTime < fadeTime)
            {
                float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeTime);
                targetSprite.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }
        
        // 销毁标靶
        Destroy(gameObject);
    }
    
    // 添加缺失的方法和属性
    public CancelableAttack GetAttack()
    {
        return parentAttack;
    }
    
    // 公共属性
    public int TargetOrder => targetOrder;
    public bool IsDestroyed => isDestroyed;
    public bool IsActive => isActive;
    public float CurrentTime => currentTime;
    public float Health => health;
}

// 添加Billboard组件让3D Text始终面向相机
public class Billboard : MonoBehaviour
{
    private Camera mainCamera;
    
    private void Start()
    {
        mainCamera = Camera.main;
    }
    
    private void Update()
    {
        if (mainCamera != null)
        {
            // 让文字始终面向相机
            transform.LookAt(mainCamera.transform);
            transform.Rotate(0, 180, 0); // 翻转180度，让文字正面朝向相机
        }
    }
}
