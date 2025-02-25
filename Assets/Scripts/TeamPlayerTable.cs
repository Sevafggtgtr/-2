using UnityEditor.U2D.Aseprite;
using UnityEngine;
using UnityEngine.UI;

public class TeamPlayerTable : MonoBehaviour
{
    [SerializeField]
    private Text _teamNameText;

    [SerializeField]
    private VerticalLayoutGroup _nicknameLayoutGroup,
                                _killsLayoutGroup,
                                _deathsLayoutGroup;

    private Teams _team;

    [SerializeField]
    private Text _textPrefab;

    public void Initialize(TeamData teamData)
    {
        _team = teamData.Team;

        _teamNameText.text = teamData.Name;
    }

    private void OnEnable()
    {
        foreach (var player in GameManager.Instance._Players)
            if (player.TryGet(out Player playerObject) && playerObject.Team.Value == _team)
                AddPlayer(playerObject);
    }

    public void AddPlayer(Player player)
    {
        var nickname = Instantiate(_textPrefab, _nicknameLayoutGroup.transform);
        nickname.text = player.Nickname.Value.ToString();
        var kills = Instantiate(_textPrefab, _killsLayoutGroup.transform);
        kills.text = player.Kills.Value.ToString();
        var deaths = Instantiate(_textPrefab, _deathsLayoutGroup.transform);
        deaths.text = player.Deaths.Value.ToString();

        player.Nickname.OnValueChanged += (o,n)
            => nickname.text = n.ToString();
        player.Kills.OnValueChanged += (o, n)
            => kills.text = n.ToString();
        player.Deaths.OnValueChanged += (o, n)
            => deaths.text = n.ToString();

        player.Team.OnValueChanged += (o, n) =>
        {
            Destroy(nickname.gameObject);
            Destroy(kills.gameObject);
            Destroy(deaths.gameObject);
        };
             
    }
}
