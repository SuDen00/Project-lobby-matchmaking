using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using Steamworks;
using System;

public class NetworkManagerExtended : NetworkManager
{
    [Scene, SerializeField] private string mainMenuSceneName;
    [Scene, SerializeField] private string lobbySceneName;
    [Scene, SerializeField] private string gameSceneName;
    [SerializeField] private GameObject lobbyPlayerPrefab;
    [SerializeField] private GameObject gamePlayerPrefab;

    private bool gameInProgress;
    
    public override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (!string.IsNullOrEmpty(lobbySceneName) &&
            SceneManager.GetActiveScene().name != lobbySceneName)
        {
            ServerChangeScene(lobbySceneName);
        }
    }
    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        if (gameInProgress)
        {
            conn.Disconnect();
            return;
        }

        if (!ulong.TryParse(conn.address, out ulong clientId))
        {
            conn.Disconnect();
            return;
        }

        var steamLobbyManager = SteamLobbyManager.Instance;
        if (steamLobbyManager == null || !steamLobbyManager.CurrentLobbyID.IsValid())
        {
            conn.Disconnect(); return; 
        }

        if (!IsInCurrentSteamLobby(clientId))
        {
            conn.Disconnect();
            return;
        }

        string lobbyType = SteamMatchmaking.GetLobbyData(
        steamLobbyManager.CurrentLobbyID, LobbyDataKeys.LobbyType);

        if (lobbyType == "friends")
        {
            bool isFriend = SteamFriends.HasFriend(new CSteamID(clientId), EFriendFlags.k_EFriendFlagImmediate);
            if (!isFriend)
            {
                string joinMethod = SteamMatchmaking.GetLobbyMemberData(
                    steamLobbyManager.CurrentLobbyID, new CSteamID(clientId), LobbyDataKeys.JoinMethod);

                bool allowedByCode = string.Equals(joinMethod, "code", StringComparison.OrdinalIgnoreCase);
                if (!allowedByCode) { conn.Disconnect(); return; }
            }
        }

        base.OnServerConnect(conn);
    }
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        if (conn.identity != null) return;

        string scene = SceneManager.GetActiveScene().name;
        var prefab = scene == gameSceneName ? gamePlayerPrefab : lobbyPlayerPrefab;

        var player = Instantiate(prefab);
        NetworkServer.AddPlayerForConnection(conn, player);
    }
    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);

        bool isGame = sceneName == gameSceneName;
        if (!isGame) gameInProgress = false;

        var lobbyId = SteamLobbyManager.Instance.CurrentLobbyID;
        if (lobbyId.IsValid())
            SteamMatchmaking.SetLobbyData(lobbyId, LobbyDataKeys.SessionState, isGame ? "game" : "lobby");
    }
    public override void OnStopServer()
    {
        var lobbyId = SteamLobbyManager.Instance.CurrentLobbyID;
        
        if (lobbyId.IsValid())
        {
            SteamMatchmaking.SetLobbyJoinable(lobbyId, false);
            SteamMatchmaking.SetLobbyData(lobbyId, LobbyDataKeys.SessionState, "closed");
        }

        base.OnStopServer();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        NetworkClient.RegisterHandler<ShutdownMessage>(msg =>
        {
            ExitCoordinator.Instance.RequestExit(msg.Reason);
        });

    }
    public override void OnClientChangeScene(string newSceneName, SceneOperation sceneOperation, bool customHandling)
    {
        base.OnClientChangeScene(newSceneName, sceneOperation, customHandling);
        
    }
    public override void OnClientSceneChanged()
    {
        if (!NetworkClient.ready)
            NetworkClient.Ready();

        if (NetworkClient.localPlayer == null)
            NetworkClient.AddPlayer();

        base.OnClientSceneChanged();
    }
    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();

        if (ExitCoordinator.Instance?.IsExiting == true)
            return;

        var reason = SteamLobbyManager.Instance.IsManualDisconnect
            ? DisconnectReason.ManualLeft
            : DisconnectReason.Disconnected;

        ExitCoordinator.Instance.RequestExit(reason);
    }
    public override void OnStopClient()
    {
        base.OnStopClient();
    }
    

    [Server]
    public void ReturnToLobby()
    {
        if (!NetworkServer.active) return;
        ServerChangeScene(lobbySceneName);
    }
    [Server]
    public void StartGame()
    {
        if (!NetworkServer.active) return;
        if (gameInProgress) return;
        if (SceneManager.GetActiveScene().name != lobbySceneName) return;

        gameInProgress = true;
        ServerChangeScene(gameSceneName);
    }
    private bool IsInCurrentSteamLobby(ulong clientId)
    {
        var lobby = SteamLobbyManager.Instance.CurrentLobbyID;
        if (!lobby.IsValid()) return false;

        int count = SteamMatchmaking.GetNumLobbyMembers(lobby);
        for (int i = 0; i < count; i++)
        {
            var member = SteamMatchmaking.GetLobbyMemberByIndex(lobby, i);
            if (member.m_SteamID == clientId) return true;
        }
        return false;
}
}
