using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class UIGunPanel : MonoBehaviour
{
    private PlayerController _player;

    [SerializeField]
    private Text _gunNameText,
                 _ammoText;

    void Change()
    {
        if (_player.Weapon is Gun)
            _ammoText.text = ((Gun)_player.Weapon).CurrentClipAmmo.ToString() + '/' + ((Gun)_player.Weapon).MaxClipAmmo.ToString() + '|' + ((Gun)_player.Weapon).CurrentAmmo.ToString();
        else
            _ammoText.text = "";
    }

    void BeginChangeGun()
    {
        if (_player.Weapon is Gun)
            ((Gun)_player.Weapon).AmmoChanged -= Change;
    }

    void ChangeGun()
    {
        if (_player.Weapon is Gun)
            ((Gun)_player.Weapon).AmmoChanged += Change;
        _gunNameText.text = _player.Weapon.Name;
        Change();


    }

    void Awake()
    {
        _player = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>();       
        _player.WeaponChanged += ChangeGun;
        _player.WeaponChange += BeginChangeGun;
    }
    
    void Update()
    {
        
    }
}
