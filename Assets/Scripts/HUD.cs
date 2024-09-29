using UnityEngine;
using Unity.Netcode;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.AI;

public class HUD : UIManager
{
    private static HUD _singleton;
    public static HUD Singleton => _singleton;

    [SerializeField]
    private Animation _vignetteAnimation;

    [SerializeField]
    private UIBlindness _blindness;

    [SerializeField]
    private Slider _healthBar;
    public Slider HealthBar => _healthBar;

    [SerializeField]
    private UIPauseMenu _pauseMenu;
    public UIPauseMenu PauseMenu => _pauseMenu;

    [SerializeField]
    private UIKillfeedPanel _killfeed;

    [SerializeField]
    private UIWeaponStore _weaponStore;

    [SerializeField]
    private Text _balanceText;

    [SerializeField]
    private UIChoiceTeamPanel _chooseTeamPanel;
    public UIChoiceTeamPanel ChooseTeamPanel => _chooseTeamPanel;

    new void Update()
    {
        if(_panel)
            base.Update();
        else if (Input.GetKeyDown(KeyCode.Escape))
            OpenPanel(_pauseMenu);
        
        if (Input.GetKeyDown(KeyCode.B))
            OpenPanel(_weaponStore);                    
    }

    public void Blindness(float time)
    {
        _blindness.Activate(time);
    }

    private void OpenPanel(UIPanel panel)
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        base.OpenPanel(panel);

        PlayerController.Singleton.IsActive = false;
    }

    private void ClosePanel()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        PlayerController.Singleton.IsActive = true;
    }

    private void Start()
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

                };
            }
            player.Died += (killer) =>
            {
                _killfeed.SpawnSlot(killer, player.GetPlayer());
            };
        };

        Player.Singleton.Balance.OnValueChanged += (pv, nv) =>
        {

            _balanceText.text = "$" + nv.ToString();
        };

        Closed += ClosePanel;
    }

    private void Awake()
    {       
        _singleton = this;
    }
}
