using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

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

public enum SlotType
{
    Knife,
    Secondary,
    Primary,
    HEGrenade,
    FlashBang,
    SmokeGranade
}

public enum ActionMode
{
    Auto,
    Single
}

[RequireComponent(typeof(AudioSource), typeof(BoxCollider), typeof(Rigidbody))]
[RequireComponent(typeof(NetworkTransform))]
public abstract class Weapon : NetworkBehaviour, IKilleable
{
    #region Variables

    [SerializeField]
    private string _name,
                   _code;
    public string Name => _name;
    public string Code => _code;

    [SerializeField]
    private Sprite _icon;
    public Sprite Icon => _icon;

    [SerializeField]
    private int _price;
    public int Price => _price;

    [SerializeField]
    private WeaponType _weaponType;
    public WeaponType WeaponType => _weaponType;

    [SerializeField]
    protected SlotType _slotType;
    public SlotType SlotType => _slotType;

    [SerializeField]
    protected ActionMode _actionMode;
    public ActionMode ActionMode => _actionMode;

    [SerializeField]
    protected int _damage;

    [SerializeField]
    private float _ownerSpeedMultiplier,
                  _scopeValue,
                  _scopeSpeed,
                  _recoilValue,
                  _recoilDecrease,
                  _firstShotMultiplier,
                  _spreadValue,
                  _spreadDecrease,
                  _scopeRecoilMultiplier,
                  _scopeSpreadMultiplier;

    public bool IsThrowable { get; private set; } = true;

    public float OwnerSpeedMultiplier => _ownerSpeedMultiplier;
    public float ScopeValue => _scopeValue;
    public float ScopeSpeed => _scopeSpeed;
    public float RecoilValue => _recoilValue;
    public float RecoilDecrease => _recoilDecrease;
    public float FirstShotMultiplier => _firstShotMultiplier;
    public float SpreadValue => _spreadValue;
    public float SpreadDecrease => _spreadDecrease;
    public float ScopeRecoilMultiplier => _scopeRecoilMultiplier;
    public float ScopeSpreadMultiplier => _scopeSpreadMultiplier;

    protected Player _owningPlayer;

    protected AudioSource _audioSource;

    protected Collider _collider;
    public Collider Collider => _collider ? _collider : GetComponent<Collider>();

    protected Rigidbody _rigidbody;
    public Rigidbody Rigidbody => _rigidbody ? _rigidbody : GetComponent<Rigidbody>();

    protected NetworkTransform _networkTransform;
    public NetworkTransform NetworkTransform => _networkTransform ? _networkTransform : GetComponent<NetworkTransform>();

    string IKilleable.KillerText => _owningPlayer.Nickname.Value.ToString();
    Sprite IKilleable.KillerSprite => _icon;
    Teams IKilleable.KillerTeam => _owningPlayer.Team.Value;

    #endregion

    #region Methods

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _collider = GetComponent<Collider>();
        _rigidbody = GetComponent<Rigidbody>();
        _networkTransform = GetComponent<NetworkTransform>();

        OnStart();
    }

    protected virtual void OnStart() { }

    public abstract void Action(Vector3 origin, Vector3 direction);

    #endregion
}
