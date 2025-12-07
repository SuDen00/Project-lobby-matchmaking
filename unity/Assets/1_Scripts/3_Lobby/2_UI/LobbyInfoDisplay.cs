using UnityEngine;
using UnityEngine.UI;  
using TMPro;
using Steamworks;

public class LobbyInfoDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text lobbyCodeText;
    [SerializeField] private TMP_Text lobbyNameText;
    [SerializeField] private Button copyIdButton;
    [SerializeField] private TMP_Text playersText; 
    [SerializeField] private TMP_Text lobbyTypeText; 

    private Callback<LobbyChatUpdate_t> onLobbyChatUpdate;

    private void OnEnable()
    {
        UpdateUI(SteamLobbyManager.Instance.CurrentLobbyID);

        copyIdButton.onClick.AddListener(CopyLobbyId);
        SteamLobbyManager.Instance.LobbyJoinSucceeded += UpdateUI;
        SteamLobbyManager.Instance.LobbyCreated += UpdateUI;

        onLobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(HandleLobbyChatUpdate); 
    }

    private void OnDisable()
    {
        copyIdButton.onClick.RemoveListener(CopyLobbyId);
        SteamLobbyManager.Instance.LobbyJoinSucceeded -= UpdateUI;
        SteamLobbyManager.Instance.LobbyCreated -= UpdateUI;

        onLobbyChatUpdate?.Dispose(); 
        onLobbyChatUpdate = null;
    }

    private void HandleLobbyChatUpdate(LobbyChatUpdate_t e)
    {
        var current = SteamLobbyManager.Instance.CurrentLobbyID;
        if (!current.IsValid()) return;
        if (e.m_ulSteamIDLobby != current.m_SteamID) return;

        UpdateUI(current);
    }

    private void UpdateUI(CSteamID steamID)
    {
        if (!steamID.IsValid())
        {
            lobbyCodeText.text = "-";
            lobbyNameText.text = "-";
            playersText?.SetText("-");     
            lobbyTypeText?.SetText("-"); 
            copyIdButton.interactable = false;
            return;
        }

        string code  = SteamMatchmaking.GetLobbyData(steamID, LobbyDataKeys.LobbyCode);
        string name  = SteamMatchmaking.GetLobbyData(steamID, LobbyDataKeys.LobbyName);
        string type  = SteamMatchmaking.GetLobbyData(steamID, LobbyDataKeys.LobbyType);

        int members = SteamMatchmaking.GetNumLobbyMembers(steamID);
        int limit   = SteamMatchmaking.GetLobbyMemberLimit(steamID);

        lobbyCodeText.text = $"Lobby Code: {code}";
        lobbyNameText.text = $"Name: {name}";

        if (playersText != null)
        {
            playersText.text = $"Players: {members}/{limit}";
        }

        if (lobbyTypeText != null)
        {
            lobbyTypeText.text = $"lobby type: {type}";
        }

        copyIdButton.interactable = true;
    }

    private void CopyLobbyId()
    {
        var steamID = SteamLobbyManager.Instance.CurrentLobbyID;
        if (!steamID.IsValid()) return;

        GUIUtility.systemCopyBuffer = SteamMatchmaking.GetLobbyData(steamID, LobbyDataKeys.LobbyCode);
    }
}
