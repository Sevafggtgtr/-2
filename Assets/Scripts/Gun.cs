using UnityEngine;
using Unity.Netcode;
using UnityEngine.Events;

public enum SlotType
{
    Primary,
    Secondary,
    Knife,
    HEGrenade,
    FlashBang,
    SmokeGranade
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
                  _shotDistance;                                                   

    [SerializeField]
    private Transform _hitPrefab,
                      _sight,
                      _pivot;

    public Transform Sight => _sight;

    private float _time;

    private bool _isShooting,
                 _isReloading,
                 _isScoping;

    public bool IsReloading => _isReloading;

    [SerializeField]
    private AudioClip _shotSound,
                      _reloadSound;

    public void Scope(bool value)
        => _isScoping = value;

    public override bool Action(Vector3 origin, Vector3 direction, Player owner)
    {
        if (_currentClipAmmo > 0 && !_isShooting && !_isReloading)
        {
            _currentClipAmmo--;
            _isShooting = true;
            _audioSource.PlayOneShot(_shotSound);

            AmmoChanged.Invoke();
            ActionServerRpc(origin, direction, owner);

            return true;
        }
        else
            return false;
    }

    [ServerRpc]
    public void ActionServerRpc(Vector3 origin, Vector3 direction, NetworkBehaviourReference owner)
    {
        if(owner.TryGet(out Player ownerObject))
        {
            RaycastHit hit;
            if (Physics.Raycast(origin, direction + Random.insideUnitSphere / 100, out hit, _shotDistance))
            {
                if (hit.transform.GetComponent<IDamageableObject>() != null)
                    hit.transform.GetComponent<IDamageableObject>().DamageServerRpc(_damage, owner, Code);
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
    }
}
