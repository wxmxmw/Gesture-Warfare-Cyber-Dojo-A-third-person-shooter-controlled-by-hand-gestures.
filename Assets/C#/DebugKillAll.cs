using UnityEngine;

/// <summary>
/// 调试快捷键：按 Space 秒杀场景内所有敌人
/// 调试完毕后直接删除此文件，并移除场景中挂载该脚本的组件即可
/// </summary>
public class DebugKillAll : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            foreach (var enemy in FindObjectsOfType<EnemyWithNewAttackSystem>())
            {
                if (!enemy.IsDead)
                    enemy.TakeDamage(float.MaxValue);
            }
        }
    }
}
