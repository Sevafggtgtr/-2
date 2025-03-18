using UnityEngine;
using UnityEngine.UI;

public class HUD : UIManager
{
    #region Variables

    new public static HUD Instance => UIManager.Instance as HUD;

    [SerializeField]
    private UITeamSelectionPanel _chooseTeamPanel;
    public UITeamSelectionPanel ChooseTeamPanel => _chooseTeamPanel;

    [SerializeField]
    private UIPlayerTable _playerTable;

    [SerializeField]
    private UIKillfeedPanel _killfeed;

    [SerializeField]
    private UIPausePanel _pauseMenu;
    public UIPausePanel PauseMenu => _pauseMenu;

    [Header("Player Settings")]
    [SerializeField]
    private Animation _vignetteAnimation;
    [SerializeField]
    private UIBlindness _blindness;
    [SerializeField]
    private Text _balanceText;
    [Header("Player Controller Settings")]
    [SerializeField]
    private GameObject _playerControllerPanel;
    [SerializeField]
    private Slider _healthBar;
    [SerializeField]
    private UIWeaponPanel _weaponPanel;
    [SerializeField]
    private UIWeaponStore _weaponStore;

    #endregion

    #region Methods

    private void Start()
    {
        _playerControllerPanel.SetActive(false);

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

            void OnPlayerControllerHealthValueChanged(int value)
            {
                _vignetteAnimation.Play();

                _healthBar.value = value;
            }

            void OnPlayerControllerDied(PlayerController playerController)
            {
                _playerControllerPanel.SetActive(false);
            }

            if (previousPlayer)
            {
                previousPlayer.Balance.OnValueChanged -= (previousValue, newValue)
                    => OnPlayerBalanceOnValueChanged(previousPlayer);

                var previousPlayeController = previousPlayer.TryGetController();
                previousPlayeController.Health.OnValueChanged -= (previousValue, newValue)
                    => OnPlayerControllerHealthValueChanged(newValue);
                previousPlayeController.Died -= (killer)
                    => OnPlayerControllerDied(previousPlayeController);
            }

            OnPlayerBalanceOnValueChanged(newPlayer);

            _playerControllerPanel.SetActive(true);

            _weaponPanel.Initialize(previousPlayer, newPlayer);

            newPlayer.Balance.OnValueChanged += (previousValue, newValue)
                => OnPlayerBalanceOnValueChanged(newPlayer);

            var newPlayerController = newPlayer.TryGetController();
            _healthBar.value = newPlayerController.Health.Value;
            newPlayerController.Health.OnValueChanged += (previousValue, newValue)
                => OnPlayerControllerHealthValueChanged(newValue);
            newPlayerController.Died += (killer)
                => OnPlayerControllerDied(newPlayerController);
        };
    }

    private void SetActive(bool value)
    {
        Cursor.visible = value;
        Cursor.lockState = value ? CursorLockMode.Locked: CursorLockMode.None;

        GameManager.Instance.IsActive = value;
    }

    public override void OpenPanel(UIPanel panel)
    {
        base.OpenPanel(panel);

        SetActive(false);
    }

    public override void ClosePanel()
    {
        base.ClosePanel();

        SetActive(true);
    }

    public void Blind(float duration)
    {
        _blindness.Activate(duration);
    }

    protected override void Update()
    {
        if (_panel)
            base.Update();
        else if (Input.GetKeyDown(KeyCode.Escape))
            OpenPanel(_pauseMenu);

        if (Input.GetKeyDown(KeyCode.B))
            OpenPanel(_weaponStore);

        if (Input.GetKeyDown(KeyCode.Tab))
            OpenPanel(_playerTable);
        if (Input.GetKeyUp(KeyCode.Tab))
            ClosePanel();

        if (Input.GetKeyDown(KeyCode.M))
            OpenPanel(_chooseTeamPanel);
    }

    #endregion
}
