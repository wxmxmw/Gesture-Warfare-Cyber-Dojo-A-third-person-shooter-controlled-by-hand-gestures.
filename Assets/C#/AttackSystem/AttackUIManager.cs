using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class AttackUIManager : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private Canvas worldSpaceCanvas;
    [SerializeField] private GameObject progressBarPrefab;
    [SerializeField] private Camera gameCamera;
    
    private Dictionary<AttackPattern, GameObject> attackUIs = new Dictionary<AttackPattern, GameObject>();
    
    private void Awake()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;
            
        if (worldSpaceCanvas == null)
            CreateWorldSpaceCanvas();
    }
    
    private void CreateWorldSpaceCanvas()
    {
        GameObject canvasGO = new GameObject("AttackUI_WorldSpace");
        canvasGO.transform.SetParent(transform);
        
        worldSpaceCanvas = canvasGO.AddComponent<Canvas>();
        worldSpaceCanvas.renderMode = RenderMode.WorldSpace;
        worldSpaceCanvas.worldCamera = gameCamera;
        
        // 添加CanvasScaler
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
    }
    
    public void CreateAttackUI(AttackPattern attack, Vector3 worldPosition)
    {
        if (attack == null || attackUIs.ContainsKey(attack)) return;
        
        GameObject uiGO = Instantiate(progressBarPrefab, worldPosition, Quaternion.identity);
        uiGO.transform.SetParent(worldSpaceCanvas.transform, false);
        
        // 设置位置
        uiGO.transform.position = worldPosition + Vector3.up * 2f;
        
        // 设置进度条
        Slider progressBar = uiGO.GetComponentInChildren<Slider>();
        if (progressBar != null)
        {
            attack.SetProgressBar(progressBar);
        }
        
        attackUIs[attack] = uiGO;
    }
    
    public void RemoveAttackUI(AttackPattern attack)
    {
        if (attackUIs.ContainsKey(attack))
        {
            GameObject uiGO = attackUIs[attack];
            if (uiGO != null)
            {
                Destroy(uiGO);
            }
            attackUIs.Remove(attack);
        }
    }
    
    public void UpdateAttackUIPosition(AttackPattern attack, Vector3 newPosition)
    {
        if (attackUIs.ContainsKey(attack))
        {
            GameObject uiGO = attackUIs[attack];
            if (uiGO != null)
            {
                uiGO.transform.position = newPosition + Vector3.up * 2f;
            }
        }
    }
    
    public void ClearAllAttackUIs()
    {
        foreach (GameObject ui in attackUIs.Values)
        {
            if (ui != null)
            {
                Destroy(ui);
            }
        }
        attackUIs.Clear();
    }
    
    private void Update()
    {
        // 让UI始终面向相机
        if (worldSpaceCanvas != null && gameCamera != null)
        {
            worldSpaceCanvas.transform.LookAt(gameCamera.transform);
        }
    }
}
