using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(Animator), typeof(NetworkAnimator))]
public class PlayerModel : NetworkBehaviour
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

    public GameObject Arms {  get; private set; }

    public void Initialize()
    {
        _animator = GetComponent<Animator>();

        _controller = GetComponentInParent<PlayerController>();

        _head = _animator.GetBoneTransform(HumanBodyBones.Head);
        _leftUpperArmTransform = _animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        _rightUpperArmTransform = _animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        _rightHandTransform = _animator.GetBoneTransform(HumanBodyBones.RightHand);

        Arms = transform.Find("mesh_Arms").gameObject;
    }

    private void LateUpdate()
    {
        var eulerAngles = _leftUpperArmTransform.eulerAngles;
        eulerAngles.z = 180 + _controller.Angle.Value;

        _leftUpperArmTransform.eulerAngles = eulerAngles;

        eulerAngles = _rightUpperArmTransform.eulerAngles;
        eulerAngles.z = _controller.Angle.Value;

        _rightUpperArmTransform.eulerAngles = eulerAngles;

        _head.localEulerAngles = new Vector3(0, 0, _controller.Angle.Value);
    }
}
