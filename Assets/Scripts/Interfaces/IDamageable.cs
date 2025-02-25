using System.Collections.Specialized;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public interface IDamageable
{
    public event UnityAction<Player,string> Died;

    public void DamageServerRpc(int value, ulong source,string causeCode);

    //public void DamageClientRpc(int value, NetworkBehaviourReference source, string causeCode);

    public void Die(Player killer, string causeCode)
    {
        //Died?.Invoke(killer);
    }


}
