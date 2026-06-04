using System.Collections.Generic;

public class EnemyStateMachine : StateMachine<EnemyState, EnemyStateType>
{
    public EnemyStateMachine(Dictionary<EnemyStateType, EnemyState> allStates) : base(allStates) { }

    public void AddState(EnemyStateType type, EnemyState state)
    {
        if (!states.ContainsKey(type))
        {
            states.Add(type, state);
        }
    }
}
public enum EnemyStateType
{
    Idle,
    Patrol,
    Follow,
    Orbit,
    Attack,
    Hit,
    Stunned,
    Death,
    Scream,
    Block
}