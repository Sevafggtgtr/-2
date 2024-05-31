using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;


public class UIMainMenu : MonoBehaviour
{
    [SerializeField]
    private Button _settingsButton,
                   _exitButton,
                   _hostButton,
                   _clientButton;

    private static UIMainMenu _singleton;
    public static UIMainMenu Singleton => _singleton;

    [SerializeField]
    private GameObject _hostPanel,
                       _clientPanel;

    [SerializeField]
    private GameObject _mainCamera;

    [SerializeField]
    private InputField _nicknameInputField;
    public string Nickname => _nicknameInputField.text;

    private void Awake()
    {
        _singleton = this;
    }

    void Start()
    {
        _hostButton.onClick.AddListener(() =>
        {
            _hostPanel.SetActive(true);
        });        
        _clientButton.onClick.AddListener(() => _clientPanel.SetActive(true));
        _exitButton.onClick.AddListener(Application.Quit);
    }

    void Update()
    {
        
    }
}
