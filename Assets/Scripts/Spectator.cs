using System.Linq;
using UnityEngine;
using System.Collections.Generic;

public class Spectator : Singleton<Spectator>
{
    private Player _player;

    private bool _isSpectate;

    void Start()
    {
        
    }

    public void Spectate(Player player)
    {
        if (_player)
        {
            HUD.Singleton.RemovePlayer(_player);

            if (_player.Controller.Value.TryGet(out PlayerController _playerController))
            {
                _playerController.ChangeModelState(PlayerController.Layers.Default);
            }
        }

        HUD.Singleton.SetPlayer(player);        

        if (player.Controller.Value.TryGet(out PlayerController playerController))
        {
            playerController.ChangeModelState(PlayerController.Layers.Hand);
        }

        _player = player;
    }

    public void Spectate(int offset)
    {
        var players = new List<Player>();//.Where(player => player.Team.Value == _player.Team.Value && player.Controller.Value.TryGet(out PlayerController playerController) && playerController.enabled).ToList();
        foreach (var player in GameManager.Instance.NetworkPlayers)
            if(player.TryGet(out Player playerObject) && playerObject.Team.Value == _player.Team.Value && playerObject.Controller.Value.TryGet(out PlayerController playerController) && playerController.enabled)
            {
                players.Add(playerObject); 
            }
        Spectate(players[(players.IndexOf(_player) + offset + players.Count) % players.Count]);
    }

    void Update()
    {
        if (_isSpectate)
        {
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                Spectate(1);
            }

            if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                Spectate(-1);
            }
        }       
    }
}
