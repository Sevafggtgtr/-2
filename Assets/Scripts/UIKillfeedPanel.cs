using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class UIKillfeedPanel : MonoBehaviour
{
    [SerializeField]
    private UIKillfeedSlot _slotPrefab;
    void Start()
    {        
        void OnPlayerDied(NetworkBehaviourReference player)
        {
            if (player.TryGet(out Player playerObject))
                playerObject.Died += (killer, causeCode) => Instantiate(_slotPrefab, transform).Initialize(killer, playerObject, causeCode);
        }

        GameManager.Instance.NetworkPlayers.OnListChanged += NLevent => OnPlayerDied(NLevent.Value);

        foreach(var player in GameManager.Instance.NetworkPlayers)        
            OnPlayerDied(player);            
    }
}
