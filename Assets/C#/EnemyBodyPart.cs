using UnityEngine;

public class EnemyBodyPart : MonoBehaviour
{
    private MonoBehaviour parentEnemy;
    private BodyPart bodyPartData;
    public bool isBoss = false;
    public GameObject DestroyEffect;// 被破坏时播放的效果

    public void Initialize(MonoBehaviour enemy, BodyPart bodyPart)
    {
        parentEnemy = enemy;
        bodyPartData = bodyPart;

        if (bodyPart.partHealth != bodyPart.maxPartHealth) 
        {
            bodyPart.partHealth = bodyPart.maxPartHealth;
        }
    }
    
    public MonoBehaviour GetParentEnemy()
    {
        return parentEnemy;
    }
    
    public BodyPart GetBodyPartData()
    {
        return bodyPartData;
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 这里可以添加触碰逻辑，比如近战攻击
        if (other.CompareTag("Player"))
        {
            // 敌人接触到玩家的逻辑
        }
    }
    
    // 部位破坏时的处理逻辑 - 可被子类重写
    public virtual void OnPartDestroyed()
    {
        if (bodyPartData != null && bodyPartData.partObject != null)
        {
            // 生成一个lighting效果出来在对应的被破坏的部位下面
            if(DestroyEffect != null) 
            {
                Instantiate(DestroyEffect, this.transform);
            }
            
            Debug.Log($"Body part {bodyPartData.partName} has been destroyed and hidden!");
        }
    }
    
    // 获取部位血量百分比
    public float GetPartHealthPercentage()
    {
        if (bodyPartData != null)
        {
            return bodyPartData.partHealth / bodyPartData.maxPartHealth;
        }
        return 0f;
    }
    
    // 检查部位是否已被破坏
    public bool IsPartDestroyed()
    {
        if (bodyPartData != null && bodyPartData.enableDestruction)
        {
            return bodyPartData.partHealth <= 0f;
        }
        return false;
    }
} 