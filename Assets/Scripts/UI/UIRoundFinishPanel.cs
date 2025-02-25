using UnityEngine;
using UnityEngine.UI;

public class UIRoundFinishPanel : MonoBehaviour
{
    [SerializeField]
    private Text _winningTeamText;

    [SerializeField]
    private Image _panelImage;

    public void Initialize(Teams winningTeam)
    {
        var winningTeamData = GameManager.Instance.GetTeamData(winningTeam);

        _winningTeamText.text = "Победила команда " + winningTeamData.Name + "!";

        _panelImage.color = winningTeamData.Color;
    }
}
