using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.Events;

public class UIRespawnMenu : MonoBehaviour
{
    public event UnityAction Disconnect;

    public event UnityAction Respawn;

    [SerializeField]
    private Button _respawnButton,
                   _exitButton;

    void Start()
    {
        _exitButton.onClick.AddListener(() => Disconnect.Invoke());

        _respawnButton.onClick.AddListener(() =>
        {
            Respawn.Invoke();

            gameObject.SetActive(false);
        });
    }    

    void Update()
    {
        
    }
}
