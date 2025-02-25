using System;
using Unity.Collections;
using Unity.Netcode;

public class SpawnManager : Singleton<SpawnManager>
{
    [ServerRpc]
    public void SpawnWeaponServerRpc(FixedString32Bytes weaponCode)
    {
        var weapon = Instantiate(GameManager.Instance.WeaponData.GetWeapon(weaponCode.ToString()));

        weapon.NetworkObject.Spawn();
    }
}
