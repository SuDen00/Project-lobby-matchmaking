using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class PlayerListManager : MonoBehaviour
{
    public static PlayerListManager Instance { get; private set; }

    [SerializeField] private Transform contentRoot;
    [SerializeField] private LobbyPlayerUI itemPrefab;

    private readonly Dictionary<LobbyPlayer, LobbyPlayerUI> uiByPlayer = new();
    

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void AddOrUpdatePlayerUI(LobbyPlayer player)
    {
        if (player == null) return;

        if (!uiByPlayer.TryGetValue(player, out var playerUI))
        {
            playerUI = Instantiate(itemPrefab, contentRoot);
            playerUI.Initialize(player);
            uiByPlayer[player] = playerUI;
        }

        playerUI.SetPlayerName(player.PlayerName);
        playerUI.SetReadyIndicator(player.IsReady);
        playerUI.SetHostIndicator(player.IsHost); 
    }


    public void RemovePlayerUI(LobbyPlayer player)
    {
        if (player == null) return;

        if (uiByPlayer.TryGetValue(player, out var playerUI))
        {
            Destroy(playerUI.gameObject);
            uiByPlayer.Remove(player);
        }
    }
}
