using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerManager : Singleton<ServerManager>
{
    public void Disconnect()
    {
        NetworkManager.Singleton.DisconnectClient(NetworkManager.Singleton.LocalClientId);

        SceneManager.LoadScene(0);
    }

    public void Shutdown()
    {
        NetworkManager.Singleton.Shutdown();

    }

    private void Start()
    {
        DontDestroyOnLoad(this);
    }
}
