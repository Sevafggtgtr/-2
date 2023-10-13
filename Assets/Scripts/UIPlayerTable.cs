using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class UIPlayerTable : MonoBehaviour
{
    [SerializeField]
    private Transform _playerTable;

    [SerializeField]
    private UINickname _nicknamePrefab;

    void Start()
    {
        foreach (var player in FindObjectsOfType<Player>())
            Instantiate(_nicknamePrefab, _playerTable).Initialize(player);

        NetworkManager.Singleton.OnClientConnectedCallback += (ID) =>
        {
            var player = NetworkManager.Singleton.ConnectedClients[ID].PlayerObject.GetComponent<Player>();
            player.Team.OnValueChanged += (o, n) =>
            {
                Instantiate(_nicknamePrefab, _playerTable).Initialize(player);
            };
        };
    }

    void Update()
    {
        
    }
}
