using Unity.Netcode;

public class NetworkSingleton<T> : NetworkBehaviour where T : NetworkBehaviour
{
    protected static T _instance;
    public static T Instance => _instance ? _instance : FindFirstObjectByType<T>();

    private void Awake()
    {
        _instance = this as T;

        Initialize();
    }

    protected virtual void Initialize() { }
}
