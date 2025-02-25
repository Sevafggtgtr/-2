using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

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

    private void Start()
    {
        foreach (var weaponGroup in _weaponGroups)
        {
            var weaponLayoutGroup = Instantiate(_weaponLayoutGroupPrefab, _weaponButtonLayoutGroup.transform);

            foreach (var weapon in GameManager.Instance.WeaponData.GetTeamWeaponData(Spectator.Instance.Player.Team.Value).Weapons.Where(weapon => weaponGroup.WeaponTypes.Contains(weapon.WeaponType)))
            {
                var weaponButton = Instantiate(_weaponButtonPrefab, weaponLayoutGroup.transform);

                weaponButton.Initialize(weapon);

                weaponButton.OnClick += () =>
                    Spectator.Instance.Player.BuyWeaponServerRpc(weapon.Code);                               
            }
        }
        
        Spectator.Instance.Player.Balance.OnValueChanged += (o,n) => CheckBalance();
    
        CheckBalance();
    }

    private void CheckBalance()
    {
        foreach (var weaponButton in _weaponButtonLayoutGroup.transform.GetComponentsInChildren<UIWeaponStoreWeaponButton>())
        {
            weaponButton.Button.interactable = weaponButton.Weapon.Price <= GameManager.Instance.Player.Balance.Value;
        }
    }

    private void OnEnable()
        => CheckBalance();
}
