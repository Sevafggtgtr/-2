using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.AI;
using Unity.Netcode;

public class Enemy : NetworkBehaviour, IDamageableObject
{
    public event UnityAction<string> Died;

    private int _health = 100;

    private Animation _animation;
    private NavMeshAgent _agent;

    private bool _alive = true;

    [SerializeField]
    private Gun _gun;

    [SerializeField]
    private float _distance,
                  _speed,
                  _preferredDistance;

    [SerializeField]
    private PlayerController _player;

    [SerializeField]
    private Slider _healthBar;

    public string Name { get; set; }

    [ClientRpc]
    public void DamegeClientRpc(int value, string killer)
    {
        _health -= value;
        _healthBar.value = _health;
        _animation.Play();
        if(_health <= 0)
        {
            _alive = false;
            _agent.enabled = false;
            Die(killer);
        }
    }

    protected void Die(string killer)
    {
        //Died?.Invoke(killer);
    }

    void Start()
    {
        _animation = GetComponent<Animation>();
        _agent = GetComponent<NavMeshAgent>();        
    }

    void Update()
    {
        if (!_alive)
            return;        
        
        transform.forward = _player.transform.position - transform.position;
        transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);

        if(_gun.CurrentClipAmmo == 0)
            _gun.Reload();

        if (Vector3.Distance(_player.transform.position, transform.position) <= _distance)
        {
            _gun.ActionServerRpc(_gun.transform.position, transform.forward, this);
            _agent.destination = transform.position;
        }
        else
            _agent.destination = _player.transform.position;

        if (_gun.IsReloading)
        {

        }
        
    }
}
