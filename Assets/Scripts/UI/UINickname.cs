using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class UINickname : MonoBehaviour
{
    [SerializeField]
    private Text _nicknameText,
                 _deathsText,
                 _killsText;

    public void Initialize(Player player)
    {
        player.Kills.OnValueChanged += (o, n) =>
        {
            _killsText.text = n.ToString();
        };
        player.Deaths.OnValueChanged += (o, n) =>
        {
            _deathsText.text = n.ToString();
        };

        player.Disconnected += () =>
            Destroy(gameObject);

        _nicknameText.text = player.Nickname.Value.ToString();

        player.Nickname.OnValueChanged += (o, n) =>
        {
            _nicknameText.text = n.ToString();
        };
    }
}
