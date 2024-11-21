using UnityEngine;
using Unity.Netcode;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Collections;

public abstract class Grenade : Weapon
{
    [Header("Grenade")]
    [SerializeField]
    private float _throwForce,
                  _throwTime;

    protected NetworkBehaviourReference _owner;

    public override bool Action(Vector3 origin, Vector3 direction, Player owner)
    {        
        //owner.ChangeWeapon(owner.Weapons.First(weapon => weapon), true);

        ActionServerRpc(origin, direction, owner);

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

    private IEnumerator Throw()
    {
        yield return new WaitForSeconds(_throwTime);

        OnThrow();
    }

    protected abstract void OnThrow();       
}
