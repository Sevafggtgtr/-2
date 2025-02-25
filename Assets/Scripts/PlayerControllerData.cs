using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerControllerData", menuName = "Scriptable Objects/PlayerControllerData")]
public class PlayerControllerData : ScriptableObject
{
    #region Movement

    [SerializeField]
    private float _speed,
                  _sensitivity,
                  _pickDistance,
                  _dropForce,
                  _jumpHeight;

    public float Speed => _speed;
    public float Sensitivity => _sensitivity;
    public float PickDistance => _pickDistance;
    public float DropForce => _dropForce;
    public float JumpHeight => _jumpHeight;

    [System.Serializable]
    public struct StateSettings
    {
        [SerializeField]
        private PlayerControllerStates _state;
        public PlayerControllerStates State => _state;
        [SerializeField]
        private float _speedMultiplier;
        public float SpeedMultiplier => _speedMultiplier;
        [SerializeField]
        private float _weaponSpreadMultiplier;
        public float WeaponSpreadMultiplier => _weaponSpreadMultiplier;
        [SerializeField]
        private float _weaponRecoilMultiplier;
        public float WeaponRecoilMultiplier => _weaponRecoilMultiplier;
    }

    [SerializeField]
    private StateSettings[] _statesSettings;
    public StateSettings[] StatesSettings => _statesSettings;

    public StateSettings GetStateSettings(PlayerControllerStates state) => _statesSettings.First(x => x.State == state);

    [SerializeField]
    private AnimationCurve _velocityDamageCurve;
    public AnimationCurve VelocityDamageCurve => _velocityDamageCurve;

    #endregion

    #region Camera

    [SerializeField]
    private float _cameraMovePeriod,
                  _scopeSpeedMultiplier;
    public float CameraMovePeriod => _cameraMovePeriod;
    public float ScopeSpeedMultiplier => _scopeSpeedMultiplier;

    [SerializeField]
    private Vector3 _cameraMoveOffset,
                    _cameraRotateOffset;
    public Vector3 CameraMoveOffset => _cameraMoveOffset;
    public Vector3 CameraRotateOffset => _cameraRotateOffset;

    #endregion

    #region Audio

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
    private SurfaceSound[] _surfaceSounds;
    public SurfaceSound[] SurfaceSounds => _surfaceSounds;

    [SerializeField]
    private AudioClip _jumpSound,
                      _crouchSound;
    public AudioClip JumpSound => _jumpSound;
    public AudioClip CrouchSound => _crouchSound;

    [SerializeField]
    private float _walkSoundDuration;
    public float WalkSoundDuration => _walkSoundDuration;

    #endregion 

    #region Weapons

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

    [SerializeField]
    private AnimationCurve _recoilDecreaseAnimationCurve;
    public AnimationCurve RecoilDecreaseAnimationCurve => _recoilDecreaseAnimationCurve;

    /*public WeaponSlot GetWeaponSlot(SlotType type)
        => _weaponSlots.First(weaponSlot => weaponSlot.SlotType == type);*/

    #endregion
}
