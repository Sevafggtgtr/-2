using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HEGrenade : Grenade
{
    [Header("HEGrenade")]
    [SerializeField]
    private float _radius;

    protected override void Activate()
    {            
        foreach (Collider target in Physics.OverlapSphere(transform.position, _radius))
        {
            if (target.GetComponent<IDamageableObject>() != null)
                target.GetComponent<IDamageableObject>().DamageClientRpc(_damage, _owner);
        }

        ActivateClientRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void ActivateClientRpc()
    {
        _audioSource.Play();

        GetComponent<MeshRenderer>().enabled = false;
        Collider.enabled = false;

        GetComponentInChildren<ParticleSystem>().Play();
    }
}
