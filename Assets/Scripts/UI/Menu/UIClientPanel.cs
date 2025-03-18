using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class UIClientPanel : UIPanel
{
    [SerializeField]
    private InputField _adressInputField;
    [SerializeField]
    private UIButton _joinButton;

    protected override void OnStart()
    {
        _joinButton.Clicked += () =>
        {
            NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = _adressInputField.text;

            NetworkManager.Singleton.StartClient();
        };
    }
}
