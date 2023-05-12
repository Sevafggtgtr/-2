using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class UIRespawnMenu : MonoBehaviour
{
    [SerializeField]
    private Button _respawnButton,
                   _exitButton;

    void Start()
    {
        _exitButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.DisconnectClient(NetworkManager.Singleton.LocalClientId);
        });
    }

    void Update()
    {
        
    }
}
