using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;

public class UITeamSelectionPanel : UIPanel
{
    #region Variables

    public event UnityAction<Teams> TeamSelected = delegate { };

    [SerializeField]
    private UIButton _terroristTeamButton,
                   _counterTerroristTeamButton;

    #endregion

    #region Methods

    protected override void OnStart()
    {
        void ChangeTeam(Teams team)
        {
            GameManager.Instance.Player.SelectTeamServerRpc(team);
            gameObject.SetActive(false);
        }

        _terroristTeamButton.Clicked += () => ChangeTeam(Teams.Terrorist);
        _counterTerroristTeamButton.Clicked += () => ChangeTeam(Teams.CounterTerrorist);
    }

    #endregion
}
