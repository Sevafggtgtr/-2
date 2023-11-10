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
using static UnityEngine.UI.GridLayoutGroup;

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
    private Weapon[] _weapons = new Weapon[4];
    public Weapon[] Weapons => _weapons;
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
        {
            Weapon weapon;

            for (int i = 0; i < _weapons.Length; i++)
            {
                if (weapons[i].TryGet(out weapon))
                {
                    weapon.gameObject.SetActive(false);
                    weapon.Collider.enabled = false;
                }                                  
            }
            if(weapons[0].TryGet(out weapon))
                weapon.gameObject.SetActive(true);

            return;
        }

        _weapons = new Weapon[4];
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i].TryGet(out Weapon weapon))
            {
                ChangeWeaponState(weapon, true);
                weapon.gameObject.SetActive(false);
            }
        }

        TakeWeapon(_weapons[0]);

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
        weapon.transform.SetParent(state ? _weaponPivot : null);
        weapon.enabled = state;
        _weapons[(int)weapon.SlotType] = state ? weapon : null;
        if (state)
        {
            _networkWeapons.Add(weapon);
            weapon.transform.localPosition = new Vector3(0, 0, 0);
            weapon.transform.localRotation = Quaternion.identity;
        }
        else
        {
            _networkWeapons.Remove(weapon);
        }       
    }

    private void DropWeapon(Weapon weapon)
    {                    
        weapon.Rigidbody.AddForce(_fpCamera.transform.forward * _dropForce, ForceMode.Impulse);

        ChangeWeaponState(weapon, false);
        weapon.gameObject.SetActive(true);
        DropWeaponServerRpc(weapon);
    }

    [ServerRpc]
    private void DropWeaponServerRpc(NetworkBehaviourReference weapon)
    {
        DropWeaponClientRpc(weapon);

        if (weapon.TryGet(out Weapon weaponObject))
            weaponObject.NetworkObject.RemoveOwnership();
    }

    [ClientRpc]
    private void DropWeaponClientRpc(NetworkBehaviourReference weapon)
    {
        if (weapon.TryGet(out Weapon weaponObject))
            weaponObject.Collider.enabled = true;
    }

    #region Disable Weapon
    private void DisableWeapon(Weapon weapon)
    {
        weapon.gameObject.SetActive(false);

        DisableWeaponServerRpc(weapon);
    }

    [ServerRpc]
    private void DisableWeaponServerRpc(NetworkBehaviourReference weapon)
    {
        DisableWeaponClientRpc(weapon);
    }

    [ClientRpc]
    private void DisableWeaponClientRpc(NetworkBehaviourReference weapon)
    {
        if (!IsOwner)
        {
            if(weapon.TryGet(out Weapon weaponObject))
                weaponObject.gameObject.SetActive(false);
        }
    }
    #endregion   

    #region Take Weapon
    private void TakeWeapon(Weapon weapon)
    {
        _weapon = weapon;
        _networkWeapon.Value = _weapon;
        _weapon.gameObject.SetActive(true);
        WeaponChanged.Invoke();
        TakeWeaponServerRpc(_weapon);
    }
    
    [ServerRpc]
    private void TakeWeaponServerRpc(NetworkBehaviourReference weapon)
    {
        TakeWeaponClientRpc(weapon);
        if (weapon.TryGet(out Weapon weaponObject))
            weaponObject.NetworkObject.ChangeOwnership(OwnerClientId);
    }

    [ClientRpc]
    private void TakeWeaponClientRpc(NetworkBehaviourReference weapon)
    {
       if (weapon.TryGet(out Weapon weaponObject))
            weaponObject.gameObject.SetActive(true);
    }
    #endregion
    

    #region Change Weapon
    public void ChangeWeapon(Weapon weapon, bool dropWeapon)
    {
        if (dropWeapon)
            DropWeapon(_weapon);
        else
            DisableWeapon(_weapon);

        TakeWeapon(weapon);

        ChangeWeaponServerRpc(_weapon, weapon, dropWeapon);
    }

    [ServerRpc]
    private void ChangeWeaponServerRpc(NetworkBehaviourReference oldWeapon, NetworkBehaviourReference newWeapon, bool dropWeapon)
    {
        ChangeWeaponClientRpc(oldWeapon,newWeapon, dropWeapon);
    }

    [ClientRpc]
    private void ChangeWeaponClientRpc(NetworkBehaviourReference oldWeapon, NetworkBehaviourReference newWeapon, bool dropWeapon)
    {
        if (oldWeapon.TryGet(out Weapon oldWeaponObject))
            if (dropWeapon)
                oldWeaponObject.Collider.enabled = true;
            else
                oldWeaponObject.gameObject.SetActive(false);
        if (newWeapon.TryGet(out Weapon newWeaponObject))
            newWeaponObject.gameObject.SetActive(true);
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

    public Weapon GetWeapon()
        => _weapons.First(weapon => weapon);

    protected void Die(Player killer)
    {
        if (IsOwner)
        {
            for (int i = 0; i < _networkWeapons.Count - 1; i++)
                DropWeapon(_weapon);

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
            for (int i = 1; i < _weapons.Length; i++)
            {
                if (_weapons[(i * mouseScroll + (int)_weapon.SlotType + _weaponSlots.Length) % _weaponSlots.Length])
                {
                    ChangeWeapon(_weapons[(i * mouseScroll + (int)_weapon.SlotType + _weaponSlots.Length) % _weaponSlots.Length],false);

                    break;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.G) && _weapon.SlotType != SlotType.Knife)
        {
            ChangeWeapon(_weapons.First(weapon => weapon && weapon != _weapon), true);       
        }
            
        if (Input.GetKeyDown(KeyCode.R) && _weapon is Gun)
            ((Gun)_weapon).Reload();

        for(int i = 0;i < _weapons.Length;i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                if (_weapons[i])
                {
                    ChangeWeapon(_weapons[i],false);
                }
                                
        }

        RaycastHit hit;
        if (Physics.Raycast(_fpCamera.transform.position, _fpCamera.transform.forward, out hit, _pickDistance))
        {
            var weapon = hit.transform.GetComponent<Gun>();
            if (weapon && Input.GetKeyDown(KeyCode.E))
            {
                if(weapon.SlotType <= _weapon.SlotType)
                {
                    ChangeWeapon(weapon, _weapons[(int)weapon.SlotType]);
                }
                else
                {
                    if (_weapons[(int)weapon.SlotType])
                    {
                        DropWeapon(_weapons[(int)weapon.SlotType]);                        
                    }
                    weapon.gameObject.SetActive(false);
                } 
                ChangeWeaponState(weapon, true);
                
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
