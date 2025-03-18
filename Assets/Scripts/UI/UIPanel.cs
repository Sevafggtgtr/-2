using UnityEngine;

public abstract class UIPanel : MonoBehaviour
{
    [SerializeField]
    protected bool _isEscapeable = true;
    public bool IsEscapeable => _isEscapeable;

    [SerializeField]
    private UIButton _exitButton;

    private void Start()
    {
        if(_exitButton)
            _exitButton.Clicked += () => UIManager.Instance.ClosePanel();

        OnStart();
    }

    protected virtual void OnStart() { }
}
