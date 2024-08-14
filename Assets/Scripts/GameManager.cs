using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;

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
    private Transform _entities;

    [SerializeField]
    private TeamData[] _teams;
    public TeamData[] Teams => _teams;

    [SerializeField]
    private WeaponData _weaponData;
    public WeaponData WeaponData => _weaponData;

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
            NetworkManager.NetworkTickSystem.Tick += () =>
            {
                //_time.Value--;
            };
        };

        NetworkManager.OnClientConnectedCallback += (ID) =>
        {
            if (IsHost)
                NetworkManager.ConnectedClients[ID].PlayerObject.GetComponent<Player>().Team.OnValueChanged += (o, n) =>
                {                   
                    StartRoundServerRpc();
                };
        };

        NetworkManager.OnClientStarted += () =>
        {           
                        
        };
    }

    [ServerRpc]
    private void StartRoundServerRpc()
    {
        Transform spawnPoint;

        var spawnPoints = new List<Transform>[2];

        foreach (var networkObject in FindObjectsOfType<NetworkObject>())
        {
            if(!networkObject.IsPlayerObject && !networkObject.IsSceneObject.Value)
            {
                print(networkObject.name);

                networkObject.Despawn();
            }                
        }
            
        for (int i = 0; i < 2; i++)
            spawnPoints[i] = Map.Singleton.GetTeamSpawnPoints()[i].SpawnPoints.ToList();

        foreach (var player in NetworkManager.ConnectedClients)
        {
            var player_ = Instantiate(_playerPrefab);

            player_.Died += (killer) =>
            {
                int[] teams = new int[2];

                foreach (var player in NetworkManager.ConnectedClients)
                    if(player.Value.PlayerObject.GetComponent<Player>().Controller.Value.TryGet(out PlayerController controller))
                        if(controller.Health > 0)
                            teams[(int)player.Value.PlayerObject.GetComponent<Player>().Team.Value - 1]++;
                if (teams[0] * teams[1] == 0)
                {                   
                    _points[teams[0] == 0 ? 1 : 0].Value ++;
                    if(Mathf.Max(teams) == 13)
                    {
                        foreach (var player in FindObjectsOfType<PlayerController>())
                            Destroy(player);
                    }                    
                    StartRoundServerRpc();
                }
            };

            player_.NetworkObject.SpawnWithOwnership(player.Value.ClientId);

            spawnPoint = spawnPoints[(int)player.Value.PlayerObject.GetComponent<Player>().Team.Value - 1][Random.Range(0, spawnPoints[(int)player.Value.PlayerObject.GetComponent<Player>().Team.Value - 1].Count)];
            player_.SpawnPlayersClientRpc(player.Value.ClientId, spawnPoint.position); ;
            
            spawnPoints[(int)player.Value.PlayerObject.GetComponent<Player>().Team.Value - 1].Remove(spawnPoint);
        }
        _time.Value = 90;

        _timerCoroutine = StartCoroutine(Timer());

        StartRoundClientRpc();
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

    IEnumerator Timer()
    {       
        while(_time.Value > 0)
        {
            yield return new WaitForSeconds(1);

            _time.Value--;                
        }                
        StartRoundServerRpc();            
    }

    void Update()
    {

    }
}
