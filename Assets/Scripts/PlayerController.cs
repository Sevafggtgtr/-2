using Palmmedia.ReportGenerator.Core.Reporting.Builders;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
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
    public event UnityAction<Player> Died;
    public event UnityAction Kill;

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
    private Weapon[] _weapons;
    private NetworkVariable<NetworkBehaviourReference> _networkWeapon = new NetworkVariable<NetworkBehaviourReference>(writePerm : NetworkVariableWritePermission.Owner);
    private NetworkList<NetworkBehaviourReference> _networkWeapons = new NetworkList<NetworkBehaviourReference>(writePerm: NetworkVariableWritePermission.Owner);
    

    public string Name { get; set; }

    private Player _player;
    public Player Player => _player;    
    
    public Player GetPlayer()
        =>  FindObjectsOfType<Player>().First(player => player.OwnerClientId == OwnerClientId);
    

    private void Start()
    {
        Spawn.Invoke(this);

        if (!IsOwner)
        {
            Weapon weapon;
            _networkWeapon.Value.TryGet(out Weapon currentWeapon);

                for (int i = 0; i < _networkWeapons.Count; i++)
                {
                    if (_networkWeapons[i].TryGet(out weapon) && weapon != currentWeapon)
                        weapon.gameObject.SetActive(false);
                    weapon.Collider.enabled = false;
                }

            _fpCamera.gameObject.SetActive(false);
            _handCamera.gameObject.SetActive(false);
        }

        else
        {
            _singleton = this;          

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;          

            _speed = _walkSpeed;

            _handCameraStartPosition = _handCamera.transform.localPosition;            
            
            SpawnWeaponsServerRpc();
        }

        HUD.Singleton.PauseMenu.UnPaused += () => _isActive = true;

        _controller = GetComponent<CharacterController>();

        _animator = GetComponent<Animator>();
    }

    #region Spawn Weapon
    [ServerRpc]
    private void SpawnWeaponsServerRpc()
    {
        var weapons = new Weapon[4];

        for (int i = 0; i < _weaponSlots.Length; i++)
        {
            weapons[i] = Instantiate(_weaponSlots[i].WeaponPrefab);

            weapons[i].GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);

            weapons[i].GetComponent<NetworkObject>().TrySetParent(transform);
            //AddWeaponServerRpc(weapon);//
        }

        SpawnWeaponsClientRpc(weapons.Select(weapon => (NetworkBehaviourReference)weapon).ToArray());
    }

    [ClientRpc]
    private void SpawnWeaponsClientRpc(NetworkBehaviourReference[] weapons)
    {
        if (!IsOwner)
            return;        

        _weapons = new Weapon[4];
        for (int i = 0; i < weapons.Length; i++)
        {
            if (_networkWeapons[i].TryGet(out Weapon weapon))
            {
                weapon.transform.localPosition = new Vector3(0, 0, 0);
                weapon.transform.localRotation = Quaternion.identity;
                ChangeWeaponState(weapon, true);
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

    }
    #endregion    

    private void ChangeWeaponState(Weapon weapon, bool state)
    {
        void ChangeLayer(GameObject parent)
        {
            parent.layer = (state) ? 3 : 0;
            for (int i = 0; i < parent.transform.childCount; i++)
                ChangeLayer(parent.transform.GetChild(i).gameObject);
        }          
        
        ChangeLayer(weapon.gameObject);

        weapon.Collider.enabled = !state;
        weapon.Rigidbody.isKinematic = state;
        weapon.gameObject.SetActive(!state);
        weapon.transform.SetParent(state ? _weaponPivot : null);
        _weapons[(int)weapon.SlotType] = state ? weapon : null;
        if(state)
        _networkWeapons.Add(weapon);
        else
        _networkWeapons.Remove(weapon);
    }

    private void DropWeapon(Weapon weapon)
    {              
        weapon.enabled = false;
        weapon.Rigidbody.AddForce(_fpCamera.transform.forward * _dropForce, ForceMode.Impulse);
        
        ChangeWeaponState(weapon, false);
        RemoveWeaponServerRpc(weapon);
    }

    #region Disable Weapon
    private void DisableWeapon()
    {
        _weapon.gameObject.SetActive(false);

        DisableWeaponServerRpc();
    }

    [ServerRpc]
    private void DisableWeaponServerRpc()
    {
        DisableWeaponClientRpc();
    }

    [ClientRpc]
    private void DisableWeaponClientRpc()
    {
        if (!IsOwner)
        {
            if(_networkWeapon.Value.TryGet(out _weapon))
                _weapon.gameObject.SetActive(false);
        }
    }
    #endregion   

    private void TakeWeapon()
    {
        _weapon.gameObject.SetActive(true);   
        TakeWeaponServerRpc(_weapon);
    }
    #region Take Weapon
    [ServerRpc]
    private void TakeWeaponServerRpc(NetworkBehaviourReference weapon)
    {
        TakeWeaponClientRpc(weapon);
    }

    [ClientRpc]
    private void TakeWeaponClientRpc(NetworkBehaviourReference weapon)
    {
       if(weapon.TryGet(out _weapon))
            _weapon.gameObject.SetActive(true);
    }
    #endregion
    [ServerRpc]
    private void AddWeaponServerRpc(NetworkBehaviourReference weapon)
    {               
        ChangeWeaponClientRpc(weapon, false);
        if(weapon.TryGet(out _weapon))
            _weapon.NetworkObject.ChangeOwnership(NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc]
    private void RemoveWeaponServerRpc (NetworkBehaviourReference weapon)
    {
        ChangeWeaponClientRpc(weapon, true);
    }

    #region Change Weapon
    [ClientRpc]
    private void ChangeWeaponClientRpc(NetworkBehaviourReference weapon, bool collider)
    {
        if(weapon.TryGet(out _weapon))
        {
            _weapon.Collider.enabled = collider;
            _weapon.Rigidbody.isKinematic = collider;
        }

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
    #endregion

    public override void OnNetworkDespawn()
    {
        //Despawn.Invoke();
    }

    [ServerRpc]
    private void KnifeDespawnServerRpc()
               => _weapon.NetworkObject.Despawn();

    [ClientRpc]
    public void DamageClientRpc(int value, NetworkBehaviourReference killer)
    {
        _health -= value;

        Damaged?.Invoke();

        if (_health <= 0)
        {
            killer.TryGet(out PlayerController player);
            Die(player.GetPlayer());
        }
    }

    protected void Die(Player killer)
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

    private void Update()
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

                    _weapon = weapon;
                    _networkWeapon.Value = _weapon;

                    WeaponChanged.Invoke();
                }
                else
                {
                    weapon.gameObject.SetActive(false);
                }   
                
                if (_weapons[(int)weapon.SlotType])
                {
                    DropWeapon(_weapons[(int)weapon.SlotType]);
                }

                ChangeWeaponState(weapon,true);
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
