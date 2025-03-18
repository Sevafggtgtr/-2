using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.AI;
using Unity.Netcode;

public class Enemy : NetworkBehaviour//, IDamageableObject
{
    #region Variables

    public event UnityAction<Player> Died;

    private int _health = 100;

    private Animation _animation;
    private NavMeshAgent _agent;

    private bool _alive = true;

    [SerializeField]
    private Weapon _weapon;

    [SerializeField]
    private float _distance,
                  _speed,
                  _preferredDistance;

    [SerializeField]
    private PlayerController _player;

    [SerializeField]
    private Slider _healthBar;

    public string Name { get; set; }

    #endregion

    #region Methods

    private void Start()
    {
        _animation = GetComponent<Animation>();
        _agent = GetComponent<NavMeshAgent>();
    }

    [ClientRpc]
    public void DamageClientRpc(int value, NetworkBehaviourReference killer)
    {
        _health -= value;
        _healthBar.value = _health;
        _animation.Play();
        if(_health <= 0)
        {
            _alive = false;
            _agent.enabled = false;
            killer.TryGet(out Player player);
            Die(player);
        }
    }

    protected void Die(Player killer)
    {
        //Died?.Invoke(killer);
    }

    private void Update()
    {
        //if (!_alive)
        //    return;        
        
        //transform.forward = _player.transform.position - transform.position;
        //transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);

        //if(_weapon.CurrentClipAmmo.Value == 0)
        //    _weapon.ReloadServerRpc();

        //if (Vector3.Distance(_player.transform.position, transform.position) <= _distance)
        //{
        //    _weapon.Action(_weapon.transform.position, transform.forward);
        //    _agent.destination = transform.position;
        //}
        //else
        //    _agent.destination = _player.transform.position;

        //if (_weapon.IsReloading)
        //{

        //}     
    }

    #endregion
}
