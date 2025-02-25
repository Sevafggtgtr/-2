using UnityEngine;
using UnityEngine.UI;

public class HUD : UIManager
{
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
    private UIChoiceTeamPanel _chooseTeamPanel;
    public UIChoiceTeamPanel ChooseTeamPanel => _chooseTeamPanel;

    [SerializeField]
    private UIWeaponPanel _gunPanel;
    public UIWeaponPanel GunPanel => _gunPanel;

    private void Awake()
    {
        Singleton = this;
    }

    new void Update()
    {
        if (_panel)
            base.Update();
        else if (Input.GetKeyDown(KeyCode.Escape))
            OpenPanel(_pauseMenu.gameObject);

        if (Input.GetKeyDown(KeyCode.B))
            OpenPanel(_weaponStore.gameObject);
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
            void OnPlayerDamaged(Player player)
            {
                _vignetteAnimation.Play();

                HealthBar.value = player.Health.Value;
            }

            void OnPlayerDied(Player player)
            {
                _playerControllerPanel.SetActive(false);
            }

            void OnPlayerBalanceOnValueChanged(Player player)
            {
                _balanceText.text = "$" + player.Balance.Value.ToString();
            }

            if (previousPlayer)
            {
                previousPlayer.Damaged -= ()
                    => OnPlayerDamaged(previousPlayer);

                previousPlayer.Died -= (killer, causeCode)
                    => OnPlayerDied(previousPlayer);

                previousPlayer.Balance.OnValueChanged -= (previousValue, newValue)
                    => OnPlayerBalanceOnValueChanged(previousPlayer);
            }

            OnPlayerBalanceOnValueChanged(newPlayer);

            _playerControllerPanel.SetActive(true);

            HealthBar.value = newPlayer.Health.Value;

            _gunPanel.Initialize(previousPlayer, newPlayer);

            newPlayer.Balance.OnValueChanged += (previousValue, newValue)
                => OnPlayerBalanceOnValueChanged(newPlayer);

            newPlayer.Damaged += ()
                => OnPlayerDamaged(newPlayer);

            newPlayer.Died += (killer, causeCode)
                => OnPlayerDied(newPlayer);
        };
    }
}
