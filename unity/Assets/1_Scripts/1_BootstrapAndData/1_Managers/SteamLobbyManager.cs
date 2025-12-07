using Mirror;
using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;

public class SteamLobbyManager : MonoBehaviour
{
    private const float InviteWhitelistTTL = 300f;
    public static SteamLobbyManager Instance { get; private set; }

    private Callback<LobbyCreated_t> onLobbyCreated;
    private Callback<LobbyEnter_t> onLobbyEntered;
    private Callback<LobbyMatchList_t> onLobbyListReceived;
    private Callback<GameLobbyJoinRequested_t> onLobbyJoinRequested;
    private Callback<LobbyChatUpdate_t> onLobbyChatUpdate;
    private Callback<LobbyInvite_t> onLobbyInvite;

    private CSteamID allowPrivateJoinFor = CSteamID.Nil;
    private readonly Dictionary<ulong, float> _inviteWhitelist = new();
    private bool _joinInProgress;
    private CSteamID _joiningLobby = CSteamID.Nil;
    private bool nextJoinByCode;

    public CSteamID CurrentLobbyID { get; private set; }
    public bool IsManualDisconnect { get; private set; }
    public bool JoinInProgress => _joinInProgress;

    public event Action<CSteamID, EChatRoomEnterResponse> LobbyJoinResultReceived;
    public event Action<EResult> LobbyCreationFailed;
    public event Action<CSteamID> LobbyJoinSucceeded;
    public event Action<List<CSteamID>> LobbyListReceived;
    public event Action<CSteamID> LobbyCreated;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

    }
    void OnEnable()
    {
        IsManualDisconnect = false;
        CurrentLobbyID = CSteamID.Nil;

        onLobbyInvite = Callback<LobbyInvite_t>.Create(HandleLobbyInvite);
        onLobbyCreated = Callback<LobbyCreated_t>.Create(HandleLobbyCreated);
        onLobbyEntered = Callback<LobbyEnter_t>.Create(HandleLobbyEntered);
        onLobbyListReceived = Callback<LobbyMatchList_t>.Create(HandleLobbyListReceived);
        onLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(HandleLobbyJoinRequested);
        onLobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(HandleLobbyChatUpdate);
    }
    void OnDisable()
    {
        onLobbyCreated?.Dispose();
        onLobbyEntered?.Dispose();
        onLobbyListReceived?.Dispose();
        onLobbyJoinRequested?.Dispose();
        onLobbyChatUpdate?.Dispose();
        onLobbyInvite?.Dispose();

        _joinInProgress = false;
        _joiningLobby = CSteamID.Nil;  
    }

    public void RequestLobbyList() => SteamMatchmaking.RequestLobbyList();
    public void JoinLobby(CSteamID lobbyID)
    {
        if (!lobbyID.IsValid()) return;
        if (_joinInProgress) return;

        _joiningLobby = lobbyID;   
        _joinInProgress = true;
        SteamMatchmaking.JoinLobby(lobbyID);
    }
    public void ClearLobbyID() => CurrentLobbyID = CSteamID.Nil;
    public void SetManualDisconnect(bool value) => IsManualDisconnect = value;
    public void AllowPrivateJoinOnce(CSteamID lobbyId) => allowPrivateJoinFor = lobbyId;
    public void CreateLobby(int maxPlayers)
    {
        ResetCurrentLobby();

        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, maxPlayers);
    }
    public void AllowNextJoinByCode() => nextJoinByCode = true;

    private void HandleLobbyCreated(LobbyCreated_t result)
    {
        if (result.m_eResult != EResult.k_EResultOK)
        {
            LobbyCreationFailed?.Invoke(result.m_eResult);
            return;
        }

        CurrentLobbyID = new CSteamID(result.m_ulSteamIDLobby);
        LobbyCreated?.Invoke(CurrentLobbyID);

        string lobbyName = PlayerPrefs.GetString(PlayerPrefsKeys.LobbyName, "Default Lobby");
        string hostName = SteamFriends.GetPersonaName();
        string lobbyType = PlayerPrefs.GetString(PlayerPrefsKeys.CreatedLobbyType, "public");
        string code = GenerateCode(Constants.CodeLength);

        SteamMatchmaking.SetLobbyJoinable(CurrentLobbyID, true);
        SteamMatchmaking.SetLobbyData(CurrentLobbyID, LobbyDataKeys.HostSteamID, SteamUser.GetSteamID().ToString());
        SteamMatchmaking.SetLobbyData(CurrentLobbyID, LobbyDataKeys.LobbyName, lobbyName);
        SteamMatchmaking.SetLobbyData(CurrentLobbyID, LobbyDataKeys.HostName, hostName);
        SteamMatchmaking.SetLobbyData(CurrentLobbyID, LobbyDataKeys.LobbyType, lobbyType); ;
        SteamMatchmaking.SetLobbyData(CurrentLobbyID, LobbyDataKeys.LobbyCode, code);
        SteamMatchmaking.SetLobbyData(CurrentLobbyID, LobbyDataKeys.SessionState, "lobby");

        NetworkManager.singleton.StartHost();
    }
    private void HandleLobbyEntered(LobbyEnter_t result)
    {
        if (NetworkServer.active) return;

        var lobbyId = new CSteamID(result.m_ulSteamIDLobby);
        var enterResponse = (EChatRoomEnterResponse)result.m_EChatRoomEnterResponse;

        
        if (_joiningLobby.IsValid() && lobbyId == _joiningLobby)
        {
            _joinInProgress = false;
            _joiningLobby = CSteamID.Nil;
        }

        LobbyJoinResultReceived?.Invoke(lobbyId, enterResponse);

        if (enterResponse != EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
        {
            nextJoinByCode = false; 
            allowPrivateJoinFor = CSteamID.Nil;
            return;
        }

        CurrentLobbyID = new CSteamID(result.m_ulSteamIDLobby);

        if (nextJoinByCode)
        {
            SteamMatchmaking.SetLobbyMemberData(CurrentLobbyID, LobbyDataKeys.JoinMethod, "code");
            nextJoinByCode = false;
        }

        string lobbyType = SteamMatchmaking.GetLobbyData(CurrentLobbyID, LobbyDataKeys.LobbyType);

        if (string.Equals(lobbyType, "private", StringComparison.OrdinalIgnoreCase)
            && CurrentLobbyID != allowPrivateJoinFor)
        {
            LobbyJoinResultReceived?.Invoke(CurrentLobbyID, EChatRoomEnterResponse.k_EChatRoomEnterResponseNotAllowed);
            SteamMatchmaking.LeaveLobby(CurrentLobbyID);
            ClearLobbyID();
            allowPrivateJoinFor = CSteamID.Nil;
            return;
        }

        allowPrivateJoinFor = CSteamID.Nil;

        string hostSteamID = SteamMatchmaking.GetLobbyData(CurrentLobbyID, LobbyDataKeys.HostSteamID);
        if (string.IsNullOrEmpty(hostSteamID))
        {
            Debug.LogError("HostAddress not found in lobby data. Aborting connection.");
            return;
        }

        NetworkManager.singleton.networkAddress = hostSteamID;
        NetworkManager.singleton.StartClient();
        
        LobbyJoinSucceeded?.Invoke(CurrentLobbyID);
    }
    private void HandleLobbyListReceived(LobbyMatchList_t result)
    {
        List<CSteamID> lobbyIDList = new List<CSteamID>();

        for (int i = 0; i < result.m_nLobbiesMatching; i++)
            lobbyIDList.Add(SteamMatchmaking.GetLobbyByIndex(i));

        LobbyListReceived?.Invoke(lobbyIDList);
    }
    private void HandleLobbyJoinRequested(GameLobbyJoinRequested_t lobbyRequest)
    {
        var target = lobbyRequest.m_steamIDLobby;
        if (!target.IsValid()) return;

        bool whitelisted = IsWhitelisted(target);
        JoinCoordinator.Instance.JoinFromInvite(target, whitelisted);
    }
    private void HandleLobbyChatUpdate(LobbyChatUpdate_t lobbyUpdate)
    {
        if (lobbyUpdate.m_ulSteamIDLobby != CurrentLobbyID.m_SteamID)
            return;

        CSteamID memberId = (CSteamID)lobbyUpdate.m_ulSteamIDUserChanged;
        EChatMemberStateChange stateChange = (EChatMemberStateChange)lobbyUpdate.m_rgfChatMemberStateChange;

        if (memberId != SteamUser.GetSteamID()) return;

        switch (stateChange)
        {
            case EChatMemberStateChange.k_EChatMemberStateChangeEntered:
                break;
            case EChatMemberStateChange.k_EChatMemberStateChangeLeft:
                IsManualDisconnect = true;
                break;
            case EChatMemberStateChange.k_EChatMemberStateChangeDisconnected:
                IsManualDisconnect = false;
                break;
            case EChatMemberStateChange.k_EChatMemberStateChangeKicked:
                IsManualDisconnect = false;
                ExitCoordinator.Instance.RequestExit(DisconnectReason.Kicked);
                break;
            case EChatMemberStateChange.k_EChatMemberStateChangeBanned:
                IsManualDisconnect = false;
                ExitCoordinator.Instance.RequestExit(DisconnectReason.Banned);
                break;
            default:
                Debug.Log($"[Lobby] Игрок {memberId} изменил статус: {stateChange}");
                break;
        }
    }
    private void HandleLobbyInvite(LobbyInvite_t data)
    {
        _inviteWhitelist[data.m_ulSteamIDLobby] = Time.realtimeSinceStartup + InviteWhitelistTTL;
    }


    private void ResetCurrentLobby()
    {
        if (CurrentLobbyID.IsValid())
        {
            SteamMatchmaking.SetLobbyJoinable(CurrentLobbyID, false);
            SteamMatchmaking.DeleteLobbyData(CurrentLobbyID, LobbyDataKeys.HostSteamID);
            SteamMatchmaking.LeaveLobby(CurrentLobbyID);
            ClearLobbyID();
        }
    }
    private string GenerateCode(int length = Constants.CodeLength)
    {
        const string digits = "0123456789";
        var sb = new System.Text.StringBuilder(length);
        var rnd = new System.Random();
        for (int i = 0; i < length; i++)
            sb.Append(digits[rnd.Next(digits.Length)]);
        return sb.ToString();
    }
    private bool IsWhitelisted(CSteamID lobby)
    {
        if (!lobby.IsValid()) return false;
        float now = Time.realtimeSinceStartup;

        if (_inviteWhitelist.TryGetValue(lobby.m_SteamID, out float expiry))
        {
            if (now <= expiry) return true;
            _inviteWhitelist.Remove(lobby.m_SteamID); 
        }
        return false;
    }

}