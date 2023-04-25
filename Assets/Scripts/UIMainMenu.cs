using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;


public class UIMainMenu : MonoBehaviour
{
    [SerializeField]
    private Button _singleGameButton,
                   _networkGameButton,
                   _settingsButton,
                   _exitButton,
                   _hostButton,
                   _clientButton;

    [SerializeField]
    private GameObject _networkPanel;

    [SerializeField]
    private GameObject _mainCamera;


    void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += (ID) =>
        {
            HUD.Singleton.gameObject.SetActive(true);
            _mainCamera.SetActive(false);
            gameObject.SetActive(false);
        };

        _singleGameButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
        });
        _networkGameButton.onClick.AddListener(() => _networkPanel.SetActive(true));
        _exitButton.onClick.AddListener(Application.Quit);
        _hostButton.onClick.AddListener(() => 
        {
            NetworkManager.Singleton.StartHost();
        });
        _clientButton.onClick.AddListener(() => 
        {
            NetworkManager.Singleton.StartClient();
        });
    }

    void Update()
    {
        
    }
}
