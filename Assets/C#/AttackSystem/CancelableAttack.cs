using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class CancelableAttack : AttackPattern
{
    [Header("Cancelable Attack Settings")]
    [SerializeField] private float preparationTime = 0.5f;
    [SerializeField] private float eliminationTime = 3f;
    [SerializeField] private GameObject targetPrefab;
    [SerializeField] private Vector3 targetOffset = Vector3.up * 3f;
    
    [Header("Target Settings")]
    [SerializeField] private int targetCount = 3;
    [SerializeField] private float targetSpacing = 1f;
    [SerializeField] private Color targetColor = Color.yellow;
    
    private TargetEliminationSystem targetSystem;
    private bool isEliminated = false;
    
    protected override void Awake()
    {
        base.Awake();
        targetSystem = FindObjectOfType<TargetEliminationSystem>();
        
        if (progressBar != null)
        {
            // 设置进度条颜色为黄色（警告）
            var fillImage = progressBar.fillRect?.GetComponent<Image>();
            if (fillImage != null)
                fillImage.color = targetColor;
        }
    }
    
    protected override IEnumerator ExecuteAttack()
    {
        // 短准备阶段
        yield return StartCoroutine(PreparationPhase());
        
        if (isInterrupted)
        {
            yield break;
        }
        
        // 生成标靶
        yield return StartCoroutine(GenerateTargets());
        
        if (isInterrupted)
        {
            yield break;
        }
        
        // 等待玩家消除标靶
        yield return StartCoroutine(WaitForElimination());
        
        // 根据消除结果决定是否造成伤害
        if (!isEliminated)
        {
            DealDamageToPlayer();
        }
        
        CompleteAttack();
    }
    
    private IEnumerator PreparationPhase()
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < preparationTime && !isInterrupted)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / preparationTime;
            UpdateProgressBar(progress);
            yield return null;
        }
    }
    
    private IEnumerator GenerateTargets()
    {
        if (targetSystem == null || targetPrefab == null)
        {
            yield break;
        }
        
        // 生成标靶
        Vector3[] targetPositions = CalculateTargetPositions();
        targetSystem.CreateTargets(targetPositions, targetPrefab, eliminationTime, this);
        
        yield return new WaitForSeconds(0.1f);
    }
    
    private Vector3[] CalculateTargetPositions()
    {
        Vector3[] positions = new Vector3[targetCount];
        Vector3 center = transform.position + targetOffset;
        
        for (int i = 0; i < targetCount; i++)
        {
            float xOffset = (i - (targetCount - 1) * 0.5f) * targetSpacing;
            positions[i] = center + new Vector3(xOffset, 0, -1);
        }
        
        return positions;
    }
    
    private IEnumerator WaitForElimination()
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < eliminationTime && !isInterrupted)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / eliminationTime;
            UpdateProgressBar(progress);
            
            // 检查是否已被消除
            if (targetSystem.IsAttackEliminated(this))
            {
                isEliminated = true;
                break;
            }
            
            yield return null;
        }
    }
    
    private void DealDamageToPlayer()
    {
        Debug.Log($"Attack not eliminated! Dealt {damage} damage to player!");
        
        // 实际实现时应该是：
        // var player = GameObject.FindGameObjectWithTag("Player");
        // if (player != null)
        // {
        //     var playerHealth = player.GetComponent<PlayerHealth>();
        //     if (playerHealth != null)
        //         playerHealth.TakeDamage(damage);
        // }
    }
    
    public void OnTargetEliminated()
    {
        isEliminated = true;
    }

    public void OnTargetTimeExpired()
    {
        Debug.Log("CancelableAttack: Target time expired notification received");
        // 标靶时间到期时的处理逻辑
        // 可以在这里添加时间到期的特殊处理
    }
}
