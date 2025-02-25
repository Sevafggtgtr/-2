using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class MeleeWeapon : Weapon
{
    #region Methods

    [SerializeField]
    private float _distance,
                  _delay;

    private Coroutine _hitCoroutine;

    public override void Action(Vector3 origin, Vector3 direction)
    {
        ActionServerRpc(origin, direction);
    }

    [ServerRpc]
    private void ActionServerRpc(Vector3 origin, Vector3 direction)
    {
        if (_hitCoroutine != null)
            return;

        _audioSource.Play();

        if (Physics.Raycast(origin, direction, out var hit, _distance))
        {
            if (hit.transform.GetComponent<IDamageable>() != null)
                hit.transform.GetComponent<IDamageable>().DamageServerRpc(_damage, _owningPlayer);
        }

        _hitCoroutine = StartCoroutine(Hit());
    }

    private IEnumerator Hit()
    {
        yield return new WaitForSeconds(_delay);
    }

    #endregion
}
