using UnityEngine;

/// <summary>
/// 敌人状态基类
/// </summary>
public abstract class EnemyState
{
    protected EnemyStateMachine stateMachine;
    protected EnemyWithNewAttackSystem enemy;
    protected Transform transform;
    
    public EnemyState(EnemyStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
        this.enemy = stateMachine.GetEnemyComponent();
        this.transform = stateMachine.transform;
    }
    
    public abstract void EnterState();
    public abstract void UpdateState();
    public abstract void ExitState();
}

/// <summary>
/// 正常状态
/// 敌人进行常规移动和行为
/// </summary>
public class EnemyNormalState : EnemyState
{
    public EnemyNormalState(EnemyStateMachine stateMachine) : base(stateMachine) { }
    
    public override void EnterState()
    {
        if (enemy != null )
        {
            enemy.PlayIdleAnimation();
            Debug.Log("enemy.PlayIdleAnimation()");
        }
    }
    
    public override void UpdateState()
    {
    }
    
    public override void ExitState()
    {
        // 退出正常状态
    }
}

/// <summary>
/// 攻击准备状态
/// 敌人准备发动攻击，播放准备动画
/// </summary>
public class EnemyAttackPreparationState : EnemyState
{
    public EnemyAttackPreparationState(EnemyStateMachine stateMachine) : base(stateMachine) { }
    
    public override void EnterState()
    {
        if (enemy != null)
        {
            enemy.PlayAttackPreparationAnimation();
        }
    }
    
    public override void UpdateState()
    {
    }
    
    public override void ExitState()
    {
    }
}

/// <summary>
/// 攻击中状态
/// 敌人正在执行攻击，等待攻击物体完成攻击
/// </summary>
public class EnemyAttackingState : EnemyState
{
    public EnemyAttackingState(EnemyStateMachine stateMachine) : base(stateMachine) { }
    
    public override void EnterState()
    {
        if (enemy != null)
        {
            enemy.PlayAttackAnimation();
        }
    }
    
    public override void UpdateState()
    {
    }
    
    public override void ExitState()
    {
    }
}
