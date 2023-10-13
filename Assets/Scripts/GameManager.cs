using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

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
    private static GameManager _singleton;
    public static GameManager Singleton => _singleton;

    [SerializeField]
    private PlayerController _playerPrefab;

    [SerializeField]
    private Map _map;

    [SerializeField]
    private TeamData[] _teams;
    public TeamData[] Teams => _teams;

    private NetworkVariable<int>[] _points = new NetworkVariable<int>[2];
    public NetworkVariable<int>[] Points => _points;
    private NetworkVariable<int> _time = new NetworkVariable<int>();
    public NetworkVariable<int> Time => _time;

    void Awake()
    {
        _singleton = this;       
    }

    private void Start()
    {
        /*NetworkManager.NetworkTickSystem.Tick += () =>
        {
            _time.Value--;
        };*/

        NetworkManager.OnClientConnectedCallback += (ID) =>
        {
            if(IsHost)
            NetworkManager.ConnectedClients[ID].PlayerObject.GetComponent<Player>().Team.OnValueChanged += (o, n) =>
            {
                StartRoundServerRpc();
            };
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
                    teams[(int)player.Value.PlayerObject.GetComponent<Player>().Team.Value - 1]++;
                if (teams[0] * teams[1] > 0)
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
            player_.transform.position = spawnPoint.position;

            spawnPoints[(int)player.Value.PlayerObject.GetComponent<Player>().Team.Value - 1].Remove(spawnPoint);
        }
    }

    void Update()
    {

    }
}
