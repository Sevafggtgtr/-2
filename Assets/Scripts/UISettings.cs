using UnityEngine;
using UnityEngine.UI;

public class UISettings : MonoBehaviour
{
    [SerializeField]
    private Slider _mouseSensitivitySlider;

    void Start()
    {
        _mouseSensitivitySlider.onValueChanged.AddListener(call => GameManager.Instance.Config.Sensitivity = call);
    }

    void Update()
    {
        
    }
}
