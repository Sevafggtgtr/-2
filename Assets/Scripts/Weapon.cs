using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(AudioSource), typeof(Rigidbody))]

public abstract class Weapon : NetworkBehaviour
{
    [SerializeField]
    protected int _damage;

    protected AudioSource _audioSource;

    [SerializeField]
    protected SlotType _slotType;
    public SlotType SlotType => _slotType;
    
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
    }

    void Update()
    {
        
    }
}
