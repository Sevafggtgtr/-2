using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButton : MonoBehaviour
{
    public event UnityAction Clicked = delegate { };

    public Button Button { get; private set; }

    private void Start()
    {
        Button = GetComponent<Button>(); 

        Button.onClick.AddListener(() =>
        {
            Clicked.Invoke();

            UIManager.Instance.PlaySound();
        });
    }
}
