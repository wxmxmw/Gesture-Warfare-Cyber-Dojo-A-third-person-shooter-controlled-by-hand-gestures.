# 激光攻击系统使用说明

## 概述

LaserAttackProjectile 是为 TestBoss3 的 Attack3 设计的激光攻击组件，支持旋转扫射、视觉效果和伤害检测。

## 组件要求

### 必需组件
- `SpriteRenderer` - 用于显示激光贴图
- `Collider2D` - 用于碰撞检测（建议使用 BoxCollider2D 或 CapsuleCollider2D）
- `LaserAttackProjectile` - 激光控制脚本

### 可选组件
- 使用 `LaserEffect.shader` 着色器可获得更好的视觉效果

## 参数配置

### 激光伤害设置
- **Damage**: 激光造成的伤害值
- **Player Layer Mask**: 玩家所在的层级掩码

### 激光旋转设置
- **Rotation Pivot**: 旋转原点（如果为null则使用自身）
- **Start Angle**: 起始角度（度）
- **End Angle**: 结束角度（度）
- **Rotation Speed**: 旋转速度（度/秒，仅在不使用固定持续时间时生效）
- **Rotation Duration**: 旋转持续时间（秒）
- **Use Fixed Duration**: 是否使用固定持续时间而非速度

### 激光生命周期
- **Warm Up Time**: 预热时间（激光出现但不造成伤害）
- **Cool Down Time**: 冷却时间（激光消失前的时间）
- **Auto Destroy**: 是否在攻击完成后自动销毁

### 视觉效果
- **Warm Up Color**: 预热阶段的颜色
- **Active Color**: 激活阶段的颜色
- **Cool Down Color**: 冷却阶段的颜色
- **Alpha Curve**: 透明度变化曲线

### 调试显示
- **Show Debug Gizmos**: 是否显示调试辅助线
- **Gizmo Length**: 调试线条长度
- **Start/End Angle Color**: 起始/结束角度线条颜色

## 使用方法

### 1. 创建激光预制体

```csharp
// 1. 创建 GameObject
GameObject laserObj = new GameObject("Laser");

// 2. 添加 SpriteRenderer
SpriteRenderer sr = laserObj.AddComponent<SpriteRenderer>();
sr.sprite = yourLaserSprite; // 设置激光贴图
sr.material = yourLaserMaterial; // 可选：使用 LaserEffect 材质

// 3. 添加 Collider2D
BoxCollider2D collider = laserObj.AddComponent<BoxCollider2D>();
collider.isTrigger = true; // 设为触发器

// 4. 添加激光组件
LaserAttackProjectile laser = laserObj.AddComponent<LaserAttackProjectile>();
```

### 2. 配置激光参数

```csharp
// 设置激光参数
laser.SetLaserParameters(
    startAngle: 0f,      // 起始角度
    endAngle: 180f,      // 结束角度
    duration: 3f,        // 持续时间
    damage: 30f          // 伤害值
);

// 设置旋转原点（可选）
laser.SetRotationPivot(pivotTransform);
```

### 3. 在 TestBoss3 中使用

激光攻击已集成到 TestBoss3 的 Attack3 中，支持三种模式：

- **从左往右**: 激光从屏幕左侧扫向右侧
- **从右往左**: 激光从屏幕右侧扫向左侧  
- **两边往中间**: 两个激光同时从两边扫向中间

## 调试功能

### Scene 视图调试
- 绿色线条：起始角度方向
- 红色线条：结束角度方向
- 黄色弧线：旋转路径
- 青色线条：当前角度（运行时）
- 白色球体：旋转原点

### 运行时信息
```csharp
// 获取旋转进度
float progress = laser.GetRotationProgress(); // 0-1

// 获取当前角度
float currentAngle = laser.GetCurrentAngle();
```

## 事件系统

```csharp
// 订阅激光完成事件
laser.OnLaserCompleted += (laserComponent) => {
    Debug.Log("Laser attack completed!");
};

// 订阅玩家击中事件
laser.OnPlayerHit += (laserComponent, playerCollider) => {
    Debug.Log("Player hit by laser!");
};
```

## 着色器效果

使用 `LaserEffect.shader` 可获得以下效果：
- **发光效果**: 可调节发光颜色和强度
- **滚动效果**: 纹理沿激光方向滚动
- **脉冲效果**: 激光亮度周期性变化
- **边缘渐变**: 激光边缘柔和渐变
- **噪声效果**: 添加能量波动感

### 着色器参数
- **Intensity**: 整体亮度倍数
- **Glow Color/Intensity**: 发光颜色和强度
- **Scroll Speed**: 滚动速度
- **Pulse Speed/Amplitude**: 脉冲频率和幅度
- **Edge Fade**: 边缘渐变范围
- **Noise Scale/Intensity**: 噪声缩放和强度

## 性能优化

1. **合理设置生命周期**: 避免激光存在时间过长
2. **使用对象池**: 频繁创建激光时考虑使用对象池
3. **碰撞检测优化**: 合理设置 Player Layer Mask
4. **着色器LOD**: 根据设备性能调整着色器复杂度

## 注意事项

1. **旋转原点**: 确保旋转原点位置合理，避免激光旋转异常
2. **碰撞器大小**: 碰撞器应与激光贴图大小匹配
3. **层级设置**: 确保玩家在正确的层级上
4. **角度范围**: 起始和结束角度会自动标准化到 0-360 度范围
5. **资源清理**: 激光完成后会自动清理相关资源

## 示例配置

### 水平扫射激光
```csharp
laser.SetLaserParameters(0f, 180f, 2f, 25f);
```

### 垂直扫射激光  
```csharp
laser.SetLaserParameters(90f, 270f, 3f, 30f);
```

### 快速扫射
```csharp
laser.SetLaserParameters(-45f, 45f, 1f, 20f);
```
