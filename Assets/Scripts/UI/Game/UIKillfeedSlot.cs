using UnityEngine;
using UnityEngine.UI;

public class UIKillfeedSlot : MonoBehaviour
{
    [SerializeField]
    private Text _killerText,
                 _targetText;
    [SerializeField]
    private Image _causeImage;

    public void Initialize(IKilleable killer, Player target)
    {        
        _killerText.text = killer.KillerText;
        _targetText.text = target.Nickname.Value.ToString();

        _killerText.color = GameManager.Instance.GetTeamData(killer.KillerTeam).Color;
        _targetText.color = GameManager.Instance.GetTeamData(target.Team.Value).Color;

        Invoke("Destroy", 5);
    }

    private void Destroy()
    {
        Destroy(gameObject);
    }
}
