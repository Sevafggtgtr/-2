using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UIWeaponStore : MonoBehaviour
{
    private UIWeaponStoreWeaponButton _weaponButtonPrefab;

    private LayoutGroup _weaponButtonLayoutGroup;

    void Start()
    {
        foreach (var weapon in GameManager.Singleton.WeaponData.TeamWeaponDatas.First(teamWeaponData => teamWeaponData.Team == Player.Singleton.Team.Value).Weapons)
        {
            var weaponButton = Instantiate(_weaponButtonPrefab, _weaponButtonLayoutGroup.transform);

            weaponButton.Initialize(weapon);

            weaponButton.OnClick += () =>
                Buy(weapon);           
        }
    }

    public void Buy(Weapon weapon)
    {
        Player.Singleton.Balance.Value -= weapon.Price;

        foreach(var weaponButton in _weaponButtonLayoutGroup.transform.GetComponentsInChildren<UIWeaponStoreWeaponButton>())
        {
           weaponButton.Button.interactable = weaponButton.Weapon.Price <= Player.Singleton.Balance.Value;
        }
    }
}
