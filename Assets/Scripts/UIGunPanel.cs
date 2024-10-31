using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class UIGunPanel : MonoBehaviour
{
    private PlayerController _playerController;

    [SerializeField]
    private Text _gunNameText,
                 _ammoText;

    void Change()
    {
        if (_playerController.Weapon is Gun)
            _ammoText.text = ((Gun)_playerController.Weapon).CurrentClipAmmo.ToString() + '/' + ((Gun)_playerController.Weapon).MaxClipAmmo.ToString() + '|' + ((Gun)_playerController.Weapon).CurrentAmmo.ToString();
        else
            _ammoText.text = "";
    }

    void BeginChangeGun()
    {
        if (_playerController.Weapon is Gun)
            ((Gun)_playerController.Weapon).AmmoChanged -= Change;
    }

    void ChangeGun()
    {
        if (_playerController.Weapon is Gun)
            ((Gun)_playerController.Weapon).AmmoChanged += Change;
        _gunNameText.text = _playerController.Weapon.Name;
        Change();
    }

    public void SetPlayer(PlayerController playerController)
    {               
        _playerController = playerController;
        _playerController.WeaponChanged += ChangeGun;                                      
    }

    public void RemovePlayer(PlayerController playerController)
    {
        _playerController = playerController;
        _playerController.WeaponChanged -= ChangeGun;
    }
}
