using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class FlashBang : TimeActivateGrenade
{
    [Header("FlashBang")]
    [SerializeField]
    private float _duration,
                  _radius;

    protected override void OnActivate()
    {
        ActivateClientRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void ActivateClientRpc()
    {
        _audioSource.Play();

        GetComponent<MeshRenderer>().enabled = false;
        Collider.enabled = false;

        //if(_radius = )
        HUD.Singleton.Blindness(_duration);
    }
}
