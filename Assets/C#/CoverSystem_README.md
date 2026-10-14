# 掩体系统 (Cover System) 使用说明

## 概述

掩体系统为游戏提供了完整的掩体机制，玩家可以躲在掩体后面避免敌人攻击，掩体会根据受到的伤害改变状态。

## 核心组件

### 1. Cover.cs - 掩体物体类
管理单个掩体的血量、状态和渲染。

**主要功能：**
- 掩体血量管理
- 状态切换（完好/破损/消失）
- 外观渲染（支持Sliced模式）
- 伤害检测和处理

**掩体状态：**
- `Perfect`：完好状态（血量50%以上）
- `Damaged`：破损状态（血量50%以下）  
- `Destroyed`：消失状态（血量≤0）

### 2. CoverController.cs - 掩体控制器
管理所有掩体的生成、删除和攻击检测。

**主要功能：**
- 掩体网格管理（屏幕分为10个网格）
- 掩体生成和销毁
- 玩家保护检测
- 批量掩体操作

### 3. 攻击系统集成
修改了 `AttackProjectile.cs` 以支持掩体保护机制。

## 使用方法

### 基础设置

1. **在场景中添加CoverController**
```csharp
// 在场景中创建一个空GameObject，添加CoverController组件
GameObject coverManager = new GameObject("CoverManager");
CoverController controller = coverManager.AddComponent<CoverController>();
```

2. **配置掩体资源**
在CoverController的Inspector中设置：
- `Perfect Cover Sprite`：完好状态贴图
- `Damaged Cover Sprite`：破损状态贴图
- `Perfect Cover Height`：完好状态高度
- `Damaged Cover Height`：破损状态高度
- `Default Cover Health`：默认掩体血量

3. **设置掩体网格**
在Inspector中的`Cover Grid`数组中勾选需要生成掩体的网格位置（0-9）。

### 代码API使用

#### 创建和管理掩体

```csharp
// 获取掩体控制器实例
CoverController coverController = CoverController.Instance;

// 创建单个掩体
Cover newCover = coverController.CreateCover(gridIndex: 3, health: 100f);

// 销毁掩体
coverController.DestroyCover(gridIndex: 3);

// 清除所有掩体
coverController.ClearAllCovers();
```

#### 快速设置掩体模式

```csharp
// 使用字符串模式设置掩体（1=有掩体，0=无掩体）
coverController.QuickSetCoverPattern("1010101010");

// 创建连续掩体（从网格2开始，创建3个连续掩体）
coverController.QuickCreateContinuousCovers(startIndex: 2, count: 3);

// 创建间隔掩体（从网格1开始，间隔2个网格，创建4个掩体）
coverController.QuickCreateIntervalCovers(startIndex: 1, interval: 2, count: 4);
```

#### 掩体操作

```csharp
// 对掩体造成伤害
cover.TakeDamage(25f);

// 修复掩体
cover.Repair(50f);

// 重置掩体到完好状态
cover.ResetCover();

// 检查点是否在掩体范围内
bool isProtected = cover.IsPointInCover(playerPosition);
```

#### 玩家保护检测

```csharp
// 检查玩家是否被掩体保护
Cover protectingCover = coverController.GetCoverProtectingPlayer(
    playerPosition, 
    attackerPosition
);

if (protectingCover != null)
{
    Debug.Log($"玩家受到掩体 {protectingCover.GridIndex} 保护");
}
```

### 关卡脚本示例

```csharp
public class LevelScript : MonoBehaviour
{
    private void Start()
    {
        // 关卡开始时设置掩体
        SetupLevelCovers();
    }
    
    private void SetupLevelCovers()
    {
        CoverController coverController = CoverController.Instance;
        
        // 第一关：稀疏掩体
        if (currentLevel == 1)
        {
            coverController.QuickSetCoverPattern("1000100010");
        }
        // 第二关：密集掩体
        else if (currentLevel == 2)
        {
            coverController.QuickSetCoverPattern("1101101101");
        }
        // Boss关：中央掩体
        else if (currentLevel == 3)
        {
            coverController.QuickCreateContinuousCovers(4, 2);
        }
    }
    
    private void OnWaveComplete()
    {
        // 波次完成时修复掩体
        RepairAllCovers();
    }
    
    private void RepairAllCovers()
    {
        var activeCovers = CoverController.Instance.ActiveCover;
        foreach (var kvp in activeCovers)
        {
            kvp.Value.ResetCover();
        }
    }
}
```

## 技术细节

### 网格系统
- 屏幕宽度被均分为10个网格（可配置）
- 每个网格的宽度 = Screen.width / 10
- 掩体位置基于网格索引自动计算

### 渲染层级
- 掩体的渲染层级设置为-1，确保在玩家后面
- 玩家的渲染层级应该设置为0或更高

### 碰撞检测
- 掩体自动添加BoxCollider2D组件
- 碰撞体尺寸根据掩体状态自动调整

### 攻击保护机制
- 只有当攻击者在玩家前方时，掩体才提供保护
- 攻击优先命中掩体，掩体受损后玩家才会受到伤害
- 支持所有继承自AttackProjectile的攻击类型

## 事件系统

### Cover事件
```csharp
// 掩体被摧毁
cover.OnCoverDestroyed += (cover) => Debug.Log($"掩体 {cover.GridIndex} 被摧毁");

// 掩体受到伤害
cover.OnCoverDamaged += (cover, damage) => Debug.Log($"掩体受到 {damage} 点伤害");

// 掩体状态改变
cover.OnCoverStateChanged += (cover, newState) => Debug.Log($"掩体状态变为 {newState}");
```

### CoverController事件
```csharp
// 掩体创建
CoverController.Instance.OnCoverCreated += (cover) => Debug.Log("掩体已创建");

// 掩体销毁
CoverController.Instance.OnCoverDestroyed += (cover) => Debug.Log("掩体已销毁");

// 网格变化
CoverController.Instance.OnCoverGridChanged += (gridIndex) => Debug.Log($"网格 {gridIndex} 发生变化");
```

## 调试和测试

### 使用CoverSystemExample
1. 在场景中添加`CoverSystemExample`组件
2. 设置`Auto Test`为true进行自动测试
3. 或使用Context Menu中的手动测试方法

### Inspector调试
- 在CoverController的Inspector中可以实时查看和修改网格配置
- 使用Context Menu快速测试各种功能

### 控制台输出
- 启用`Show Debug Info`查看详细的掩体操作日志
- 使用`GetAllCoversInfo()`获取所有掩体的状态信息

## 性能优化建议

1. **合理设置掩体数量**：避免同时创建过多掩体
2. **及时清理销毁的掩体**：系统会自动处理，但可以手动调用清理
3. **使用对象池**：对于频繁创建销毁的场景，考虑实现掩体对象池
4. **批量操作**：使用快速设置方法而不是逐个创建掩体

## 扩展建议

1. **掩体类型**：可以扩展不同材质的掩体（金属、木头等）
2. **修复机制**：添加玩家主动修复掩体的功能
3. **动态掩体**：支持可移动或可升降的掩体
4. **视觉效果**：添加掩体受击的粒子效果和音效
5. **AI集成**：让敌人AI也能利用掩体进行战术移动
