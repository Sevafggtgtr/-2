using System;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public enum PlayerControllerStates
{
    Idle,
    Walk,
    Run,
    Jump,
    CrouchIdle,
    CrouchWalk
}

[RequireComponent(typeof(CharacterController), typeof(NetworkAudioSource))]
public class PlayerController : NetworkBehaviour, IDamageable
{
    #region Variables

    [SerializeField]
    private PlayerControllerData _data;

    #region Events

    public event UnityAction Kill = delegate { };
    public event UnityAction Damaged = delegate { };
    public event UnityAction<Player> Died = delegate { };

    public event UnityAction<Weapon, Weapon> WeaponChanged = delegate { };

    #endregion

    private NetworkVariable<int> _health = new NetworkVariable<int>(100);
    public NetworkVariable<int> Health => _health;

    #region Movement

    private NetworkVariable<PlayerControllerStates> _state = new NetworkVariable<PlayerControllerStates>();
    //public PlayerState PlayerState => _playerState;

    public Transform Arms { get; private set; }

    private bool _isScoping;

    private float _velocity,
                  _weaponRecoilMultiplier;

    [SerializeField]
    private Camera _fpCamera,
                   _handCamera;
    public Camera FpCamera => _fpCamera;
    public Camera HandCamera => _handCamera;

    private Vector3 _handCameraStartPosition;
    public Vector3 HandCameraStartPosition => _handCameraStartPosition;

    private NetworkVariable<float> _angle = new NetworkVariable<float>();
    public NetworkVariable<float> Angle => _angle;

    #endregion

    #region Weapons

    private NetworkVariable<NetworkBehaviourReference> _weapon = new NetworkVariable<NetworkBehaviourReference>();
    public NetworkVariable<NetworkBehaviourReference> Weapon => _weapon;
    public Weapon TryGetWeapon() => _weapon.Value.TryGet(out Weapon weapon) ? weapon : null;
    
    private NetworkList<NetworkBehaviourReference> _weapons = new NetworkList<NetworkBehaviourReference>();
    private Weapon[] TryGetWeapons()
    {
        var weapons = new Weapon[_weapons.Count];
        for (int i = 0; i < weapons.Length; i++)
            if (_weapons[i].TryGet(out Weapon weapon))
                weapons[i] = weapon;
        return weapons.OrderBy(weapon => weapon.SlotType).ToArray();
    }

    private NetworkVariable<bool> _canChangeWeapon = new NetworkVariable<bool>(true);
    public NetworkVariable<bool> CanChangeWeapon => _canChangeWeapon;

    #endregion

    //public enum Constraints { None, Move, MoveAndCamera }*/
    //[HideInInspector]
    //public Constraints Constraint;

    private float _time,
                  _recoil,
                  _spread,
                  _handCameraRecoilAngle;

    private Coroutine _recoilCoroutine,
                      _spreadCoroutine,
                      _moveAudioCoroutine;

    private CharacterController _controller;
    private NetworkAudioSource _networkAudioSource;
    private PlayerModel _model;
    private AudioListener _audioListener;

    #endregion

    #region Methods

    public enum Layers
    {
        Hand,
        Default
    }

    private void ChangeLayer(GameObject parent, Layers layer)
    {
        parent.layer = LayerMask.NameToLayer(layer.ToString());
        for (int i = 0; i < parent.transform.childCount; i++)
            ChangeLayer(parent.transform.GetChild(i).gameObject, layer);
    }

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        _networkAudioSource = GetComponent<NetworkAudioSource>();
        _audioListener = GetComponentInChildren<AudioListener>();

        if (!IsOwner)
        {
            if (_weapon.Value.TryGet(out Weapon currentWeapon))
            {
                foreach (var weapon in _weapons)
                    if (weapon.TryGet(out Weapon weaponObject) && weaponObject != currentWeapon)
                        weaponObject.gameObject.SetActive(false);
            }

            _fpCamera.gameObject.SetActive(false);
            _handCamera.gameObject.SetActive(false);
        }
        else
        {
            InitializeServerRpc();
        }

        _controller.enabled = true;
    }

    public void ChangeModelState(Layers layer)
    {
        _fpCamera.enabled = _handCamera.enabled = _audioListener.enabled = layer switch
        {
            Layers.Hand => true,
            Layers.Default => false
        };

        foreach (var weapon in _weapons)
        {
            if (weapon.TryGet(out Weapon weaponObject))
                ChangeLayer(weaponObject.gameObject, layer);
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
        var team = GameManager.Instance.GetPlayer(OwnerClientId).Team.Value;
        var defaultWeapons = GameManager.Instance.WeaponData.GetTeamWeaponData(team).Weapons;

        _model = Instantiate(Map.Singleton.Data.SkinPackDatas.First(skinPackData => skinPackData.Team == team).GetRandomSkin(), transform);
        _model.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
        _model.GetComponent<NetworkObject>().TrySetParent(transform);

        var weapons = new NetworkBehaviourReference[GameManager.Instance.GameMode.DefaultWeaponIndices.Length];
        for (int i = 0; i < weapons.Length; i++)
        {
            var weapon = Instantiate(defaultWeapons[GameManager.Instance.GameMode.DefaultWeaponIndices[i]]);
            weapon.NetworkObject.SpawnWithOwnership(OwnerClientId);
            weapon.NetworkObject.TrySetParent(transform);
            weapons[i] = weapon;
        }

        InitializeClientRpc(OwnerClientId, _model, weapons);
    }

    [ClientRpc]
    private void InitializeClientRpc(ulong id, NetworkBehaviourReference model, NetworkBehaviourReference[] weapons)
    {
        if (OwnerClientId == id)
        {
            if (model.TryGet(out PlayerModel modelObject))
                _model = modelObject;

            _model.Initialize();

            Arms = _model.transform.Find("mesh_Arms");

            _handCamera.transform.SetParent(Arms, true);
            _handCameraStartPosition = _handCamera.transform.localPosition;

            if (IsOwner)
            {
                Spectator.Instance.Spectate(GameManager.Instance.Player);
                for (int i = 0; i < weapons.Length; i++)
                {
                    ChangeWeaponStateServerRpc(weapons[i], ChangeWeaponStates.Take);
                    if (i != weapons.Length - 1 && weapons[i].TryGet(out Weapon weapon))
                        weapon.gameObject.SetActive(false);
                    else
                        SelectWeaponServerRpc(weapons[i]);
                }
            }
        }
    }

    #endregion

    #region Movement

    [ServerRpc]
    private void MoveServerRpc(Vector3 velocity)
    {
        _controller.Move(transform.TransformDirection(velocity) * Time.deltaTime);

        _model.Animator.SetFloat("Speed_f", new Vector3(_controller.velocity.x, 0, _controller.velocity.z).magnitude / _data.GetStateSettings(PlayerControllerStates.Walk).SpeedMultiplier / 4);
    }

    [ServerRpc]
    private void RotateServerRpc(Vector2 rotation)
    {
        transform.Rotate(0, rotation.y * Time.deltaTime, 0);

        _angle.Value -= rotation.x * Time.deltaTime;
        _angle.Value = Mathf.Clamp(_angle.Value, -90, 90);
    }

    [ServerRpc]
    private void ChangeStateServerRpc(PlayerControllerStates state)
    {
        _state.Value = state;

        IEnumerator Coroutine()
        {
            var t = 0f;

            var a = _weaponRecoilMultiplier;

            var b = _data.GetStateSettings(_state.Value).WeaponRecoilMultiplier;

            while (_weaponRecoilMultiplier != b)
            {
                _weaponRecoilMultiplier = Mathf.Lerp(a, b, t);

                t += Time.deltaTime / 1;

                yield return null;
            }         
        }
        StartCoroutine(Coroutine());
    }

    [ServerRpc]
    private void JumpServerRpc()
    {
        if (_controller.isGrounded)
        {
            _velocity = _data.JumpHeight - Physics.gravity.y / 2;
            _networkAudioSource.PlayAudio(_data.JumpSound);
            _model.Animator.SetTrigger("Jump_trig");
        }
    }

    #endregion

    #region Weapons

    #region Methods

    #region Change State

    public enum ChangeWeaponStates
    {
        Take,
        Drop
    }

    [ServerRpc]
    public void ChangeWeaponStateServerRpc(NetworkBehaviourReference weapon, ChangeWeaponStates state)
    {
        if (weapon.TryGet(out Weapon weaponObject))
        {
            switch (state)
            {
                case ChangeWeaponStates.Take:

                    _weapons.Add(weaponObject);

                    weaponObject.NetworkObject.ChangeOwnership(OwnerClientId);
                    weaponObject.NetworkObject.TrySetParent(transform);

                    break;

                case ChangeWeaponStates.Drop:

                    _weapons.Remove(weaponObject);

                    weaponObject.NetworkObject.RemoveOwnership();
                    weaponObject.NetworkObject.TryRemoveParent();

                    if (weaponObject.IsThrowable)
                        weaponObject.Rigidbody.AddForce(_fpCamera.transform.forward * _data.DropForce, ForceMode.Impulse);

                    break;
            }
        }

        ChangeWeaponStateClientRpc(OwnerClientId, weapon, state);
    }

    [ClientRpc]
    private void ChangeWeaponStateClientRpc(ulong id, NetworkBehaviourReference weapon, ChangeWeaponStates state)
    {
        if (OwnerClientId == id && weapon.TryGet(out Weapon weaponObject))
        {
            if (IsOwner)
            {
                ChangeLayer(weaponObject.gameObject, state switch
                {
                    ChangeWeaponStates.Take => Layers.Hand,
                    ChangeWeaponStates.Drop => Layers.Default
                });
            }

            weaponObject.Collider.enabled = weaponObject.NetworkTransform.enabled = state switch
            {
                ChangeWeaponStates.Take => false,
                ChangeWeaponStates.Drop => true
            };
            weaponObject.enabled = state switch
            {
                ChangeWeaponStates.Take => true,
                ChangeWeaponStates.Drop => false
            };
            weaponObject.Rigidbody.isKinematic = state switch
            {
                ChangeWeaponStates.Take => true,
                ChangeWeaponStates.Drop => false
            };
            weaponObject.transform.SetParent(state switch
            {
                ChangeWeaponStates.Take => _model.Hand,
                ChangeWeaponStates.Drop => null
            });
            if(state == ChangeWeaponStates.Take)
                weaponObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(0, 90, 90));
        }
    }

    //private void ChangeWeaponState(Weapon weapon, ChangeWeaponStates state)
    //{
    //    if (OwnerClientId == id && weapon.TryGet(out Weapon weaponObject))
    //    {
    //        if (IsOwner)
    //        {
    //            ChangeLayer(weaponObject.gameObject, state switch
    //            {
    //                ChangeWeaponStates.Take => Layers.Hand,
    //                ChangeWeaponStates.Drop => Layers.Default
    //            });
    //        }

    //        weaponObject.Collider.enabled = weaponObject.NetworkTransform.enabled = state switch
    //        {
    //            ChangeWeaponStates.Take => false,
    //            ChangeWeaponStates.Drop => true
    //        };
    //        weaponObject.enabled = state switch
    //        {
    //            ChangeWeaponStates.Take => true,
    //            ChangeWeaponStates.Drop => false
    //        };
    //        weaponObject.Rigidbody.isKinematic = state switch
    //        {
    //            ChangeWeaponStates.Take => true,
    //            ChangeWeaponStates.Drop => false
    //        };
    //        weaponObject.transform.SetParent(state switch
    //        {
    //            ChangeWeaponStates.Take => _model.Hand,
    //            ChangeWeaponStates.Drop => null
    //        });
    //        //weaponObject.transform.localPosition = state switch
    //        //{
    //        //    ChangeWeaponStates.Take => Vector3.zero,
    //        //    ChangeWeaponStates.Drop => Vector3.zero
    //        //};
    //    }
    //}

    #endregion

    #region Select

    [ServerRpc]
    public void SelectWeaponServerRpc(NetworkBehaviourReference weapon)
    {
        if (weapon.TryGet(out Weapon weaponObject))
            _model.Animator.SetFloat("WeaponType_f", (int)weaponObject.SlotType);
        var previousWeapon = TryGetWeapon();
        _weapon.Value = weapon;
        SelectWeaponClientRpc(OwnerClientId, previousWeapon, weapon);
    }

    [ClientRpc]
    private void SelectWeaponClientRpc(ulong id, NetworkBehaviourReference previousWeapon, NetworkBehaviourReference newWeapon)
    {
        if (OwnerClientId == id)
        {
            if (previousWeapon.TryGet(out Weapon previousWeaponObject))
                previousWeaponObject.gameObject.SetActive(false);

            if (newWeapon.TryGet(out Weapon newWeaponObject))
                newWeaponObject.gameObject.SetActive(true);

            WeaponChanged.Invoke(previousWeaponObject, newWeaponObject);
        }
    }

    #endregion

    #region Shoot

    [ClientRpc]
    public void ShootClientRpc()
    {
        _model.Animator.SetTrigger("Shoot_t");

        var weapon = TryGetWeapon();

        var recoil = weapon.RecoilValue * (Input.GetMouseButtonDown(0) ? weapon.FirstShotMultiplier : 1) * _data.GetStateSettings(_state.Value).WeaponRecoilMultiplier * (_isScoping ? weapon.ScopeRecoilMultiplier : 1);
        if(IsServer)
            _angle.Value -= recoil;
        _recoil += recoil;
        if (_recoilCoroutine != null)
            StopCoroutine(_recoilCoroutine);
        _recoilCoroutine = StartCoroutine(Recoil());

        _spread += weapon.SpreadValue * (Input.GetMouseButtonDown(0) && _state.Value == PlayerControllerStates.Idle && _state.Value == PlayerControllerStates.CrouchIdle ? 0 : 1) * _data.GetStateSettings(_state.Value).WeaponSpreadMultiplier * (_isScoping ? weapon.ScopeSpreadMultiplier : 1);
        if (_spreadCoroutine != null)
            StopCoroutine(_spreadCoroutine);
        _spreadCoroutine = StartCoroutine(Spread());
    }

    #endregion

    #endregion

    #region Coroutines

    private IEnumerator Recoil()
    {
        float maxRecoil = _recoil;

        float time = 0;

        while (_recoil != 0)
        {
            _angle.Value += (_recoil - _data.RecoilDecreaseAnimationCurve.Evaluate(1 - time) * maxRecoil);

            _recoil = _data.RecoilDecreaseAnimationCurve.Evaluate(1 - time) * maxRecoil;

            time += TryGetWeapon().RecoilDecrease / maxRecoil * Time.deltaTime;

            _handCamera.transform.localRotation = Quaternion.Euler(_recoil, 0, 0);

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
            _spread = _data.RecoilDecreaseAnimationCurve.Evaluate(1 - time) * maxSpread;

            time += TryGetWeapon().SpreadDecrease / maxSpread * Time.deltaTime;

            yield return null;
        }
    }

    #endregion

    #endregion

    #region Damage

    [ServerRpc]
    public void BlindServerRpc(float duration)
        => BlindClientRpc(duration);

    [ClientRpc]
    private void BlindClientRpc(float duration)
        => HUD.Instance.Blind(duration);

    [ServerRpc]
    public void DamageServerRpc(int value, NetworkBehaviourReference source)
    {
        _health.Value = Mathf.Clamp(_health.Value - value, 0, 101);
        if (_health.Value == 0)
            DieClientRpc(source);
        else
            DamageClientRpc();
    }

    [ClientRpc]
    public void DamageClientRpc()
    {
        Damaged.Invoke();
    }

    #endregion

    #region Die

    [ServerRpc]
    public void DieServerRpc(NetworkBehaviourReference cause)
    {
        if (IsOwner)
        {
            for (int i = 0; i < _weapons.Count; i++)
                ChangeWeaponStateServerRpc(_weapon.Value, ChangeWeaponStates.Drop);
        }

        DieClientRpc(cause);
    }

    [ClientRpc]
    public void DieClientRpc(NetworkBehaviourReference killer)
    {
        if (OwnerClientId == OwnerClientId)
        {
            enabled = _controller.enabled = _model.enabled = false;
        }

        if (killer.TryGet(out Player killerPlayer))
            Died.Invoke(killerPlayer);
    }

    #endregion

    #region Audio

    private IEnumerator MoveSound()
    {
        float time = 0;

        PlaySurfaceSound(PlayerControllerData.SurfaceSound.Action.Move);

        while (time < _data.WalkSoundDuration * _data.GetStateSettings(PlayerControllerStates.Walk).SpeedMultiplier / _data.GetStateSettings(_state.Value).SpeedMultiplier)
        {
            time += Time.deltaTime;

            yield return null;
        }
        _moveAudioCoroutine = null;
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

    #endregion

    private void Update()
    {        
        if (!IsOwner)
            return;

        if (IsServer)
        {
            if (!_controller.isGrounded || _velocity > 0)
            {
                _velocity += Physics.gravity.y * Time.deltaTime;
                MoveServerRpc(new Vector3(0, _velocity, 0));
            }
            else if (_velocity < 0)
            {
                var damage = (int)_data.VelocityDamageCurve.Evaluate(Mathf.Abs(_velocity * Time.deltaTime + Physics.gravity.y * Mathf.Pow(Time.deltaTime, 2) / 2));

                if (damage > 0)
                    DamageServerRpc(damage, GameManager.Instance.GetPlayer(OwnerClientId));

                _velocity = 0;
            }

            //_model.Animator.SetBool("Jump_b", _velocity == 0 ? false : true);

            MoveServerRpc(Vector3.zero);
        }

        if (!GameManager.Instance.IsActive)
            return;

        #region Move

        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            _model.Animator.SetBool("Crouch_b", true);
            ChangeStateServerRpc(PlayerControllerStates.CrouchIdle);
            _networkAudioSource.PlayAudio(_data.CrouchSound);
        }

        if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            _model.Animator.SetBool("Crouch_b", false);
            ChangeStateServerRpc(PlayerControllerStates.Idle);
        }

        if (GameManager.Instance.IsPlayersActive.Value)
        {
            if (Input.GetKeyDown(KeyCode.Space) && _controller.isGrounded && _state.Value != PlayerControllerStates.CrouchIdle && _state.Value != PlayerControllerStates.CrouchWalk)
                JumpServerRpc();

            if (_state.Value == PlayerControllerStates.Jump && _controller.isGrounded)
            {
                ChangeStateServerRpc(PlayerControllerStates.Idle);

                PlaySurfaceSound(PlayerControllerData.SurfaceSound.Action.Land);
            }

            var movement = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            if (movement.magnitude > 0)
            {
                //if (_moveAudioCoroutine == null)
                //    _moveAudioCoroutine = StartCoroutine(MoveSound());

                if (Input.GetKey(KeyCode.LeftShift) && (_state.Value == PlayerControllerStates.Walk || _state.Value == PlayerControllerStates.Idle))
                {
                    ChangeStateServerRpc(PlayerControllerStates.Run);

                    _isScoping = false;
                }
                if (_state.Value == PlayerControllerStates.Idle || (Input.GetKeyUp(KeyCode.LeftShift) && (_state.Value == PlayerControllerStates.Idle || _state.Value == PlayerControllerStates.Run)))
                {
                    ChangeStateServerRpc(PlayerControllerStates.Walk);
                }
                if (_state.Value == PlayerControllerStates.CrouchIdle)
                    ChangeStateServerRpc(PlayerControllerStates.CrouchWalk);

                MoveServerRpc(movement * _data.Speed * _data.GetStateSettings(_state.Value).SpeedMultiplier * (TryGetWeapon() ? TryGetWeapon().OwnerSpeedMultiplier * (_isScoping ? _data.ScopeSpeedMultiplier : 1) : 1));
            }
            else
                ChangeStateServerRpc(_state.Value == PlayerControllerStates.CrouchWalk ? PlayerControllerStates.CrouchIdle : PlayerControllerStates.Idle);
        }

        RotateServerRpc(new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * _data.Sensitivity * Time.deltaTime);
       
        Arms.transform.localRotation = _fpCamera.transform.localRotation = Quaternion.Euler(_angle.Value, 0, 0);

        #endregion

        #region Weapon

        for (int i = 0; i < Enum.GetValues(typeof(SlotType)).Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                foreach (var weapon in _weapons)
                    if (weapon.TryGet(out Weapon weaponObject) && (int)weaponObject.SlotType == i)
                        SelectWeaponServerRpc(weaponObject);
        }

        if (Physics.Raycast(_fpCamera.transform.position, _fpCamera.transform.forward, out var hit, _data.PickDistance))
        {
            var weapon = hit.transform.GetComponent<Gun>();
            if (weapon && Input.GetKeyDown(KeyCode.E))
            {
                ChangeWeaponStateServerRpc(weapon, ChangeWeaponStates.Take);
            }
        }

        if (TryGetWeapon())
        {
            if ((Input.GetMouseButtonDown(0)
                || (Input.GetMouseButton(0)
                && TryGetWeapon().ActionMode == ActionMode.Auto))
                && GameManager.Instance.IsPlayersActive.Value)
            {
                TryGetWeapon().ActivateServerRpc(_fpCamera.transform.position, _fpCamera.transform.forward);
            }

            if (TryGetWeapon() is Gun)
            {
                if (Input.GetMouseButton(1) && _state.Value != PlayerControllerStates.Run)
                {
                    _handCamera.transform.position += (((Gun)TryGetWeapon()).Sight.position - _handCamera.transform.position) * Time.deltaTime * ((Gun)TryGetWeapon()).ScopeSpeed;
                    _fpCamera.fieldOfView = Mathf.Lerp(_fpCamera.fieldOfView, 60 / ((Gun)TryGetWeapon()).ScopeValue, Time.deltaTime * ((Gun)TryGetWeapon()).ScopeSpeed);

                    _isScoping = true;
                }
                else
                {
                    _handCamera.transform.localPosition = Vector3.Lerp(_handCamera.transform.localPosition, HandCameraStartPosition, ((Gun)TryGetWeapon()).ScopeSpeed * Time.deltaTime);
                    _fpCamera.fieldOfView = Mathf.Lerp(_fpCamera.fieldOfView, 60, Time.deltaTime * ((Gun)TryGetWeapon()).ScopeSpeed);

                    _isScoping = false;
                }

                if (Input.GetKeyDown(KeyCode.R))
                {
                    ((Gun)TryGetWeapon()).ReloadServerRpc();

                    _model.Animator.SetTrigger("Reload_t");
                }
            }

            int mouseScroll = (int)(Input.GetAxis("Mouse ScrollWheel") * -10);
            if (mouseScroll != 0)
            {
                //while(TryGetWeapon().SlotType > weapon.SlotType)              
            }

            if (Input.GetKeyDown(KeyCode.G) && TryGetWeapon().SlotType != SlotType.Knife)
            {
                ChangeWeaponStateServerRpc(TryGetWeapon(), ChangeWeaponStates.Drop);
            }
        }

        #endregion

        else
        {
            _fpCamera.transform.localPosition = Mathf.Sin(_time / _data.CameraMovePeriod * _weaponRecoilMultiplier * 360 * Mathf.Deg2Rad) * _data.CameraMoveOffset;
            _time += Time.deltaTime;
        }
    }
}

#endregion