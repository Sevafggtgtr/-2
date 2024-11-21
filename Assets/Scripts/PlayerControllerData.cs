using System.Linq;
using System;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerControllerData", menuName = "Scriptable Objects/PlayerControllerData")]
public class PlayerControllerData : ScriptableObject
{
    [SerializeField]
    private PlayerStateSettings[] _playerStatesSettings;
    public PlayerStateSettings[] PlayerStatesSettings => _playerStatesSettings;

    [System.Serializable]
    public struct PlayerStateSettings
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
    public PlayerStateSettings GetPlayerStateSettings(PlayerState state) => _playerStatesSettings.First(x => x.PlayerState == state);

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

    [SerializeField]
    private float _cameraWeaponRecoilAngleUnit,
                  _weaponPivotMaxRecoilOffset,
                  _weaponPivotRecoilOffsetUnit;
    public float CameraWeaponRecoilAngleUnit => _cameraWeaponRecoilAngleUnit;
    public float WeaponPivotMaxRecoilOffset => _weaponPivotMaxRecoilOffset;
    public float WeaponPivotRecoilOffsetUnit => _weaponPivotRecoilOffsetUnit;

    [SerializeField]
    private AnimationCurve _weaponAnimationCurve;   
    public AnimationCurve WeaponAnimationCurve => _weaponAnimationCurve;

    [Header("Move")]
    [SerializeField]
    private float _cameraMovePeriod;
    public float CameraMovePeriod => _cameraMovePeriod;

    [SerializeField]
    private SurfaceSound[] _surfaceSounds;
    public SurfaceSound[] SurfaceSounds => _surfaceSounds;

    [SerializeField]
    private AudioClip _jumpSound,
                      _crouchSound;
    public AudioClip JumpSound => _jumpSound;
    public AudioClip CrouchSound => _crouchSound;

    [System.Serializable]
    public struct SurfaceSound
    {
        public enum Action
        {
            Move,
            Land
        }

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
    public float WalkSoundDuration => _walkSoundDuration;

    [SerializeField]
    private Vector3 _cameraMoveOffset,
                    _cameraRotateOffset;
    public Vector3 CameraMoveOffset => _cameraMoveOffset;
    public Vector3 CameraRotateOffset => _cameraRotateOffset;

    [SerializeField]
    private float _sensitivity,
                  _pickDistance,
                  _dropForce,
                  _jumpForce;
    public float Sensitivity => _sensitivity;
    public float PickDistance => _pickDistance;
    public float DropForce => _dropForce;
    public float JumpForce => _jumpForce;

    [SerializeField]
    private AnimationCurve _velocityDamageCurve;
    public AnimationCurve VelocityDamageCurve => _velocityDamageCurve;

    [SerializeField]
    private float _scopeSpeedMultiplier;
    public float ScopeSpeedMultiplier => _scopeSpeedMultiplier;  
}
