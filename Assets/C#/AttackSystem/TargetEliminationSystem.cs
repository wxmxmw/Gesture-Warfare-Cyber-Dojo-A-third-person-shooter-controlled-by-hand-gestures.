using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class TargetEliminationSystem : MonoBehaviour
{
    [Header("Target System Settings")]
    [SerializeField] private int maxTargets = 20;
    [SerializeField] private LayerMask targetLayerMask = -1;
    
    private List<Target> activeTargets = new List<Target>();
    private Dictionary<CancelableAttack, List<Target>> attackTargets = new Dictionary<CancelableAttack, List<Target>>();
    
    public void CreateTargets(Vector3[] positions, GameObject targetPrefab, float timeLimit, CancelableAttack attack)
    {
        if (attack == null) return;
        
        List<Target> targets = new List<Target>();
        
        for (int i = 0; i < positions.Length; i++)
        {
            if (activeTargets.Count >= maxTargets) break;
            
            GameObject targetGO = Instantiate(targetPrefab, positions[i], Quaternion.identity);
            Target target = targetGO.GetComponent<Target>();
            
            if (target == null)
            {
                target = targetGO.AddComponent<Target>();
            }
            
            target.Initialize(attack, this, i);
            activeTargets.Add(target);
            targets.Add(target);
        }
        
        attackTargets[attack] = targets;
        Debug.Log($"Created {targets.Count} targets for attack {attack.name}");
    }
    
    public bool IsAttackEliminated(CancelableAttack attack)
    {
        if (!attackTargets.ContainsKey(attack)) return false;
        
        List<Target> targets = attackTargets[attack];
        foreach (Target target in targets)
        {
            if (target != null && !target.IsDestroyed)
            {
                return false;
            }
        }
        
        return true;
    }
    
    public void OnTargetDestroyed(Target target)
    {
        Debug.Log($"TargetEliminationSystem: Target {target.TargetOrder} destroyed");
        
        if (activeTargets.Contains(target))
        {
            activeTargets.Remove(target);
            Debug.Log($"Removed target from active targets, remaining: {activeTargets.Count}");
        }
        
        // 通知攻击已被消除
        if (target.GetAttack() != null)
        {
            target.GetAttack().OnTargetEliminated();
            Debug.Log($"Notified attack {target.GetAttack().name} that target was eliminated");
        }
        
        // 检查是否所有标靶都被消除
        CheckAllTargetsEliminated();
    }

    public void OnTargetTimeExpired(Target target)
    {
        Debug.Log($"TargetEliminationSystem: Target {target.TargetOrder} time expired");
        
        // 从活动标靶列表中移除
        if (activeTargets.Contains(target))
        {
            activeTargets.Remove(target);
            Debug.Log($"Removed expired target from active targets, remaining: {activeTargets.Count}");
        }
        
        // 通知攻击时间到期
        if (target.GetAttack() != null)
        {
            target.GetAttack().OnTargetTimeExpired();
            Debug.Log($"Notified attack {target.GetAttack().name} that target time expired");
        }
        
        // 检查是否所有标靶都被消除
        CheckAllTargetsEliminated();
    }
    
    private void CheckAllTargetsEliminated()
    {
        // 清理已销毁的标靶
        activeTargets.RemoveAll(t => t == null || t.IsDestroyed);
        
        Debug.Log($"Active targets remaining: {activeTargets.Count}");
        
        // 检查每个攻击的标靶状态
        foreach (var kvp in attackTargets)
        {
            CancelableAttack attack = kvp.Key;
            List<Target> targets = kvp.Value;
            
            bool allEliminated = true;
            foreach (Target target in targets)
            {
                if (target != null && !target.IsDestroyed)
                {
                    allEliminated = false;
                    break;
                }
            }
            
            if (allEliminated)
            {
                Debug.Log($"All targets for attack {attack.name} have been eliminated");
            }
        }
    }
    
    public void ClearAllTargets()
    {
        foreach (Target target in activeTargets.ToArray())
        {
            if (target != null)
            {
                Destroy(target.gameObject);
            }
        }
        
        activeTargets.Clear();
        attackTargets.Clear();
        Debug.Log("All targets cleared");
    }
    
    public List<Target> GetActiveTargets()
    {
        return activeTargets;
    }
    
    public int GetActiveTargetCount()
    {
        return activeTargets.Count;
    }
}
