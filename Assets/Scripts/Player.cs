using System.Collections;
using System.Collections.Generic;
using System.Data;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class Player : NetworkBehaviour
{
    public static event UnityAction TeamChoosed;
    public event UnityAction Disconnected;

    public NetworkVariable<Teams> Team = new NetworkVariable<Teams>(Teams.Terrorist,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);

    private NetworkVariable<int> _kills = new NetworkVariable<int>(writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> _deaths = new NetworkVariable<int>(writePerm: NetworkVariableWritePermission.Owner);

    public NetworkVariable<int> Kills => _kills;
    public NetworkVariable<int> Deaths => _deaths;

    private NetworkVariable<FixedString32Bytes> _nickname = new NetworkVariable<FixedString32Bytes>(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<FixedString32Bytes> Nickname => _nickname;

    void Start()
    {

        if (IsOwner)
        {
            PlayerController.Spawn += (player) =>
            {
                if (player.IsOwner)
                {
                    player.Died += (killer) =>                    
                        _deaths.Value++;
                    player.Kill += () =>
                        _kills.Value++;
                }
            };

            _nickname.Value = UIMainMenu.Singleton.Nickname;
            print(_nickname.Value);
            print(UIMainMenu.Singleton.Nickname);

            HUD.Singleton.ChooseTeamPanel.TeamChoosed += (team) =>
            {
                Team.Value = team;

                TeamChoosed?.Invoke();
            };
        }
    }

    public override void OnDestroy()
    {
        Disconnected.Invoke();
    }

    void Update()
    {
        
    }
}
