using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UISlider : MonoBehaviour
{
    public event UnityAction<float> ValueChanged;

    [SerializeField]
    private Text _text;
    [SerializeField]
    private Slider _slider;
    [SerializeField]
    private InputField _inputField;

    public void ChangeValue(float value)
    {
        _slider.SetValueWithoutNotify(value);
        _inputField.SetTextWithoutNotify(value.ToString("0.00"));
        ValueChanged.Invoke(value);
    }

    void Start()
    {        
        _slider.onValueChanged.AddListener(ChangeValue);
        _inputField.onValueChanged.AddListener(call => ChangeValue(Mathf.Clamp01(float.Parse(call))));
    }
}
