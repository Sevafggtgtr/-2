using UnityEngine;

public abstract class UIPanel : MonoBehaviour
{
    [SerializeField]
    private UIButton _exitButton;

    private void Start()
    {
        _exitButton.OnClick += () => UIManager.Instance.ClosePanel();

        OnStart();
    }

    protected virtual void OnStart() { }
}
