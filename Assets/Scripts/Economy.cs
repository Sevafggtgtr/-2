using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class Economy : ScriptableObject
{
    [SerializeField]
    private int _killAward,
                _winAward,
                _loseAward;
    public int KillAward => _killAward;
    public int winAward => _winAward;
    public int LoseAward => _loseAward;
}
