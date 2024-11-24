using UnityEngine;

public abstract class ExplosiveGrenade : TimeActivateGrenade
{
    [SerializeField]
    private float _radius;

    protected override void Activate()
    {
        foreach (Collider target in Physics.OverlapSphere(transform.position, _radius))
        {
            if (target.GetComponent<IDamageableObject>() != null && !Physics.Linecast(transform.position, target.transform.position))
                Explode();
        }
    }

    protected abstract void Explode();      
}
