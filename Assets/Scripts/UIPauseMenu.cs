using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.Events;

public class UIPauseMenu : MonoBehaviour
{
    public event UnityAction UnPaused;

    [SerializeField]
    private Button _exitButton,
                   _settingsButton,
                   _continueButton;

    void Start()
    {
        _exitButton.onClick.AddListener(DisconnectRpc);

        _continueButton.onClick.AddListener(Continue);
    }

    [ServerRpc]
    private void DisconnectRpc()
    {
        NetworkManager.Singleton.DisconnectClient(NetworkManager.Singleton.LocalClientId);
    }

    private void Continue()
    {
        UnPaused.Invoke();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        gameObject.SetActive(false);
    }

    [ServerRpc]
    private void SettingsRpc()
    {

    }

    void Update()
    {
        
    }
}
