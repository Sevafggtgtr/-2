using UnityEngine;

public class UISelectebleButton : UIButton
{
    [SerializeField]
    private Color _selectionColor;

    public void Select(bool value)
    {
        Button.image.color = value ? _selectionColor : Color.white;
    }

}
