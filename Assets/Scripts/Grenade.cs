using UnityEngine;
using Unity.Netcode;
using System.Linq;

public class Grenade : Weapon
{
    [SerializeField]
    private float _radius,
                  _explodeTime,
                  _throwForce;

    private NetworkBehaviourReference _owner;

    public override void Action(Vector3 origin, Vector3 direction, PlayerController owner)
    {        
        owner.ChangeWeapon(owner.GetWeapon(), true);

        ActionServerRpc(origin, direction, owner);
    }    

    [ServerRpc]
    public void ActionServerRpc(Vector3 origin, Vector3 direction, NetworkBehaviourReference owner)
    {
        if (owner.TryGet(out PlayerController ownerObject))
        {
            _owner = ownerObject;

            Invoke("Explode", _explodeTime);

            Rigidbody.AddForce(direction * _throwForce, ForceMode.Impulse);
        }
    }

    private void Destroy()
    {
        NetworkObject.Despawn();
    }
    
    private void Explode()
    {
        foreach (Collider target in Physics.OverlapSphere(transform.position, _radius))
        {
            if (target.GetComponent<IDamageableObject>()!= null)
                target.GetComponent<IDamageableObject>().DamageClientRpc(_damage, _owner);
        }

        Invoke("Destroy", _audioSource.clip.length);
        
        ExplodeClientRpc();
    }

    [ClientRpc]
    private void ExplodeClientRpc()
    {
        _audioSource.Play();

        GetComponent<MeshRenderer>().enabled = false;
        Collider.enabled = false;
    }
}
