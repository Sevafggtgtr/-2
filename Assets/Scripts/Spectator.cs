using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Spectator : Singleton<Spectator>
{
    public event UnityAction<Player, Player> PlayerChanged;

    public Player Player { get; private set; }

    private bool _isSpectate;

    void Start()
    {

    }

    public void Spectate(Player player)
    {
        if (Player && Player.Controller.Value.TryGet(out PlayerController playerController))
        {
            playerController.ChangeModelState(PlayerController.Layers.Default);
        }

        if (player.Controller.Value.TryGet(out playerController))
        {
            playerController.ChangeModelState(PlayerController.Layers.Hand);
        }

        PlayerChanged.Invoke(Player, player);

        Player = player;
    }

    public void Spectate(int offset)
    {
        var players = new List<Player>();//.Where(player => player.Team.Value == _player.Team.Value && player.Controller.Value.TryGet(out PlayerController playerController) && playerController.enabled).ToList();
        foreach (var player in GameManager.Instance.NetworkPlayers)
            if (player.TryGet(out Player playerObject) && playerObject.Team.Value == Player.Team.Value && playerObject.Controller.Value.TryGet(out PlayerController playerController) && playerController.enabled)
            {
                players.Add(playerObject);
            }
        Spectate(players[(players.IndexOf(Player) + offset + players.Count) % players.Count]);
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
