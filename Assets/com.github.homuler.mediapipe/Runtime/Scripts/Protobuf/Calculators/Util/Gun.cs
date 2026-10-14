using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    [Header("目标准星")]
    public Transform targetCrosshair; // 把准星拖到这里

    void Update()
    {
        // 1. 如果没有绑定准星，直接跳过防报错
        if (targetCrosshair == null) return;

        // 2. 核心逻辑：计算小人中心到准星的连线方向
        Vector3 direction = targetCrosshair.position - transform.position;

        // 3. 计算连线的角度（反正切函数计算出弧度，再转成角度）
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // 4. 把算出来的角度应用到枪的 Z 轴旋转上
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}