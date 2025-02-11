using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class UIPlayerTable : MonoBehaviour
{
    private Dictionary<Teams, TeamPlayerTable> _teamPlayerTables = new Dictionary<Teams, TeamPlayerTable>();

    [SerializeField]
    private TeamPlayerTable _teamPlayerTablePrefab;

    [SerializeField]
    private VerticalLayoutGroup _teamPlayerTableLayoutGroup;

    private void Start()
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
}
