using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SmokeGrenade : Grenade
{
    protected override IEnumerator OnThrow()
    {
        while (!Rigidbody.IsSleeping())
            yield return null;
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
