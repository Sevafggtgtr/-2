using UnityEngine.UI;
using UnityEngine;

public class UIMenuPanel : UIPanel
{
    #region Variables

    [SerializeField]
    private UIButton _hostButton,
                     _clientButton,
                     _settingsButton;

    private UIServerCreationPanel _hostPanel;
    private UIClientPanel _clientPanel;
    private UISettingsPanel _settingsPanel;

    [SerializeField]
    private InputField _nicknameInputField;
    public string Nickname => _nicknameInputField.text;

    #endregion

    protected override void OnStart()
    {
        _hostPanel = transform.parent.GetComponentInChildren<UIServerCreationPanel>(true);
        _clientPanel = transform.parent.GetComponentInChildren<UIClientPanel>(true);
        _settingsPanel = transform.parent.GetComponentInChildren<UISettingsPanel>(true);

        _hostButton.Clicked += () => UIMainMenu.Instance.OpenPanel(_hostPanel);
        _clientButton.Clicked += () => UIMainMenu.Instance.OpenPanel(_clientPanel);
        _settingsButton.Clicked += () => UIMainMenu.Instance.OpenPanel(_settingsPanel);
    }
}
