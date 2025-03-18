using UnityEngine;

public interface IKilleable
{
    public string KillerText { get; }
    public Sprite KillerSprite { get; }
    public Teams KillerTeam { get; }


}
