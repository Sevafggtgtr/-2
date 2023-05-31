using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
public enum PlayerState
{
    Idle,
    Walk,
    Run,
    Jump,
    Zoom
}

public class PlayerController : NetworkBehaviour, IDamageableObject
{
    public static event UnityAction Spawn;
    public static event UnityAction Despawn;
    public event UnityAction WeaponChange;
    public event UnityAction WeaponChanged;
    public event UnityAction<string> Died;

    public event UnityAction Damaged;

    private static PlayerController _singleton;
    public static PlayerController Singleton => _singleton;

    [System.Serializable]
    private struct WeaponSlot
    {
        [SerializeField]
        private SlotType _slotType;
        public SlotType SlotType => _slotType;
        [SerializeField]
        private Weapon _weaponPrefab;
        public Weapon WeaponPrefab => _weaponPrefab;
    }
    [SerializeField]
    private WeaponSlot[] _weaponSlots;

    [SerializeField]
    private float _walkSpeed,
                  _runSpeed,
                  _sensitivity,
                  _pickDistance,
                  _dropForce,
                  _jumpForce;

    [SerializeField]
    private Transform _head,
                      _weaponPivot;

    private float _velocity,
                  _cameraVelocity;

    private NetworkList<NetworkBehaviourReference> _networkWeapons = new NetworkList<NetworkBehaviourReference>();
    private List<Weapon> _weapons = new List<Weapon>();


    private PlayerState _playerState;
    public PlayerState PlayerState => _playerState;

    private Vector3 _handCameraStartPosition;
    public Vector3 HandCameraStartPosition => _handCameraStartPosition;

    [SerializeField]
    private Camera _fpCamera,
                   _handCamera;
    public Camera FpCamera => _fpCamera;
    public Camera HandCamera => _handCamera;

    private float _angle,
                  _speed;

    private int _health = 100;
    public int Health => _health;

    private int _weaponIndex;

    private CharacterController _controller;

    private Animator _animator;

    private bool _isActive = true;

    private Weapon _weapon;
    public Weapon Weapon => _weapon;

    public string Name { get; set; }

    [ClientRpc]
    public void DamageClientRpc(int value, string killer)
    {
        _health -= value;
        
        Damaged?.Invoke();
        
        if (_health <= 0)
        {
            Die(killer);
        }
    }

    private void Start()
    {
        _singleton = this;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _controller = GetComponent<CharacterController>();

        _speed = _walkSpeed;

        _handCameraStartPosition = _handCamera.transform.localPosition;

        _animator = GetComponent<Animator>();

        if (!IsOwner)
        {
            _fpCamera.gameObject.SetActive(false);
            _handCamera.gameObject.SetActive(false);

        }

        else
        {
            Spawn.Invoke();

            _networkWeapons.OnListChanged += (_) =>
            {
                _weapons = new List<Weapon>(_networkWeapons.Count);
                for (int i = 0; i < _networkWeapons.Count; i++)
                {
                    if (_networkWeapons[i].TryGet(out Weapon weapon))
                    {
                        _weapons.Add(weapon);

                        weapon.gameObject.layer = 3;
                        for (int j = 0; j < weapon.transform.childCount; j++)
                        {
                            weapon.transform.GetChild(j).gameObject.layer = 3;
                            for (int k = 0; k < weapon.transform.GetChild(j).childCount; k++)
                                weapon.transform.GetChild(j).GetChild(k).gameObject.layer = 3;
                        }

                        weapon.transform.SetParent(_weaponPivot);
                        weapon.transform.localPosition = new Vector3(0, 0, 0);
                        weapon.transform.localRotation = Quaternion.identity;
                        weapon.gameObject.SetActive(false);

                        weapon.Rigidbody.isKinematic = true;
                        weapon.Collider.enabled = false;
                    }
                }

                _weapon = _weapons[0];
                WeaponChanged.Invoke();
                _weapon.gameObject.SetActive(true);
            };
            SpawnWeaponsServerRpc();           
        }

        HUD.Singleton.PauseMenu.UnPaused += () => _isActive = true;        
    }

    [ServerRpc]
    private void SpawnWeaponsServerRpc()
    {
        for (int i = 0; i < _weaponSlots.Length; i++)
        {
            var weapon = Instantiate(_weaponSlots[i].WeaponPrefab);

            weapon.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);

            weapon.GetComponent<NetworkObject>().TrySetParent(transform);
            _networkWeapons.Add(weapon);
        }
    }

    void DropWeapon()
    {
        _weapon.gameObject.layer = 0;
        for (int i = 0; i < _weapon.transform.childCount; i++)
        {
            _weapon.transform.GetChild(i).gameObject.layer = 0;
            for (int j = 0; j < _weapon.transform.GetChild(i).childCount; j++)
                _weapon.transform.GetChild(i).GetChild(j).gameObject.layer = 0;
        }
        _weapon.transform.SetParent(null);
        _weapon.Collider.enabled = true;
        _weapon.Rigidbody.isKinematic = false;
        _weapon.enabled = false;
        _weapons[_weaponIndex] = null;
        _weapon.Rigidbody.AddForce(_fpCamera.transform.forward * _dropForce, ForceMode.Impulse);
    }
    void RemoveWeapon()
    {
        _weapon.gameObject.SetActive(false);
    }
    void TakeWeapon()
    {
        _weapon.gameObject.SetActive(true);
    }

    public void ChangeWeapon()
    {
        DropWeapon();

        for (int i = 0; i < _weaponSlots.Length; i++)
            if (_weapons[i])
            {
                WeaponChange?.Invoke();
                _weapon = _weapons[i];
                _weaponIndex = i;
                WeaponChanged?.Invoke();

                break;
            }

        TakeWeapon();
    }

    public override void OnNetworkDespawn()
    {
        //Despawn.Invoke();
    }

    [ServerRpc]
    void KnifeDespawnServerRpc()
               => _weapon.NetworkObject.Despawn();

    protected void Die(string killer)
    {
        for(int i = 0; i < _weaponSlots.Length -1; i++)
            ChangeWeapon();

        KnifeDespawnServerRpc();

        _animator.SetBool("Death_b", true);

        Died.Invoke(killer);

        _controller.enabled = false;

        enabled = false;

    }

    void Update()
    {
        if(!IsOwner)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _isActive = !_isActive;
        }

        if (!_controller.isGrounded)
            _velocity += Physics.gravity.y * Time.deltaTime * 2;
        else if (_velocity < -0.001f)
            _velocity = -0.001f;

        _controller.Move(Vector3.up * _velocity * Time.deltaTime);

        if (!_isActive)
            return;

        if ((Input.GetMouseButtonDown(0))
                    || (Input.GetMouseButton(0)))
            _weapon.Action(_fpCamera.transform.position, _fpCamera.transform.forward, this);

        if (_weapon is Gun)
        {
            if (Input.GetMouseButton(1))
            {

                _handCamera.transform.position += (((Gun)_weapon).Sight.position - _handCamera.transform.position) * Time.deltaTime * ((Gun)_weapon).ScopeSpeed;
                _fpCamera.fieldOfView = Mathf.Lerp(_fpCamera.fieldOfView, 60 / ((Gun)_weapon).ScopeValue, Time.deltaTime * ((Gun)_weapon).ScopeSpeed);
                ((Gun)_weapon).SetSpread(Mathf.Lerp(((Gun)_weapon).GetSpread(), ((Gun)_weapon).SpreadScopeValue, Mathf.InverseLerp(60, 60 / ((Gun)_weapon).ScopeValue, _fpCamera.fieldOfView)));
            }

            else
            {
                _handCamera.transform.localPosition = Vector3.Lerp(_handCamera.transform.localPosition, HandCameraStartPosition, ((Gun)_weapon).ScopeSpeed * Time.deltaTime);
                _fpCamera.fieldOfView = Mathf.Lerp(_fpCamera.fieldOfView, 60, Time.deltaTime * ((Gun)_weapon).ScopeSpeed);
                ((Gun)_weapon).SetSpread(Mathf.Lerp(((Gun)_weapon).SpreadScopeValue, ((Gun)_weapon).GetSpread(), Mathf.InverseLerp(60 / ((Gun)_weapon).ScopeValue, 60, _fpCamera.fieldOfView)));
            }
        }

        int mouseScroll = (int)(Input.GetAxis("Mouse ScrollWheel") * -10);
        if (mouseScroll != 0)
        {
            for (int i = 1; i < _weaponSlots.Length; i++)
            {
                if (_weapons[(i * mouseScroll + _weaponIndex + _weaponSlots.Length) % _weaponSlots.Length])
                {
                    RemoveWeapon();

                    _weaponIndex = (i * mouseScroll + _weaponIndex + _weaponSlots.Length) % _weaponSlots.Length;
                    WeaponChange.Invoke();
                    _weapon = _weapons[_weaponIndex];

                    TakeWeapon();

                    WeaponChanged.Invoke();

                    break;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.G) && _weapon.SlotType != SlotType.Knife)
        {
            ChangeWeapon();
        }

        if (Input.GetKeyDown(KeyCode.R) && _weapon is Gun)
            ((Gun)_weapon).Reload();

        RaycastHit hit;
        if (Physics.Raycast(_fpCamera.transform.position, _fpCamera.transform.forward, out hit, _pickDistance))
        {
            var weapon = hit.transform.GetComponent<Gun>();
            if (weapon && Input.GetKeyDown(KeyCode.E))
            {
                RemoveWeapon();

                for (int i = 0; i < _weaponSlots.Length; i++)
                    if (_weaponSlots[i].SlotType == weapon.SlotType)
                    {
                        if (_weapons[i])
                        {
                            _weapon = _weapons[i];

                            DropWeapon();
                        }

                        _weapons[i] = weapon;
                        _weaponIndex = i;

                        break;
                    }
                WeaponChange?.Invoke();

                _weapon = weapon;

                WeaponChanged?.Invoke();
                _weapon.gameObject.layer = 3;
                for (int i = 0; i < _weapon.transform.childCount; i++)
                {
                    _weapon.transform.GetChild(i).gameObject.layer = 3;
                    for (int j = 0; j < _weapon.transform.GetChild(i).childCount; j++)
                        _weapon.transform.GetChild(i).GetChild(j).gameObject.layer = 3;
                }


                Destroy(_weapon.gameObject.GetComponent<Rigidbody>());
                hit.collider.enabled = false;
                _weapon.transform.SetParent(_weaponPivot);

                _weapon.transform.localPosition = Vector3.zero;
                _weapon.transform.localRotation = Quaternion.identity;
                _weapon.enabled = true;
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftShift))
            _speed = _runSpeed;
        if (Input.GetKeyUp(KeyCode.LeftShift))
            _speed = _walkSpeed;

        if (Input.GetKeyDown(KeyCode.Space) && _controller.isGrounded)
            _velocity = _jumpForce;        

        _controller.Move(transform.TransformDirection
            (Vector3.ClampMagnitude(new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")), 1) * _speed * Time.deltaTime));

        transform.Rotate(0, Input.GetAxis("Mouse X") * _sensitivity * Time.deltaTime, 0);
           
        _angle -= Input.GetAxis("Mouse Y") * _sensitivity * Time.deltaTime;
        _angle = Mathf.Clamp(_angle, -90, 90);
        _head.localRotation = Quaternion.Euler(0, _angle, 0);        
    }
}
