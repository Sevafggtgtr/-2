using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GameMode : ScriptableObject
{
    [SerializeField]
    private int[] _defaultWeaponIndices;
    public int[] DefaultWeaponIndices => _defaultWeaponIndices;

    [SerializeField]
    private int _roundsCount,
            _warmupTime,
            _roundTime,
            _teamPlayersCount,
            _buyTime,
            _roundEndTime,
            _startBalance;
    public int RoundCount => _roundsCount;
    public int WarmupTime => _warmupTime;
    public int RoundTime => _roundTime;
    public int TeamPlayersCount => _teamPlayersCount;
    public int BuyTime => _buyTime;
    public int RoundEndTime => _roundEndTime;
    public int StartBalance => _startBalance;
}
