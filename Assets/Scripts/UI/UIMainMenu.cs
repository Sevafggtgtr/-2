using UnityEngine;
using UnityEngine.UI;

public class UIMainMenu : UIManager
{
    #region Variables

    [SerializeField]
    private Button _settingsButton,
                   _exitButton,
                   _hostButton,
                   _clientButton;

    [SerializeField]
    private UIPanel _hostPanel,
                    _clientPanel,
                    _settingsPanel;


    [SerializeField]
    private InputField _nicknameInputField;
    public string Nickname => _nicknameInputField.text;

    #endregion

    protected override void Initialize()
    {
        base.Initialize();

        _hostButton.onClick.AddListener(() => OpenPanel(_hostPanel.gameObject));
        _clientButton.onClick.AddListener(() => OpenPanel(_clientPanel.gameObject));
        _exitButton.onClick.AddListener(Application.Quit);
        _settingsButton.onClick.AddListener(() => OpenPanel(_settingsPanel.gameObject));
    }
}
