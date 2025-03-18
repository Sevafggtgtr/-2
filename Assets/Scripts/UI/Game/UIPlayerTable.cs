using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPlayerTable : UIPanel
{
    #region Variables

    private Dictionary<Teams, TeamPlayerTable> _teamPlayerTables = new Dictionary<Teams, TeamPlayerTable>();

    [SerializeField]
    private VerticalLayoutGroup _teamPlayerTableLayoutGroup;

    [SerializeField]
    private TeamPlayerTable _teamPlayerTablePrefab;

    #endregion

    #region Methods

    protected override void OnStart()
    {   
        GameManager.Instance.PlayerConnected += (player) =>
        {
            player.Team.OnValueChanged += (previousValue, newValue)
                => _teamPlayerTables[newValue].AddPlayer(player);
        };

        /*GameManager.Instance.PlayerDisconnect += (player) 
            => Initialize();*/      
    }

    private void OnEnable()
    {
        foreach(var team in _teamPlayerTables.Values)
            Destroy(team.gameObject);

        foreach (var teamData in GameManager.Instance.TeamDatas)
        {
            var teamPlayerTable = Instantiate(_teamPlayerTablePrefab, _teamPlayerTableLayoutGroup.transform);

            teamPlayerTable.Initialize(teamData);

            _teamPlayerTables.Add(teamData.Team, teamPlayerTable);
        }
    }

    #endregion
}
