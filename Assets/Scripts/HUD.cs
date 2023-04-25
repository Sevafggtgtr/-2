using UnityEngine;
using Unity.Netcode;

public class HUD : MonoBehaviour
{
    private static HUD _singleton;
    public static HUD Singleton => _singleton;

    [SerializeField]
    private Animation _vignettAnimation;

    void Start()
    {
        NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>().Damaged += () => _vignettAnimation.Play();
    }

    void Update()
    {
        
    }

    private void Awake()
    {
        _singleton = this;
        gameObject.SetActive(false);
    }
}
