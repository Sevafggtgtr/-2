using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
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
