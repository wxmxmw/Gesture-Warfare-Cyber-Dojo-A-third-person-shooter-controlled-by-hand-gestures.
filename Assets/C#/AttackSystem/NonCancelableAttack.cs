using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class NonCancelableAttack : AttackPattern
{
    [Header("Non-Cancelable Attack Settings")]
    [SerializeField] protected float preparationTime = 2f;
    [SerializeField] protected float attackRange = 3f;
    [SerializeField] protected LayerMask playerLayerMask = -1;
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject preparationEffect;
    [SerializeField] private GameObject attackEffect;
    [SerializeField] private Color preparationColor = Color.red;
    
    private Transform player;
    private bool isInRange = false;
    
    protected override void Awake()
    {
        base.Awake();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        /*
        if (progressBar != null)
        {
            // 设置进度条颜色为红色（危险）
            var fillImage = progressBar.fillRect?.GetComponent<Image>();
            if (fillImage != null)
                fillImage.color = preparationColor;
        }*/
    }
    
    protected override IEnumerator ExecuteAttack()
    {
        // 检查是否在攻击范围内
        if (!CheckPlayerInRange())
        {
            CompleteAttack();
            yield break;
        }
        
        // 攻击准备阶段
        yield return StartCoroutine(PreparationPhase());
        
        if (isInterrupted)
        {
            yield break;
        }
        
        // 执行攻击
        yield return StartCoroutine(AttackPhase());
        
        CompleteAttack();
    }
    
    private IEnumerator PreparationPhase()
    {
        float elapsedTime = 0f;
        
        // 显示准备特效
        if (preparationEffect != null)
        {
            GameObject effect = Instantiate(preparationEffect, transform.position, Quaternion.identity);
            effect.transform.SetParent(transform);
        }
        
        while (elapsedTime < preparationTime && !isInterrupted)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / preparationTime;
            UpdateProgressBar(progress);
            
            // 检查玩家是否仍在范围内
            if (!CheckPlayerInRange())
            {
                isInterrupted = true;
                break;
            }
            
            yield return null;
        }
        
        // 清理准备特效
        var effects = GetComponentsInChildren<ParticleSystem>();
        foreach (var effect in effects)
        {
            if (effect != null)
                Destroy(effect.gameObject);
        }
    }
    
    private IEnumerator AttackPhase()
    {
        if (isInterrupted) yield break;
        
        // 显示攻击特效
        if (attackEffect != null)
        {
            GameObject effect = Instantiate(attackEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // 立即对玩家造成伤害
        DealDamageToPlayer();
        
        yield return new WaitForSeconds(0.1f);
    }
    
    private bool CheckPlayerInRange()
    {
        if (player == null) return false;
        
        float distance = Vector3.Distance(transform.position, player.position);
        isInRange = distance <= attackRange;
        return isInRange;
    }
    
    private void DealDamageToPlayer()
    {
        if (player == null) return;
        
        // 这里需要PlayerHealth组件，暂时用Debug代替
        Debug.Log($"Dealt {damage} damage to player!");
        
        // 实际实现时应该是：
        // var playerHealth = player.GetComponent<PlayerHealth>();
        // if (playerHealth != null)
        //     playerHealth.TakeDamage(damage);
    }
    
    private void OnDrawGizmosSelected()
    {
        // 显示攻击范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
