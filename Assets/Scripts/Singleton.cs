using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    protected static T _instance;
    public static T Instance => _instance;
    void Awake()
    {
        _instance = this as T;
    }

}
