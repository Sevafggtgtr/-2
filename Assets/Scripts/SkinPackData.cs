using System.Collections;
using UnityEngine;

[CreateAssetMenu]
public class SkinPackData : ScriptableObject
{
    [SerializeField]
    private Teams _team;
    public Teams Team => _team;

    [SerializeField]
    private PlayerAnimator[] _skins;
    public PlayerAnimator[] Skins => _skins;

    public PlayerAnimator GetRandomSkin()
        => _skins[Random.Range(0, _skins.Length)];
    

    
}
