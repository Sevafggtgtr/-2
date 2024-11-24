using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

public class HEGrenade : ExplosiveGrenade
{
    protected override void Explode()
    {
        PlayerController.Instance.DamageServerRpc(_damage, _owner, Code);
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
