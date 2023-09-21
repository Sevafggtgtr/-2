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

    [SerializeField]
    private UIKillfeedPanel _killfeed;

    private GameObject _panel;

    [SerializeField]
    private UIChoiceTeamPanel _chooseTeamPanel;
    public UIChoiceTeamPanel ChooseTeamPanel => _chooseTeamPanel;

    void Start()
    {
        
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && (!_panel || _panel == _pauseMenu.gameObject))
        {
            
            Cursor.visible = !Cursor.visible;
            Cursor.lockState = Cursor.visible ?CursorLockMode.None : CursorLockMode.Locked;

            _pauseMenu.gameObject.SetActive(!_pauseMenu.gameObject.activeSelf);
            
            _panel = _pauseMenu.gameObject;
        }
    }

    private void Awake()
    {
        PlayerController.Spawn += (player) =>
        {
            if (player.IsOwner)
            {
                NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>().Damaged += () =>
                {
                    _vignettAnimation.Play();

                    HealthBar.value = PlayerController.Singleton.Health;
                };
                NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>().Died += (killer) =>
                {
                    Cursor.visible = !Cursor.visible;
                    Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;

                    if (_panel != null)
                        _panel.SetActive(false);

                    _panel = _respawnMenu.gameObject;

                    _respawnMenu.gameObject.SetActive(true);
                };
            }         
            player.Died += (killer) =>
            {
                _killfeed.SpawnSlot(killer, player.Name);
            };
        };

        _respawnMenu.Respawn += () =>
        {
            _panel = null;

            _healthBar.value = 100;
        };

        _singleton = this;
        gameObject.SetActive(false);
    }
}
