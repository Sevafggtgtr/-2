using UnityEngine;
using UnityEngine.UI;

public class UISettings : UIPanel
{
    [SerializeField]
    private UISlider _mouseSensitivitySlider,
                     _volumeSlider;

    [SerializeField]
    private UIButton _applyButton,
                     _restoreButton;

    protected override void OnStart()
    {
        _mouseSensitivitySlider.ValueChanged += call => GameManager.Instance.Config.Sensitivity = call;
        _volumeSlider.ValueChanged += call =>
        {
            GameManager.Instance.Config.Volume = call;

            AudioListener.volume = call;
        };

        _mouseSensitivitySlider.ChangeValue(GameManager.Instance.Config.Sensitivity);
        _volumeSlider.ChangeValue(GameManager.Instance.Config.Volume);

        _applyButton.OnClick += GameManager.Instance.Config.Save;

    }
}
