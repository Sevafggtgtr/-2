using UnityEngine;
using UnityEngine.UI;

public class UIRoundFinishPanel : MonoBehaviour
{
    [SerializeField]
    private Text _winningTeamText;

    private Image _panelImage;

    void Start()
    {
        _panelImage = GetComponent<Image>();
    }

    public void Initialize(Teams winningTeam)
    {
        var winningTeamData = GameManager.Singleton.GetTeamData(winningTeam);

        _winningTeamText.text = "Победила команда " + winningTeamData.Name + "!";

        _panelImage.color = winningTeamData.Color;
    }
}
