using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UIKillfeedSlot : MonoBehaviour
{
    [SerializeField]
    private Text _killerText,
                 _targetText;

    public void Initialize(Player killer,Player target)
    {        
        _killerText.text = killer.Nickname.Value.ToString();
        _targetText.text = target.Nickname.Value.ToString();

        _killerText.color = GameManager.Singleton.Teams.First(team => team.Team == killer.Team.Value).Color;
        _targetText.color = GameManager.Singleton.Teams.First(team => team.Team == target.Team.Value).Color;

        Invoke("Destroy", 5);
    }

    private void Destroy()
    {
        Destroy(gameObject);
    }
}
