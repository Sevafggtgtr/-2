using System.Collections;
using UnityEngine;

public abstract class TimeActivateGrenade : Grenade
{
    [SerializeField]
    private float _activateTime;

    protected override void OnThrow()
    {
        StartCoroutine(Activate());
    }

    private IEnumerator Activate()
    {
        yield return new WaitForSeconds(_activateTime);

        OnActivate();
    }

    protected abstract void OnActivate();      
}
