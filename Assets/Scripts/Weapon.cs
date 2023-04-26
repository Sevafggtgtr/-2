using UnityEngine;
using Unity.Netcode;

public enum ActionMode
{
    Auto,
    Single
}

[RequireComponent(typeof(AudioSource), typeof(Rigidbody))]
public abstract class Weapon : NetworkBehaviour
{
    [SerializeField]
    protected int _damage;

    [SerializeField]
    protected ActionMode _actionMode;
    public ActionMode ActionMode => _actionMode;
    [SerializeField]
    protected SlotType _slotType;
    public SlotType SlotType => _slotType;

    protected AudioSource _audioSource;
    protected Collider _collider;
    public Collider Collider => _collider;
    protected Rigidbody _rigidbody;
    public Rigidbody Rigidbody => _rigidbody;

    public abstract void Action(Vector3 origin, Vector3 direction, NetworkBehaviourReference owner);

    public virtual void Scope(bool value)
    {

    }

    public virtual void Reload()
    {

    }

    protected void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _collider = GetComponent<Collider>();
        _rigidbody = GetComponent<Rigidbody>();
    }

    void Update()
    {
        
    }
}
