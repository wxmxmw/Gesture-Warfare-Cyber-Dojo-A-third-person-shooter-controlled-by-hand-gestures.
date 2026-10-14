using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TrajectoryLine : MonoBehaviour
{
    [Header("目标引用")]
    [Tooltip("拖入玩家(Player)对象")]
    public Transform playerTransform;
    
    [Tooltip("拖入准星(Crosshair)对象")]
    public Transform crosshairTransform;

    [Header("弹道设置")]
    [Tooltip("弹道在屏幕上停留的时间(秒)")]
    public float displayDuration = 0.05f;

    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        // 我们只需要画一条直线，起点和终点
        lineRenderer.positionCount = 2;
        // 游戏开始时默认隐藏弹道
        lineRenderer.enabled = false;
    }

    // 这个方法专门留给 crosshairTarget 脚本在开火时调用
    public void ShowShootLine()
    {
        if (playerTransform == null || crosshairTransform == null)
        {
            Debug.LogWarning("TrajectoryLine: 玩家或准星的引用为空，无法画线！");
            return;
        }

        // 停止之前的协程（防止连续快速开火时发生冲突）
        StopAllCoroutines();

        // 开启显示弹道的协程
        StartCoroutine(RenderLineCoroutine());
    }

    private IEnumerator RenderLineCoroutine()
    {
        // 1. 获取坐标，并强制把 Z 轴拉到前面（比如 -1），防止被 2D 背景遮挡
        Vector3 startPos = playerTransform.position;
        startPos.z = -1f; 
        
        Vector3 endPos = crosshairTransform.position;
        endPos.z = -1f;

        // 2. 设置线的起点和终点
        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);

        // 3. 显示弹道
        lineRenderer.enabled = true;

        // 4. 等待零点几秒（营造开火瞬间的闪烁感）
        yield return new WaitForSeconds(displayDuration);

        // 5. 隐藏弹道
        lineRenderer.enabled = false;
    }
}