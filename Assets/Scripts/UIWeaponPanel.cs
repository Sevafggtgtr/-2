using UnityEngine;
using UnityEngine.UI;

public class UIWeaponPanel : MonoBehaviour
{
    [SerializeField]
    private Text _weaponNameText,
                 _ammoText;

    public void Initialize()
    {
        Spectator.Instance.PlayerChanged += (previousPlayer, newPlayer) =>
        {
            if(newPlayer.Controller.Value.TryGet(out PlayerController playerController))
                playerController.WeaponChanged += OnWeaponChanged;

            if(previousPlayer && previousPlayer.Controller.Value.TryGet(out playerController))
                playerController.WeaponChanged -= OnWeaponChanged;
        };
    }

    private void OnWeaponChanged(Weapon previousWeapon, Weapon newWeapon)
    {
        _weaponNameText.text = newWeapon.Name;

        _ammoText.text = "";

        if(previousWeapon && previousWeapon is Gun)
            ((Gun)previousWeapon).AmmoChanged -= ()
                => OnAmmoChanged((Gun)previousWeapon);

        if (newWeapon is Gun)
            ((Gun)newWeapon).AmmoChanged += ()
                => OnAmmoChanged((Gun)newWeapon);
    }

    private void OnAmmoChanged(Gun gun)
    {
        _ammoText.text = gun.CurrentClipAmmo.ToString() + '/' + gun.MaxClipAmmo.ToString() + '|' + gun.CurrentAmmo.ToString();
    }

}
