public class UIMainMenu : UIManager
{
    private UIMenuPanel _menuPanel;

    private void Start()
    {
        _menuPanel = GetComponentInChildren<UIMenuPanel>();

        _panel = _menuPanel;
    }

    public override void OpenPanel(UIPanel panel)
    {
        base.ClosePanel();

        base.OpenPanel(panel);
    }

    public override void ClosePanel()
    {
        if (_panel == _menuPanel)
        {

        }
        else
        {
            OpenPanel(_menuPanel);
        }
    }
}
