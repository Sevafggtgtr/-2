using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class Player : NetworkBehaviour, IKilleable
{
    #region Variables

    #region Events

    public event UnityAction Disconnected = delegate { };

    public event UnityAction TeamChanged = delegate { };

    public event UnityAction<Player> Died = delegate { };

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
    public PlayerController Controller => _controller.Value.TryGet(out PlayerController controller) ? controller : null;

    [SerializeField]
    private Sprite _deathSprite;

    string IKilleable.KillerText => _nickname.Value.ToString();
    Sprite IKilleable.KillerSprite => _deathSprite;
    Teams IKilleable.KillerTeam => _team.Value;
    #endregion

    #region Methods

    [ServerRpc]
    public void SelectTeamServerRpc(Teams team)
    {
        _team.Value = team;

        if(Controller)
            Controller.DieServerRpc(this);

        SelectTeamClientRpc();
    }

    [ClientRpc]
    private void SelectTeamClientRpc()
    {
        print($"SelectTeamClientRpc: " + OwnerClientId);

        TeamChanged.Invoke();
    }

    [ServerRpc]
    public void SpawnServerRpc(Vector3 position, Quaternion rotation)
    {
        var controller = Instantiate(_playerControllerPrefab, position, rotation);
        controller.NetworkObject.SpawnWithOwnership(OwnerClientId);
        controller.Kill += () =>
            _kills.Value++;
        controller.Died += (cause) =>
            _deaths.Value++;

        _controller.Value = controller;
        SpawnClientRpc(controller);
    }

    [ClientRpc]
    private void SpawnClientRpc(NetworkBehaviourReference controller)
    {
        print(OwnerClientId);

        if (controller.TryGet(out PlayerController controllerObject))
        {
            controllerObject.Died += killer =>
            {
                Died.Invoke(killer);

                Spectator.Instance.Spectate(0);
            };
        }
    }

    [ServerRpc]
    public void BuyWeaponServerRpc(FixedString32Bytes weaponCode)
    {
        if (!Controller)
            return;

        var weaponData = GameManager.Instance.WeaponData.GetTeamWeaponData(Team.Value).Weapons.First(weapon => weaponCode == weapon.Code);
       
        Balance.Value -= weaponData.Price;

        var weapon = Instantiate(GameManager.Instance.WeaponData.GetWeapon(weaponCode.ToString()));
        weapon.NetworkObject.SpawnWithOwnership(OwnerClientId);

        Controller.ChangeWeaponStateServerRpc(weapon, PlayerController.ChangeWeaponStates.Take);

        BuyWeaponClientRpc(OwnerClientId);
    }

    [ClientRpc]
    private void BuyWeaponClientRpc(ulong id)
    {
    }

    #endregion
}
