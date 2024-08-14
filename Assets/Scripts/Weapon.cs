using UnityEngine;
using Unity.Netcode;

public enum ActionMode
{
    Auto,
    Single
}

public enum WeaponType
{
    HandGun,
    Auto,
    Auto_0,
    Shotgun,
    Rifle,
    Rifle_0,
    SMG,
    RPG,
    MiniGun,
    Grenade,
    Bow,
    Knife
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

    [SerializeField]
    private WeaponType _weaponType;
    public WeaponType WeaponType => _weaponType;

    [SerializeField]
    private float _recoilValue,
                  _recoilDecrease,
                  _firstShotMultiplier;
    public float RecoilValue => _recoilValue;
    public float RecoilDecrease => _recoilDecrease;
    public float FirstShotMultiplier => _firstShotMultiplier;

    [SerializeField]
    private float _spreadValue,
                  _spreadDecrease;
    public float SpreadValue => _spreadValue;
    public float SpreadDecrease => _spreadDecrease;

    [SerializeField]
    private float _scopeValue,
                  _scopeSpeed,
                  _scopeRecoilMultiplier,
                  _scopeSpreadMultiplier;
    public float ScopeValue => _scopeValue;
    public float ScopeSpeed => _scopeSpeed;
    public float ScopeRecoilMultiplier => _scopeRecoilMultiplier;
    public float ScopeSpreadMultiplier => _scopeSpreadMultiplier;

    [SerializeField]
    private float _ownerSpeedMultiplier;
    public float OwnerSpeedMultiplier => _ownerSpeedMultiplier;

    [SerializeField]
    private string _name;
    public string Name => _name;

    [SerializeField]
    private Sprite _icon;
    public Sprite Icon => _icon;

    protected AudioSource _audioSource;

    [SerializeField]
    private int _price;
    public int Price => _price;

    protected Collider _collider;
    public Collider Collider => _collider;

    protected Rigidbody _rigidbody;
    public Rigidbody Rigidbody => _rigidbody;

    public abstract bool Action(Vector3 origin, Vector3 direction, PlayerController owner);

    protected void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _collider = GetComponent<Collider>();
        _rigidbody = GetComponent<Rigidbody>();
    }
}
