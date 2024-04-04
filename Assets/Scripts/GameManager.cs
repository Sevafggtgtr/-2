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

    public TeamData(string name, Color color, Teams team)
    {
        _name = name;
        _color = color;
        _team = team;
    }

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
    private Map _map;

    [SerializeField]
    private TeamData[] _teams;
    public TeamData[] Teams => _teams;

    private NetworkVariable<int>[] _points = new NetworkVariable<int>[2] {new NetworkVariable<int>(),new NetworkVariable<int>()};
    public NetworkVariable<int>[] Points => _points;
    private int _time;
    public int Time => _time;

    private Coroutine _timerCoroutine;

    void Awake()
    {
        _singleton = this;       
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
                    _timerCoroutine = StartCoroutine(Timer());
                    StartRoundServerRpc();
                };
        };

        NetworkManager.OnClientStarted += () =>
        {           
            if(!IsHost)
                _timerCoroutine = StartCoroutine(Timer());            
        };
    }

    [ServerRpc]
    private void StartRoundServerRpc()
    {
        Transform spawnPoint;

        var spawnPoints = new List<Transform>[2];

        foreach (var player in FindObjectsOfType<PlayerController>())
            player.NetworkObject.Despawn();

        for (int i = 0; i < 2; i++)
            spawnPoints[i] = _map.GetTeamSpawnPoints()[i].SpawnPoints.ToList();

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
        StartRoundClientRpc();
    }
     
    [ClientRpc]
    private void StartRoundClientRpc()
    {
        _time = 90;        

        //RoundStarted.Invoke(90);
    }

    IEnumerator Timer()
    {
        while(true)
        {
            if(_time > 0)
            {
                yield return new WaitForSeconds(1);

                _time--;

                
            }     
            else
               if (IsHost)
                 StartRoundServerRpc();
        }     
    }

    void Update()
    {

    }
}
