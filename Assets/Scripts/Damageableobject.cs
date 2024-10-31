using System.Collections.Specialized;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public interface IDamageableObject
{
    public event UnityAction<Player,string> Died;

    public string Name { get; set; }

    public void DamageClientRpc(int value, NetworkBehaviourReference source,string causeCode);

    protected void Die(Player killer,string causeCode)
    {
        //Died?.Invoke(killer);
    }


}
