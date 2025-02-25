using System.Linq;
using System.Collections;
using UnityEngine;
using Unity.Netcode;

public abstract class Grenade : Weapon
{
    [Header("Grenade")]
    [SerializeField]
    private float _throwForce,
                  _throwTime;

    public override void Action(Vector3 origin, Vector3 direction)
    {
        ActionServerRpc(origin, direction);
    }

    [ServerRpc]
    private void ActionServerRpc(Vector3 origin, Vector3 direction)
    {
        StartCoroutine(Throw(origin, direction));

        Rigidbody.AddForce(direction * _throwForce, ForceMode.Impulse);
    }

    private IEnumerator Throw(Vector3 origin, Vector3 direction)
    {
        //ownerObject.CanChangeWeapon = false;

        yield return new WaitForSeconds(_throwTime);

        //ownerObject.CanChangeWeapon = true;

        //ownerObject.ChangeWeapon(ownerObject.Weapons.First(weapon => weapon), true);

        Action(origin, direction);

        StartCoroutine(OnThrow());
    }

    protected abstract IEnumerator OnThrow();

    [ClientRpc]
    protected virtual void OnEndThrowClientRpc()
    {
        _audioSource.Play();

        GetComponent<MeshRenderer>().enabled = false;
        Collider.enabled = false;
        Rigidbody.isKinematic = true;

        var particleSystem = GetComponentInChildren<ParticleSystem>();
        if(particleSystem)
            particleSystem.Play();
    }
}
