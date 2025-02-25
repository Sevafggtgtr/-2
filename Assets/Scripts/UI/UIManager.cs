using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]
public class UIManager : Singleton<UIManager>
{
    protected event UnityAction Closed = delegate { };
    protected event UnityAction Opened = delegate { };

    protected GameObject _panel;

    private AudioSource _audioSource;

    protected override void Initialize()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    public virtual void OpenPanel(GameObject panel)
    {
        if (!_panel)
        {
            panel.gameObject.SetActive(true);

            _panel = panel;

            Opened.Invoke();
        }        
    }

    public virtual void ClosePanel()
    {
        _panel.gameObject.SetActive(false);

        _panel = null;

        Closed.Invoke();
    }

    public void PlaySound()
    {
        _audioSource.Play();
    }

    protected virtual void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && _panel)       
            ClosePanel();
    }
}
