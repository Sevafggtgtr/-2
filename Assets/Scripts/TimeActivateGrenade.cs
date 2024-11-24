using System.Collections;
using UnityEngine;

public abstract class TimeActivateGrenade : Grenade
{
    [SerializeField]
    private float _activateTime;

    protected override IEnumerator OnThrow()
    {
        yield return new WaitForSeconds(_activateTime);

        Activate();
    }

    protected abstract void Activate();
}
