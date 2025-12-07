using Mirror;
using Steamworks;
using System.Collections;

public class LobbyPlayer : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnPlayerNameChanged))] private string _playerName;
    [SyncVar(hook = nameof(OnReadyStateChanged))] private bool _isReady = false;
    [SyncVar(hook = nameof(OnHostChanged))] private bool _isHost;
    [SyncVar] private ulong _steamId;

    public string PlayerName => _playerName;
    public ulong SteamId => _steamId;
    public bool IsReady => _isReady;
    public bool IsHost => _isHost;

    public override void OnStartServer()
    {
        base.OnStartServer();
        _isHost = connectionToClient == NetworkServer.localConnection;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        PlayerListManager.Instance?.AddOrUpdatePlayerUI(this);
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        var name = SteamFriends.GetPersonaName();
        var id = SteamUser.GetSteamID().m_SteamID;

        CmdInitPlayer(FormatPlayerName(name), id);
        LobbyManager.Instance.RefreshReadyButton(IsReady);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        PlayerListManager.Instance?.RemovePlayerUI(this);
    }

    private void OnPlayerNameChanged(string _, string __)
    {
        PlayerListManager.Instance?.AddOrUpdatePlayerUI(this);
    }

    private void OnReadyStateChanged(bool _, bool __)
    {
        PlayerListManager.Instance?.AddOrUpdatePlayerUI(this);

        LobbyManager.Instance?.RefreshStartButton();
        
        if (isLocalPlayer) 
            LobbyManager.Instance?.RefreshReadyButton(IsReady);
    }

    private void OnHostChanged(bool _, bool __)
    {
        PlayerListManager.Instance?.AddOrUpdatePlayerUI(this);
    }


    [Command]
    private void CmdInitPlayer(string name, ulong id)
    {
        _playerName = FormatPlayerName(name);
        _steamId = id;
        _isReady = false;
    }

    [Command]
    public void CmdSetReady(bool value)
    {
        _isReady = value;
    }

    [Command]
    public void CmdKickPlayer(ulong playerID)
    {
        if (connectionToClient != NetworkServer.localConnection)
            return;

        ServerKickBySteamId(playerID);
    }

    [Server]
    public void ServerKickBySteamId(ulong targetSteamId)
    {
        if (connectionToClient != NetworkServer.localConnection)
            return;

        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn?.identity == null) continue;
            if (!conn.identity.TryGetComponent(out LobbyPlayer lp)) continue;
            if (lp.SteamId != targetSteamId) continue;

            StartCoroutine(KickNextFrame(conn));
            break;
        }
    }

    [Server]
    private IEnumerator KickNextFrame(NetworkConnectionToClient conn)
    {
        if (conn == null) yield break;

        if (conn.identity != null)
            NetworkServer.DestroyPlayerForConnection(conn);

        conn.Send(new ShutdownMessage { Reason = DisconnectReason.Kicked });
        yield return null;    
        conn.Disconnect();
    }

    private static string FormatPlayerName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Player";

        const int maxLen = Constants.MaxPlayerNameLength;
        return name.Length <= maxLen ? name : name.Substring(0, maxLen);
    }
}
