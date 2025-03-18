using UnityEngine;
using UnityEngine.UI;

public class UIPauseMenu : UIPanel
{
    #region Variables

    [SerializeField]
    private UIButton _continueButton,
                     _settingsButton,
                     _disconnectButton;

    [SerializeField]
    private UISettingsPanel _settingsPanel;

    #endregion

    #region Methods

    protected override void OnStart()
    {
        _continueButton.Clicked += Continue;
        _disconnectButton.Clicked += ServerManager.Instance.Disconnect;
        _settingsButton.Clicked += () => HUD.Instance.OpenPanel(_settingsPanel);
    }

    private void Continue()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        gameObject.SetActive(false);
    }

    #endregion
}