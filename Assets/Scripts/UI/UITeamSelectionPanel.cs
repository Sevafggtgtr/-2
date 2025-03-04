using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class UITeamSelectionPanel : MonoBehaviour
{
    public event UnityAction<Teams> TeamSelected = delegate { };

    [SerializeField]
    private Button _terroristTeamButton,
                   _counterTerroristTeamButton;

    private void Start()
    {
        void ChangeTeam(Teams team)
        {
            GameManager.Instance.Player.SelectTeamServerRpc(team);
            gameObject.SetActive(false);
        }

        _terroristTeamButton.onClick.AddListener(() => ChangeTeam(Teams.Terrorist));
        _counterTerroristTeamButton.onClick.AddListener(() => ChangeTeam(Teams.CounterTerrorist));
    }
}
