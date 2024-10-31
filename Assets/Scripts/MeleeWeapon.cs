using UnityEngine;
using Unity.Netcode;

public class MeleeWeapon : Weapon
{
    [SerializeField]
    private float _distance,
                  _delay;

    private float _time;

    private bool _isAction;

    public override bool Action(Vector3 origin, Vector3 direction, PlayerController owner)
    {
        ActionServerRpc(origin, direction, owner);
        return true;
    }       

    [ServerRpc]
    public  void ActionServerRpc(Vector3 origin, Vector3 direction, NetworkBehaviourReference owner)
    {
        if (owner.TryGet(out PlayerController ownerObject))
        {
            if (_isAction)
                return;

            _audioSource.Play();

            RaycastHit hit;
            if (Physics.Raycast(origin, direction, out hit, _distance))
            {
                if (hit.transform.GetComponent<IDamageableObject>() != null)
                    hit.transform.GetComponent<IDamageableObject>().DamageClientRpc(_damage, owner, Code);
            }
            _isAction = true;
        }
        
    }
    void Start()
    {

    }

    void Update()
    {
        if (_isAction)
            _time += Time.deltaTime;

        if (_time >= _delay)
        {
            _isAction = false;
            _time = 0;
        }
    }
}
