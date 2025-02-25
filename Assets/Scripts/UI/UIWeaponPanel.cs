using UnityEngine;
using UnityEngine.UI;

public class UIWeaponPanel : MonoBehaviour
{
    [SerializeField]
    private Text _weaponNameText;

    public void Initialize(Player previousPlayer, Player newPlayer)
    {
        if (previousPlayer && previousPlayer.Controller)
            previousPlayer.Controller.WeaponChanged -= OnWeaponChanged;

        if (newPlayer.Controller)
        {
            newPlayer.Controller.WeaponChanged += OnWeaponChanged;
            OnWeaponChanged(null, newPlayer.Controller.Weapon);
        }
    }

    private void OnWeaponChanged(Weapon previousWeapon, Weapon newWeapon)
    {
        _weaponNameText.text = newWeapon ? newWeapon.Name : "";

        GetComponentInChildren<UIGunPanel>().Initialize(previousWeapon, newWeapon);
    }


}
