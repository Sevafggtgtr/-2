using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ChatInputFied : NetworkBehaviour
{
    [SerializeField]
    private Text _messagePrefab;

    [SerializeField]
    private ScrollRect _chatScrollRect;

    [SerializeField]
    private InputField _chatInputField;

    void Start()
    {
        _chatInputField.onSubmit.AddListener(call =>
        {
            SendMessageServerRpc($"<color=#{ColorUtility.ToHtmlStringRGB(GameManager.Instance.GetTeamData(Spectator.Instance.Player.Team.Value).Color)}>{Spectator.Instance.Player.Nickname.Value}</color>: {call}");

            _chatInputField.text = "";
        });
    }

    [ServerRpc]
    private void SendMessageServerRpc(string text)
    {
        SendMessageClientRpc(text);
    }

    [ClientRpc]
    private void SendMessageClientRpc(string text)
    {
        Instantiate(_messagePrefab, _chatScrollRect.content).text = text;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (!_chatInputField.gameObject.activeSelf)
            {
                HUD.Singleton.OpenPanel(_chatInputField.gameObject);

                _chatInputField.Select();
            }
                
            //else
            //HUD.Singleton.ClosePanel();     
        }


    }
}
