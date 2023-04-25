using UnityEngine;


public interface IDamageableObject
{
    public event System.Action<string> Died;

    public string Name { get; set; }

    public void DamegeClientRpc(int value, string source);

    protected void Die(string killer)
    {
        //Died?.Invoke(killer);
    }


}
