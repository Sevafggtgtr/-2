using System;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class Player : NetworkBehaviour
{
    public event UnityAction Disconnected;

    public event UnityAction Damaged;
    public event UnityAction<Player, string> Died;

    public NetworkVariable<Teams> Team = new NetworkVariable<Teams>(Teams.Terrorist, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private NetworkVariable<int> _kills = new NetworkVariable<int>(writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> _deaths = new NetworkVariable<int>(writePerm: NetworkVariableWritePermission.Owner);

    public NetworkVariable<int> Kills => _kills;
    public NetworkVariable<int> Deaths => _deaths;

    private NetworkVariable<FixedString32Bytes> _nickname = new NetworkVariable<FixedString32Bytes>(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<FixedString32Bytes> Nickname => _nickname;

    private PlayerController _controller;
    public NetworkVariable<NetworkBehaviourReference> Controller = new NetworkVariable<NetworkBehaviourReference>(writePerm: NetworkVariableWritePermission.Owner);

    private NetworkVariable<int> _balance = new NetworkVariable<int>();
    public NetworkVariable<int> Balance => _balance;

    private NetworkVariable<int> _health = new NetworkVariable<int>(100);
    public NetworkVariable<int> Health => _health;

    [SerializeField]
    private PlayerController _playerControllerPrefab;

    void Start()
    {
        if (IsOwner)
        {
            _nickname.Value = UIMainMenu.Singleton.Nickname;
        }
    }

    public void Spawn(Vector3 spawnPoint)
    {
        var playerController = Instantiate(_playerControllerPrefab);

        playerController.NetworkObject.SpawnWithOwnership(OwnerClientId);

        playerController.SpawnPlayersClientRpc(spawnPoint);

        SpawnClientRpc(playerController);
    }

    [ClientRpc]
    private void SpawnClientRpc(NetworkBehaviourReference playerController)
    {
        if (playerController.TryGet(out PlayerController controller) && controller.IsOwner)
        {
            controller.Died += (killer, causeCode) =>
            {
                _deaths.Value++;
                Died.Invoke(killer, causeCode);

                //var spectatedPlayer = GameManager.Instance.NetworkPlayers.FirstOrDefault(player => player.Team.Value == Player.Team.Value && player.Controller.Value.TryGet(out PlayerController playerController) && playerController.enabled);

                Spectator.Instance.Spectate(0);
            };

            controller.Kill += () =>
                _kills.Value++;
            Controller.Value = controller;

            _controller = controller;
        }
    }

    [ServerRpc]
    public void BuyServerRpc(FixedString32Bytes weaponCode)
    {
        var weapon = GameManager.Instance.WeaponData.GetTeamWeaponData(Team.Value).Weapons.First(weapon => weaponCode == weapon.Code);
            Balance.Value -= weapon.Price;

        _controller.AddWeaponServerRpc(Array.IndexOf(GameManager.Instance.WeaponData.GetTeamWeaponData(Team.Value).Weapons, weapon));
    }
    [ClientRpc]
    private void BuyClientRpc()
    {

    }

    public override void OnDestroy()
    {
        Disconnected.Invoke();
    }

    public void ChangeTeam(Teams team)
    {
        Team.Value = team;
    }

    void Update()
    {

    }
}
