using Unity.Netcode;
using UnityEngine.Events;

public interface IDamageable
{
    public event UnityAction<Player> Died;

    public void DamageServerRpc(int value, NetworkBehaviourReference source);
    public void DamageClientRpc();

    public void DieServerRpc(NetworkBehaviourReference cause);
    public void DieClientRpc(NetworkBehaviourReference cause);
}
