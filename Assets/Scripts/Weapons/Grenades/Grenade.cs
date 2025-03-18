using System.Linq;
using System.Collections;
using UnityEngine;
using Unity.Netcode;

public abstract class Grenade : Weapon
{
    [Header("Grenade")]
    [SerializeField]
    private float _throwForce,
                  _throwTime;

    protected PlayerController _ownerPlayerController;

    [ServerRpc]
    public override void ActivateServerRpc(Vector3 origin, Vector3 direction)
    {
        IEnumerator Throw(Vector3 origin, Vector3 direction)
        {
            var ownerPlayerController = _ownerPlayerController = GameManager.Instance.GetPlayer(OwnerClientId).TryGetController();

            ownerPlayerController.CanChangeWeapon.Value = false;

            yield return new WaitForSeconds(_throwTime);

            ownerPlayerController.CanChangeWeapon.Value = true;

            ownerPlayerController.ChangeWeaponStateServerRpc(this, PlayerController.ChangeWeaponStates.Drop);

            Rigidbody.AddForce(direction * _throwForce, ForceMode.Impulse);

            StartCoroutine(OnThrown());
        }

        StartCoroutine(Throw(origin, direction));
    }

    protected abstract IEnumerator OnThrown();

    [ClientRpc]
    protected virtual void OnEndThrowClientRpc()
    {
        _audioSource.Play();

        GetComponent<MeshRenderer>().enabled = Collider.enabled = NetworkTransform.enabled = false;
        Rigidbody.isKinematic = true;

        var particleSystem = GetComponentInChildren<ParticleSystem>();
        if(particleSystem)
            particleSystem.Play();
    }
}
