using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Mirror;

public class LobbyPlayerUI : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private Button kickButton;
    [SerializeField] private GameObject hostIndicator;
    [SerializeField] private GameObject readyIndicator;


    private LobbyPlayer lobbyPlayer;

    public void Initialize(LobbyPlayer player)
    {
        lobbyPlayer = player;
        playerNameText.text = player.PlayerName;

        kickButton.gameObject.SetActive(NetworkServer.active && !player.isLocalPlayer);

        SetHostIndicator(player.IsHost);
        SetReadyIndicator(player.IsReady);

        kickButton.onClick.RemoveAllListeners();
        kickButton.onClick.AddListener(OnKickClicked);
    }

    public void SetPlayerName(string name)
    {
        playerNameText.text = name;
    }

    public void SetReadyIndicator(bool isReady)
    {
        readyIndicator.SetActive(isReady);
    }

    public void SetHostIndicator(bool isHost)
    {
        hostIndicator.SetActive(isHost);
    }

    private void OnKickClicked()
    {
        if (lobbyPlayer == null) return;

        NetworkIdentity localIdentity = NetworkClient.localPlayer;
        if (localIdentity == null) return;

        kickButton.interactable = false;

        if (localIdentity.TryGetComponent(out LobbyPlayer localLobbyPlayer))
            localLobbyPlayer.CmdKickPlayer(lobbyPlayer.SteamId);

        StartCoroutine(TurnOnButtonOnError(Constants.TimeoutValue));
    }
    
    private System.Collections.IEnumerator TurnOnButtonOnError(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (this != null && gameObject != null) 
            kickButton.interactable = true;
    }
}
