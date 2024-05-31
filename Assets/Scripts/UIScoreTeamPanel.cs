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
    private Text[] _teamScoreTexts;

    void Start()
    {
        for (int i = 0; i < _teamScoreTexts.Length; i++)
        {
            int index = i;
            GameManager.Singleton.Points[index].OnValueChanged += (oldPoints, newPoints) =>
            {
                _teamScoreTexts[index].text = newPoints.ToString();
            };
        }

        GameManager.Singleton.Time.OnValueChanged += (oldTime, newTime) =>
        {
            _timeText.text = (GameManager.Singleton.Time.Value / 60).ToString() + ":" + (GameManager.Singleton.Time.Value % 60).ToString();
        };        
    }       

    void Update()
    {
        
    }
}
