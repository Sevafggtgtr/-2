using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu]
public class MapData : ScriptableObject
{
    [SerializeField]
    private Object _scene;
    public Object Scene => _scene;

    [SerializeField]
    private Sprite _icon;
    public Sprite Icon => _icon;

    [SerializeField]
    private SkinPackData[] _skinPackDatas;
    public SkinPackData[] SkinPackDatas => _skinPackDatas;
}
