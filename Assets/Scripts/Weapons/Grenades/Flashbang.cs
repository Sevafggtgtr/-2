using UnityEngine;

public class Flashbang : ExplosiveGrenade
{
    [Header("Flashbang Settings")]
    [SerializeField]
    private float _duration;

    protected override void Explode(PlayerController playerController)
    {
        playerController.BlindServerRpc(_duration);
    }
}
