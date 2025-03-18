using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class UIWeaponPanel : MonoBehaviour
{
    [SerializeField]
    private Text _weaponNameText,
                 _gunAmmoText;

    public void Initialize(Player previousPlayer, Player newPlayer)
    {
        if (previousPlayer)
        {
            previousPlayer.TryGetController().Weapon.OnValueChanged -= OnWeaponChanged;
        }

        newPlayer.TryGetController().Weapon.OnValueChanged += OnWeaponChanged;

        OnWeaponChanged(null, newPlayer.TryGetController().Weapon.Value);
    }

    private void OnWeaponChanged(NetworkBehaviourReference previousWeapon, NetworkBehaviourReference newWeapon)
    {
        if (previousWeapon.TryGet(out Weapon previousWeaponObject))
        {
            var previousGun = previousWeaponObject as Gun;
            if (previousGun)
                previousGun.CurrentClipAmmo.OnValueChanged -= (previousValue, newValue) => OnCurrentClipAmmoValueChanged(previousGun);
        }
        if (newWeapon.TryGet(out Weapon newWeaponObject))
        {
            _weaponNameText.text = newWeaponObject.Name;

            var newGun = newWeaponObject as Gun;
            _gunAmmoText.gameObject.SetActive(newGun);
            if (newGun)
            {
                OnCurrentClipAmmoValueChanged(newGun);
                newGun.CurrentClipAmmo.OnValueChanged += (previousValue, newValue) => OnCurrentClipAmmoValueChanged(newGun);
            }
        }
    }

    private void OnCurrentClipAmmoValueChanged(Gun gun)
    {
        _gunAmmoText.text = gun.CurrentClipAmmo.Value.ToString() + '/' + gun.ClipAmmo.ToString() + '|' + gun.CurrentAmmo.Value.ToString();
    }
}
