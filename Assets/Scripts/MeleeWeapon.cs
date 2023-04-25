using UnityEngine;
using Unity.Netcode;

public class MeleeWeapon : Weapon
{
    [SerializeField]
    private float _distance,
                  _delay;

    private float _time;

    private bool _isAction;

    public override void Action(Vector3 origin, Vector3 direction, NetworkBehaviourReference owner)
       => ActionServerRpc(origin, direction, owner);

    [ServerRpc]
    public  void ActionServerRpc(Vector3 origin, Vector3 direction, NetworkBehaviourReference owner)
    {
        if (owner.TryGet(out PlayerController ownerObject))
        {
            if (_isAction)
                return;

            _audioSource.Play();

            RaycastHit hit;
            if (Physics.SphereCast(origin, _distance / 2, direction, out hit))
            {
                if (hit.transform.GetComponent<IDamageableObject>() != null)
                    hit.transform.GetComponent<IDamageableObject>().DamegeClientRpc(_damage, ownerObject.Name);
            }
            _isAction = true;
        }
        
    }
    new void Start()
    {
        base.Start();
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
