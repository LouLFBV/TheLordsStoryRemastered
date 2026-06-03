using UnityEngine;
public class PlayerUIState : PlayerState
{
    public PlayerUIState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        base.Enter(); 
        if (player.RequestedPanelType != UIPanelType.Dialogue)
        {
            
            Time.timeScale = 0f;
            UIManagerSystem.Instance.OpenPanel(player.RequestedPanelType);
        }
        if (player.Animator != null)
        {
            player.Animator.SetFloat("Speed", 0f);
            // Si tu as des paramètres Horizontal/Vertical, mets les aussi à 0
        }

        player.Input.SwitchActionMap("UI");
        player.OnOpenUIEvent(true);
        UIManagerSystem.Instance.ToggleCursor(true);
    }

    public override void Update()
    {
        // Si on réappuie sur Inventaire ou Cancel pendant qu'on est dans cet état
        if (player.Input.InventoryPressed || player.Input.CancelPressed)
        {
            player.StateMachine.ChangeState(PlayerStateType.Idle);
        }
    }

    public override void Exit()
    {
        base.Exit();
        if (player.RequestedPanelType != UIPanelType.Dialogue)
        {
            Time.timeScale = 1f;
            UIManagerSystem.Instance.CloseAll();
        }
        player.RequestedPanelType = UIPanelType.None;
        player.Input.SwitchActionMap("Player");
        player.OnOpenUIEvent(false);
        UIManagerSystem.Instance.ToggleCursor(false);
        Tooltip.Instance.Hide();
        InventorySystem.instance.itemActionsSystem.CloseActionPanel();
    }
}

public enum UIPanelType
{
    Inventory,
    Equipment,
    Map,
    Quests,
    PauseMenu,
    Options,
    Commandes,
    Dialogue,
    None
}