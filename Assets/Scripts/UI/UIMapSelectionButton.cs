using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIMapSelectionButton : UISelectebleButton
{
    private MapData _map;
    public MapData Map => _map;

    [SerializeField]
    private Image _image;

    [SerializeField]
    private Text _nameText;

    public void Initialize(MapData map)
    {
        _map = map;

        _image.sprite = _map.Icon;

        _nameText.text = _map.Name;
    }

    void Update()
    {
        
    }
}
