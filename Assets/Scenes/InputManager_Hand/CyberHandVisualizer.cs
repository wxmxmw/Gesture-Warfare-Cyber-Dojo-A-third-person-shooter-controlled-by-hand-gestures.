using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 赛博朋克风格手部可视化增强器
/// 为手部关键点添加科技感的视觉效果
/// </summary>
public class CyberHandVisualizer : MonoBehaviour
{
    [Header("赛博朋克材质设置")]
    // 材质现在直接在BoneSphere预制体中设置
    
    [Header("发光效果设置")]
    [SerializeField] private float glowIntensity = 2.0f;
    [SerializeField] private float pulseSpeed = 3.0f;
    [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("颜色变化设置")]
    [SerializeField] private Color baseColor = new Color(0.1f, 0.8f, 1f, 1f);
    [SerializeField] private Color activeColor = new Color(1f, 0.2f, 0.8f, 1f);
    [SerializeField] private Color wristColor = new Color(1f, 0.2f, 0.8f, 1f);
    
    [Header("粒子效果设置")]
    [SerializeField] private bool enableParticleEffects = true;
    
    [Header("连线效果设置")]
    [SerializeField] private float lineWidth = 0.05f;
    [SerializeField] private Color lineColor = new Color(0.2f, 0.9f, 1f, 0.8f);
    [SerializeField] private bool enableLineGlow = true;
    
    private InputManager_Hand handManager;
    private List<Renderer> handRenderers = new List<Renderer>();
    private List<Renderer> wristRenderers = new List<Renderer>();
    private List<ParticleSystem> particleSystems = new List<ParticleSystem>();
    private LineRenderer[] lineRenderers;
    
    // 手部骨骼连接定义
    private static readonly int[,] handConnections = new int[,]
    {
        // 手腕到手掌连接
        {0, 1},   // 手腕 -> 大拇指手掌连接点
        {0, 5},   // 手腕 -> 食指手掌连接处
        {0, 9},   // 手腕 -> 中指手掌连接处
        {0, 13},  // 手腕 -> 无名指手掌连接处
        {0, 17},  // 手腕 -> 小指手掌连接处
        
        // 手掌连接线
        {1, 5},   // 大拇指手掌连接点 -> 食指手掌连接处
        {5, 9},   // 食指手掌连接处 -> 中指手掌连接处
        {9, 13},  // 中指手掌连接处 -> 无名指手掌连接处
        {13, 17}, // 无名指手掌连接处 -> 小指手掌连接处
        
        // 大拇指骨骼
        {1, 2},   // 大拇指手掌连接点 -> 大拇指底
        {2, 3},   // 大拇指底 -> 大拇指中
        {3, 4},   // 大拇指中 -> 大拇指尖
        
        // 食指骨骼
        {5, 6},   // 食指手掌连接处 -> 食指底
        {6, 7},   // 食指底 -> 食指中
        {7, 8},   // 食指中 -> 食指尖
        
        // 中指骨骼
        {9, 10},  // 中指手掌连接处 -> 中指底
        {10, 11}, // 中指底 -> 中指中
        {11, 12}, // 中指中 -> 中指尖
        
        // 无名指骨骼
        {13, 14}, // 无名指手掌连接处 -> 无名指底
        {14, 15}, // 无名指底 -> 无名指中
        {15, 16}, // 无名指中 -> 无名指尖
        
        // 小指骨骼
        {17, 18}, // 小指手掌连接处 -> 小指底
        {18, 19}, // 小指底 -> 小指中
        {19, 20}  // 小指中 -> 小指尖
    };
    
    private float pulseTimer = 0f;
    private bool isInitialized = false;

    void Start()
    {
        InitializeCyberpunkVisuals();
    }
    
    void Update()
    {
        if (!isInitialized) return;
        
        UpdatePulseEffect();
        UpdateHandStateVisuals();
        UpdateHandConnections();
    }
    
    /// <summary>
    /// 初始化赛博朋克视觉效果
    /// </summary>
    void InitializeCyberpunkVisuals()
    {
        handManager = FindObjectOfType<InputManager_Hand>();
        if (handManager == null)
        {
            Debug.LogError("CyberpunkHandVisualizer: 未找到 InputManager_Hand 组件！");
            return;
        }
        
        // 手部关键点材质和手腕材质已在预制体中设置，无需动态创建
        
        StartCoroutine(DelayedInitialization());
    }
    
    /// <summary>
    /// 延迟初始化，等待手部对象创建完成
    /// </summary>
    IEnumerator DelayedInitialization()
    {
        yield return new WaitForSeconds(0.5f);
        
        SetupLineRenderers();
        
        if (enableParticleEffects)
        {
            SetupParticleEffects();
        }
        
        isInitialized = true;
    }
    
    
    /// <summary>
    /// 设置连线渲染器
    /// </summary>
    void SetupLineRenderers()
    {
        // 创建足够数量的LineRenderer来绘制所有手部连接
        int connectionCount = handConnections.GetLength(0);
        
        // 为每只手创建连线渲染器（左手和右手各一套）
        GameObject lineContainer = new GameObject("HandConnectionLines");
        lineContainer.transform.SetParent(transform);
        
        lineRenderers = new LineRenderer[connectionCount * 2]; // 两只手
        
        for (int i = 0; i < connectionCount * 2; i++)
        {
            GameObject lineObj = new GameObject($"HandConnection_{i}");
            lineObj.transform.SetParent(lineContainer.transform);
            
            LineRenderer line = lineObj.AddComponent<LineRenderer>();
            line.material = CreateGlowLineMaterial();
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.useWorldSpace = true;
            line.positionCount = 2;
            
            // 启用发光效果
            if (enableLineGlow)
            {
                line.material.EnableKeyword("_EMISSION");
                line.material.SetColor("_EmissionColor", lineColor * glowIntensity);
            }
            
            lineRenderers[i] = line;
        }
    }
    
    /// <summary>
    /// 设置粒子效果 - 粒子效果现在直接在BoneSphere预制体中设置
    /// </summary>
    void SetupParticleEffects()
    {
        // 粒子效果现在直接在BoneSphere预制体中，无需动态创建
        // 收集现有的粒子系统用于后续控制
        ParticleSystem[] allParticles = FindObjectsOfType<ParticleSystem>();
        foreach (ParticleSystem particles in allParticles)
        {
            if (particles.name == "CyberpunkParticleEffect")
            {
                particleSystems.Add(particles);
            }
        }
    }
    
    /// <summary>
    /// 更新脉冲效果
    /// </summary>
    void UpdatePulseEffect()
    {
        pulseTimer += Time.deltaTime * pulseSpeed;
        float pulseValue = pulseCurve.Evaluate(Mathf.PingPong(pulseTimer, 1f));
        
        // 更新手部关键点发光强度
        foreach (Renderer renderer in handRenderers)
        {
            if (renderer != null && renderer.material != null)
            {
                Color emissionColor = baseColor * (glowIntensity + pulseValue);
                renderer.material.SetColor("_EmissionColor", emissionColor);
            }
        }
        
        // 更新手腕中心发光强度
        foreach (Renderer renderer in wristRenderers)
        {
            if (renderer != null && renderer.material != null)
            {
                Color emissionColor = wristColor * (glowIntensity + pulseValue * 0.5f);
                renderer.material.SetColor("_EmissionColor", emissionColor);
            }
        }
        
        // 更新连线发光强度
        if (lineRenderers != null)
        {
            foreach (LineRenderer line in lineRenderers)
            {
                if (line != null && line.material != null)
                {
                    Color emissionColor = lineColor * (glowIntensity + pulseValue * 0.3f);
                    line.material.SetColor("_EmissionColor", emissionColor);
                }
            }
        }
    }
    
    /// <summary>
    /// 更新手部状态视觉效果
    /// </summary>
    void UpdateHandStateVisuals()
    {
        if (handManager == null) return;
        
        // 根据手势状态改变颜色
        bool isGestureActive = handManager.CurrentHandGestureState != HandGestureState.None;
        Color targetColor = isGestureActive ? activeColor : baseColor;
        
        // 平滑过渡颜色
        foreach (Renderer renderer in handRenderers)
        {
            if (renderer != null && renderer.material != null)
            {
                Color currentColor = renderer.material.GetColor("_Color");
                Color newColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * 5f);
                renderer.material.SetColor("_Color", newColor);
            }
        }
        
        // 更新粒子效果强度
        foreach (ParticleSystem particles in particleSystems)
        {
            if (particles != null)
            {
                var emission = particles.emission;
                emission.rateOverTime = isGestureActive ? 20f : 10f;
            }
        }
    }
    
    /// <summary>
    /// 更新手部骨骼连接
    /// </summary>
    void UpdateHandConnections()
    {
        if (lineRenderers == null || handManager == null) return;
        
        int connectionCount = handConnections.GetLength(0);
        
        // 更新左手连线（手部索引1）
        if (handManager.HasHandData(1))
        {
            for (int i = 0; i < connectionCount; i++)
            {
                if (lineRenderers[i] != null)
                {
                    int startNodeIndex = handConnections[i, 0];
                    int endNodeIndex = handConnections[i, 1];
                    
                    Vector3 startPos = handManager.GetHandNodePosition(1, startNodeIndex);
                    Vector3 endPos = handManager.GetHandNodePosition(1, endNodeIndex);
                    
                    // 只有当位置有效时才绘制连线
                    if (startPos != Vector3.zero && endPos != Vector3.zero)
                    {
                        lineRenderers[i].enabled = true;
                        lineRenderers[i].SetPosition(0, startPos);
                        lineRenderers[i].SetPosition(1, endPos);
                    }
                    else
                    {
                        lineRenderers[i].enabled = false;
                    }
                }
            }
        }
        else
        {
            // 隐藏左手连线
            for (int i = 0; i < connectionCount; i++)
            {
                if (lineRenderers[i] != null)
                {
                    lineRenderers[i].enabled = false;
                }
            }
        }
        
        // 更新右手连线（手部索引2）
        if (handManager.HasHandData(2))
        {
            for (int i = 0; i < connectionCount; i++)
            {
                int rightHandLineIndex = connectionCount + i;
                if (rightHandLineIndex < lineRenderers.Length && lineRenderers[rightHandLineIndex] != null)
                {
                    int startNodeIndex = handConnections[i, 0];
                    int endNodeIndex = handConnections[i, 1];
                    
                    Vector3 startPos = handManager.GetHandNodePosition(2, startNodeIndex);
                    Vector3 endPos = handManager.GetHandNodePosition(2, endNodeIndex);
                    
                    // 只有当位置有效时才绘制连线
                    if (startPos != Vector3.zero && endPos != Vector3.zero)
                    {
                        lineRenderers[rightHandLineIndex].enabled = true;
                        lineRenderers[rightHandLineIndex].SetPosition(0, startPos);
                        lineRenderers[rightHandLineIndex].SetPosition(1, endPos);
                    }
                    else
                    {
                        lineRenderers[rightHandLineIndex].enabled = false;
                    }
                }
            }
        }
        else
        {
            // 隐藏右手连线
            for (int i = 0; i < connectionCount; i++)
            {
                int rightHandLineIndex = connectionCount + i;
                if (rightHandLineIndex < lineRenderers.Length && lineRenderers[rightHandLineIndex] != null)
                {
                    lineRenderers[rightHandLineIndex].enabled = false;
                }
            }
        }
    }
    
    /// <summary>
    /// 创建发光连线材质
    /// </summary>
    Material CreateGlowLineMaterial()
    {
        Material material = new Material(Shader.Find("Sprites/Default"));
        material.SetColor("_Color", lineColor);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", lineColor * glowIntensity);
        
        return material;
    }
    
    /// <summary>
    /// 设置发光强度
    /// </summary>
    public void SetGlowIntensity(float intensity)
    {
        glowIntensity = intensity;
    }
    
    /// <summary>
    /// 设置脉冲速度
    /// </summary>
    public void SetPulseSpeed(float speed)
    {
        pulseSpeed = speed;
    }
    
    /// <summary>
    /// 启用/禁用粒子效果
    /// </summary>
    public void SetParticleEffects(bool enabled)
    {
        enableParticleEffects = enabled;
        
        foreach (ParticleSystem particles in particleSystems)
        {
            if (particles != null)
            {
                if (enabled)
                    particles.Play();
                else
                    particles.Stop();
            }
        }
    }
    
    void OnDestroy()
    {
        // 材质现在由预制体管理，无需手动销毁
    }
}