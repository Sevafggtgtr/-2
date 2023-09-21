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
        _terroristTeamButton.onClick.AddListener(() => TeamChoosed.Invoke(Teams.Terrorist));
        _counterTerroristTeamButton.onClick.AddListener(() => TeamChoosed.Invoke(Teams.CounterTerrorist));
    }
}
