using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

public class HEGrenade : ExplosiveGrenade
{
    protected override void Explode(PlayerController playerController)
    {
        playerController.DamageServerRpc(_damage, _owner, Code);
    }
}
