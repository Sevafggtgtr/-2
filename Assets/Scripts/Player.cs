using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class Player : NetworkBehaviour
{
    #region Variables

    #region Events

    public event UnityAction Disconnected;

    public event UnityAction Damaged = delegate { };
    public event UnityAction<Player, string> Died = delegate { };

    #endregion

    [SerializeField]
    private PlayerController _playerControllerPrefab;

    private NetworkVariable<FixedString32Bytes> _nickname = new NetworkVariable<FixedString32Bytes>(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<FixedString32Bytes> Nickname => _nickname;

    private NetworkVariable<Teams> _team = new NetworkVariable<Teams>();
    public NetworkVariable<Teams> Team => _team;

    private NetworkVariable<int> _kills = new NetworkVariable<int>();
    public NetworkVariable<int> Kills => _kills;

    private NetworkVariable<int> _deaths = new NetworkVariable<int>();
    public NetworkVariable<int> Deaths => _deaths;

    private NetworkVariable<int> _balance = new NetworkVariable<int>();
    public NetworkVariable<int> Balance => _balance;

    private NetworkVariable<NetworkBehaviourReference> _controller = new NetworkVariable<NetworkBehaviourReference>();
    public NetworkVariable<NetworkBehaviourReference> Controller => _controller;

    private NetworkVariable<int> _health = new NetworkVariable<int>(100);
    public NetworkVariable<int> Health => _health;

    #endregion

    #region Methods

    private void Start()
    {
        if (IsOwner)
        {
            _nickname.Value = UIMainMenu.Singleton.Nickname;
        }
    }

    [ServerRpc]
    public void ChangeTeamServerRpc(Teams team)
    {
        Team.Value = team;
    }

    [ServerRpc]
    public void SpawnServerRpc(Vector3 position, Quaternion rotation)
    {
        var controller = Instantiate(_playerControllerPrefab, position, rotation);
        controller.NetworkObject.SpawnWithOwnership(OwnerClientId);
        controller.Kill += () =>
            _kills.Value++;
        controller.Died += (killer, causeCode) =>
            _deaths.Value++;

        _controller.Value = controller;

        SpawnClientRpc(controller);
    }

    [ClientRpc]
    private void SpawnClientRpc(NetworkBehaviourReference controller)
    {
        if (controller.TryGet(out PlayerController controllerObject))
        {
            controllerObject.Died += (killer, causeCode) =>
            {
                Died.Invoke(killer, causeCode);

                Spectator.Instance.Spectate(0);
            };
        }
    }

    [ServerRpc]
    public void BuyWeaponServerRpc(FixedString32Bytes weaponCode)
    {
        var weaponData = GameManager.Instance.WeaponData.GetTeamWeaponData(Team.Value).Weapons.First(weapon => weaponCode == weapon.Code);
            Balance.Value -= weaponData.Price;

        var weapon = Instantiate(GameManager.Instance.WeaponData.GetWeapon(weaponCode.ToString()));
        weapon.NetworkObject.SpawnWithOwnership(OwnerClientId);

        if(Controller.Value.TryGet(out PlayerController controller))
            controller.ChangeWeaponStateServerRpc(weapon, PlayerController.ChangeWeaponStates.Take);

        BuyWeaponClientRpc(OwnerClientId);
    }
    [ClientRpc]
    private void BuyWeaponClientRpc(ulong id)
    {
        print(OwnerClientId);
    }

    #endregion
}
