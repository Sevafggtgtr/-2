using UnityEngine;
using Unity.Netcode;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

public abstract class Grenade : Weapon
{
    [Header("Grenade")]
    [SerializeField]
    private float _activateTime,
                  _throwForce;

    protected NetworkBehaviourReference _owner;

    public override bool Action(Vector3 origin, Vector3 direction, PlayerController owner)
    {        
        owner.ChangeWeapon(owner.Weapons.Min(weapon => weapon), true);

        ActionServerRpc(origin, direction, owner);

        return true;
    }    

    [ServerRpc]
    public void ActionServerRpc(Vector3 origin, Vector3 direction, NetworkBehaviourReference owner)
    {
        if (owner.TryGet(out PlayerController ownerObject))
        {
            _owner = ownerObject;

            StartCoroutine(Extensions.Timer(_activateTime, Activate));

            Rigidbody.AddForce(direction * _throwForce, ForceMode.Impulse);
        }
    }

    protected abstract void Activate();       
}
