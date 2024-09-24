using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UIWeaponStore : UIPanel
{
    [SerializeField]
    private WeaponGroup[] _weaponGroups;

    [System.Serializable]
    public struct WeaponGroup
    {
        [SerializeField]
        private string _name;
        public string Name =>_name;

        [SerializeField]
        private WeaponType[] _weaponTypes;
        public WeaponType[] WeaponTypes => _weaponTypes;
    }

    [SerializeField]
    private VerticalLayoutGroup _weaponLayoutGroupPrefab;

    [SerializeField]
    private UIWeaponStoreWeaponButton _weaponButtonPrefab;

    [SerializeField]
    private HorizontalLayoutGroup _weaponButtonLayoutGroup;

    void Start()
    {
        foreach (var weaponGroup in _weaponGroups)
        {
            var weaponLayoutGroup = Instantiate(_weaponLayoutGroupPrefab, _weaponButtonLayoutGroup.transform);

            foreach (var weapon in GameManager.Singleton.WeaponData.GetTeamWeaponData(Player.Singleton.Team.Value).Weapons.Where(weapon => weaponGroup.WeaponTypes.Contains(weapon.WeaponType)))
            {
                var weaponButton = Instantiate(_weaponButtonPrefab, weaponLayoutGroup.transform);

                weaponButton.Initialize(weapon);

                weaponButton.OnClick += () =>
                    Buy(weapon);
            }
        }
    }

    private void CheckBalance()
    {
        foreach (var weaponButton in _weaponButtonLayoutGroup.transform.GetComponentsInChildren<UIWeaponStoreWeaponButton>())
        {
            weaponButton.Button.interactable = weaponButton.Weapon.Price <= Player.Singleton.Balance.Value;
        }
    }

    private void OnEnable()
        => CheckBalance();   

    public void Buy(Weapon weapon)
    {
        Player.Singleton.Balance.Value -= weapon.Price;
        
        CheckBalance();

        PlayerController.Singleton.AddWeaponServerRpc(Array.IndexOf(GameManager.Singleton.WeaponData.GetTeamWeaponData(Player.Singleton.Team.Value).Weapons, weapon));
    }
}
