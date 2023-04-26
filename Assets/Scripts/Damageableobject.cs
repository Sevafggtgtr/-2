using UnityEngine;
using UnityEngine.Events;

public interface IDamageableObject
{
    public event UnityAction<string> Died;

    public string Name { get; set; }

    public void DamegeClientRpc(int value, string source);

    protected void Die(string killer)
    {
        //Died?.Invoke(killer);
    }


}
