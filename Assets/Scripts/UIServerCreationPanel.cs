using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIServerCreationPanel : UIPanel
{
    [SerializeField]
    private UIMapSelectionButton _mapSelectionButtonPrefab;

    [SerializeField]
    private LayoutGroup _mapLayoutGroup;

    [SerializeField]
    private Button _startGameButton;

    private UIMapSelectionButton _mapSelectionButton;   

    protected override void OnStart()
    {
        foreach(var map in GameManager.Instance.Maps)
        {
            var mapSelectionButton = Instantiate(_mapSelectionButtonPrefab, _mapLayoutGroup.transform);

            mapSelectionButton.Initialize(map);

            mapSelectionButton.OnClick += () => 
            {
                _mapSelectionButton?.Select(false);

                _mapSelectionButton = mapSelectionButton;

                mapSelectionButton.Select(true);
            };
        }

        _startGameButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();

            NetworkManager.Singleton.SceneManager.LoadScene(_mapSelectionButton.Map.Scene.name,LoadSceneMode.Single);
        });
    }

    void Update()
    {
        
    }
}
