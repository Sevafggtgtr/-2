using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ChatPanel : NetworkBehaviour
{
    #region Variables

    [SerializeField]
    private ScrollRect _scrollRect;

    [SerializeField]
    private InputField _inputField;

    [SerializeField]
    private Text _messagePrefab;

    #endregion

    #region Methods

    private void Start()
    {
        _inputField.onSubmit.AddListener(call =>
        {
            SendMessageServerRpc($"<color=#{ColorUtility.ToHtmlStringRGB(GameManager.Instance.GetTeamData(Spectator.Instance.Player.Team.Value).Color)}>{Spectator.Instance.Player.Nickname.Value}</color>: {call}");

            _inputField.text = "";
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
        Instantiate(_messagePrefab, _scrollRect.content).text = text;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (!_inputField.gameObject.activeSelf)
            {
                //HUD.Instance.OpenPanel(_inputField.gameObject);

                _inputField.Select();
            }   
        }
    }

    #endregion
}
