using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]
public class Gun : Weapon
{
    #region Variables

    [SerializeField]
    private int _ammo,
                _clipAmmo,
                _fireRate;

    public int Ammo => _ammo;
    public int ClipAmmo => _clipAmmo;

    private NetworkVariable<int> _currentAmmo = new NetworkVariable<int>();
    public NetworkVariable<int> CurrentAmmo => _currentAmmo;
    private NetworkVariable<int> _currentClipAmmo = new NetworkVariable<int>();
    public NetworkVariable<int> CurrentClipAmmo => _currentClipAmmo;

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

    protected override void OnStart()
    {
        _currentAmmo.Value = _ammo;
        _currentClipAmmo.Value = _clipAmmo;
    }

    public void Scope(bool value)
        => _isScoping = value;

    [ServerRpc]
    public override void ActivateServerRpc(Vector3 origin, Vector3 direction)
    {
        if (_currentClipAmmo.Value > 0 && !_isShooting && !_isReloading)
        {
            _isShooting = true;
            _currentClipAmmo.Value--;
            _audioSource.PlayOneShot(_shotSound);

            if (Physics.Raycast(origin, direction + Random.insideUnitSphere / 100, out RaycastHit hit, _shotDistance))
            {
                if (hit.transform.GetComponent<IDamageable>() != null)
                    hit.transform.GetComponent<IDamageable>().DamageServerRpc(_damage, this);
                else
                {
                    var bulletHit = Instantiate(_hitPrefab);
                    bulletHit.transform.position = hit.point + hit.normal * .001f;
                    bulletHit.transform.forward = -hit.normal;
                    bulletHit.Spawn();
                }
            }

            ActivateClientRpc();
            GameManager.Instance.GetPlayer(OwnerClientId).TryGetController().ShootClientRpc();
        }
    }

    [ClientRpc]
    private void ActivateClientRpc()
    {
        _audioSource.PlayOneShot(_shotSound);
    }

    [ServerRpc]
    public void ReloadServerRpc()
    {
        if (!_isReloading && !_isShooting && _currentClipAmmo.Value < _clipAmmo && _currentAmmo.Value > 0)
        {
            _isReloading = true;
            _audioSource.PlayOneShot(_reloadSound);
        }
    }

    private void Update()
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
                var ammo = Mathf.Min(_currentAmmo.Value, _clipAmmo - _currentClipAmmo.Value);
                _currentAmmo.Value -= ammo;
                _currentClipAmmo.Value += ammo;
                _time = 0;
                _isReloading = false;
            }
        }
    }

    #endregion
}
