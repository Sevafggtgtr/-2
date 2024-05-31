using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIMapSelectionButton : MonoBehaviour
{
    private MapData _map;
    public MapData Map => _map;

    [SerializeField]
    private Image _image;

    void Start()
    {
        
    }

    public void Initialize(MapData map)
    {
        _map = map;

        _image.sprite = _map.Icon;
    }

    void Update()
    {
        
    }
}
