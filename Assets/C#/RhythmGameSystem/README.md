# 音游小游戏系统 (Rhythm Game System)

## 概述

这是一个集成到Gesture Warfare游戏中的音游风格小游戏系统，实现了类似于音游"掉方块然后在限定时间段打掉"的机制。

## 核心组件

### 1. RhythmGameController（主控制器）
- **文件位置**: `Assets/C#/RhythmGameSystem/RhythmGameController.cs`
- **功能**: 管理整个音游系统的核心逻辑
- **主要特性**:
  - 支持多种退出条件（时间、消除数量、敌人全灭）
  - 自动生成和管理判定圆弧
  - 处理判定逻辑和伤害系统
  - 提供完整的事件系统

### 2. JudgmentCircle（判定圆圈）
- **文件位置**: `Assets/C#/RhythmGameSystem/JudgmentCircle.cs`
- **功能**: 屏幕中心的大圆圈，提供判定范围的视觉反馈
- **主要特性**:
  - 可配置的圆圈大小和颜色
  - 扇形判定范围显示
  - 脉冲动画效果
  - 透明度渐变效果

### 3. JudgmentArc（判定圆弧）
- **文件位置**: `Assets/C#/RhythmGameSystem/JudgmentArc.cs`
- **功能**: 从远处向中心移动的圆弧状判定块
- **主要特性**:
  - 自动向目标移动
  - 颜色渐变效果（从青色到红色）
  - 判定窗口检测
  - 击中动画效果

### 4. RhythmGameBossState（Boss状态集成）
- **文件位置**: `Assets/C#/RhythmGameSystem/RhythmGameBossState.cs`
- **功能**: 将音游机制集成到Boss状态机中
- **主要特性**:
  - 继承IBossState接口
  - 支持TestBoss和TestBoss1
  - 可配置的游戏参数

### 5. RhythmGameManager（管理器）
- **文件位置**: `Assets/C#/RhythmGameSystem/RhythmGameManager.cs`
- **功能**: 全局管理音游系统的预制体和设置
- **主要特性**:
  - 单例模式
  - 预制体管理
  - 控制器创建

## 使用方法

### 1. 快速测试（无需预制体）

添加 `RhythmGameTester` 脚本到场景中的任意GameObject：

```csharp
// 自动创建控制器和UI组件
// 按 Space 开始游戏，按 Esc 停止游戏
// 在Inspector中调整参数：圆弧角度、判定角度等
```

### 2. 基本使用

```csharp
// 获取或创建音游控制器
RhythmGameController controller = FindObjectOfType<RhythmGameController>();

// 启动音游（时间模式，持续30秒）
controller.StartRhythmGame(
    RhythmGameController.RhythmGameExitCondition.Timer,
    30f, // 持续时间
    10   // 目标消除数量（在时间模式下不使用）
);

// 停止音游
controller.StopRhythmGame();
```

### 2. 在Boss状态中使用

```csharp
// 在Boss状态的EnterState方法中
public void EnterState(BossController bossController)
{
    // 切换到音游状态
    var rhythmGameState = new TestBoss1RhythmGameState(
        RhythmGameController.RhythmGameExitCondition.Timer,
        15f, // 15秒
        8    // 需要消除8个判定块
    );
    
    bossController.GetComponent<BossStateMachine>().ChangeState(rhythmGameState);
}
```

### 3. 退出条件类型

- **Timer**: 按时间退出，达到指定时间后自动结束
- **ArcsDestroyed**: 按消除数量退出，消除指定数量的判定块后结束
- **AllEnemiesDead**: 按敌人状态退出，所有敌人和Boss死亡后结束

## 游戏机制详解

### 判定系统
1. **圆弧生成**: 圆弧从屏幕边缘的随机角度生成
2. **移动**: 圆弧以固定速度向中心圆圈移动
3. **判定窗口**: 当圆弧接近中心圆圈时进入判定窗口
4. **扇形检测**: 检查玩家准心是否在对应的扇形范围内
5. **结果处理**: 
   - 成功：对所有敌人造成伤害
   - 失败：对玩家造成伤害

### 视觉效果
1. **颜色变化**: 圆弧从青色渐变到红色，表示紧迫感
2. **透明度动画**: 判定范围根据剩余时间调整透明度
3. **脉冲效果**: 中心圆圈具有脉冲动画
4. **击中效果**: 成功击中时播放放大淡出动画

### 伤害系统
- **成功判定**: 对场上所有活着的敌人和Boss造成伤害
- **失败判定**: 对玩家造成伤害
- **伤害数值**: 可在RhythmGameController中配置

## 配置参数

### RhythmGameController主要参数
```csharp
[Header("游戏参数")]
public float circleRadius = 150f;           // 圆圈半径
public float arcSpawnDistance = 400f;       // 圆弧生成距离
public float arcMoveSpeed = 100f;           // 圆弧移动速度
public float judgmentWindow = 1f;           // 判定窗口时间
public float arcSpawnInterval = 2f;         // 圆弧生成间隔

[Header("伤害设置")]
public float successDamageToEnemies = 20f;  // 成功时对敌人的伤害
public float failureDamageToPlayer = 10f;   // 失败时对玩家的伤害
```

## 扩展性

### 添加新的Boss状态
1. 继承`RhythmGameBossState`类
2. 重写`EnterState`方法添加特定的动画或效果
3. 在`AllBossStates.cs`中添加新的状态类

### 自定义判定逻辑
1. 修改`RhythmGameController.IsPlayerAimInSector`方法
2. 调整扇形角度范围
3. 添加新的判定条件

### 添加新的视觉效果
1. 在`JudgmentCircle`或`JudgmentArc`中添加新的动画
2. 使用Unity的Animation System
3. 添加粒子效果

## 事件系统

```csharp
// 订阅音游事件
controller.OnRhythmGameStarted += () => Debug.Log("Game Started!");
controller.OnRhythmGameEnded += () => Debug.Log("Game Ended!");
controller.OnJudgmentResult += (success) => 
{
    if (success)
        Debug.Log("Perfect!");
    else
        Debug.Log("Miss!");
};
```

## 注意事项

1. **预制体设置**: 确保在RhythmGameController中设置了正确的预制体引用
2. **Canvas配置**: 使用Screen Space - Overlay模式的Canvas效果最佳
3. **性能优化**: 大量圆弧时注意性能，可以限制同时存在的圆弧数量
4. **准心系统**: 依赖crosshaircontroller系统，确保其正常工作
5. **坐标系统**: 使用UI坐标系统，注意世界坐标和屏幕坐标的转换

## 调试建议

1. 启用Debug模式查看详细日志
2. 使用Unity的Scene视图观察圆弧移动路径
3. 调整判定窗口时间以获得最佳体验
4. 测试不同的扇形角度范围

## 未来扩展计划

- [ ] 添加音效系统
- [ ] 支持更多判定块形状
- [ ] 添加连击系统
- [ ] 支持多玩家模式
- [ ] 添加难度等级系统
