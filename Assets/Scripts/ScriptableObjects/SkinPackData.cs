using System.Collections;
using UnityEngine;

[CreateAssetMenu]
public class SkinPackData : ScriptableObject
{
    [SerializeField]
    private Teams _team;
    public Teams Team => _team;

    [SerializeField]
    private PlayerModel[] _skins;
    public PlayerModel[] Skins => _skins;

    public PlayerModel GetRandomSkin()
        => _skins[Random.Range(0, _skins.Length)];
    

    
}
