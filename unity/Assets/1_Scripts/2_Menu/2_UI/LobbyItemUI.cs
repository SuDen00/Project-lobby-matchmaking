using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Steamworks;
using UnityEngine.EventSystems;
using System.Linq;

public class LobbyItemUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TMP_Text lobbyNameText;
    [SerializeField] private TMP_Text hostNameText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private Image background;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color selectedColor = Color.green;

    public CSteamID LobbyID { get; private set; }

    public event Action<LobbyItemUI> LobbySelected;

    public void SetLobbyID(CSteamID lobbyID)
    {
        LobbyID = lobbyID;
        Refresh();
    }
    
    public void Refresh()
    {
        if (!LobbyID.IsValid()) return;

        string lobbyName = SteamMatchmaking.GetLobbyData(LobbyID, LobbyDataKeys.LobbyName);
        string hostName = SteamMatchmaking.GetLobbyData(LobbyID, LobbyDataKeys.HostName);
        int playerCount = SteamMatchmaking.GetNumLobbyMembers(LobbyID);
        int maxPlayers = SteamMatchmaking.GetLobbyMemberLimit(LobbyID);

        lobbyName = (lobbyName ?? string.Empty).Trim().ToUpperInvariant();
        hostName = (hostName ?? string.Empty).Trim().ToUpperInvariant();

        if (hostName.Length > Constants.MaxPlayerNameLength) hostName = hostName.Substring(0, Constants.MaxPlayerNameLength);
        if (lobbyName.Length > Constants.MaxLobbyNameLength) lobbyName = lobbyName.Substring(0, Constants.MaxLobbyNameLength);

        bool asciiLatin = hostName.All(ch =>
        (ch >= 'A' && ch <= 'Z') ||
        (ch >= '0' && ch <= '9') ||
        ch == ' ' || ch == '_' || ch == '-');

        hostName = asciiLatin ? $"{hostName}'S LOBBY" : $"{hostName} LOBBY";

        lobbyNameText.text = string.IsNullOrEmpty(lobbyName) ? "ЛОББИ" : lobbyName;
        hostNameText.text = string.IsNullOrEmpty(hostName) ? "ИГРОК" : hostName;
        playerCountText.text = $"{playerCount}/{maxPlayers}";
    }

    public void SetSelected(bool selected)
    {
        if (background) background.color = selected ? selectedColor : defaultColor;
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        
        LobbySelected?.Invoke(this);
    }
}
