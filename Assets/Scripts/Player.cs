using System.Collections;
using System.Collections.Generic;
using System.Data;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class Player : Singleton<Player>
{
    public event UnityAction Disconnected;

    public NetworkVariable<Teams> Team = new NetworkVariable<Teams>(Teams.Terrorist,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);

    private NetworkVariable<int> _kills = new NetworkVariable<int>(writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> _deaths = new NetworkVariable<int>(writePerm: NetworkVariableWritePermission.Owner);

    public NetworkVariable<int> Kills => _kills;
    public NetworkVariable<int> Deaths => _deaths;

    private NetworkVariable<FixedString32Bytes> _nickname = new NetworkVariable<FixedString32Bytes>(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<FixedString32Bytes> Nickname => _nickname;

    public NetworkVariable<NetworkBehaviourReference> Controller = new NetworkVariable<NetworkBehaviourReference>(writePerm: NetworkVariableWritePermission.Owner);

    private NetworkVariable<int> _balance = new NetworkVariable<int>();
    public NetworkVariable<int> Balance => _balance;

    void Start()
    {
        if (IsOwner)
        {
            PlayerController.Spawn += (player) =>
            {
                if (player.IsOwner)
                {
                    player.Died += (killer, causeCode) =>                    
                        _deaths.Value++;
                    player.Kill += () =>
                        _kills.Value++;
                    Controller.Value = player;
                }
            };

            _nickname.Value = UIMainMenu.Singleton.Nickname;
        }
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
