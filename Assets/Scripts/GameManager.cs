using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public enum Teams
{
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
    private static GameManager _singleton;
    public static GameManager Singleton => _singleton;

    [SerializeField]
    private PlayerController _playerPrefab;

    [SerializeField]
    private Map _map;

    [SerializeField]
    private TeamData[] _teams;
    public TeamData[] Teams => _teams;

    private NetworkList<int> _points = new NetworkList<int>();
    public NetworkList<int> Points => _points;

    private NetworkVariable<int> _time = new NetworkVariable<int>();
    public NetworkVariable<int> Time => _time;

    void Awake()
    {
        _singleton = this;

        if (IsHost)
        {
            Player.TeamChoosed += () =>
            {
                int[] team = new int[2];

                foreach (var player in NetworkManager.ConnectedClients)
                    team[(int)player.Value.PlayerObject.GetComponent<Player>().Team.Value]++;
                if (team[0] * team[1] > 0)
                {
                    StartRoundServerRpc();
                }
            };
        }
    }

    private void Start()
    {
        /*NetworkManager.NetworkTickSystem.Tick += () =>
        {
            _time.Value--;
        };*/
    }

    [ServerRpc]
    private void StartRoundServerRpc()
    {
        Transform spawnPoint;

        var spawnPoints = new List<Transform>[2];

        for(int i = 0; i < 2; i++)
            spawnPoints[i] = _map.GetTeamSpawnPoints()[i].SpawnPoints.ToList();

        foreach (var player in NetworkManager.ConnectedClients)
        {
            var player_ = Instantiate(_playerPrefab);

            player_.NetworkObject.SpawnAsPlayerObject(player.Value.ClientId);

            spawnPoint = spawnPoints[(int)player.Value.PlayerObject.GetComponent<Player>().Team.Value][Random.Range(0, spawnPoints[(int)player.Value.PlayerObject.GetComponent<Player>().Team.Value].Count)];
            player_.transform.position = spawnPoint.position;

            spawnPoints[(int)player.Value.PlayerObject.GetComponent<Player>().Team.Value].Remove(spawnPoint);
        }
    }

    void Update()
    {
         
    }
}
