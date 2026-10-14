using UnityEngine;

public class BossStateMachine : MonoBehaviour
{
    private IBossState currentState;
    public IBossState CurrentState => currentState;

    public void ChangeState(IBossState newState)
    {
        currentState?.ExitState(this.GetComponent<BossController>());
        currentState = newState;
        currentState?.EnterState(this.GetComponent<BossController>());
    }

    private void Update()
    {
        currentState?.UpdateState(this.GetComponent<BossController>());
    }
}

public interface IBossState
{
    // 进入状态时调用
    void EnterState(BossController boss);
    
    // 状态更新时调用
    void UpdateState(BossController boss);
    
    // 退出状态时调用
    void ExitState(BossController boss);
}
