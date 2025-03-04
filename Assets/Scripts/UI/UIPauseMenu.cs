using UnityEngine;
using UnityEngine.UI;

public class UIPauseMenu : UIPanel
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
        _continueButton.OnClick += Continue;
        _disconnectButton.OnClick += ServerManager.Instance.Disconnect;
    }

    private void Continue()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        gameObject.SetActive(false);
    }

    #endregion
}
