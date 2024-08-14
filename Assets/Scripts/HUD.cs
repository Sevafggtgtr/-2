using UnityEngine;
using Unity.Netcode;
using UnityEngine.Events;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    private static HUD _singleton;
    public static HUD Singleton => _singleton;

    [SerializeField]
    private Animation _vignetteAnimation,
                      _blindnessAnimation;

    [SerializeField]
    private Slider _healthBar;
    public Slider HealthBar => _healthBar;

    [SerializeField]
    private UIPauseMenu _pauseMenu;
    public UIPauseMenu PauseMenu => _pauseMenu;

    [SerializeField]
    private UIKillfeedPanel _killfeed;

    private GameObject _panel;

    [SerializeField]
    private UIChoiceTeamPanel _chooseTeamPanel;
    public UIChoiceTeamPanel ChooseTeamPanel => _chooseTeamPanel;

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

    public void Blindness()
    {
        _blindnessAnimation.Play();
    }

    private void Awake()
    {
        PlayerController.Spawn += (player) =>
        {
            if (player.IsOwner)
            {
                HealthBar.value = 100;

                player.Damaged += () =>
                {
                    _vignetteAnimation.Play();

                    HealthBar.value = PlayerController.Singleton.Health;
                };
                player.Died += (killer) =>
                {
                    Cursor.visible = !Cursor.visible;
                    Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;

                    if (_panel != null)
                        _panel.SetActive(false);
                };
            }         
            player.Died += (killer) =>
            {
                _killfeed.SpawnSlot(killer, player.GetPlayer());
            };
        };

        _singleton = this;
    }
}
