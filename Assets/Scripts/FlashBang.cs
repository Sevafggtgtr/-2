using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class FlashBang : ExplosiveGrenade
{    
    [Header("FlashBang")]
    [SerializeField]
    private float _duration;
 
    protected override void Explode()
    {
        HUD.Singleton.Blindness(_duration);

        ActivateClientRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void ActivateClientRpc()
    {
        _audioSource.Play();

        GetComponent<MeshRenderer>().enabled = false;
        Collider.enabled = false;        
    }
}
