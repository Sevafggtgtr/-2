using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIServerCreationPanel : MonoBehaviour
{
    [SerializeField]
    private UIMapSelectionButton _mapSelectionButtonPrefab;

    [SerializeField]
    private LayoutGroup _mapLayoutGroup;

    [SerializeField]
    private Button _startGameButton;

    private MapData _map;

    void Start()
    {
        foreach(var map in GameManager.Singleton.Maps)
        {
            var mapSelectionButton = Instantiate(_mapSelectionButtonPrefab, _mapLayoutGroup.transform);

            mapSelectionButton.Initialize(map);

            mapSelectionButton.GetComponent<Button>().onClick.AddListener(() => _map =  map);
        }

        _startGameButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();

            NetworkManager.Singleton.SceneManager.LoadScene(_map.Scene.name,LoadSceneMode.Single);
        });
    }

    void Update()
    {
        
    }
}
