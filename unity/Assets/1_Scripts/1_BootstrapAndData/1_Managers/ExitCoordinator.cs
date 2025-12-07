using System;
using System.Collections;
using Mirror;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitCoordinator : MonoBehaviour
{
    public static ExitCoordinator Instance { get; private set; }

    [Scene, SerializeField] private string mainMenuScene;

    public bool IsExiting { get; private set; }
    
    public event Action<DisconnectReason> ExitStarted; // ? Не используется
    public event Action<DisconnectReason> ExitCompleted; // ? Не используется

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

    public void RequestExit(DisconnectReason reason)
    {
        if (IsExiting) return;
        IsExiting = true;
        
        ExitStarted?.Invoke(reason);
        StartCoroutine(ExitCoroutine(reason));
    }

    private IEnumerator ExitCoroutine(DisconnectReason reason)
    {
        try
        {
            ErrorType? pendingError = ReasonToError(reason);

            SteamLobbyManager.Instance?.SetManualDisconnect(reason == DisconnectReason.ManualLeft);

            if (NetworkServer.active && NetworkManager.singleton != null)
            {
                if (reason == DisconnectReason.ManualLeft || reason == DisconnectReason.ServerShutdown)
                {
                    foreach (var conn in NetworkServer.connections.Values)
                        conn?.Send(new ShutdownMessage { Reason = DisconnectReason.ServerShutdown });

                    yield return new WaitForEndOfFrame();
                    yield return new WaitForSecondsRealtime(0.05f);
                }
                
                NetworkManager.singleton.StopHost();
            }
            else if (NetworkClient.active && NetworkManager.singleton != null)
            {
                NetworkManager.singleton.StopClient();
            }

            var steamManager = SteamLobbyManager.Instance;
            if (steamManager != null && steamManager.CurrentLobbyID.IsValid())
            {
                SteamMatchmaking.LeaveLobby(steamManager.CurrentLobbyID);
                steamManager.ClearLobbyID();
            }
            steamManager?.SetManualDisconnect(false);

            bool needLoad = !string.IsNullOrEmpty(mainMenuScene)
                            && SceneManager.GetActiveScene().name != mainMenuScene;

            if (needLoad)
            {
                var op = SceneManager.LoadSceneAsync(mainMenuScene, LoadSceneMode.Single);
                while (!op.isDone) yield return null;
                yield return null;
            }
            else
            {
                yield return null;
            }

            if (pendingError.HasValue)
                ErrorPopupManager.Instance?.ShowError(pendingError.Value);
        }
        finally
        {
            IsExiting = false;
            ExitCompleted?.Invoke(reason);
        }
    }

    private ErrorType? ReasonToError(DisconnectReason reason) => reason switch
    {
        DisconnectReason.ManualLeft => null,
        DisconnectReason.ServerShutdown => ErrorType.HostExit,
        DisconnectReason.Kicked => ErrorType.Kicked,
        DisconnectReason.Banned => ErrorType.Banned,
        DisconnectReason.Disconnected => ErrorType.ConnectionLost,
        _ => null
    };
}
