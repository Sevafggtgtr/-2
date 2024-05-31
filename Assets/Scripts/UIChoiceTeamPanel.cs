using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.Events;

public class UIChoiceTeamPanel : MonoBehaviour
{
    public event UnityAction<Teams> TeamChoosed;

    [SerializeField]
    private Button _terroristTeamButton,
                   _counterTerroristTeamButton;

    void Start()
    {
        _terroristTeamButton.onClick.AddListener(() =>
        {
            Player.Singleton.ChangeTeam(Teams.Terrorist);
            gameObject.SetActive(false);
        });

        _counterTerroristTeamButton.onClick.AddListener(() =>
        {
            Player.Singleton.ChangeTeam(Teams.CounterTerrorist);
            gameObject.SetActive(false);
        });
    }
}
