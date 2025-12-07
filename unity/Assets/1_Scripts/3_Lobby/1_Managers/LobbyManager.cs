using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Mirror;
using TMPro;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [Scene][SerializeField] private string mainMenuSceneName;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button readyGameButton;
    [SerializeField] private Button exitGameButton;

    private TMP_Text readyBtnText;
    private bool isStarting;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        readyBtnText = readyGameButton.GetComponentInChildren<TMP_Text>(true);
        
        exitGameButton.onClick.AddListener(OnExitLobbyPressed);
        readyGameButton.onClick.AddListener(OnReadyPressed);
    }
    private void Start()
    {
        if (NetworkServer.active)
        {
            startGameButton.onClick.AddListener(OnStartGamePressed);
            startGameButton.interactable = CanStartGame();
        }
        else
            startGameButton.gameObject.SetActive(false);
    }

    public void RefreshStartButton()
    {
        if (NetworkServer.active && !isStarting)
            startGameButton.interactable = CanStartGame();
    }
    public void RefreshReadyButton(bool isReady)
    {
        readyBtnText.text = isReady ? "Unready" : "Ready";
    }

    private void OnExitLobbyPressed()
    {
        ExitCoordinator.Instance.RequestExit(DisconnectReason.ManualLeft);
    }
    private void OnStartGamePressed()
    {
        if (!NetworkServer.active) return;

        if (isStarting) return;

        if (!CanStartGame())
            return;

        isStarting = true;

        ((NetworkManagerExtended)NetworkManager.singleton).StartGame();
    }
    private void OnReadyPressed()
    {
        NetworkIdentity localPlayer = NetworkClient.localPlayer;
        if (localPlayer == null) return;

        if (localPlayer.TryGetComponent(out LobbyPlayer player))
        {
            player.CmdSetReady(!player.IsReady);
        }
    }
    
    private bool CanStartGame()
    {
        var nm = (NetworkManagerExtended)NetworkManager.singleton;
        int maxPlayers = nm.maxConnections;

        int players = 0;
        int readyCount = 0;

        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn == null) continue;

            // игрок ещё не полностью присоединился/не готов -> старт запрещён
            if (!conn.isAuthenticated || !conn.isReady) return false;
            if (conn.identity == null)                 return false;

            players++;

            if (!conn.identity.TryGetComponent(out LobbyPlayer lp))
                return false; // у соединения нет LobbyPlayer — тоже стоп

            if (lp.IsReady) readyCount++;
        }
        
        if (players < Constants.MinPlayersCount || players > maxPlayers) return false;
        return readyCount == players;
    }
}
