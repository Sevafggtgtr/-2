using UnityEngine;
using Unity.Netcode;

public class Grenade : Weapon
{
    [SerializeField]
    private float _radius,
                  _explodeTime,
                  _throwForce;

    private IDamageableObject _owner;

    public override void Action(Vector3 origin, Vector3 direction, NetworkBehaviourReference owner)
       => ActionServerRpc(origin, direction, owner);

    [ServerRpc]
    public void ActionServerRpc(Vector3 origin, Vector3 direction, NetworkBehaviourReference owner)
    {
        if (owner.TryGet(out PlayerController ownerObject))
        {
            _owner = ownerObject;

            Invoke("Explode", _explodeTime);

            GetComponentInParent<PlayerController>().ChangeWeapon();

            GetComponent<Rigidbody>().AddForce(direction * _throwForce, ForceMode.Impulse);
        }
                       
    }

    private void Destroy()
    {
        Destroy(gameObject);
    }

    private void Explode()
    {
        foreach (Collider target in Physics.OverlapSphere(transform.position, _radius))
        {
            if (target.GetComponent<IDamageableObject>()!= null)
                target.GetComponent<IDamageableObject>().DamegeClientRpc(_damage, _owner.Name);
        }
        _audioSource.Play();

        Invoke("Destroy", _audioSource.clip.length);

        GetComponent<MeshRenderer>().enabled = false;
        GetComponent<Collider>().enabled = false;

    }

    new void Start()
    {
        base.Start();
    }

    void Update()
    {
        
    }
}
