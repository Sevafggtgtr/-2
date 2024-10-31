using System.Collections;
using System.Linq;
using System.Net.Http.Headers;
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
    public enum Action
    {
        Move,
        Land
    }

    public static event UnityAction<PlayerController> Spawn;
    public static event UnityAction Despawn;
    public event UnityAction WeaponChanged;
    public event UnityAction<Player, string> Died;
    public event UnityAction Kill;
    public event UnityAction Damaged;

    private static PlayerController _singleton;
    public static PlayerController Singleton => _singleton;

    [Header("States")]

    [SerializeField]
    private PlayerStateSettings[] _playerStatesSettings;

    [System.Serializable]
    private struct PlayerStateSettings
    {
        [SerializeField]
        private PlayerState _playerState;
        public PlayerState PlayerState => _playerState;
        [SerializeField]
        private float _speed;
        public float Speed => _speed;
        [SerializeField]
        private float _cameraMoveRate;
        public float CameraMoveRate => _cameraMoveRate;
        [SerializeField]
        private float _weaponSpreadMultiplier;
        public float WeaponSpreadMultiplier => _weaponSpreadMultiplier;
        [SerializeField]
        private float _weaponRecoilMultiplier;
        public float WeaponRecoilMultiplier => _weaponRecoilMultiplier;
    }

    private PlayerStateSettings GetPlayerStateSettings() => _playerStatesSettings.First(x => x.PlayerState == PlayerState);
    private PlayerStateSettings GetPlayerStateSettings(PlayerState state) => _playerStatesSettings.First(x => x.PlayerState == state);

    private PlayerState _playerState;
    public PlayerState PlayerState => _playerState;

    [Header("Weapons")]
    private Weapon _weapon;
    public Weapon Weapon => _weapon;
    [SerializeField]
    private Weapon[] _weapons;
    public Weapon[] Weapons => _weapons;
    [System.Serializable]
    public struct WeaponSlot
    {
        [SerializeField]
        private SlotType _slotType;
        public SlotType SlotType => _slotType;
        [SerializeField]
        private int _weaponCount;
        public int WeaponCount => _weaponCount;
        private Weapon[] _weapons;
        public Weapon[] Weapons => _weapons;

        public void Initialize()
        {
            _weapons = new Weapon[_weaponCount];
        }
    }

    /*public WeaponSlot GetWeaponSlot(SlotType type)
        => _weaponSlots.First(weaponSlot => weaponSlot.SlotType == type);*/

    private NetworkVariable<NetworkBehaviourReference> _networkWeapon = new NetworkVariable<NetworkBehaviourReference>(writePerm: NetworkVariableWritePermission.Owner);
    private NetworkList<NetworkBehaviourReference> _networkWeapons = new NetworkList<NetworkBehaviourReference>(writePerm: NetworkVariableWritePermission.Owner);

    [SerializeField]
    private float _cameraWeaponRecoilAngleUnit,
                  _weaponPivotMaxRecoilOffset,
                  _weaponPivotRecoilOffsetUnit;

    [SerializeField]
    private AnimationCurve _weaponAnimationCurve;

    private float _recoil,
                  _spread,
                  _handCameraRecoilAngle;


    private Coroutine _recoilCoroutine,
                      _spreadCoroutine;

    [Header("Move")]
    [SerializeField]
    private float _cameraMovePeriod;

    [SerializeField]
    private SurfaceSound[] _surfaceSounds;

    [SerializeField]
    private AudioClip _jumpSound,
                      _crouchSound;

    [System.Serializable]
    private struct SurfaceSound
    {
        [SerializeField]
        private SurfaceType _surfaceType;
        public SurfaceType SurfaceType => _surfaceType;

        [SerializeField]
        private Action _actionType;
        public Action ActionType => _actionType;

        [SerializeField]
        private AudioClip _sound;
        public AudioClip Sound => _sound;
    }

    [SerializeField]
    private float _walkSoundDuration;

    [SerializeField]
    private NetworkAudioSource _moveNetworkAudioSource;

    private Coroutine _moveSound;

    [SerializeField]
    private Vector3 _cameraMoveOffset,
                    _cameraRotateOffset;

    [SerializeField]
    private float _sensitivity,
                  _pickDistance,
                  _dropForce,
                  _jumpForce;

    private Transform _arms;

    [SerializeField]
    private AnimationCurve _velocityDamageCurve;

    private bool _scope;
    [SerializeField]
    private float _scopeSpeedMultiplier;

    private float _velocity;

    private Vector3 _handCameraStartPosition;
    public Vector3 HandCameraStartPosition => _handCameraStartPosition;

    [SerializeField]
    private Camera _fpCamera,
                   _handCamera;
    public Camera FpCamera => _fpCamera;
    public Camera HandCamera => _handCamera;

    private float _angle;
    public float Angle => _angle;

    private NetworkVariable<int> _health = new NetworkVariable<int>(100);
    public NetworkVariable<int> Health => _health;

    private CharacterController _controller;

    private PlayerAnimator _model;

    private Animator _animator;

    private NetworkAudioSource _networkAudioSource;

    /*[HideInInspector]
    public Constraints Constraint;
    
    public enum Constraints { None, Move, MoveAndCamera}*/

    [HideInInspector]
    public bool IsActive = true;

    public string Name { get; set; }

    private Player _player;
    public Player Player => _player;

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
        Spawn.Invoke(this);

        _controller = GetComponent<CharacterController>();

        _networkAudioSource = GetComponent<NetworkAudioSource>();

        if (!IsOwner)
        {
            Weapon weapon;
            _networkWeapon.Value.TryGet(out Weapon currentWeapon);
          
            _fpCamera.gameObject.SetActive(false);
            _handCamera.gameObject.SetActive(false);
        }

        else
        {
            _singleton = this;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            InitializeServerRpc();

            transform.position = _spawnPointPosition;
        }

        _controller.enabled = true;
    }

    public void ChangeModelState(Layers layer)
    {
        foreach(Weapon weapon in _weapons)
        {
            ChangeLayer(weapon.gameObject, layer);
        }

        ChangeLayer(_arms.gameObject, layer);

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
        _model = Instantiate(Map.Singleton.Data.SkinPackDatas.First(team => team.Team == Player.Singleton.Team.Value).GetRandomSkin(), transform);

        _model.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);

        _model.GetComponent<NetworkObject>().TrySetParent(transform);

        InitializeClientRpc((NetworkBehaviourReference)_model);

        var weapons = GameManager.Singleton.GameMode.DefaultWeaponIndices;

        for (int i = 0; i < weapons.Length; i++)
            AddWeaponServerRpc(weapons[i]);
    }

    [ClientRpc]
    private void InitializeClientRpc(NetworkBehaviourReference model)
    {
        if (model.TryGet(out PlayerAnimator modelObject))
            _model = modelObject;

        _model.Initialize();

        _animator = _model.Animator;

        _arms = _model.transform.Find("mesh_Arms");

        ChangeModelState(Layers.Hand);

        _handCamera.transform.SetParent(_arms, true);

        _handCameraStartPosition = _handCamera.transform.localPosition;
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
        var weapon = Instantiate(GameManager.Singleton.WeaponData.GetTeamWeaponData(Player.Singleton.Team.Value).Weapons[weaponIndex]);

        weapon.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);

        weapon.GetComponent<NetworkObject>().TrySetParent(transform);

        AddWeaponClientRpc(weapon);
    }

    [Rpc(SendTo.Owner)]
    private void AddWeaponClientRpc(NetworkBehaviourReference weapon)
    {
        if (weapon.TryGet(out Weapon weaponObject))
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

        weapon.Collider.enabled = state switch
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
        weapon.Rigidbody.AddForce(_fpCamera.transform.forward * _dropForce, ForceMode.Impulse);

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
        _animator.SetInteger("WeaponType_int", (int)weapon.WeaponType + 1);
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
        _health.Value -= damage;

        DamageClientRpc(_health.Value, source, causeCode);
    }

    [Rpc(SendTo.Me)]
    public void DamageClientRpc(int health, NetworkBehaviourReference source, string causeCode)
    {      
        Damaged?.Invoke();

        if (health <= 0)
        {
            if (source.TryGet(out Player player))
            {
                //player.Player.Balance.Value += GameManager.Singleton.GameMode.KillAward;

                Die(player,causeCode);
            }

        }
    }

    protected void Die(Player killer, string causeCode)
    {
        if (IsOwner)
        {
            for (int i = 0; i < _networkWeapons.Count - 1; i++)
                DropWeapon(_weapon);

            KnifeDespawnServerRpc();

            _animator.SetBool("Death_b", true);

            ChangeModelState(Layers.Default);

            _model.enabled = false;
        }

        Spectator.Instance.Spectate();

        Died.Invoke(killer, causeCode);

        _controller.enabled = false;

        enabled = false;
    }
    #endregion
    private IEnumerator Recoil()
    {
        float maxRecoil = _recoil;

        float time = 0;

        while (_recoil != 0)
        {
            _angle += (_recoil - _weaponAnimationCurve.Evaluate(1 - time) * maxRecoil) * _cameraWeaponRecoilAngleUnit;

            _recoil = _weaponAnimationCurve.Evaluate(1 - time) * maxRecoil;

            time += _weapon.RecoilDecrease / maxRecoil * Time.deltaTime;

            _handCamera.transform.localRotation = Quaternion.Euler(_recoil * _cameraWeaponRecoilAngleUnit, 0, 0);

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
            _spread = _weaponAnimationCurve.Evaluate(1 - time) * maxSpread;

            time += _weapon.SpreadDecrease / maxSpread * Time.deltaTime;

            yield return null;
        }
    }

    private IEnumerator MoveSound()
    {
        float time = 0;

        PlaySurfaceSound(Action.Move);

        while (time < _walkSoundDuration * GetPlayerStateSettings(PlayerState.Walk).Speed / GetPlayerStateSettings().Speed)
        {
            time += Time.deltaTime;

            yield return null;
        }
        _moveSound = null;
    }

    private void PlaySurfaceSound(Action action)
    {

        if (Physics.Raycast(transform.position, -transform.up, out RaycastHit hit, _pickDistance))
        {
            var surface = hit.transform.GetComponent<Surface>();

            if (surface != null)
            {
                _networkAudioSource.PlayAudio(_surfaceSounds.First(surfaceSound => surfaceSound.SurfaceType == surface.SurfaceType && action == surfaceSound.ActionType).Sound);
            }
        }
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        _fpCamera.transform.localPosition = Mathf.Sin(_time / _cameraMovePeriod * GetPlayerStateSettings(_playerState).CameraMoveRate * 360 * Mathf.Deg2Rad) * _cameraMoveOffset;
        _time += Time.deltaTime;

        if (!_controller.isGrounded)
            _velocity += Physics.gravity.y * Time.deltaTime * 2;
        else if (_velocity < -_controller.skinWidth)
        {
            var damage = (int)_velocityDamageCurve.Evaluate(Mathf.Abs(_velocity * Time.deltaTime + Physics.gravity.y * Mathf.Pow(Time.deltaTime, 2) / 2));

            if (damage > 0)
            {
                DamageServerRpc(damage, Player, "death");
            }       
            _velocity = -_controller.skinWidth;
        }
            

        _controller.Move(Vector3.up * _velocity * Time.deltaTime);

        _animator.SetBool("Jump_b", _controller.velocity.y == 0 ? false : true);

        if (!IsActive)
            return;

        #region Weapon
        if ((Input.GetMouseButtonDown(0)
                  || (Input.GetMouseButton(0) && Weapon.ActionMode == ActionMode.Auto))
                        && GameManager.Singleton.IsPlayersActive.Value && _weapon.Action(_fpCamera.transform.position, _fpCamera.transform.forward, this))
        {
            _animator.SetBool("Shoot_b", true);

            var recoil = _weapon.RecoilValue * (Input.GetMouseButtonDown(0) ? _weapon.FirstShotMultiplier : 1) * GetPlayerStateSettings().WeaponRecoilMultiplier * (_scope ? _weapon.ScopeRecoilMultiplier : 1);

            _angle -= recoil * _cameraWeaponRecoilAngleUnit;

            _recoil += recoil;

            if (_recoilCoroutine != null)
                StopCoroutine(_recoilCoroutine);

            _recoilCoroutine = StartCoroutine(Recoil());

            _spread += _weapon.SpreadValue * (Input.GetMouseButtonDown(0) && PlayerState == PlayerState.Idle && PlayerState == PlayerState.CrouchIdle ? 0 : 1) * GetPlayerStateSettings().WeaponSpreadMultiplier * (_scope ? _weapon.ScopeSpreadMultiplier : 1);

            if (_spreadCoroutine != null)
                StopCoroutine(_spreadCoroutine);

            _spreadCoroutine = StartCoroutine(Spread());
        }

        else
            _animator.SetBool("Shoot_b", false);



        if (_weapon is Gun)
        {
            if (Input.GetMouseButton(1) && PlayerState != PlayerState.Run)
            {
                _handCamera.transform.position += (((Gun)_weapon).Sight.position - _handCamera.transform.position) * Time.deltaTime * ((Gun)_weapon).ScopeSpeed;
                _fpCamera.fieldOfView = Mathf.Lerp(_fpCamera.fieldOfView, 60 / ((Gun)_weapon).ScopeValue, Time.deltaTime * ((Gun)_weapon).ScopeSpeed);

                _scope = true;
            }

            else
            {
                _handCamera.transform.localPosition = Vector3.Lerp(_handCamera.transform.localPosition, HandCameraStartPosition, ((Gun)_weapon).ScopeSpeed * Time.deltaTime);
                _fpCamera.fieldOfView = Mathf.Lerp(_fpCamera.fieldOfView, 60, Time.deltaTime * ((Gun)_weapon).ScopeSpeed);

                _scope = false;
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

            _animator.SetBool("Reload_b", true);
        }

        else
            _animator.SetBool("Reload_b", false);

        for (int i = 0; i < _weapons.Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                if (_weapons[i])
                {
                    ChangeWeapon(_weapons[i], false);
                }
        }

        RaycastHit hit;
        if (Physics.Raycast(_fpCamera.transform.position, _fpCamera.transform.forward, out hit, _pickDistance))
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
            _animator.SetBool("Crouch_b", true);
            _playerState = PlayerState.CrouchIdle;
            _networkAudioSource.PlayAudio(_crouchSound);
        }

        if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            _animator.SetBool("Crouch_b", false);
            _playerState = PlayerState.Idle;
        }

        if (GameManager.Singleton.IsPlayersActive.Value)
        {
            if (Input.GetKeyDown(KeyCode.Space) && _controller.isGrounded && _playerState != PlayerState.CrouchIdle && _playerState != PlayerState.CrouchWalk)
            {
                _velocity = _jumpForce;
                _animator.SetTrigger("Jump_trig");
                _networkAudioSource.PlayAudio(_jumpSound);
            }

            if (_playerState == PlayerState.Jump && _controller.isGrounded)
            {
                _playerState = PlayerState.Idle;

                PlaySurfaceSound(Action.Land);
            }

            if (new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")).magnitude > 0)
            {
                if (_moveSound == null)
                    _moveSound = StartCoroutine(MoveSound());

                if (Input.GetKey(KeyCode.LeftShift) && (_playerState == PlayerState.Walk || _playerState == PlayerState.Idle))
                {
                    _playerState = PlayerState.Run;

                    _scope = false;
                }
                if (_playerState == PlayerState.Idle || (Input.GetKeyUp(KeyCode.LeftShift) && (_playerState == PlayerState.Idle || _playerState == PlayerState.Run)))
                    _playerState = PlayerState.Walk;
                if (_playerState == PlayerState.CrouchIdle)
                    _playerState = PlayerState.CrouchWalk;
            }
            else
                _playerState = _playerState == PlayerState.CrouchWalk ? PlayerState.CrouchIdle : PlayerState.Idle;

            _controller.Move(transform.TransformDirection
                (Vector3.ClampMagnitude(new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")), 1) * GetPlayerStateSettings().Speed * Time.deltaTime * _weapon.OwnerSpeedMultiplier * (_scope ? _scopeSpeedMultiplier : 1)));

            _animator.SetFloat("Speed_f", new Vector3(_controller.velocity.x, 0, _controller.velocity.z).magnitude / GetPlayerStateSettings(PlayerState.Walk).Speed / 4);
        }

        transform.Rotate(0, Input.GetAxis("Mouse X") * _sensitivity * Time.deltaTime, 0);

        _angle -= Input.GetAxis("Mouse Y") * _sensitivity * Time.deltaTime;
        _angle = Mathf.Clamp(_angle, -90, 90);
        _arms.transform.localRotation = _fpCamera.transform.localRotation = Quaternion.Euler(_angle, 0, 0);
        #endregion
    }
}
