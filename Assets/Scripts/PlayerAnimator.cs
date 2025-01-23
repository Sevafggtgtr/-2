using Unity.Netcode;
using UnityEngine;

public class PlayerAnimator : NetworkBehaviour
{
    private Animator _animator;
    public Animator Animator => _animator;

    private PlayerController _controller;

    public Transform Head => _head;
    public Transform Hand => _rightHandTransform;

    private Transform _head,
                      _leftUpperArmTransform,
                      _rightUpperArmTransform,
                      _rightHandTransform;   

    public void Start()
    {
        _animator = GetComponent<Animator>();

        _controller = GetComponentInParent<PlayerController>();
    }

    public void Initialize()
    {
        _animator = GetComponent<Animator>();

        _controller = GetComponentInParent<PlayerController>();

        _head = _animator.GetBoneTransform(HumanBodyBones.Head);
        _leftUpperArmTransform = _animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        _rightUpperArmTransform = _animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        _rightHandTransform = _animator.GetBoneTransform(HumanBodyBones.RightHand);
    }

    private void LateUpdate()
    {
        var eulerAngles = _leftUpperArmTransform.eulerAngles;
        eulerAngles.z = 180 + _controller.Angle.Value;

        _leftUpperArmTransform.eulerAngles = eulerAngles;

        eulerAngles = _rightUpperArmTransform.eulerAngles;
        eulerAngles.z = _controller.Angle.Value;

        _rightUpperArmTransform.eulerAngles = eulerAngles;
    }
}
