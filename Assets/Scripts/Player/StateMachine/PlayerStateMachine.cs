using System.Collections.Generic;

public class PlayerStateMachine : StateMachine<PlayerState, PlayerStateType>
{
    public PlayerStateMachine(Dictionary<PlayerStateType, PlayerState> allStates) : base(allStates) { }
}

public enum PlayerStateType
{
    Idle,
    Move,
    Attack,
    Roll,
    Hit,
    Stunned,
    Jump,
    Fall,
    Aim,
    Equip,
    Unequip,
    BowCharge,
    Crouch,
    Death,
    UI,
    Consume
}