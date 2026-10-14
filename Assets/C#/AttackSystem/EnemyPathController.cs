using UnityEngine;
using System.Collections.Generic;

public class EnemyPathController : MonoBehaviour
{
    [Header("Path Settings")]
    [SerializeField] private List<Vector3> pathPoints = new List<Vector3>();
    [SerializeField] private bool isLooping = true; // 是否循环路径
    [SerializeField] private Color pathColor = Color.green; // 路径颜色
    [SerializeField] private float pointSize = 0.5f; // 路径点大小
    
    [Header("Path Creation")]
    [SerializeField] private bool showPathEditor = true; // 是否显示路径编辑器
    
    private void OnDrawGizmos()
    {
        if (!showPathEditor || pathPoints.Count == 0) return;
        
        // 绘制路径点
        Gizmos.color = pathColor;
        for (int i = 0; i < pathPoints.Count; i++)
        {
            Vector3 worldPos = transform.TransformPoint(pathPoints[i]);
            Gizmos.DrawWireSphere(worldPos, pointSize);
            
            // 绘制路径点编号
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(worldPos + Vector3.up * pointSize, i.ToString());
            #endif
        }
        
        // 绘制路径连线
        if (pathPoints.Count > 1)
        {
            Gizmos.color = pathColor;
            for (int i = 0; i < pathPoints.Count; i++)
            {
                Vector3 currentPoint = transform.TransformPoint(pathPoints[i]);
                Vector3 nextPoint = transform.TransformPoint(pathPoints[(i + 1) % pathPoints.Count]);
                
                Gizmos.DrawLine(currentPoint, nextPoint);
            }
        }
    }
    
    // 公共方法
    public Vector3 GetPathPoint(int index)
    {
        //if (pathPoints.Count == 0) return transform.position;
        
        index = index % pathPoints.Count;
        return transform.TransformPoint(pathPoints[index]);
    }
    
    public int GetPathPointCount()
    {
        return pathPoints.Count;
    }
    
    public void AddPathPoint(Vector3 localPosition)
    {
        pathPoints.Add(localPosition);
    }
    
    public void RemovePathPoint(int index)
    {
        if (index >= 0 && index < pathPoints.Count)
        {
            pathPoints.RemoveAt(index);
        }
    }
    
    public void ClearPath()
    {
        pathPoints.Clear();
    }
    
    public void SetPathPoint(int index, Vector3 localPosition)
    {
        if (index >= 0 && index < pathPoints.Count)
        {
            pathPoints[index] = localPosition;
        }
    }
    
    // 编辑器方法
    [ContextMenu("Add Point at Current Position")]
    public void AddPointAtCurrentPosition()
    {
        Vector3 localPos = transform.InverseTransformPoint(transform.position);
        AddPathPoint(localPos);
    }
    
    [ContextMenu("Clear All Points")]
    public void ClearAllPoints()
    {
        ClearPath();
    }
    
    [ContextMenu("Create Circle Path")]
    public void CreateCirclePath()
    {
        ClearPath();
        int pointCount = 8;
        float radius = 5f;
        
        for (int i = 0; i < pointCount; i++)
        {
            float angle = i * (360f / pointCount) * Mathf.Deg2Rad;
            Vector3 point = new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0f
            );
            AddPathPoint(point);
        }
    }
    
    [ContextMenu("Create Square Path")]
    public void CreateSquarePath()
    {
        ClearPath();
        float size = 5f;
        
        AddPathPoint(new Vector3(-size, -size, 0f));
        AddPathPoint(new Vector3(size, -size, 0f));
        AddPathPoint(new Vector3(size, size, 0f));
        AddPathPoint(new Vector3(-size, size, 0f));
    }
}
