
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDemo : MonoBehaviour
{
    private Vector3 lastValidPosition; // 保存最后一个有效位置

    void Start()
    {
        lastValidPosition = transform.position; // 初始化为当前位置
    }

    void Update()
    {

        // 检查是否有手部数据可用
        if (InputManager_Hand.Instance != null && (InputManager_Hand.Instance.HasHandData(1) || InputManager_Hand.Instance.HasHandData(2)))
        {
            Vector3 newPosition = InputManager_Hand.Instance.GetPlayerPosition();
            if (newPosition.x != 0)
            {
                // 更新位置并保存为最后有效位置
                transform.position = newPosition;
                lastValidPosition = newPosition;
            }
            else//手腕出屏幕
            {
                transform.position = lastValidPosition;
            }
        }
        else
        {
            // 当手部数据不可用时，保持在最后有效位置
            transform.position = lastValidPosition;
        }
    }
}
