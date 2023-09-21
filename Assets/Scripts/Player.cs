using System.Collections;
using System.Collections.Generic;
using System.Data;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class Player : NetworkBehaviour
{
    public static event UnityAction TeamChoosed;

    public NetworkVariable<Teams> Team = new NetworkVariable<Teams>(Teams.Terrorist,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);

    void Start()
    {
        HUD.Singleton.ChooseTeamPanel.TeamChoosed += (team) =>
        {
            Team.Value = team;

            TeamChoosed?.Invoke();
        };
    }

    void Update()
    {
        
    }
}
