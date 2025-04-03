using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public enum Teams
{
    Spectator,
    Terrorist,
    CounterTerrorist
}

[System.Serializable]
public struct TeamData
{
    [SerializeField]
    private string _name;
    public string Name => _name;

    [SerializeField]
    private Color _color;
    public Color Color => _color;

    [SerializeField]
    private Teams _team;
    public Teams Team => _team;

    [SerializeField]
    private Texture[] _playerSkins;
    public Texture[] PlayerSkins => _playerSkins;
}

public class GameManager : NetworkSingleton<GameManager>
{
    #region Variables

    #region Events

    public event UnityAction RoundStarted = delegate { };
    public event UnityAction<Teams> RoundFinished = delegate { };
    public event UnityAction<Player> PlayerConnected = delegate { }, PlayerDisconnect = delegate { };


    #endregion

    [SerializeField]
    private MapData[] _maps;
    public MapData[] Maps => _maps;

    [SerializeField]
    private GameMode _gameMode;
    public GameMode GameMode => _gameMode;

    public Dictionary<Teams, int> Points { get; private set; }

    #region Team Data

    [SerializeField]
    private TeamData[] _teamDatas;
    public TeamData[] TeamDatas => _teamDatas;
    public TeamData GetTeamData(Teams team)
        => _teamDatas.First(team_ => team_.Team == team);

    #endregion

    #region Players

    private NetworkList<NetworkBehaviourReference> _players = new NetworkList<NetworkBehaviourReference>();
    public NetworkList<NetworkBehaviourReference> Players => _players;
    public Player[] TryGetPlayers()
    {
        var players = new Player[_players.Count];
        for (int i = 0; i < players.Length; i++)
            players[i] = _players[i].TryGet(out Player player) ? player : null;

        return players;
    }
    public Player GetPlayer(ulong id)
        => TryGetPlayers().First(player => id == player.OwnerClientId);

    private NetworkVariable<bool> _isPlayersActive = new NetworkVariable<bool>();
    public NetworkVariable<bool> IsPlayersActive => _isPlayersActive;

    #endregion

    [SerializeField]
    private WeaponData _weaponData;
    public WeaponData WeaponData => _weaponData;

    public Config Config { get; private set; }

    private NetworkVariable<int> _time = new NetworkVariable<int>();
    public NetworkVariable<int> Time => _time;

    private Coroutine _timerCoroutine;

    public bool IsActive = true;

    public Player Player { get; private set; }

    #endregion

    #region Methods

    protected override void Initialize()
    {
        DontDestroyOnLoad(gameObject);

        Points = new Dictionary<Teams, int> { { Teams.Terrorist, 0 }, { Teams.CounterTerrorist, 0 } };

        Config = Config.Load();
    }

    private void Start()
    {
        NetworkManager.OnServerStarted += () =>
        {
            StartWarmUpServerRpc();

            NetworkManager.Singleton.OnClientStarted += () =>
            {

            };

            NetworkManager.OnClientConnectedCallback += (id) =>
            {
                var player = NetworkManager.ConnectedClients[id].PlayerObject.GetComponent<Player>();

                _players.Add(player);

                OnPlayerConnectedClientRpc(player);
            };

            NetworkManager.OnClientDisconnectCallback += (id) =>
            {
                var player = GetPlayer(id);

                _players.Remove(player);

                OnPlayerDisconnectClientRpc(player);
            };
        };
    }

    [ClientRpc]
    private void OnPlayerConnectedClientRpc(NetworkBehaviourReference player)
    {
        print(OwnerClientId);
        if (player.TryGet(out Player playerObject))
        {
            PlayerConnected.Invoke(playerObject);

            if (playerObject.IsOwner)
                Player = playerObject;
        }
    }

    [ClientRpc]
    private void OnPlayerDisconnectClientRpc(NetworkBehaviourReference player)
    {
        if (player.TryGet(out Player playerObject))
            PlayerDisconnect.Invoke(playerObject);
    }

    #region Warmup

    [ServerRpc]
    private void StartWarmUpServerRpc()
    {
        ClearMapServerRpc();

        _isPlayersActive.Value = true;

        void OnPlayerConnected(Player player)
        {
            player.TeamChanged += () =>
            {
                SpawnPlayerServerRpc(player.OwnerClientId);

                player.Died += killer => OnDied(player);
            };
        };

        void OnDied(Player player)
        {
            if (player.TryGetController())
                player.TryGetController().NetworkObject.Despawn();

            SpawnPlayerServerRpc(player.OwnerClientId);
        }

        _timerCoroutine = StartCoroutine(Timer(GameMode.WarmupTime, () =>
        {
            PlayerConnected -= OnPlayerConnected;

            foreach (var player in TryGetPlayers())
                player.Died -= killer => OnDied(player);

            FinishWarmUpServerRpc();
        }));

        PlayerConnected += OnPlayerConnected;

        StartRoundClientRpc();
    }

    [ServerRpc]
    private void FinishWarmUpServerRpc()
    {
        void OnDied()
        {
            var teams = new Dictionary<Teams, int> { { Teams.Terrorist, 0 }, { Teams.CounterTerrorist, 0 } };
            var players = TryGetPlayers();

            foreach (var player in players)
                if (player.TryGetController() && player.TryGetController().Health.Value != 0)
                    teams[player.Team.Value]++;

            if (teams.ContainsValue(0))
            {
                var winningTeam = teams.OrderBy(team => team.Value).Last().Key;

                foreach (var player in players)
                {
                    player.Balance.Value += player.Team.Value == winningTeam ? GameMode.WinAward : GameMode.LossAward;
                }

                if (++Points[winningTeam] == _gameMode.RoundCount / 2 + 1)
                {
                    foreach (var player in players)
                        if (player.TryGetController())
                            player.TryGetController().NetworkObject.Despawn();
                }

                FinishRoundTimeServerRpc();
                FinishRoundClientRpc(winningTeam);
            }

        }

        foreach (var player in TryGetPlayers())
        {
            player.Died += killer => OnDied();
        }

        PlayerConnected += player => OnDied();
    }

    #endregion

    #region Round

    [ServerRpc]
    private void StartRoundServerRpc()
    {
        ClearMapServerRpc();

        _isPlayersActive.Value = false;

        _timerCoroutine = StartCoroutine(Timer(GameMode.BuyTime, FinishBuyTimeServerRpc));

        foreach (var player in TryGetPlayers())
        {
            SpawnPlayerServerRpc(player.OwnerClientId);
        }

        StartRoundClientRpc();
    }

    [ClientRpc]
    private void StartRoundClientRpc()
    {
        RoundStarted?.Invoke();
    }

    [ServerRpc]
    public void FinishBuyTimeServerRpc()
    {
        _timerCoroutine = StartCoroutine(Timer(GameMode.RoundTime, FinishRoundTimeServerRpc));

        _isPlayersActive.Value = true;
    }

    [ServerRpc]
    public void FinishRoundTimeServerRpc()
    {
        _timerCoroutine = StartCoroutine(Timer(GameMode.RoundEndTime, StartRoundServerRpc));

        /*foreach (var player in NetworkManager.ConnectedClients)
        {
            player.Value.PlayerObject.GetComponent<Player>().Balance.Value += ;
        }*/
    }

    [ClientRpc]
    private void FinishRoundClientRpc(Teams winningTeam)
    {
        RoundFinished.Invoke(winningTeam);
    }

    #endregion

    [ServerRpc]
    private void ClearMapServerRpc()
    {
        foreach (var networkObject in FindObjectsByType<NetworkObject>(FindObjectsSortMode.None))
        {
            if (!networkObject.IsPlayerObject && !networkObject.IsSceneObject.Value)
            {
                print(networkObject.name);

                networkObject.Despawn();
            }
        }
    }

    [ServerRpc]
    private void SpawnPlayerServerRpc(ulong id)
    {
        var player = GetPlayer(id);

        var spawnpoints = Map.Singleton.GetTeamSpawnPoints(player.Team.Value).SpawnPoints.Where(spawnPoint => !Physics.OverlapSphere(spawnPoint.position, 1).Any(collider => collider.GetComponent<PlayerController>())).ToArray();
        var spawnpoint = spawnpoints[Random.Range(0, spawnpoints.Length)];

        player.Spawn(spawnpoint.position, spawnpoint.rotation);
    }

    private IEnumerator Timer(int time, UnityAction callback)
    {
        _time.Value = time;

        while (_time.Value > 0)
        {
            yield return new WaitForSeconds(1);

            _time.Value--;
        }

        callback.Invoke();
    }

    #endregion
}

