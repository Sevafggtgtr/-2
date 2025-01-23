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

    private void Start()
    {
        Initialize();

        GameManager.Instance.PlayerConnected += (player) =>
        {
            Initialize();

            player.Team.OnValueChanged += (previousValue, newValue)
                => Initialize();
        };

        GameManager.Instance.PlayerDisconnect += (player) 
            => Initialize();
    }

    private void Initialize()
    {
        foreach (Transform child in _playerTable)
            Destroy(child.gameObject);

        foreach (var player in GameManager.Instance.NetworkPlayers)
            if (player.TryGet(out Player playerObject))
                Instantiate(_nicknamePrefab, _playerTable).Initialize(playerObject);
    }
}
