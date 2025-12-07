using System;
using UnityEngine;
using Steamworks;
using Mirror;

public enum LobbyState
{
    Idle,
    Creating,
    Joining,
    InLobby,
    Exiting
}

public interface ILobbyService
{
    LobbyState State { get; }
    bool IsBusy { get; }
    CSteamID CurrentLobby { get; }

    event Action<LobbyState> StateChanged;

    event Action CreationStarted;
    event Action<CSteamID> CreationSucceeded;
    event Action<ErrorType> CreationFailed;

    event Action<CSteamID> JoinStarted;
    event Action<CSteamID> JoinSucceeded;
    event Action<CSteamID, ErrorType, bool> JoinFailed;

    event Action<DisconnectReason> ExitStarted;
    event Action<DisconnectReason> ExitCompleted;

    void CreateLobby(int maxPlayers, string name, string lobbyType);
    void JoinById(CSteamID id, bool allowPrivateOnce = false);
    void JoinByCode(string code);
    void Exit(DisconnectReason reason);
}

[DefaultExecutionOrder(-200)]
public sealed class LobbyService : MonoBehaviour, ILobbyService
{
    [SerializeField] private SteamLobbyManager steam;
    [SerializeField] private JoinCoordinator joiner;
    [SerializeField] private ExitCoordinator exiter;

    public LobbyState State { get; private set; } = LobbyState.Idle;
    public bool IsBusy => State == LobbyState.Creating || State == LobbyState.Joining || State == LobbyState.Exiting;
    public CSteamID CurrentLobby => steam.CurrentLobbyID;

    public event Action<LobbyState> StateChanged;

    public event Action CreationStarted;
    public event Action<CSteamID> CreationSucceeded;
    public event Action<ErrorType> CreationFailed;

    public event Action<CSteamID> JoinStarted;
    public event Action<CSteamID> JoinSucceeded;
    public event Action<CSteamID, ErrorType, bool> JoinFailed;

    public event Action<DisconnectReason> ExitStarted;
    public event Action<DisconnectReason> ExitCompleted;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        Debug.Assert(steam != null, "SteamLobbyManager ref is not set");
        Debug.Assert(joiner != null, "JoinCoordinator ref is not set");
        Debug.Assert(exiter != null, "ExitCoordinator ref is not set");
    }

    void OnEnable()
    {
        // Steam (создание)
        steam.LobbyCreated += OnSteamLobbyCreated;
        steam.LobbyCreationFailed += OnSteamLobbyCreationFailed;

        // Join
        joiner.JoinStarted += OnJoinerJoinStarted;
        joiner.JoinSucceeded += OnJoinerJoinSucceeded;
        joiner.JoinFailed += OnJoinerJoinFailed;

        // Exit
        exiter.ExitStarted += OnExitStarted;
        exiter.ExitCompleted += OnExitCompletedInternal;
    }

    void OnDisable()
    {
        steam.LobbyCreated -= OnSteamLobbyCreated;
        steam.LobbyCreationFailed -= OnSteamLobbyCreationFailed;

        joiner.JoinStarted -= OnJoinerJoinStarted;
        joiner.JoinSucceeded -= OnJoinerJoinSucceeded;
        joiner.JoinFailed -= OnJoinerJoinFailed;

        exiter.ExitStarted -= OnExitStarted;
        exiter.ExitCompleted -= OnExitCompletedInternal;
    }

    public void CreateLobby(int maxPlayers, string name, string lobbyType)
    {
        if (IsBusy) return;

        SetState(LobbyState.Creating);
        CreationStarted?.Invoke();

        NetworkManager.singleton.maxConnections = maxPlayers;
        PlayerPrefs.SetInt(PlayerPrefsKeys.MaxPlayers, maxPlayers);
        PlayerPrefs.SetString(PlayerPrefsKeys.LobbyName, name.Trim());
        PlayerPrefs.SetString(PlayerPrefsKeys.CreatedLobbyType, lobbyType);
        PlayerPrefs.Save();

        steam.CreateLobby(maxPlayers);
    }

    public void JoinById(CSteamID id, bool allowPrivateOnce = false)
    {
        if (IsBusy) return;

        SetState(LobbyState.Joining);
        joiner.JoinByLobbyId(id, allowPrivateOnce);
        CheckJoinActuallyStarted();
    }

    public void JoinByCode(string code)
    {
        if (IsBusy) return;

        SetState(LobbyState.Joining);
        joiner.JoinByCode(code);
        CheckJoinActuallyStarted();
    }

    public void Exit(DisconnectReason reason)
    {
        if (State == LobbyState.Exiting) return;

        SetState(LobbyState.Exiting);
        ExitStarted?.Invoke(reason);
        exiter.RequestExit(reason);
    }

    // Steam creation -> success/failed
    void OnSteamLobbyCreated(CSteamID id)
    {
        CreationSucceeded?.Invoke(id);
        SetState(LobbyState.InLobby);
    }
    void OnSteamLobbyCreationFailed(EResult _)
    {
        CreationFailed?.Invoke(ErrorType.LobbyCreationFailed);
        SnapState();
    }

    // Join -> progress
    void OnJoinerJoinStarted(CSteamID id)
    {
        JoinStarted?.Invoke(id);
        SetState(LobbyState.Joining);
    }
    void OnJoinerJoinSucceeded(CSteamID id)
    {
        JoinSucceeded?.Invoke(id);
        SetState(LobbyState.InLobby);
    }
    void OnJoinerJoinFailed(CSteamID id, ErrorType error, bool hard)
    {
        JoinFailed?.Invoke(id, error, hard);
        SnapState();
    }

    // Exit -> completed
    void OnExitStarted(DisconnectReason _)
    {
        SetState(LobbyState.Exiting);
    }
    void OnExitCompletedInternal(DisconnectReason reason)
    {
        SetState(LobbyState.Idle);
        ExitCompleted?.Invoke(reason);
    }

    void CheckJoinActuallyStarted()
    {
        // Если JoinCoordinator отказал (например, уже в лобби) — вернём состояние к реальному миру.
        if (joiner.IsJoining || steam.JoinInProgress) return;
        SnapState();
    }

    void SnapState()
    {
        if (exiter.IsExiting) { SetState(LobbyState.Exiting); return; }
        if (joiner.IsJoining || steam.JoinInProgress) { SetState(LobbyState.Joining); return; }
        if (steam.CurrentLobbyID.IsValid()) { SetState(LobbyState.InLobby); return; }
        SetState(LobbyState.Idle);
    }

    void SetState(LobbyState next)
    {
        if (State == next) return;
        State = next;
        StateChanged?.Invoke(State);
    }
}
