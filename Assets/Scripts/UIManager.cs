using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour
{
    protected event UnityAction Closed = delegate { };
    protected event UnityAction Opened = delegate { };

    protected GameObject _panel;

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

    protected void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && _panel)
        {            
            ClosePanel();
        }
    }
}
