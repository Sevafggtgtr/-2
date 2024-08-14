using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIWeaponStoreWeaponButton : UIButton
{
    [SerializeField]
    private Text _nameText,
                 _priceText;

    [SerializeField]
    private Image _iconImage;

    public Weapon Weapon { get; private set; }

    public void Initialize(Weapon weapon)
    {
        _nameText.text = weapon.Name;
        _priceText.text = weapon.Price.ToString();
        _iconImage.sprite = weapon.Icon;
        Weapon = weapon;
    }
}
