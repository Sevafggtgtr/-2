using UnityEngine;

public class UIMainMenu : UIManager
{
    private UIMenuPanel _menuPanel;

    private void Start()
    {
        _menuPanel = GetComponentInChildren<UIMenuPanel>();

        _panel = _menuPanel;
    }

    public override void ClosePanel()
    {
        if(_panel == _menuPanel)
            base.ClosePanel();
        else
            OpenPanel(_menuPanel);
    }
}
