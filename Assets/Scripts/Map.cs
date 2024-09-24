using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Map : MonoBehaviour
{
    private static Map _singleton;
    public static Map Singleton => _singleton;

    [System.Serializable]
    public struct TeamSpawnPoints
    {
        [SerializeField]
        private Teams _team;
        public Teams Team => _team;

        [SerializeField]
        private Transform[] _spawnPoints;
        public Transform[] SpawnPoints => _spawnPoints;
    }

    [SerializeField]
    private MapData _data;
    public MapData Data => _data;

    [SerializeField]
    private TeamSpawnPoints[] _teamSpawnPoints;
    public TeamSpawnPoints GetTeamSpawnPoints(Teams team) => _teamSpawnPoints.First(teamSpawnPoints => team == teamSpawnPoints.Team);    

    void Awake()
    {
        _singleton = this;
    }

    void Update()
    {
        
    }
}
