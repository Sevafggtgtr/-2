using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Hit : NetworkBehaviour
{
    void Start()
    {
        if(IsServer)
            Invoke("DestroyServerRpc", 3);
    }

    [ServerRpc]
    private void DestroyServerRpc()
    {
        NetworkObject.Despawn();
    }

    void Update()
    {
        
    }
}
