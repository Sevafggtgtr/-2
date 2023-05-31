using UnityEngine;
using Unity.Netcode;
using UnityEngine.Events;

public enum SlotType
{
    Primary,
    Secondary,
    Knife,
    Grenade
}
public enum GunType
{
    Pistol,
    SMG,
    Assault_Rifle,
    MG
}

[RequireComponent(typeof(AudioSource))]
public class Gun : Weapon
{
    public event UnityAction AmmoChanged;

    [SerializeField]
    private int _currentClipAmmo,
                _maxClipAmmo,
                _currentAmmo,
                _maxAmmo,
                _fireRate;
                        
    public int CurrentClipAmmo => _currentClipAmmo;
    public int MaxClipAmmo => _maxClipAmmo;
    public int CurrentAmmo => _currentAmmo;
    public int MaxAmmo => _maxAmmo;

    [SerializeField]
    private float _reloadTime,
                  _shotDistance,
                  _spreadAngle,
                  _scopeValue,
                  _scopeSpeed,
                  _spreadScopeValue,
                  _recoilValue,
                  _recoilDecrease;

    public float ScopeValue => _scopeValue;
    public float ScopeSpeed => _scopeSpeed;
    public float SpreadScopeValue => _spreadScopeValue;

    [System.Serializable]
    private struct Spread
    {   [SerializeField]
        private PlayerState _playerState;
        public PlayerState PlayerState => _playerState;
        [SerializeField]
        private float _value;
        public float Value => _value;
    }
    [SerializeField]
    private Spread[] _spreads;

    [SerializeField]
    private Transform _hitPrefab,
                      _sight,
                      _pivot;

    public Transform Sight => _sight;

    private float _time,
                  _spread,
                  _recoil;

    public float GetSpread() => _spread;

    public void SetSpread(float spread)
       => _spread = spread;

    private Vector3 _recoilVelocity;

    private bool _isShooting,
                 _isReloading,
                 _isScoping;

    public bool IsReloading => _isReloading;

    [SerializeField]
    private AudioClip _shotSound,
                      _reloadSound;

    [SerializeField]
    private GunType _gunType;

    void Start()
    {       
        _spread = _spreadAngle;
    }

    public void Scope(bool value)
        => _isScoping = value;

    public override void Action(Vector3 origin, Vector3 direction, NetworkBehaviourReference owner)
    {
        if (_currentClipAmmo > 0 && !_isShooting && !_isReloading)
        {
            _recoilVelocity += new Vector3(Random.Range(0, _recoilValue), Random.Range(-_recoilValue, _recoilValue) / 2);

            _currentClipAmmo--;
            _isShooting = true;
            _audioSource.PlayOneShot(_shotSound);

            AmmoChanged.Invoke();
            ActionServerRpc(origin, direction, owner);
        }
    }

    [ServerRpc]
    public void ActionServerRpc(Vector3 origin, Vector3 direction, NetworkBehaviourReference owner)
    {
        if(owner.TryGet(out PlayerController ownerObject))
        {
            RaycastHit hit;
            if (Physics.Raycast(origin, direction + Random.insideUnitSphere / 100 * _spread, out hit, _shotDistance))
            {
                if (hit.transform.GetComponent<IDamageableObject>() != null)
                    hit.transform.GetComponent<IDamageableObject>().DamageClientRpc(_damage, ownerObject.Name);
                else
                {
                    var bulletHit = Instantiate(_hitPrefab);
                    bulletHit.position = hit.point + hit.normal * .001f;
                    bulletHit.forward = -hit.normal;
                    bulletHit.GetComponent<NetworkObject>().Spawn();
                }
            }

            ActionClientRpc();
        }       
    }

    [ClientRpc]
    private void ActionClientRpc()
    {    
        _audioSource.PlayOneShot(_shotSound);       
    }

    public void Reload()
    {
        if (!_isReloading && !_isShooting && _currentClipAmmo < _maxClipAmmo && _currentAmmo > 0)
        {
            _isReloading = true;
            _audioSource.PlayOneShot(_reloadSound);
        }      
    }
   
    void Update()
    {
        if (!IsOwner)
            return;

        if (_isShooting)
        {
            _time += Time.deltaTime;
            if (_time >= 60f / _fireRate)
            {
                _isShooting = false;
                _time = 0;
            }
        }
        if (_isReloading)
        {
            _time += Time.deltaTime;
            if (_time >= _reloadTime)
            {
                int ammo = Mathf.Min(_currentAmmo, _maxClipAmmo - _currentClipAmmo);
                _currentClipAmmo += ammo;
                _currentAmmo -= ammo;
                _time = 0;
                _isReloading = false;
                AmmoChanged?.Invoke();
            }           
        }
      
        transform.localEulerAngles = _recoilVelocity;
        _recoilVelocity = Vector3.Lerp(_recoilVelocity, Vector3.zero, _recoilDecrease * Time.deltaTime);
    }
}
