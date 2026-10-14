using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ScreenResolution
{
    public int width = 800;
    public int height = 600;
    public string name = "Custom";
    
    public ScreenResolution(int w, int h, string n)
    {
        width = w;
        height = h;
        name = n;
    }
}

public class CameraController : MonoBehaviour
{
    [Header("Screen Resolution Settings")]
    [SerializeField] private ScreenResolution customResolution = new ScreenResolution(800, 600, "Custom");
    
    [Header("Preset Resolutions")]
    [SerializeField] private ScreenResolution[] presetResolutions = new ScreenResolution[]
    {
        new ScreenResolution(800, 600, "4:3 Standard"),
        new ScreenResolution(1080, 1920, "9:16 Portrait"),
        new ScreenResolution(750, 1334, "iPhone 6/7/8"),
        new ScreenResolution(828, 1792, "iPhone XR"),
        new ScreenResolution(1080, 2340, "Android Standard"),
        new ScreenResolution(720, 1280, "HD Portrait")
    };
    
    [Header("Camera Settings")]
    [SerializeField] private Camera gameCamera;
    [SerializeField] private bool usePerspective = true; // 改为透视相机
    [SerializeField] private float fieldOfView = 60f; // 透视相机的视野角度
    [SerializeField] private float nearClipPlane = 0.1f; // 近裁剪面
    [SerializeField] private float farClipPlane = 1000f; // 远裁剪面
    
    [Header("Background")]
    [SerializeField] private BackgroundController backgroundController;
    
    private void Awake()
    {
        if (gameCamera == null)
            gameCamera = Camera.main ?? GetComponent<Camera>();
            
        SetupCamera();
        ApplyResolution();
    }
    
    private void Start()
    {
        if (backgroundController != null)
            backgroundController.SetupBackground(gameCamera);
    }
    
    private void SetupCamera()
    {
        if (gameCamera != null)
        {
            gameCamera.orthographic = !usePerspective; // 设置为透视相机
            if (usePerspective)
            {
                gameCamera.fieldOfView = fieldOfView;
                gameCamera.nearClipPlane = nearClipPlane;
                gameCamera.farClipPlane = farClipPlane;
            }
        }
    }
    
    public void ApplyResolution()
    {
        if (Application.isEditor)
        {
            // 在编辑器中设置Game窗口分辨率
            #if UNITY_EDITOR
            SetGameViewResolution(customResolution.width, customResolution.height);
            #endif
        }
        else
        {
            // 在构建版本中设置屏幕分辨率
            Screen.SetResolution(customResolution.width, customResolution.height, false);
        }
        
        // 强制竖屏
        Screen.orientation = ScreenOrientation.Portrait;
        
        UpdateCameraForResolution();
    }
    
    private void UpdateCameraForResolution()
    {
        if (gameCamera != null && usePerspective)
        {
            float aspectRatio = (float)customResolution.width / customResolution.height;
            // 根据屏幕比例调整透视相机视野
            // 透视相机的视野会根据屏幕比例自动调整，不需要手动计算
            gameCamera.fieldOfView = fieldOfView;
        }
    }
    
    #if UNITY_EDITOR
    private void SetGameViewResolution(int width, int height)
    {
        // 这个方法需要反射来在编辑器中设置Game窗口分辨率
        var gameViewType = System.Type.GetType("UnityEditor.GameView,UnityEditor");
        var getMainGameView = gameViewType.GetMethod("GetMainGameView", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        if (getMainGameView != null)
        {
            var gameView = getMainGameView.Invoke(null, null);
            if (gameView != null)
            {
                var sizeField = gameViewType.GetField("m_TargetSize", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (sizeField != null)
                {
                    sizeField.SetValue(gameView, new Vector2(width, height));
                }
            }
        }
    }
    #endif
    
    // Inspector 按钮
    [ContextMenu("Apply Current Resolution")]
    public void ApplyCurrentResolution()
    {
        ApplyResolution();
    }
    
    public void SetResolution(int width, int height)
    {
        customResolution.width = width;
        customResolution.height = height;
        ApplyResolution();
    }
    
    public void UsePresetResolution(int presetIndex)
    {
        if (presetIndex >= 0 && presetIndex < presetResolutions.Length)
        {
            customResolution = new ScreenResolution(
                presetResolutions[presetIndex].width,
                presetResolutions[presetIndex].height,
                presetResolutions[presetIndex].name
            );
            ApplyResolution();
        }
    }
    
    // 获取世界坐标边界（用于背景适配）- 透视相机版本
    public Bounds GetCameraBounds()
    {
        if (gameCamera == null) return new Bounds();
        
        if (usePerspective)
        {
            // 透视相机的视野边界计算
            float distance = 10f; // 设置一个参考距离
            float height = 2f * distance * Mathf.Tan(gameCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float width = height * gameCamera.aspect;
            
            Vector3 center = gameCamera.transform.position + gameCamera.transform.forward * distance;
            return new Bounds(center, new Vector3(width, height, 0));
        }
        else
        {
            // 保留原有的正交相机边界计算（以防万一需要切换回正交）
            float height = gameCamera.orthographicSize * 2f;
            float width = height * gameCamera.aspect;
            
            return new Bounds(gameCamera.transform.position, new Vector3(width, height, 0));
        }
    }
}
