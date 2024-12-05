using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class FlashBang : ExplosiveGrenade
{    
    [Header("FlashBang")]
    [SerializeField]
    private float _duration;
 
    protected override void Explode(PlayerController playerController)
    {
        ExplodeClientRpc(playerController);
    }

    [Rpc(SendTo.Everyone)]
    private void ExplodeClientRpc(NetworkBehaviourReference playerController)
    {
        print("+");
        if(playerController.TryGet(out PlayerController playerControllerObject) && playerControllerObject == PlayerController.Instance)
        {
            print("-");

            HUD.Singleton.Blindness(_duration);
        }
            
    }
}
