using UnityEngine;
using Unity.Netcode;
using UnityEngine.Events;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    private static HUD _singleton;
    public static HUD Singleton => _singleton;  

    [SerializeField]
    private Animation _vignettAnimation;

    [SerializeField]
    private Slider _healthBar;
    public Slider HealthBar => _healthBar;


    [SerializeField]
    private UIRespawnMenu _respawnMenu;
    public UIRespawnMenu RespawnMenu => _respawnMenu;

    [SerializeField]
    private UIPauseMenu _pauseMenu;
    public UIPauseMenu PauseMenu => _pauseMenu;

    private GameObject _panel;

    void Start()
    {
        NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>().Damaged += () => _vignettAnimation.Play();

        NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>().Died += (killer) =>
        {
            Cursor.visible = !Cursor.visible;
            Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;

            _panel = _respawnMenu.gameObject;

            _respawnMenu.gameObject.SetActive(true);
        };

        _respawnMenu.Respawn += () => _panel = null;
    }   

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !_panel)
        {          
            Cursor.visible = !Cursor.visible;
            Cursor.lockState = Cursor.visible ?CursorLockMode.None : CursorLockMode.Locked;

            _pauseMenu.gameObject.SetActive(!_pauseMenu.gameObject.activeSelf);
        }
    }

    private void Awake()
    {
        _singleton = this;
        gameObject.SetActive(false);
    }
}
