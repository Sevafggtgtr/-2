using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEditor.Experimental.GraphView;
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
    public static event UnityAction<PlayerController> Spawn;
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
    public Weapon[] _weapons;


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
    
    private CharacterController _controller;

    private Animator _animator;

    private bool _isActive = true;

    private Weapon _weapon;   
    public Weapon Weapon => _weapon;
    private NetworkVariable<NetworkBehaviourReference> _networkWeapon = new NetworkVariable<NetworkBehaviourReference>(writePerm : NetworkVariableWritePermission.Owner);

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
        Spawn.Invoke(this);

        if (!IsOwner)
        {
            Weapon currentWeapon;
            _networkWeapon.Value.TryGet(out currentWeapon);

            void DisableWeapon()
            {            
                for (int i = 0; i < _networkWeapons.Count; i++)
                {

                    if (_networkWeapons[i].TryGet(out Weapon weapon) && weapon != currentWeapon) ;
                    //weapon.gameObject.SetActive(false);
                    else
                    {
                        currentWeapon.gameObject.SetActive(true);
                    }
                }
            }
            _fpCamera.gameObject.SetActive(false);
            _handCamera.gameObject.SetActive(false);          
            if (_networkWeapons.Count != 0)
                DisableWeapon();
            else
            _networkWeapons.OnListChanged += (_) =>
            {
                if (currentWeapon)
                    DisableWeapon();
            };
            _networkWeapon.OnValueChanged += (o, n) =>
            {
                _networkWeapon.Value.TryGet(out currentWeapon);
                DisableWeapon();
            };
            
        }

        else
        {
            _singleton = this;

            

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            _controller = GetComponent<CharacterController>();

            _speed = _walkSpeed;

            _handCameraStartPosition = _handCamera.transform.localPosition;

            _animator = GetComponent<Animator>();

            _networkWeapons.OnListChanged += (_) =>
            {
                _weapons = new Weapon[4];
                for (int i = 0; i < _networkWeapons.Count; i++)
                {
                    if (_networkWeapons[i].TryGet(out Weapon weapon))
                    {
                        _weapons[(int)weapon.SlotType] = weapon;

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

                if (!_weapon)
                {
                    _weapon = _weapons[0];
                    _networkWeapon.Value = _weapon;
                }                
                WeaponChanged.Invoke();
                _weapon.gameObject.SetActive(true);
                _weapon.enabled = true;
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
            AddWeaponServerRpc(weapon);
        }
    }

    void DropWeapon(Weapon weapon)
    {
        weapon.gameObject.layer = 0;
        for (int i = 0; i < weapon.transform.childCount; i++)
        {
            weapon.transform.GetChild(i).gameObject.layer = 0;
            for (int j = 0; j < weapon.transform.GetChild(i).childCount; j++)
                weapon.transform.GetChild(i).GetChild(j).gameObject.layer = 0;
        }
        weapon.transform.SetParent(null);
        weapon.Collider.enabled = true;
        weapon.Rigidbody.isKinematic = false;
        weapon.enabled = false;
        weapon.Rigidbody.AddForce(_fpCamera.transform.forward * _dropForce, ForceMode.Impulse);
        _weapons[(int)weapon.SlotType] = null;

        weapon.gameObject.SetActive(true);
        RemoveWeaponServerRpc(weapon);
    }

    [ServerRpc]
    void DisableWeaponServerRpc()
    {
        DisableWeaponClientRpc();
    }

    [ClientRpc]
    void DisableWeaponClientRpc()
    {
        if (!IsOwner)
        {
            _networkWeapon.Value.TryGet(out _weapon);
            _weapon.gameObject.SetActive(false);
        }
    }

    void DisableWeapon()
    {
        _weapon.gameObject.SetActive(false);

        DisableWeaponServerRpc();
    }

    void TakeWeapon()
    {
        _weapon.gameObject.SetActive(true);       
    }

    [ServerRpc]
    void AddWeaponServerRpc(NetworkBehaviourReference weapon)
    {        
        _networkWeapons.Add(weapon);
        ChangeWeaponClientRpc(weapon, false);
        weapon.TryGet(out _weapon);
        _weapon.NetworkObject.ChangeOwnership(NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc]
    private void RemoveWeaponServerRpc (NetworkBehaviourReference weapon)
    {
        _networkWeapons.Remove(weapon);
        ChangeWeaponClientRpc(weapon, true);
    }

    [ClientRpc]
    private void ChangeWeaponClientRpc(NetworkBehaviourReference weapon, bool collider)
    {
        weapon.TryGet(out _weapon);
        _weapon.Collider.enabled = collider;
    }

    public void ChangeWeapon()
    {
        DropWeapon(_weapon);

        for (int i = 0; i < _weaponSlots.Length; i++)
            if (_weapons[i])
            {
                WeaponChange?.Invoke();
                _weapon = _weapons[i];
                _networkWeapon.Value = _weapon;
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
        if (IsOwner)
        {
            for (int i = 0; i < _networkWeapons.Count - 1; i++)
                ChangeWeapon();

            KnifeDespawnServerRpc();

            _animator.SetBool("Death_b", true);            
        }
        
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
                if (_weapons[(i * mouseScroll + (int)_weapon.SlotType + _weaponSlots.Length) % _weaponSlots.Length])
                {
                    DisableWeapon();
 
                    WeaponChange.Invoke();
                    _weapon = _weapons[(i * mouseScroll + (int)_weapon.SlotType + _weaponSlots.Length) % _weaponSlots.Length];
                    _networkWeapon.Value = _weapon;

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
                if(weapon.SlotType <= _weapon.SlotType)
                {
                    DisableWeapon();

                    WeaponChange?.Invoke();

                    _weapon = weapon;
                    _networkWeapon.Value = _weapon;

                    WeaponChanged?.Invoke();
                }
                else
                {
                    weapon.gameObject.SetActive(false);
                }   
                
                if (_weapons[(int)weapon.SlotType])
                {
                    DropWeapon(_weapons[(int)weapon.SlotType]);
                }

                AddWeaponServerRpc(weapon);
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
