using UnityEngine;

/// <summary>
/// 弹道效果类型枚举
/// </summary>
public enum TrajectoryEffectType
{
    /// <summary>
    /// 魔法弹道效果 - 从屏幕上方弯曲的魔法轨迹
    /// </summary>
    MagicTrajectory,
    
    /// <summary>
    /// 直接射击效果 - 从敌人位置直接射击到目标位置
    /// </summary>
    DirectShot,
    
    /// <summary>
    /// 双弧曳光弹效果 - 两条对称的弧线曳光弹从敌人飞向玩家
    /// </summary>
    DualArc
}

/// <summary>
/// 弹道效果配置
/// </summary>
[System.Serializable]
public class TrajectoryEffectConfig
{
    [Header("基础设置")]
    public TrajectoryEffectType effectType = TrajectoryEffectType.MagicTrajectory;
    public bool enableTrajectory = true;
    
    [Header("直接射击设置")]
    public float directShotDuration = 0.5f;           // 直接射击持续时间
    public float directShotWidth = 0.2f;              // 直接射击宽度
    public Color directShotColor = Color.white;       // 直接射击颜色
    
    [Header("亮度变化设置")]
    public float brightnessRiseTime = 0.1f;           // 亮度上升时间
    public float brightnessHoldTime = 0.2f;           // 亮度保持时间
    public float brightnessFadeTime = 0.3f;           // 亮度衰减时间
    public float maxBrightness = 2f;                  // 最大亮度倍数
    public AnimationCurve brightnessCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("视觉效果")]
    public Material directShotMaterial;               // 直接射击材质
    public bool enableParticles = true;              // 启用粒子效果
    public float particleIntensity = 1f;              // 粒子强度
    
    [Header("双弧曳光弹设置")]
    public float dualArcWidth = 0.15f;               // 双弧曳光弹宽度
    public Color dualArcColor = Color.blue;          // 双弧曳光弹颜色
    public float dualArcCurvature = 0.5f;            // 弧线弯曲程度 (0-1, 0=直线, 1=接近圆)
    public float dualArcOffset = 1.5f;               // 两条弧线的偏移距离
    public int dualArcSegments = 30;                 // 弧线分段数量（越多越平滑）
    public float dualArcAnimationSpeed = 2f;         // 曳光弹动画速度
    public Material dualArcMaterial;                 // 双弧曳光弹材质
    public bool dualArcUseGradient = true;           // 是否使用颜色渐变
    public Gradient dualArcColorGradient;            // 双弧颜色渐变
}
