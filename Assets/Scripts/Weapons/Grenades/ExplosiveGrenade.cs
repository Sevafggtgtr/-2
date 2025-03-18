using Unity.Netcode;
using UnityEngine;

public abstract class ExplosiveGrenade : TimeActivatedGrenade
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

    protected abstract void Explode(PlayerController playerController);
}
