using UnityEngine;
using UnityEngine.UI;

public class HUD : UIManager
{
    #region Variables

    public static HUD Singleton { get; private set; }

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
    private UITeamSelectionPanel _chooseTeamPanel;
    public UITeamSelectionPanel ChooseTeamPanel => _chooseTeamPanel;

    [SerializeField]
    private UIWeaponPanel _gunPanel;
    public UIWeaponPanel GunPanel => _gunPanel;

    [SerializeField]
    private UIPlayerTable _playerTable;

    #endregion

    #region Methods

    private void Awake()
    {
        Singleton = this;
    }

    protected override void Update()
    {
        if (_panel)
            base.Update();
        else if (Input.GetKeyDown(KeyCode.Escape))
            OpenPanel(_pauseMenu.gameObject);

        if (Input.GetKeyDown(KeyCode.B))
            OpenPanel(_weaponStore.gameObject);

        if(Input.GetKeyDown(KeyCode.Tab))
            OpenPanel(_playerTable.gameObject);
        if (Input.GetKeyUp(KeyCode.Tab))
            ClosePanel();

        if(Input.GetKeyDown(KeyCode.M))
            OpenPanel(_chooseTeamPanel.gameObject);
    }

    public void Blindness(float time)
    {
        _blindness.Activate(time);
    }

    private void Start()
    {
        _playerControllerPanel.SetActive(false);

        Closed += () =>
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            GameManager.Instance.IsActive = true;
        };
        Opened += () =>
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            GameManager.Instance.IsActive = false;
        }; ;

        var roundFinishPanel = GetComponentInChildren<UIRoundFinishPanel>(true);

        GameManager.Instance.RoundStarted += () =>
        {
            roundFinishPanel.gameObject.SetActive(false);
        };

        GameManager.Instance.RoundFinished += (team) =>
        {
            roundFinishPanel.gameObject.SetActive(true);

            roundFinishPanel.Initialize(team);
        };

        Spectator.Instance.PlayerChanged += (previousPlayer, newPlayer) =>
        {
            void OnPlayerBalanceOnValueChanged(Player player)
            {
                _balanceText.text = "$" + player.Balance.Value.ToString();
            }

            void OnPlayerControllerDamaged(PlayerController playerController)
            {
                _vignetteAnimation.Play();

                HealthBar.value = playerController.Health.Value;
            }

            void OnPlayerControllerDied(PlayerController playerController)
            {
                _playerControllerPanel.SetActive(false);
            }

            if (previousPlayer)
            {
                previousPlayer.Balance.OnValueChanged -= (previousValue, newValue)
                    => OnPlayerBalanceOnValueChanged(previousPlayer);

                var previousPlayeController = previousPlayer.Controller;
                previousPlayeController.Damaged -= ()
                    => OnPlayerControllerDamaged(previousPlayeController);
                previousPlayeController.Died -= (killer)
                    => OnPlayerControllerDied(previousPlayeController);
            }

            OnPlayerBalanceOnValueChanged(newPlayer);

            _playerControllerPanel.SetActive(true);

            _gunPanel.Initialize(previousPlayer, newPlayer);

            newPlayer.Balance.OnValueChanged += (previousValue, newValue)
                => OnPlayerBalanceOnValueChanged(newPlayer);

            var newPlayerController = newPlayer.Controller;
            HealthBar.value = newPlayerController.Health.Value;
            newPlayerController.Damaged += ()
                => OnPlayerControllerDamaged(newPlayerController);
            newPlayerController.Died += (killer)
                => OnPlayerControllerDied(newPlayerController);
        };
    }

    #endregion
}
