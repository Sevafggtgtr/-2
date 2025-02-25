using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]
public class Gun : Weapon
{
    #region Variables

    public event UnityAction AmmoChanged = delegate { };

    [SerializeField]
    private int _maxAmmo,
                _maxClipAmmo,
                _fireRate;

    public int MaxAmmo => _maxAmmo;
    public int MaxClipAmmo => _maxClipAmmo;

    private NetworkVariable<int> _currentAmmo = new NetworkVariable<int>();
    private NetworkVariable<int> _currentClipAmmo = new NetworkVariable<int>();

    public int CurrentAmmo => _currentAmmo.Value;
    public int CurrentClipAmmo => _currentClipAmmo.Value;

    [SerializeField]
    private float _shotDistance,
                  _reloadTime;

    [SerializeField]
    private NetworkObject _hitPrefab;

    [SerializeField]
    private Transform _sight,
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

    #endregion

    #region Methods

    public void Scope(bool value)
        => _isScoping = value;

    public override void Action(Vector3 origin, Vector3 direction)
    {
        ActionServerRpc(origin, direction);
    }

    [ServerRpc]
    private void ActionServerRpc(Vector3 origin, Vector3 direction)
    {
        if (_currentClipAmmo.Value > 0 && !_isShooting && !_isReloading)
        {
            _isShooting = true;
            _currentClipAmmo.Value--;
            _audioSource.PlayOneShot(_shotSound);

            if (Physics.Raycast(origin, direction + Random.insideUnitSphere / 100, out RaycastHit hit, _shotDistance))
            {
                if (hit.transform.GetComponent<IDamageable>() != null)
                    hit.transform.GetComponent<IDamageable>().DamageServerRpc(_damage, _owningPlayer);
                else
                {
                    var bulletHit = Instantiate(_hitPrefab);
                    bulletHit.transform.position = hit.point + hit.normal * .001f;
                    bulletHit.transform.forward = -hit.normal;
                    bulletHit.Spawn();
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

    [ServerRpc]
    public void ReloadServerRpc()
    {
        if (!_isReloading && !_isShooting && _currentClipAmmo.Value < _maxClipAmmo && _currentAmmo.Value > 0)
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
                var ammo = Mathf.Min(_currentAmmo.Value, _maxClipAmmo - _currentClipAmmo.Value);
                _currentAmmo.Value -= ammo;
                _currentClipAmmo.Value += ammo;
                _time = 0;
                _isReloading = false;
                AmmoChanged.Invoke();
            }
        }
    }

    #endregion
}
