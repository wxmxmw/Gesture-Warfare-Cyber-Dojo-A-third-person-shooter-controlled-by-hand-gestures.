using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public abstract class AttackPattern : MonoBehaviour
{
    [Header("Attack Base Settings")]
    [SerializeField] protected string attackName = "Default Attack";
    [SerializeField] protected float damage = 20f;
    [SerializeField] protected float cooldown = 3f;
    [SerializeField] protected bool canInterrupt = true;
    
    [Header("UI Elements")]
    [SerializeField] protected Slider progressBar;
    [SerializeField] protected Canvas attackUI;
    
    protected Enemy owner;
    protected bool isAttacking = false;
    protected bool isInterrupted = false;
    protected float lastAttackTime = 0f;
    
    // 事件
    public System.Action<AttackPattern> OnAttackStart;
    public System.Action<AttackPattern> OnAttackComplete;
    public System.Action<AttackPattern> OnAttackInterrupted;
    
    protected virtual void Awake()
    {
        owner = GetComponent<Enemy>();
        SetupUI();
    }
    
    protected virtual void SetupUI()
    {
        if (progressBar == null)
        {
            CreateProgressBar();
        }
    }
    
    protected virtual void CreateProgressBar()
    {
        if (attackUI == null)
        {
            GameObject uiGO = new GameObject("AttackUI");
            uiGO.transform.SetParent(transform);
            attackUI = uiGO.AddComponent<Canvas>();
            attackUI.renderMode = RenderMode.WorldSpace;
            attackUI.worldCamera = Camera.main;
        }
        
        GameObject progressGO = new GameObject("ProgressBar");
        progressGO.transform.SetParent(attackUI.transform, false);
        
        progressBar = progressGO.AddComponent<Slider>();
        progressBar.minValue = 0f;
        progressBar.maxValue = 1f;
        progressBar.value = 0f;
        
        // 设置进度条位置
        RectTransform rect = progressBar.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(100, 10);
        rect.localPosition = Vector3.up * 2f; // 在敌人上方
    }
    
    // 新增：设置进度条的方法
    public virtual void SetProgressBar(Slider newProgressBar)
    {
        progressBar = newProgressBar;
        if (progressBar != null)
        {
            progressBar.minValue = 0f;
            progressBar.maxValue = 1f;
            progressBar.value = 0f;
        }
    }
    
    public virtual bool CanAttack()
    {
        return !isAttacking && Time.time >= lastAttackTime + cooldown;
    }
    
    public virtual void StartAttack()
    {
        if (!CanAttack()) return;
        
        isAttacking = true;
        isInterrupted = false;
        lastAttackTime = Time.time;
        
        OnAttackStart?.Invoke(this);
        StartCoroutine(ExecuteAttack());
    }
    
    protected abstract IEnumerator ExecuteAttack();
    
    public virtual void InterruptAttack()
    {
        if (!canInterrupt || !isAttacking) return;
        
        isInterrupted = true;
        isAttacking = false;
        OnAttackInterrupted?.Invoke(this);
        
        StopAllCoroutines();
        UpdateProgressBar(0f);
    }
    
    protected virtual void UpdateProgressBar(float progress)
    {
        if (progressBar != null)
        {
            progressBar.value = progress;
        }
    }
    
    protected virtual void CompleteAttack()
    {
        isAttacking = false;
        OnAttackComplete?.Invoke(this);
        UpdateProgressBar(0f);
    }
    
    public virtual bool IsAttacking()
    {
        return isAttacking;
    }
    
    public virtual string GetAttackName()
    {
        return attackName;
    }
    
    public virtual float GetDamage()
    {
        return damage;
    }
}
