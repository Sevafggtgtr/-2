using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class UIScoreTeamPanel : MonoBehaviour
{
    [SerializeField]
    private Text _timeText;

    [SerializeField]
    private TeamScoreText[] _teamScoreTexts;

    [Serializable]
    private struct TeamScoreText
    {
        [SerializeField]
        private Teams _team;
        public Teams Team => _team;

        [SerializeField]
        private Text _text;
        public Text Text => _text;
    }

    void Start()
    {
        void ChangeTeamScoreTexts()
        {
            for (int i = 0; i < _teamScoreTexts.Length; i++)
                _teamScoreTexts[i].Text.text = GameManager.Instance.Points[_teamScoreTexts[i].Team].ToString();
        }

        ChangeTeamScoreTexts();

        GameManager.Instance.RoundFinished += (winningTeam) =>
            ChangeTeamScoreTexts();

        GameManager.Instance.Time.OnValueChanged += (oldTime, newTime) =>
        {
            _timeText.text = (GameManager.Instance.Time.Value / 60).ToString() + ":" + (GameManager.Instance.Time.Value % 60).ToString();
        };        
    }       

    void Update()
    {
        
    }
}
