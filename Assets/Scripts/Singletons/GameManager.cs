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

public class GameManager : Singleton<GameManager>
{
    #region Events

    public event UnityAction<Teams> RoundFinished;
    public event UnityAction RoundStarted;
    public event UnityAction<Player> PlayerConnected, PlayerDisconnect;

    #endregion

    #region Variables

    [SerializeField]
    private MapData[] _maps;
    public MapData[] Maps => _maps;

    [SerializeField]
    private GameMode _gameMode;
    public GameMode GameMode => _gameMode;

    [SerializeField]
    private Transform _entities;

    public NetworkList<NetworkBehaviourReference> NetworkPlayers = new NetworkList<NetworkBehaviourReference>();
    private List<Player> _players;

    public Player GetPlayer(ulong id)
        => _players.First(player => id == player.OwnerClientId);  

    private NetworkVariable<bool> _isPlayersActive = new NetworkVariable<bool>();
    public NetworkVariable<bool> IsPlayersActive => _isPlayersActive;

    [SerializeField]
    private TeamData[] _teamDatas;
    public TeamData[] TeamDatas => _teamDatas;

    [SerializeField]
    private WeaponData _weaponData;
    public WeaponData WeaponData => _weaponData;

    public TeamData GetTeamData(Teams team)
        => _teamDatas.First(team_ => team_.Team == team);

    public Dictionary<Teams, int> Points { get; private set; }

    private NetworkVariable<int> _time = new NetworkVariable<int>();
    public NetworkVariable<int> Time => _time;

    private Coroutine _timerCoroutine;

    public bool IsActive = true;

    [SerializeField]
    private Player _playerPrefab;
    public Player Player { get; private set; }

    #endregion

    #region Methods

    protected override void Initialize()
    {
        Points = new Dictionary<Teams, int> { { Teams.Terrorist, 0 }, { Teams.CounterTerrorist, 0 } };

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        NetworkManager.OnServerStarted += () =>
        {
            _players = new List<Player>();

            StartWarmUpServerRpc();

            NetworkManager.OnClientStarted += () =>
            {

            };

            NetworkManager.OnClientConnectedCallback += (id) =>
            {
                //var player = Instantiate(_playerPrefab);
                //player.NetworkObject.SpawnAsPlayerObject(id);
                var player = NetworkManager.ConnectedClients[id].PlayerObject.GetComponent<Player>();

                _players.Add(player);
                NetworkPlayers.Add(player);

                OnPlayerConnectedClientRpc(player);
            };

            NetworkManager.OnClientDisconnectCallback += (id) =>
            {
                var player = GetPlayer(id);

                NetworkPlayers.Remove(player);

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

            if(playerObject.IsOwner)
                GameManager.Instance.Player = playerObject;
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

        void OnPlayerAdded(Player player)
        {
            player.Team.OnValueChanged += (o, n) =>
            {
                player.Balance.Value = GameMode.MaxBalance;

                SpawnPlayerServerRpc(player.OwnerClientId);

                player.Died += (killer, causeCode) => OnDiedCallback(player);
            };
        };

        void OnDiedCallback(Player player)
        {
            if(player.Controller.Value.TryGet(out PlayerController playerController))
                playerController.NetworkObject.Despawn();

            SpawnPlayerServerRpc(player.OwnerClientId);
        }

        _timerCoroutine = StartCoroutine(Timer(GameMode.WarmupTime, () =>
        {
            PlayerConnected -= OnPlayerAdded;

            foreach (var player in _players)
                player.Died -= (killer, causeCode) => OnDiedCallback(player);

            FinishWarmUpServerRpc();
        }));

        PlayerConnected += OnPlayerAdded;

        StartRoundClientRpc();
    }

    [ServerRpc]
    private void FinishWarmUpServerRpc()
    {
        void OnDied()
        {
            var teams = new Dictionary<Teams, int> { { Teams.Terrorist, 0 }, { Teams.CounterTerrorist, 0 } };

            foreach (var player in _players)
                if (player.Health.Value > 0)
                    teams[player.Team.Value]++;

            if (teams.ContainsValue(0))
            {
                var winningTeam = teams.OrderBy(team => team.Value).Last().Key;

                foreach (var player in _players)
                {
                    player.Balance.Value += player.Team.Value == winningTeam ? GameMode.WinAward : GameMode.LossAward;
                }

                if (++Points[winningTeam] == _gameMode.RoundCount / 2 + 1)
                {
                    foreach (var player in FindObjectsOfType<PlayerController>())
                        player.NetworkObject.Despawn();
                }
                FinishRoundTimeServerRpc();
                FinishRoundClientRpc(winningTeam);
            }

        }

        foreach (var player in _players)
        {
            player.Died += (killer, causeCode) => OnDied();
        }

        PlayerConnected += player => OnDied();
    }

    #endregion

    [ServerRpc]
    private void ClearMapServerRpc()
    {
        foreach (var networkObject in FindObjectsOfType<NetworkObject>())
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

        player.SpawnServerRpc(spawnpoint.position, spawnpoint.rotation);
    }

    #region Round

    [ServerRpc]
    private void StartRoundServerRpc()
    {
        ClearMapServerRpc();

        _isPlayersActive.Value = false;

        _timerCoroutine = StartCoroutine(Timer(GameMode.BuyTime, FinishBuyTimeServerRpc));

        foreach (var player in _players)
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

    /*[Rpc(SendTo.Server)]
    public T SpawnEntityServerRpc<T>(T entityPrefab) where T : Object
    {
        return Instantiate(entityPrefab,_entities);
    }*/

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