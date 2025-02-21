using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;


public class UIMainMenu : UIManager
{
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
    private GameObject _mainCamera;

    [SerializeField]
    private InputField _nicknameInputField;
    public string Nickname => _nicknameInputField.text;

    void Start()
    {
        _hostButton.onClick.AddListener(() =>
        {
            OpenPanel(_hostPanel.gameObject);
        });
        _clientButton.onClick.AddListener(() => OpenPanel(_clientPanel.gameObject));
        _exitButton.onClick.AddListener(Application.Quit);
        _settingsButton.onClick.AddListener(() => OpenPanel(_settingsPanel.gameObject));
    }
}
