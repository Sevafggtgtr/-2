using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine.UIElements;
using static UnityEditor.Experimental.GraphView.GraphView;

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

public class GameManager : NetworkBehaviour
{
    public event UnityAction RoundEnded;
    public event UnityAction<int> RoundStarted;

    private static GameManager _singleton;
    public static GameManager Singleton => _singleton;

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

    private NetworkVariable<bool> _isPlayersActive = new NetworkVariable<bool>();
    public NetworkVariable<bool> IsPlayersActive => _isPlayersActive;

    [SerializeField]
    private TeamData[] _teams;
    public TeamData[] Teams => _teams;

    [SerializeField]
    private WeaponData _weaponData;
    public WeaponData WeaponData => _weaponData;

    [SerializeField]
    private Economy _economy;
    public Economy Economy => _economy;   

    public TeamData GetTeamData(Teams team)
        => _teams.First(team_ => team_.Team == team);

    private NetworkVariable<int>[] _points = new NetworkVariable<int>[2] {new NetworkVariable<int>(),new NetworkVariable<int>()};
    public NetworkVariable<int>[] Points => _points;
    private NetworkVariable<int> _time = new NetworkVariable<int>();
    public NetworkVariable<int> Time => _time;

    private Coroutine _timerCoroutine;

    void Awake()
    {
        _singleton = this;    
        
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        NetworkManager.OnServerStarted += () =>
        {
            StartWarmUp();
        };

        NetworkManager.OnClientStarted += () =>
        {           
                        
        };
    }

    private void StartWarmUp()
    {
        ClearMap();

        void OnClientConnectedCallback(ulong ID)
        {
            if (IsHost)
            {
                var player = NetworkManager.ConnectedClients[ID].PlayerObject.GetComponent<Player>();

                void OnDiedCallback(PlayerController playerObject)
                {
                    playerObject.NetworkObject.Despawn();
                    SpawnPlayer(player, OnDiedCallback);
                }

                player.Team.OnValueChanged += (o, n) =>
                {
                    SpawnPlayer(player, OnDiedCallback);
                };
            }
                
        };

        _timerCoroutine = StartCoroutine(Timer(GameMode.WarmupTime, () =>
        {
            NetworkManager.OnClientConnectedCallback -= OnClientConnectedCallback;

            StartRoundServerRpc();
        }));

        NetworkManager.OnClientConnectedCallback += OnClientConnectedCallback;
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
        _timerCoroutine = StartCoroutine(Timer(GameMode.BuyTime, FinishBuyTime));

        foreach (var player in NetworkManager.ConnectedClients)
        {
            SpawnPlayer(player.Value.PlayerObject.GetComponent<Player>(), (playerObject) =>
            {
                int[] teams = new int[2];

                foreach (var player in NetworkManager.ConnectedClients)
                    if (player.Value.PlayerObject.GetComponent<Player>().Controller.Value.TryGet(out PlayerController controller))
                        if (controller.Health > 0)
                            teams[(int)player.Value.PlayerObject.GetComponent<Player>().Team.Value - 1]++;
                if (teams[0] * teams[1] == 0)
                {
                    _points[teams[0] == 0 ? 1 : 0].Value++;
                    if (Mathf.Max(teams) == _gameMode.RoundCount / 2 + 1)
                    {
                        foreach (var player in FindObjectsOfType<PlayerController>())
                            player.NetworkObject.Despawn();
                    }
                    StartRoundServerRpc();
                }
            });
        }

        StartRoundClientRpc();

        ClearMap();
    }
    
    private void SpawnPlayer(Player player, UnityAction<PlayerController> diedCallback)
    {       
         var player_ = Instantiate(_playerPrefab);

         player_.Died += (killer) =>
         {
             diedCallback.Invoke(player_);
         };

         player_.NetworkObject.SpawnWithOwnership(player.OwnerClientId);

        var spawnPoints = Map.Singleton.GetTeamSpawnPoints(player.Team.Value).SpawnPoints.Where(spawnPoint => !Physics.OverlapSphere(spawnPoint.position, 1).Any(collider => collider.GetComponent<PlayerController>())).ToArray();
         player_.SpawnPlayersClientRpc(spawnPoints[Random.Range(0, spawnPoints.Length)].position);
        
    }

    public void FinishBuyTime()
    {
        _timerCoroutine = StartCoroutine(Timer(GameMode.RoundTime, FinishRoundTime));
    }

    public void FinishRoundTime()
    {
        _timerCoroutine = StartCoroutine(Timer(GameMode.RoundEndTime, StartRoundServerRpc));
    }

    [ClientRpc]
    private void StartRoundClientRpc()
    {
        //RoundStarted.Invoke(90);
    }

    /*[Rpc(SendTo.Server)]
    public T SpawnEntityServerRpc<T>(T entityPrefab) where T : Object
    {
        return Instantiate(entityPrefab,_entities);
    }*/

    IEnumerator Timer(int time,UnityAction callback)
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
