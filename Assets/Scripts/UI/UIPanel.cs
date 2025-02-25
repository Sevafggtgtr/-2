using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class UIPanel : MonoBehaviour
{
    [SerializeField]
    private UIButton _exitButton;

    void Start()
    {
        _exitButton.OnClick += () => UIManager.Instance.ClosePanel();

        OnStart();
    }

    protected abstract void OnStart();
}
