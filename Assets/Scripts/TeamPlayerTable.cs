using UnityEngine;
using UnityEngine.UI;

public class TeamPlayerTable : MonoBehaviour
{
    #region Variables

    [SerializeField]
    private Text _teamNameText;

    [SerializeField]
    private VerticalLayoutGroup _nicknameLayoutGroup,
                                _killsLayoutGroup,
                                _deathsLayoutGroup;

    private Teams _team;

    [SerializeField]
    private Text _textPrefab;

    #endregion Variables

    #region Methods

    public void Initialize(TeamData teamData)
    {
        GetComponent<Image>().color = teamData.Color;

        _teamNameText.text = teamData.Name;

        _team = teamData.Team;
    }

    private void OnEnable()
    {
        foreach (var player in GameManager.Instance.Players)
            if (player.TryGet(out Player playerObject) && playerObject.Team.Value == _team)
                AddPlayer(playerObject);
    }

    public void AddPlayer(Player player)
    {
        var nickname = Instantiate(_textPrefab, _nicknameLayoutGroup.transform);
        nickname.text = player.Nickname.Value.ToString();
        var kills = Instantiate(_textPrefab, _killsLayoutGroup.transform);
        kills.text = player.Kills.ToString();
        var deaths = Instantiate(_textPrefab, _deathsLayoutGroup.transform);
        deaths.text = player.Deaths.ToString();

        player.Nickname.OnValueChanged += (previousValue, newValue)
            => nickname.text = newValue.ToString();
        player.Kills.OnValueChanged += (previousValue, newValue)
            => kills.text = newValue.ToString();
        player.Deaths.OnValueChanged += (previousValue, newValue)
            => deaths.text = newValue.ToString();

        player.Team.OnValueChanged += (previousValue, newValue) =>
        {
            Destroy(nickname.gameObject);
            Destroy(kills.gameObject);
            Destroy(deaths.gameObject);
        };    
    }

    #endregion
}
