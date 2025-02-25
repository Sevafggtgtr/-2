public class HEGrenade : ExplosiveGrenade
{
    protected override void Explode(PlayerController playerController)
    {
        playerController.DamageServerRpc(_damage, 0, Code);
    }
}
