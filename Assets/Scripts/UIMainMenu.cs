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

    private static UIMainMenu _singleton;
    public static UIMainMenu Singleton => _singleton;

    [SerializeField]
    private UIPanel _hostPanel,
                    _clientPanel;

    [SerializeField]
    private GameObject _mainCamera;

    private AudioSource _audioSource;

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
            OpenPanel(_hostPanel);
        });
        //_clientButton.onClick.AddListener(() => OpenPanel(_clientPanel));
        _clientButton.onClick.AddListener(() => NetworkManager.Singleton.StartClient());
        _exitButton.onClick.AddListener(Application.Quit);

        _audioSource = GetComponent<AudioSource>();
    }

    public void PlaySound()
    {
        _audioSource.Play();
    }
}
