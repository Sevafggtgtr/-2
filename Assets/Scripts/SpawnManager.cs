using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpawnManager : NetworkBehaviour
{
    [SerializeField]
    private PlayerController _playerPrefab;

    public GameObject[] _enemes;
    private int _index;

    void Start()
    {
        InvokeRepeating("Spawn", 2, 60f);

        HUD.Singleton.RespawnMenu.Respawn += () =>
        {
            PlayerController.Singleton.FpCamera.gameObject.SetActive(false);
            PlayerController.Singleton.HandCamera.gameObject.SetActive(false);

            Destroy(PlayerController.Singleton);

            RespawnServerRpc();
        };      
    }

    [ServerRpc]
    private void DisconnectServerRpc()
    {
        NetworkManager.Singleton.DisconnectClient(NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RespawnServerRpc()
    {
        var player = Instantiate(_playerPrefab);

        player.NetworkObject.SpawnAsPlayerObject(NetworkManager.Singleton.LocalClientId);
    }

    void Spawn()
    {
        Vector3 position = new Vector3(Random.Range(10, 30), 0, Random.Range(20, -40));
        _index = Random.Range(0, _enemes.Length);
        Instantiate(_enemes[_index], position, _enemes[_index].transform.rotation);
    }
}
