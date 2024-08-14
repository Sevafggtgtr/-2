using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class WeaponData : ScriptableObject
{
    [System.Serializable]
    public struct TeamWeaponData
    {
        [SerializeField]
        private Teams _team;
        public Teams Team => _team;

        [SerializeField]
        private Weapon[] _weapons;
        public Weapon[] Weapons => _weapons;
    }

    [SerializeField]
    private TeamWeaponData[] _teamWeaponDatas;
    public TeamWeaponData[] TeamWeaponDatas => _teamWeaponDatas;   
}
