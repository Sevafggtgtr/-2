using UnityEngine;
using Unity.Netcode;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Collections;
using static UnityEngine.UI.Image;

public abstract class Grenade : Weapon
{
    [Header("Grenade")]
    [SerializeField]
    private float _throwForce,
                  _throwTime;

    protected NetworkBehaviourReference _owner;

    public override bool Action(Vector3 origin, Vector3 direction, Player owner)
    {
        StartCoroutine(Throw(origin, direction, owner));       

        return true;
    }    

    [ServerRpc]
    public void ActionServerRpc(Vector3 origin, Vector3 direction, NetworkBehaviourReference owner)
    {
        if (owner.TryGet(out Player ownerObject))
        {
            _owner = ownerObject;           

            Rigidbody.AddForce(direction * _throwForce, ForceMode.Impulse);
        }
    }

    private IEnumerator Throw(Vector3 origin, Vector3 direction, Player owner)
    {
        if (owner.Controller.Value.TryGet(out PlayerController ownerObject))
        {
            ownerObject.CanChangeWeapon = false;

            yield return new WaitForSeconds(_throwTime);

            ownerObject.CanChangeWeapon = true;

            ownerObject.ChangeWeapon(ownerObject.Weapons.First(weapon => weapon), true);

            ActionServerRpc(origin, direction, owner);

            StartCoroutine(OnThrow());
        }

    }

    protected abstract IEnumerator OnThrow();

    [Rpc(SendTo.Everyone)]
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
