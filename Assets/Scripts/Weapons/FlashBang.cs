using Unity.Netcode;
using UnityEngine;

public class FlashBang : ExplosiveGrenade
{
    [Header("FlashBang")]
    [SerializeField]
    private float _duration;

    protected override void Explode(PlayerController playerController)
    {
        ExplodeClientRpc(playerController);
    }

    [ClientRpc]
    private void ExplodeClientRpc(NetworkBehaviourReference playerController)
    {
        HUD.Singleton.Blindness(_duration);
    }
}
