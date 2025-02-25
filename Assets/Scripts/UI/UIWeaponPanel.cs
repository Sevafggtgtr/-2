using UnityEngine;
using UnityEngine.UI;

public class UIWeaponPanel : MonoBehaviour
{
    [SerializeField]
    private Text _weaponNameText;

    public void Initialize(Player previousPlayer, Player newPlayer)
    {
        if (previousPlayer && previousPlayer.Controller.Value.TryGet(out PlayerController playerController))
            playerController.WeaponChanged -= OnWeaponChanged;

        if (newPlayer.Controller.Value.TryGet(out playerController))
        {
            playerController.WeaponChanged += OnWeaponChanged;
            OnWeaponChanged(null, playerController.Weapon);
        }
    }

    private void OnWeaponChanged(Weapon previousWeapon, Weapon newWeapon)
    {
        _weaponNameText.text = newWeapon ? newWeapon.Name : "";

        GetComponentInChildren<UIGunPanel>().Initialize(previousWeapon, newWeapon);
    }


}
