using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UIWeaponStore : UIPanel
{
    #region Variables

    [SerializeField]
    private WeaponGroup[] _weaponGroups;

    [System.Serializable]
    public struct WeaponGroup
    {
        [SerializeField]
        private string _name;
        public string Name => _name;

        [SerializeField]
        private WeaponType[] _weaponTypes;
        public WeaponType[] WeaponTypes => _weaponTypes;
    }

    [SerializeField]
    private HorizontalLayoutGroup _weaponGroupLayoutGroup;

    [Header("Prefabs")]
    [SerializeField]
    private VerticalLayoutGroup _weaponLayoutGroupPrefab;
    [SerializeField]
    private UIWeaponStoreWeaponButton _weaponButtonPrefab;


    #endregion

    #region Methods

    protected override void OnStart()
    {
        foreach (var weaponGroup in _weaponGroups)
        {
            var weaponLayoutGroup = Instantiate(_weaponLayoutGroupPrefab, _weaponGroupLayoutGroup.transform);

            foreach (var weapon in GameManager.Instance.WeaponData.GetTeamWeaponData(Spectator.Instance.Player.Team.Value).Weapons.Where(weapon => weaponGroup.WeaponTypes.Contains(weapon.WeaponType)))
            {
                var weaponButton = Instantiate(_weaponButtonPrefab, weaponLayoutGroup.transform);

                weaponButton.Initialize(weapon);

                weaponButton.Clicked += () =>
                    Spectator.Instance.Player.BuyWeaponServerRpc(weapon.Code);
            }
        }

        GameManager.Instance.Player.Balance.OnValueChanged += (o, n) => CheckBalance();

        CheckBalance();
    }

    private void OnEnable()
        => CheckBalance();

    private void CheckBalance()
    {
        foreach (var weaponButton in _weaponGroupLayoutGroup.transform.GetComponentsInChildren<UIWeaponStoreWeaponButton>())
        {
            weaponButton.Button.interactable = weaponButton.Weapon.Price <= GameManager.Instance.Player.Balance.Value;
        }
    }

    #endregion
}
