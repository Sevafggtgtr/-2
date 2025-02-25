using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UIKillfeedSlot : MonoBehaviour
{
    [SerializeField]
    private Text _killerText,
                 _targetText;
    [SerializeField]
    private Image _causeImage;

    public void Initialize(Player killer,Player target,string causeCode)
    {        
        _killerText.text = killer.Nickname.Value.ToString();
        _targetText.text = target.Nickname.Value.ToString();
        //_causeImage.sprite = Resources.Load<Sprite>(causeCode);

        _killerText.color = GameManager.Instance.GetTeamData(killer.Team.Value).Color;
        _targetText.color = GameManager.Instance.GetTeamData(target.Team.Value).Color;

        Invoke("Destroy", 5);
    }

    private void Destroy()
    {
        Destroy(gameObject);
    }
}
