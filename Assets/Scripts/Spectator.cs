using UnityEngine;

public class Spectator : Singleton<Spectator>
{
    private Player _player;

    void Start()
    {
        
    }

    public void Spectate(Player player)
    {
        HUD.Singleton.RemovePlayer(player);

        HUD.Singleton.SetPlayer(player);

        if (player.Controller.Value.TryGet(out PlayerController playerController))
        {
            playerController.ChangeModelState(PlayerController.Layers.Hand);
        }
    }

    void Update()
    {
        
    }
}
