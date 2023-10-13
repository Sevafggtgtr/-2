using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public interface IDamageableObject
{
    public event UnityAction<Player> Died;

    public string Name { get; set; }

    public void DamageClientRpc(int value, NetworkBehaviourReference source);

    protected void Die(Player killer)
    {
        //Died?.Invoke(killer);
    }


}
