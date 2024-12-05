using Unity.Netcode;
using UnityEngine;

public abstract class ExplosiveGrenade : TimeActivateGrenade
{
    [SerializeField]
    private float _radius;

    protected override void Activate()
    {
        foreach (Collider target in Physics.OverlapSphere(transform.position, _radius))
        {
            var playerController = target.GetComponent<PlayerController>();

            if (playerController != null && !Physics.Linecast(transform.position, target.transform.position))
                Explode(playerController);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void ExplodeClientRpc()
    {

    }

    protected abstract void Explode(PlayerController playerController);
       
}
