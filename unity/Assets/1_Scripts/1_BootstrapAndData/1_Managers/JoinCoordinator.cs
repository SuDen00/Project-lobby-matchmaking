using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Steamworks;
using UnityEngine;

public sealed class JoinCoordinator : MonoBehaviour
{
    private const float joinTimeout = Constants.JoinTimeout;
    public static JoinCoordinator Instance { get; private set; }

    public bool IsJoining { get; private set; }
    public CSteamID PendingLobby { get; private set; } = CSteamID.Nil;

    private Coroutine timeoutRoutine;
    private Coroutine findRoutine;

    private Action<CSteamID, EChatRoomEnterResponse> joinResultHandler;
    private Action<List<CSteamID>> ListHandler;

    public event Action<CSteamID> JoinStarted;
    public event Action<CSteamID> JoinSucceeded;
    public event Action<CSteamID, ErrorType, bool> JoinFailed;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    void OnDisable()
    {
        UnsubscribeJoinCallbacks();

        if (ListHandler != null && SteamLobbyManager.Instance != null)
        {
            SteamLobbyManager.Instance.LobbyListReceived -= ListHandler;
            ListHandler = null;
        }

        if (timeoutRoutine != null) { StopCoroutine(timeoutRoutine); timeoutRoutine = null; }
        if (findRoutine != null)    { StopCoroutine(findRoutine);    findRoutine = null; }

        IsJoining = false;
        PendingLobby = CSteamID.Nil;
    }

    public void JoinByLobbyId(CSteamID lobbyId, bool allowPrivateOnce = false)
    {
        if (!lobbyId.IsValid()) return;
        if (!CanStartJoin()) return;

        IsJoining = true;
        PendingLobby = lobbyId;

        SubscribeJoinCallbacks();
        if (allowPrivateOnce) SteamLobbyManager.Instance?.AllowPrivateJoinOnce(lobbyId);

        JoinStarted?.Invoke(lobbyId);
        SteamLobbyManager.Instance?.JoinLobby(lobbyId);

        RestartTimeoutRoutine();
    }
    public void JoinByCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != Constants.CodeLength) return;
        if (!CanStartJoin()) return;

        if (findRoutine != null) StopCoroutine(findRoutine);
        findRoutine = StartCoroutine(FindByCodeAndJoin(code));
    }
    public void JoinFromInvite(CSteamID lobbyId, bool isWhitelisted)
    {
        JoinByLobbyId(lobbyId, allowPrivateOnce: isWhitelisted);
    }

    private void OnJoinSucceeded(CSteamID lobby)
    {
        if (!IsJoining) return;

        IsJoining = false;
        var okLobby = PendingLobby;
        PendingLobby = CSteamID.Nil;

        UnsubscribeJoinCallbacks();
        StopTimeout();       
        JoinSucceeded?.Invoke(okLobby);
    }
    private void OnJoinFailed(CSteamID lobby, EChatRoomEnterResponse response)
    {
        if (!IsJoining) return;
        if (PendingLobby.IsValid() && lobby != PendingLobby) return;

        if (response == EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
            return;

        var error = MapJoinError(response);
        bool hard = IsHardError(response);
        FinishJoinWithError(error, hard);
    }

    private IEnumerator FindByCodeAndJoin(string code)
    {
        IsJoining = true;

        SteamMatchmaking.AddRequestLobbyListStringFilter(LobbyDataKeys.LobbyCode, code, ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListStringFilter(LobbyDataKeys.SessionState, "lobby", ELobbyComparison.k_ELobbyComparisonEqual);

        CSteamID found = CSteamID.Nil;
        bool anyWithCodeFull = false;

        ListHandler = (list) =>
        {
            SteamLobbyManager.Instance.LobbyListReceived -= ListHandler;
            ListHandler = null;

            foreach (var id in list ?? new List<CSteamID>())
            {
                if (!id.IsValid()) continue;
                if (SteamMatchmaking.GetLobbyData(id, LobbyDataKeys.LobbyCode) != code) continue;

                int members = SteamMatchmaking.GetNumLobbyMembers(id);
                int limit   = SteamMatchmaking.GetLobbyMemberLimit(id);

                if (limit > 0 && members >= limit) { anyWithCodeFull = true; continue; }
                found = id; break;
            }
        };

        SteamLobbyManager.Instance.LobbyListReceived += ListHandler;
        SteamLobbyManager.Instance.RequestLobbyList();

        float t = 0f;
        while (t < Constants.TimeoutValue && ListHandler != null)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (ListHandler != null)
        {
            SteamLobbyManager.Instance.LobbyListReceived -= ListHandler;
            ListHandler = null;
        }

        if (!found.IsValid())
        {
            var error = anyWithCodeFull ? ErrorType.LobbyFull : ErrorType.LobbyNotFound;
            FinishJoinWithError(error, hard: error == ErrorType.LobbyNotFound);
            findRoutine = null;
            yield break;
        }

        SubscribeJoinCallbacks();
        JoinStarted?.Invoke(found);
        SteamLobbyManager.Instance.AllowNextJoinByCode();
        SteamLobbyManager.Instance.AllowPrivateJoinOnce(found);
        PendingLobby = found;
        SteamLobbyManager.Instance.JoinLobby(found);
        RestartTimeoutRoutine();

        findRoutine = null;
    }
    private IEnumerator JoinTimeout()
    {
        float t = 0f;
        while (t < joinTimeout && IsJoining)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        if (IsJoining)
        {
            FinishJoinWithError(ErrorType.Timeout, hard: false);
        }
        timeoutRoutine = null;
    }
    private void RestartTimeoutRoutine()
    {
        if (timeoutRoutine != null) StopCoroutine(timeoutRoutine);
        timeoutRoutine = StartCoroutine(JoinTimeout());
    }
    private void FinishJoinWithError(ErrorType error, bool hard)
    {
        IsJoining = false;
        var failedLobby = PendingLobby;
        PendingLobby = CSteamID.Nil;

        UnsubscribeJoinCallbacks();
        StopTimeout();       
        ErrorPopupManager.Instance?.ShowError(error);
        JoinFailed?.Invoke(failedLobby, error, hard);
    }
    private void SubscribeJoinCallbacks()
    {
        UnsubscribeJoinCallbacks();
        joinResultHandler = OnJoinFailed;
        SteamLobbyManager.Instance.LobbyJoinResultReceived += joinResultHandler;
        SteamLobbyManager.Instance.LobbyJoinSucceeded += OnJoinSucceeded;
    }
    private void UnsubscribeJoinCallbacks()
    {
        if (joinResultHandler != null && SteamLobbyManager.Instance != null)
        {
            SteamLobbyManager.Instance.LobbyJoinResultReceived -= joinResultHandler;
            joinResultHandler = null;
        }
        if (SteamLobbyManager.Instance != null)
        {
            SteamLobbyManager.Instance.LobbyJoinSucceeded -= OnJoinSucceeded;
        }
    }
    private bool CanStartJoin()
    {
        if (IsJoining || (ExitCoordinator.Instance?.IsExiting ?? false))
        {
            return false;
        }

        if (NetworkServer.active || NetworkClient.active || (SteamLobbyManager.Instance?.CurrentLobbyID.IsValid() ?? false))
        {
            ErrorPopupManager.Instance?.ShowError(ErrorType.AlreadyInLobby);
            return false;
        }

        if (!SteamManager.Initialized)
        {
            ErrorPopupManager.Instance?.ShowError(ErrorType.ConnectionFailed);
            return false;
        }

        return true;
    }
    private void StopTimeout()
    {
        if (timeoutRoutine != null) { StopCoroutine(timeoutRoutine); timeoutRoutine = null; }
    }


    private static ErrorType MapJoinError(EChatRoomEnterResponse r) => r switch
    {
        EChatRoomEnterResponse.k_EChatRoomEnterResponseFull => ErrorType.LobbyFull,
        EChatRoomEnterResponse.k_EChatRoomEnterResponseDoesntExist => ErrorType.LobbyNotFound,
        EChatRoomEnterResponse.k_EChatRoomEnterResponseNotAllowed => ErrorType.AccessDenied,
        EChatRoomEnterResponse.k_EChatRoomEnterResponseBanned => ErrorType.Banned,
        EChatRoomEnterResponse.k_EChatRoomEnterResponseCommunityBan => ErrorType.CommunityBanned,
        EChatRoomEnterResponse.k_EChatRoomEnterResponseMemberBlockedYou => ErrorType.MemberBlocked,
        EChatRoomEnterResponse.k_EChatRoomEnterResponseYouBlockedMember => ErrorType.YouBlockedMember,
        EChatRoomEnterResponse.k_EChatRoomEnterResponseRatelimitExceeded => ErrorType.RateLimitExceeded,
        EChatRoomEnterResponse.k_EChatRoomEnterResponseLimited => ErrorType.LimitedAccount,
        EChatRoomEnterResponse.k_EChatRoomEnterResponseClanDisabled => ErrorType.ClanDisabled,
        _ => ErrorType.GenericJoinError
    };
    private static bool IsHardError(EChatRoomEnterResponse r) => r switch
    {
        EChatRoomEnterResponse.k_EChatRoomEnterResponseDoesntExist  => true,
        EChatRoomEnterResponse.k_EChatRoomEnterResponseNotAllowed   => true,
        EChatRoomEnterResponse.k_EChatRoomEnterResponseBanned       => true,
        EChatRoomEnterResponse.k_EChatRoomEnterResponseCommunityBan => true,
        _ => false
    };
}
