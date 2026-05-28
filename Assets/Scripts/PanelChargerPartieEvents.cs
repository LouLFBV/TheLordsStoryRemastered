using UnityEngine;
using UnityEngine.UI;
public class PanelChargerPartieEvents : MonoBehaviour
{
    [SerializeField] Button buttonMenu, buttonLoadSave;

    private void Awake()
    {
        buttonMenu.image.raycastTarget = false;
        buttonLoadSave.image.raycastTarget = false;
    }
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


    public void AE_SetToUIState()
    {
        PlayerController.Instance.StateMachine.ChangeState(PlayerStateType.UI);
        buttonLoadSave.image.raycastTarget = true;
        buttonMenu.image.raycastTarget = true;
    }
}
