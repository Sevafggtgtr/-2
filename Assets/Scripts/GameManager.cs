using NUnit.Framework;
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
    public event UnityAction<Teams> RoundFinished;
    public event UnityAction RoundStarted;

    [SerializeField]
    private PlayerController _playerPrefab;

    [SerializeField]
    private MapData[] _maps;
    public MapData[] Maps => _maps;

    [SerializeField]
    private GameMode _gameMode;
    public GameMode GameMode => _gameMode;

    [SerializeField]
    private Transform _entities;

    public List<Player> Players { get; private set; }

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
    
    public Dictionary<Teams, int> Points {get; private set;}
    private NetworkVariable<int> _time = new NetworkVariable<int>();
    public NetworkVariable<int> Time => _time;

    private Coroutine _timerCoroutine;

    private void Start()
    {
        Players = new List<Player>();

        Points = new Dictionary<Teams, int> { { Teams.Terrorist, 0 }, { Teams.CounterTerrorist, 0 } };

        DontDestroyOnLoad(gameObject);

        NetworkManager.OnServerStarted += () =>
        {
            StartWarmUp();
        };

        NetworkManager.OnClientStarted += () =>
        {

        };

        NetworkManager.OnClientConnectedCallback += (id) =>
        {
            Players.Add(NetworkManager.ConnectedClients[id].PlayerObject.GetComponent<Player>());
        };

        NetworkManager.OnClientDisconnectCallback += (id) =>
        {
            Players.Remove(NetworkManager.ConnectedClients[id].PlayerObject.GetComponent<Player>());
        };
    }

    private void StartWarmUp()
    {
        ClearMap();

        _isPlayersActive.Value = true;

        void OnClientConnectedCallback(ulong ID)
        {
            var player = NetworkManager.ConnectedClients[ID].PlayerObject.GetComponent<Player>();
          
            player.Team.OnValueChanged += (o, n) =>
            {
                void OnDiedCallback(PlayerController playerController)
                {
                    playerController.NetworkObject.Despawn();
                    playerController = SpawnPlayer(player);
                    playerController.Died += (killer,causeCode) => OnDiedCallback(playerController);
                }

                player.Balance.Value = GameMode.MaxBalance;

                var playerController = SpawnPlayer(player);
                playerController.Died += (killer, causeCode) => OnDiedCallback(playerController);
            };
        };

        _timerCoroutine = StartCoroutine(Timer(GameMode.WarmupTime, () =>
        {
            NetworkManager.OnClientConnectedCallback -= OnClientConnectedCallback;

            StartRoundServerRpc();
        }));

        NetworkManager.OnClientConnectedCallback += OnClientConnectedCallback;

        StartRoundClientRpc();
    }

    private void ClearMap()
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
    private void StartRoundServerRpc()
    {
        ClearMap();

        _isPlayersActive.Value = false;

        _timerCoroutine = StartCoroutine(Timer(GameMode.BuyTime, FinishBuyTime));

        foreach (var client in NetworkManager.ConnectedClients)
        {
            var player = client.Value.PlayerObject.GetComponent<Player>();

            var playerController = SpawnPlayer(player);
            playerController.Died += (killer, causeCode) =>
            {
                var teams = new Dictionary<Teams, int> { {Teams.Terrorist, 0}, { Teams.CounterTerrorist, 0} };

                foreach (var player in Players)
                    if (player.Controller.Value.TryGet(out PlayerController controller))
                        if (controller.Health.Value > 0)
                            teams[player.Team.Value]++;
                if (teams.ContainsValue(0))
                {
                    var winningTeam = teams.Max().Key;

                    foreach (var player in Players)
                    {
                        player.Balance.Value += player.Team.Value == winningTeam ? GameMode.WinAward : GameMode.LossAward;
                    }
                  
                    if (++Points[winningTeam] == _gameMode.RoundCount / 2 + 1)
                    {
                        foreach (var player in FindObjectsOfType<PlayerController>())
                            player.NetworkObject.Despawn();
                    }
                    FinishRoundClientRpc(winningTeam);
                }
            };
        }

        StartRoundClientRpc();
    }

    private PlayerController SpawnPlayer(Player player)
    {
        var playerController = Instantiate(_playerPrefab);

        playerController.NetworkObject.SpawnWithOwnership(player.OwnerClientId);

        var spawnPoints = Map.Singleton.GetTeamSpawnPoints(player.Team.Value).SpawnPoints.Where(spawnPoint => !Physics.OverlapSphere(spawnPoint.position, 1).Any(collider => collider.GetComponent<PlayerController>())).ToArray();
        playerController.SpawnPlayersClientRpc(spawnPoints[Random.Range(0, spawnPoints.Length)].position);

        return playerController;
    }

    public void FinishBuyTime()
    {
        _timerCoroutine = StartCoroutine(Timer(GameMode.RoundTime, FinishRoundTime));

        _isPlayersActive.Value = true;
    }

    public void FinishRoundTime()
    {
        _timerCoroutine = StartCoroutine(Timer(GameMode.RoundEndTime, StartRoundServerRpc));

        /*foreach (var player in NetworkManager.ConnectedClients)
        {
            player.Value.PlayerObject.GetComponent<Player>().Balance.Value += ;
        }*/
    }

    [ClientRpc]
    private void InitializeClientRpc()
    {
        
    }

    [ClientRpc]
    private void StartRoundClientRpc()
    {
        RoundStarted?.Invoke();
    }

    [ClientRpc]
    private void FinishRoundClientRpc(Teams winningTeam)
    {
        RoundFinished.Invoke(winningTeam);
    }

    /*[Rpc(SendTo.Server)]
    public T SpawnEntityServerRpc<T>(T entityPrefab) where T : Object
    {
        return Instantiate(entityPrefab,_entities);
    }*/

    IEnumerator Timer(int time, UnityAction callback)
    {
        _time.Value = time;

        while (_time.Value > 0)
        {
            yield return new WaitForSeconds(1);

            _time.Value--;
        }
        callback.Invoke();
    }

    void Update()
    {

    }
}
