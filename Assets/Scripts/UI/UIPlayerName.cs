using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPlayerName : MonoBehaviour
{
    [SerializeField]
    private Text _nicknameText;

    public void NickName(Player player)
    {
        _nicknameText.text = player.Nickname.Value.ToString();

        player.Nickname.OnValueChanged += (o, n) =>
        {
            _nicknameText.text = n.ToString();
        };
    }
}
