using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerAnimator : NetworkBehaviour
{
    private Animator _animator;
    public Animator Animator => _animator;

    private PlayerController _controller;

    [SerializeField]
    private Transform _head,
                      _hand;
    public Transform Head => _head;
    public Transform Hand => _hand;

    public void Initialize()
    {
        _animator = GetComponent<Animator>();
        
        _controller = GetComponentInParent<PlayerController>();
    }
        

    private void OnAnimatorIK(int layerIndex)
    {
        if (IsOwner)
        {
            var angle = _animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).InverseTransformDirection(Vector3.up);
            var angle1 = _animator.GetBoneTransform(HumanBodyBones.RightUpperArm).InverseTransformDirection(Vector3.up);

            _animator.SetBoneLocalRotation(HumanBodyBones.LeftUpperArm, Quaternion.Euler(_animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).localEulerAngles - _controller.Angle * angle));
            _animator.SetBoneLocalRotation(HumanBodyBones.RightUpperArm, Quaternion.Euler(_animator.GetBoneTransform(HumanBodyBones.RightUpperArm).localEulerAngles + _controller.Angle * angle1));
            _animator.SetBoneLocalRotation(HumanBodyBones.Head, Quaternion.Euler(_animator.GetBoneTransform(HumanBodyBones.Head).localEulerAngles + _controller.Angle * Vector3.up));
        }    
    }
}
