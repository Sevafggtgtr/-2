using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UISelectebleButton : UIButton
{
    [SerializeField]
    private Color _selectionColor;

    public void Select(bool value)
    {
        _button.image.color = value ? _selectionColor : Color.white;
    }

}
