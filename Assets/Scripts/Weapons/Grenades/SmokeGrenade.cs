using System.Collections;
using UnityEngine;

public class SmokeGrenade : Grenade
{
    protected override IEnumerator OnThrown()
    {
        while (!Rigidbody.IsSleeping())
            yield return null;

        OnEndThrowClientRpc();
    }
}
