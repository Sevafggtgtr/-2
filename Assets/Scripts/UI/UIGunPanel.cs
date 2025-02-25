using UnityEngine;
using UnityEngine.UI;

public class UIGunPanel : MonoBehaviour
{
    [SerializeField]
    private Text _ammoText;

    public void Initialize(Weapon previousWeapon, Weapon newWeapon)
    {
        if (previousWeapon)
        {
            var previousGun = previousWeapon as Gun;
            if(previousGun)
                previousGun.AmmoChanged -= () => OnAmmoChanged(previousGun);
        }

        if (newWeapon)
        {
            var newGun = newWeapon as Gun;
            if (newGun)
                newGun.AmmoChanged += () => OnAmmoChanged(newGun);
        }
    }

    private void OnAmmoChanged(Gun gun)
    {
        _ammoText.text = gun.CurrentClipAmmo.ToString() + '/' + gun.MaxClipAmmo.ToString() + '|' + gun.CurrentAmmo.ToString();
    }

}
