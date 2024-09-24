using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour
{
    protected event UnityAction Closed = delegate { };

    protected UIPanel _panel;

    protected void OpenPanel(UIPanel panel)
    {
        panel.gameObject.SetActive(true);

        _panel = panel;
    }

    private void ClosePanel()
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
