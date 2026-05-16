using UnityEngine;

public class PanelChargerPartieEvents : MonoBehaviour
{
    public void OnOpenAnimationFinished()
    {
        Menu.Instance.OnOpenAnimationFinished();
    }

    public void AE_ActiveAnimIcone()
    {
        TransitionPanel.Instance.SetLoadingIconVisible(1);
    }
    public void AE_DesactiveAnimIcone()
    {
        TransitionPanel.Instance.SetLoadingIconVisible(0);
    }


    public void AE_SetToUIState() => PlayerController.Instance.StateMachine.ChangeState(PlayerStateType.UI);
}
