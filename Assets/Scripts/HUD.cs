using UnityEngine;
using UnityEngine.UI;

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
    private GameObject _playerControllerPanel;

    [SerializeField]
    private UIChoiceTeamPanel _chooseTeamPanel;
    public UIChoiceTeamPanel ChooseTeamPanel => _chooseTeamPanel;

    [SerializeField]
    private UIGunPanel _gunPanel;
    public UIGunPanel GunPanel => _gunPanel;

    private Player _player;

    new void Update()
    {
        if (_panel)
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

    void OnPlayerControllerDamaged()
    {
        _vignetteAnimation.Play();

        HealthBar.value = PlayerController.Singleton.Health.Value;
    }

    void OnPlayerControllerDied()
    {

    }

    void OnPlayerBalanceOnValueChanged()
    {
        _balanceText.text = "$" + _player.Balance.Value.ToString();
    }

    public void SetPlayer(Player player)
    {
        if (player.Controller.Value.TryGet(out PlayerController playerController))
        {
            HealthBar.value = playerController.Health.Value;

            _gunPanel.SetPlayer(playerController);

            playerController.Damaged += () =>
            {
                OnPlayerControllerDamaged();
            };
            playerController.Died += (killer, causeCode) =>
            {
                OnPlayerControllerDied();
            };
        }

        _balanceText.text = "$" + player.Balance.Value.ToString();

        player.Balance.OnValueChanged += (pv, nv) =>
        {
            OnPlayerBalanceOnValueChanged();
        };
    }

    public void RemovePlayer(Player player)
    {
        if (player.Controller.Value.TryGet(out PlayerController playerController))
        {
            _gunPanel.RemovePlayer(playerController);

            playerController.Damaged -= () =>
            {
                OnPlayerControllerDamaged();
            };
            playerController.Died -= (killer, causeCode) =>
            {
                OnPlayerControllerDied();
            };
        }

        player.Balance.OnValueChanged -= (pv, nv) =>
        {
            OnPlayerBalanceOnValueChanged();
        };
    }


    private void Start()
    {
        _playerControllerPanel.SetActive(false);

        Closed += ClosePanel;
    }

    private void Awake()
    {
        _singleton = this;
    }
}
