using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Map : MonoBehaviour
{
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
    private TeamSpawnPoints[] _teamSpawnPoints;
    public TeamSpawnPoints[] GetTeamSpawnPoints() => _teamSpawnPoints;

    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
