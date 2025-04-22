using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]
public class UIManager : Singleton<UIManager>
{
    #region Variables

    //protected event UnityAction PanelOpened = delegate { };
    //protected event UnityAction<UIPanel> PanelClose = delegate { };
    //protected event UnityAction PanelClosed = delegate { };

    protected UIPanel _panel;

    private AudioSource _audioSource;

    #endregion

    #region Methods

    protected override void Initialize()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    public virtual void OpenPanel(UIPanel panel)
    {
        panel.gameObject.SetActive(true);

        _panel = panel;

        //PanelOpened.Invoke();
    }

    public virtual void ClosePanel()
    {
        if (!_panel || !_panel.IsEscapeable)
            return;
        
        _panel.gameObject.SetActive(false);

        _panel = null;

        //PanelClosed.Invoke();
    }

    public void PlaySound()
    {
        _audioSource.Play();
    }

    protected virtual void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && _panel && _panel.IsEscapeable)    
            ClosePanel();
    }

    #endregion
}
