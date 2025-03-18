using System.Collections;
using Unity.Netcode;
using UnityEngine;

public abstract class TimeActivatedGrenade : Grenade
{
    [SerializeField]
    private float _activateTime;

    protected override IEnumerator OnThrown()
    {
        yield return new WaitForSeconds(_activateTime);

        Activate();

        OnEndThrowClientRpc();
    }

    protected virtual void Activate() { }
}
