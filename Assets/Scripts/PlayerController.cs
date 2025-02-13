using System;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public enum PlayerState
{
    Idle,
    Walk,
    Run,
    Jump,
    CrouchIdle,
    CrouchWalk
}

[RequireComponent(typeof(NetworkAudioSource))]
public class PlayerController : NetworkBehaviour, IDamageableObject
{
    public static event UnityAction<PlayerController> Spawn;
    public static event UnityAction Despawn;
    public event UnityAction<Weapon, Weapon> WeaponChanged = delegate { };
    public event UnityAction<Player, string> Died;
    public event UnityAction Kill;
    public event UnityAction<int> Damaged;

    [SerializeField]
    private PlayerControllerData _data;

    [Header("States")]
    
    private PlayerState _playerState;
    public PlayerState PlayerState => _playerState;

    [Header("Weapons")]
    private Weapon _weapon;
    public Weapon Weapon => _weapon;
    private Weapon[] _weapons = new Weapon[Enum.GetNames(typeof(SlotType)).Length];
    public Weapon[] Weapons => _weapons;    

    private NetworkVariable<NetworkBehaviourReference> _networkWeapon = new NetworkVariable<NetworkBehaviourReference>(writePerm: NetworkVariableWritePermission.Owner);
    private NetworkList<NetworkBehaviourReference> _networkWeapons = new NetworkList<NetworkBehaviourReference>(writePerm: NetworkVariableWritePermission.Owner);

    private float _recoil,
                  _spread,
                  _handCameraRecoilAngle;

    private Coroutine _recoilCoroutine,
                      _spreadCoroutine;

    [Header("Move")]
    
    private NetworkAudioSource _moveNetworkAudioSource;

    private Coroutine _moveSound;

    public Transform Arms { get; private set; }

    private bool _isScoping;
    public bool CanChangeWeapon = true;

    private float _velocity;

    private Vector3 _handCameraStartPosition;
    public Vector3 HandCameraStartPosition => _handCameraStartPosition;

    [SerializeField]
    private Camera _fpCamera,
                   _handCamera;
    public Camera FpCamera => _fpCamera;
    public Camera HandCamera => _handCamera;

    private NetworkVariable<float> _angle = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<float> Angle => _angle;

    private CharacterController _controller;

    private PlayerAnimator _model;

    private NetworkAudioSource _networkAudioSource;

    /*[HideInInspector]
    public Constraints Constraint;
    
    public enum Constraints { None, Move, MoveAndCamera}*/

    [HideInInspector]
    public bool IsActive = true;

    public string Name { get; set; }

    //private Player _player;
    //public Player Player => _player;

    private Vector3 _spawnPointPosition;

    private float _time;

    public Player GetPlayer()
        => FindObjectsByType<Player>(FindObjectsSortMode.None).First(player => player.OwnerClientId == OwnerClientId);

    [ClientRpc]
    public void SpawnPlayersClientRpc(Vector3 position)
    {
        if (IsOwner)
            _spawnPointPosition = position;

    }

    public enum Layers
    {
        Hand,
        Default
    }

    void ChangeLayer(GameObject parent, Layers layer)
    {
        parent.layer = LayerMask.NameToLayer(layer.ToString());
        for (int i = 0; i < parent.transform.childCount; i++)
            ChangeLayer(parent.transform.GetChild(i).gameObject, layer);
    }

    private void Start()
    {
        _controller = GetComponent<CharacterController>();

        _networkAudioSource = GetComponent<NetworkAudioSource>();

        //_player = GameManager.Instance.Players.First(player => player.OwnerClientId == OwnerClientId);

        if (!IsOwner)
        {
            _networkWeapon.Value.TryGet(out Weapon currentWeapon);

            foreach (var weapon in _networkWeapons)
                if(weapon.TryGet(out Weapon weaponObject) && weaponObject != currentWeapon)
                    weaponObject.gameObject.SetActive(false);
                      
            _fpCamera.gameObject.SetActive(false);
            _handCamera.gameObject.SetActive(false);
        }

        else
        {   
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            InitializeServerRpc();

            transform.position = _spawnPointPosition;
        }

        _controller.enabled = true;
    }

    public void ChangeModelState(Layers layer)
    {
        _fpCamera.enabled = _handCamera.enabled = layer switch
        {
            Layers.Hand => true,
            Layers.Default => false
        };

        foreach(Weapon weapon in _weapons)
        {
            if(weapon)
                ChangeLayer(weapon.gameObject, layer);
        }

        ChangeLayer(Arms.gameObject, layer);

        foreach (Transform child in _model.transform)
        {
            if (child.name.Contains("mesh_") && !child.name.Contains("Arms"))
            {
                child.gameObject.SetActive(layer switch
                {
                    Layers.Hand => false,
                    Layers.Default => true
                });
            }
        }
    }

    #region Initialize
    [ServerRpc]
    private void InitializeServerRpc()
    {
        _model = Instantiate(Map.Singleton.Data.SkinPackDatas.First(team => team.Team == GameManager.Instance.Player.Team.Value).GetRandomSkin(), transform);

        _model.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);

        _model.GetComponent<NetworkObject>().TrySetParent(transform);

        InitializeClientRpc(OwnerClientId, _model);

        var weapons = GameManager.Instance.GameMode.DefaultWeaponIndices;

        for (int i = 0; i < weapons.Length; i++)
            AddWeaponServerRpc(weapons[i]);
    }

    [ClientRpc]
    private void InitializeClientRpc(ulong id, NetworkBehaviourReference model)
    {
        if(id == OwnerClientId)
        {
            if (model.TryGet(out PlayerAnimator modelObject))
                _model = modelObject;

            _model.transform.localPosition = Vector3.zero;            

            _model.Initialize();

            Arms = _model.transform.Find("mesh_Arms");

            if(IsOwner)
                Spectator.Instance.Spectate(GameManager.Instance.Player);

            _handCamera.transform.SetParent(Arms, true);

            _handCameraStartPosition = _handCamera.transform.localPosition;
        }
    }
    #endregion    

    private void AddWeapon(Weapon weapon)
    {
        if (!_weapon)
            TakeWeapon(weapon);
        else
        {
            if (weapon.SlotType <= _weapon.SlotType)
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
        }
        ChangeWeaponState(weapon, WeaponStates.Take);
    }

    [Rpc(SendTo.Server)]
    public void AddWeaponServerRpc(int weaponIndex)
    {
        var weapon = Instantiate(GameManager.Instance.WeaponData.GetTeamWeaponData(GameManager.Instance.GetPlayer(OwnerClientId).Team.Value).Weapons[weaponIndex]);

        weapon.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);

        AddWeaponClientRpc(OwnerClientId, weapon);
    }

    [Rpc(SendTo.Owner)]
    private void AddWeaponClientRpc(ulong id,NetworkBehaviourReference weapon)
    {
        if (id == OwnerClientId && weapon.TryGet(out Weapon weaponObject))
            AddWeapon(weaponObject);
    }

    enum WeaponStates
    {
        Take,
        Drop
    }

    private void ChangeWeaponState(Weapon weapon, WeaponStates state)
    {
        if (IsOwner)
        {
            ChangeLayer(weapon.gameObject, state switch
            {
                WeaponStates.Take => Layers.Hand,
                WeaponStates.Drop => Layers.Default
            });

            switch (state)
            {
                case WeaponStates.Take:

                    _networkWeapons.Add(weapon);
                    break;


                case WeaponStates.Drop:

                    _networkWeapons.Remove(weapon);
                    break;

            }
        }

        weapon.Collider.enabled = weapon.NetworkTransform.enabled = state switch
        {
            WeaponStates.Take => false,
            WeaponStates.Drop => true
        };
        weapon.Rigidbody.isKinematic = state switch
        {
            WeaponStates.Take => true,
            WeaponStates.Drop => false
        };
        weapon.transform.SetParent(state switch
        {
            WeaponStates.Take => _model.Hand,
            WeaponStates.Drop => null
        });
        weapon.enabled = state switch
        {
            WeaponStates.Take => true,
            WeaponStates.Drop => false
        };
        _weapons[(int)weapon.SlotType] = state switch
        {
            WeaponStates.Take => weapon,
            WeaponStates.Drop => null
        };
        

        switch (state)
        {
            case WeaponStates.Take:

                weapon.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(0, 90, 90));
                break;
        }       
    }

    private void DropWeapon(Weapon weapon)
    {
        weapon.Rigidbody.AddForce(_fpCamera.transform.forward * _data.DropForce, ForceMode.Impulse);

        ChangeWeaponState(weapon, WeaponStates.Drop);
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
            if (weapon.TryGet(out Weapon weaponObject))
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
        _model.Animator.SetInteger("WeaponType_int", (int)weapon.WeaponType + 1);

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
        {
            weaponObject.gameObject.SetActive(true);
        }
    }
    #endregion


    #region Change Weapon
    public void ChangeWeapon(Weapon weapon, bool dropWeapon)
    {
        if(!CanChangeWeapon)
            return;

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
        ChangeWeaponClientRpc(oldWeapon, newWeapon, dropWeapon);
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

        WeaponChanged.Invoke(oldWeaponObject, newWeaponObject);
    }
    #endregion

    public override void OnNetworkDespawn()
    {
        //Despawn.Invoke();
    }

    [ServerRpc]
    private void KnifeDespawnServerRpc()
               => _weapon.NetworkObject.Despawn();
    #region Controller

    [Rpc(SendTo.Server)]
    public void DamageServerRpc(int damage, NetworkBehaviourReference source, string causeCode)
    {
        Damaged.Invoke(damage);
    }

    public void Die(Player killer, string causeCode)
    {
        if (IsOwner)
        {
            for (int i = 0; i < _networkWeapons.Count - 1; i++)
                DropWeapon(_weapon);

            KnifeDespawnServerRpc();

            _model.Animator.SetBool("Death_b", true);

            ChangeModelState(Layers.Default);

            _model.enabled = false;

            DieServerRpc();
        }

        Died.Invoke(killer, causeCode);       

        _controller.enabled = false;

        enabled = false;       
    }
    #endregion

    [ServerRpc]
    private void DieServerRpc()
    {
        DieClientRpc(OwnerClientId);
    }

    [ClientRpc]
    private void DieClientRpc(ulong id)
    {
        if(OwnerClientId == id)
        {
            _model.Animator.enabled = false;
        }
    }

    private IEnumerator Recoil()
    {
        float maxRecoil = _recoil;

        float time = 0;

        while (_recoil != 0)
        {
            _angle.Value += (_recoil - _data.WeaponAnimationCurve.Evaluate(1 - time) * maxRecoil) * _data.CameraWeaponRecoilAngleUnit;

            _recoil = _data.WeaponAnimationCurve.Evaluate(1 - time) * maxRecoil;

            time += _weapon.RecoilDecrease / maxRecoil * Time.deltaTime;

            _handCamera.transform.localRotation = Quaternion.Euler(_recoil * _data.CameraWeaponRecoilAngleUnit, 0, 0);

            //_handCamera.transform.localPosition = new Vector3(0, 0, Mathf.Clamp(_recoil * _weaponPivotRecoilOffsetUnit, 0, _weaponPivotMaxRecoilOffset));

            yield return null;
        }
    }

    private IEnumerator Spread()
    {
        float maxSpread = _spread;

        float time = 0;

        while (_spread != 0)
        {
            _spread = _data.WeaponAnimationCurve.Evaluate(1 - time) * maxSpread;

            time += _weapon.SpreadDecrease / maxSpread * Time.deltaTime;

            yield return null;
        }
    }

    private IEnumerator MoveSound()
    {
        float time = 0;

        PlaySurfaceSound(PlayerControllerData.SurfaceSound.Action.Move);

        while (time < _data.WalkSoundDuration * _data.GetPlayerStateSettings(PlayerState.Walk).Speed / _data.GetPlayerStateSettings(_playerState).Speed)
        {
            time += Time.deltaTime;

            yield return null;
        }
        _moveSound = null;
    }

    private void PlaySurfaceSound(PlayerControllerData.SurfaceSound.Action action)
    {

        if (Physics.Raycast(transform.position, -transform.up, out RaycastHit hit, _data.PickDistance))
        {
            var surface = hit.transform.GetComponent<Surface>();

            if (surface != null)
            {
                _networkAudioSource.PlayAudio(_data.SurfaceSounds.First(surfaceSound => surfaceSound.SurfaceType == surface.SurfaceType && action == surfaceSound.ActionType).Sound);
            }
        }
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        _fpCamera.transform.localPosition = Mathf.Sin(_time / _data.CameraMovePeriod * _data.GetPlayerStateSettings(_playerState).CameraMoveRate * 360 * Mathf.Deg2Rad) * _data.CameraMoveOffset;
        _time += Time.deltaTime;

        if (!_controller.isGrounded)
            _velocity += Physics.gravity.y * Time.deltaTime * 2;
        else if (_velocity < 0)
        {
            var damage = (int)_data.VelocityDamageCurve.Evaluate(Mathf.Abs(_velocity * Time.deltaTime + Physics.gravity.y * Mathf.Pow(Time.deltaTime, 2) / 2));

            if (damage > 0)
            {
                DamageServerRpc(damage, null, "death");
            }       
            _velocity = 0;
        }
            

        _controller.Move(Vector3.up * _velocity * Time.deltaTime);

        _model.Animator.SetBool("Jump_b", _controller.velocity.y == 0 ? false : true);

        if (!IsActive)
            return;

        #region Weapon
        


        if ((Input.GetMouseButtonDown(0)
                  || (Input.GetMouseButton(0) && Weapon.ActionMode == ActionMode.Auto))
                        && GameManager.Instance.IsPlayersActive.Value && _weapon.Action(_fpCamera.transform.position, _fpCamera.transform.forward, Spectator.Instance.Player))
        {
            _model.Animator.SetBool("Shoot_b", true);

            var recoil = _weapon.RecoilValue * (Input.GetMouseButtonDown(0) ? _weapon.FirstShotMultiplier : 1) * _data.GetPlayerStateSettings(_playerState).WeaponRecoilMultiplier * (_isScoping ? _weapon.ScopeRecoilMultiplier : 1);

            _angle.Value -= recoil * _data.CameraWeaponRecoilAngleUnit;

            _recoil += recoil;

            if (_recoilCoroutine != null)
                StopCoroutine(_recoilCoroutine);

            _recoilCoroutine = StartCoroutine(Recoil());

            _spread += _weapon.SpreadValue * (Input.GetMouseButtonDown(0) && PlayerState == PlayerState.Idle && PlayerState == PlayerState.CrouchIdle ? 0 : 1) * _data.GetPlayerStateSettings(_playerState).WeaponSpreadMultiplier * (_isScoping ? _weapon.ScopeSpreadMultiplier : 1);

            if (_spreadCoroutine != null)
                StopCoroutine(_spreadCoroutine);

            _spreadCoroutine = StartCoroutine(Spread());
        }

        else
            _model.Animator.SetBool("Shoot_b", false);

        _weapon.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(0, 90, 90));

        if (_weapon is Gun)
        {
            if (Input.GetMouseButton(1) && PlayerState != PlayerState.Run)
            {
                _handCamera.transform.position += (((Gun)_weapon).Sight.position - _handCamera.transform.position) * Time.deltaTime * ((Gun)_weapon).ScopeSpeed;
                _fpCamera.fieldOfView = Mathf.Lerp(_fpCamera.fieldOfView, 60 / ((Gun)_weapon).ScopeValue, Time.deltaTime * ((Gun)_weapon).ScopeSpeed);

                _isScoping = true;
            }

            else
            {
                _handCamera.transform.localPosition = Vector3.Lerp(_handCamera.transform.localPosition, HandCameraStartPosition, ((Gun)_weapon).ScopeSpeed * Time.deltaTime);
                _fpCamera.fieldOfView = Mathf.Lerp(_fpCamera.fieldOfView, 60, Time.deltaTime * ((Gun)_weapon).ScopeSpeed);

                _isScoping = false;
            }

            if (Input.GetMouseButtonDown(1))
            {

            }
            else if (Input.GetMouseButtonUp(1))
            {

            }

        }

        int mouseScroll = (int)(Input.GetAxis("Mouse ScrollWheel") * -10);
        if (mouseScroll != 0)
        {
            for (int i = 1; i < _weapons.Length; i++)
            {
                if (_weapons[(i * mouseScroll + (int)_weapon.SlotType + _weapons.Length) % _weapons.Length])
                {
                    ChangeWeapon(_weapons[(i * mouseScroll + (int)_weapon.SlotType + _weapons.Length) % _weapons.Length], false);

                    break;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.G) && _weapon.SlotType != SlotType.Knife)
        {
            ChangeWeapon(_weapons.First(weapon => weapon && weapon != _weapon), true);
        }

        if (Input.GetKeyDown(KeyCode.R) && _weapon is Gun)
        {
            ((Gun)_weapon).Reload();

            _model.Animator.SetBool("Reload_b", true);
        }

        else
            _model.Animator.SetBool("Reload_b", false);

        for (int i = 0; i < _weapons.Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                if (_weapons[i])
                {
                    ChangeWeapon(_weapons[i], false);
                }
        }

        RaycastHit hit;
        if (Physics.Raycast(_fpCamera.transform.position, _fpCamera.transform.forward, out hit, _data.PickDistance))
        {
            var weapon = hit.transform.GetComponent<Gun>();
            if (weapon && Input.GetKeyDown(KeyCode.E))
            {
                AddWeapon(weapon);
            }
        }
        #endregion

        #region Move
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            _model.Animator.SetBool("Crouch_b", true);
            _playerState = PlayerState.CrouchIdle;
            _networkAudioSource.PlayAudio(_data.CrouchSound);
        }

        if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            _model.Animator.SetBool("Crouch_b", false);
            _playerState = PlayerState.Idle;
        }

        if (GameManager.Instance.IsPlayersActive.Value)
        {
            if (Input.GetKeyDown(KeyCode.Space) && _controller.isGrounded && _playerState != PlayerState.CrouchIdle && _playerState != PlayerState.CrouchWalk)
            {
                _velocity = _data.JumpForce;
                _model.Animator.SetTrigger("Jump_trig");
                _networkAudioSource.PlayAudio(_data.JumpSound);
            }

            if (_playerState == PlayerState.Jump && _controller.isGrounded)
            {
                _playerState = PlayerState.Idle;

                PlaySurfaceSound(PlayerControllerData.SurfaceSound.Action.Land);
            }

            var moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

            if (moveDirection.magnitude > 0)
            {
                if (_moveSound == null)
                    _moveSound = StartCoroutine(MoveSound());

                if (Input.GetKey(KeyCode.LeftShift) && (_playerState == PlayerState.Walk || _playerState == PlayerState.Idle))
                {
                    _playerState = PlayerState.Run;

                    _isScoping = false;
                }
                if (_playerState == PlayerState.Idle || (Input.GetKeyUp(KeyCode.LeftShift) && (_playerState == PlayerState.Idle || _playerState == PlayerState.Run)))
                    _playerState = PlayerState.Walk;
                if (_playerState == PlayerState.CrouchIdle)
                    _playerState = PlayerState.CrouchWalk;

                _controller.Move(transform.TransformDirection
                (Vector3.ClampMagnitude(moveDirection, 1) * _data.GetPlayerStateSettings(_playerState).Speed * Time.deltaTime * _weapon.OwnerSpeedMultiplier * (_isScoping ? _data.ScopeSpeedMultiplier : 1)));

            }
            else
                _playerState = _playerState == PlayerState.CrouchWalk ? PlayerState.CrouchIdle : PlayerState.Idle;
            
            _model.Animator.SetFloat("Speed_f", new Vector3(_controller.velocity.x, 0, _controller.velocity.z).magnitude / _data.GetPlayerStateSettings(PlayerState.Walk).Speed / 4);
        }

        transform.Rotate(0, Input.GetAxis("Mouse X") * _data.Sensitivity * Time.deltaTime, 0);

        _angle.Value -= Input.GetAxis("Mouse Y") * _data.Sensitivity * Time.deltaTime;
        _angle.Value = Mathf.Clamp(_angle.Value, -90, 90);
        Arms.transform.localRotation = _fpCamera.transform.localRotation = Quaternion.Euler(_angle.Value, 0, 0);
        #endregion
    }
}
