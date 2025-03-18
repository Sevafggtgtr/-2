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
    private UISettings _settingsPanel;

    #endregion

    #region Methods

    protected override void OnStart()
    {
        _continueButton.OnClick += Continue;
        _disconnectButton.OnClick += ServerManager.Instance.Disconnect;
        _settingsButton.OnClick += () => HUD.Instance.OpenPanel(_settingsPanel.gameObject);
    }

    private void Continue()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        gameObject.SetActive(false);
    }

    #endregion
}
