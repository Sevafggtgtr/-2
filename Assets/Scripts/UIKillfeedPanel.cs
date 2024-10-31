using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class UIKillfeedPanel : MonoBehaviour
{
    [SerializeField]
    private UIKillfeedSlot _slotPrefab;
    void Start()
    {        
        PlayerController.Spawn += playerController => playerController.Died += (killer,causeCode) => Instantiate(_slotPrefab, transform).Initialize(killer, playerController.Player, causeCode);
    }
}
