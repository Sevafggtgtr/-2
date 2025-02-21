using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.Events;

public class UIPauseMenu : UIPanel
{
    [SerializeField]
    private Button _exitButton,
                   _settingsButton,
                   _continueButton;

    protected override void OnStart()
    {
        _exitButton.onClick.AddListener(ServerManager.Instance.Disconnect);

        _continueButton.onClick.AddListener(Continue);
    }

    private void Continue()
    {
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
