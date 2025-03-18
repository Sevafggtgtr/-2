using Unity.Netcode;
using UnityEngine.SceneManagement;

public class ServerManager : NetworkSingleton<ServerManager>
{
    public void Disconnect()
    {        
        if(IsHost)
            NetworkManager.Singleton.Shutdown();
        else
            NetworkManager.Singleton.DisconnectClient(NetworkManager.Singleton.LocalClientId);

        SceneManager.LoadScene(0);
    }

    public void Shutdown()
    {
       
    }

    private void Start()
    {
        DontDestroyOnLoad(this);
    }
}
