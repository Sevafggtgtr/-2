using UnityEngine;

public class UIPausePanel : UIPanel
{
    #region Variables

    [SerializeField]
    private UIButton _continueButton,
                     _settingsButton,
                     _disconnectButton;

    #endregion

    #region Methods

    protected override void OnStart()
    {
        _continueButton.Clicked += HUD.Instance.ClosePanel;
        _disconnectButton.Clicked += ServerManager.Instance.Disconnect;
    }

    #endregion
}
