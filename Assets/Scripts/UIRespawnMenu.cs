using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class UIRespawnMain : MonoBehaviour
{
    [SerializeField]
    private Button _respawnButton,
                   _exitButton;

    void Start()
    {
        _exitButton.onClick.AddListener(DisconnectRpc);

        _respawnButton.onClick.AddListener(RespawnRpc);       
    }

    [ServerRpc]
    private void DisconnectRpc()
    {
        NetworkManager.Singleton.DisconnectClient(NetworkManager.Singleton.LocalClientId);       
    }

    [ServerRpc]
    private void RespawnRpc()
    {
        var player = Instantiate(NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject());

        player.SpawnAsPlayerObject(NetworkManager.Singleton.LocalClientId);
    }

    void Update()
    {
        
    }
}
