using Unity.Netcode;
using UnityEngine;

public class UIKillfeedPanel : MonoBehaviour
{
    [SerializeField]
    private UIKillfeedSlot _slotPrefab;

    private void Start()
    {        
        void OnPlayerDied(Player player)
        {
            player.Died += killer => Instantiate(_slotPrefab, transform).Initialize(killer, player);
        }

        foreach(var player in GameManager.Instance.Players)        
            OnPlayerDied(player);            
    }
}
