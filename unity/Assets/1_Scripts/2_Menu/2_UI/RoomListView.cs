using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Steamworks;
using TMPro;

public sealed class RoomListView : MonoBehaviour
{
    [SerializeField] private GameObject lobbyItemPrefab;
    [SerializeField] private Transform lobbyListRoot;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_Text noLobbyMessage;

    private readonly List<LobbyItemUI> lobbyItems = new();
    private LobbyItemUI selectedItem;

    public CSteamID SelectedLobbyID => selectedItem?.LobbyID ?? CSteamID.Nil;

    public event Action RefreshClicked;
    public event Action JoinClicked;
    public event Action<CSteamID> SelectionChanged;

    private void OnEnable()
    {
        refreshButton.onClick.AddListener(OnRefreshClick);
        joinButton.onClick.AddListener(OnJoinClick);

        SetJoinInteractable(false);
        SetNoLobbiesVisible(false);
    }
    private void OnDisable()
    {
        refreshButton.onClick.RemoveListener(OnRefreshClick);
        joinButton.onClick.RemoveListener(OnJoinClick);
        
        SetJoinInteractable(false);
        ClearItems(suppressUi: true);
    }
   
    public void ShowLobbies(List<CSteamID> lobbyIds)
    {
        ClearItems();

        foreach (var id in lobbyIds)
        {
            var item = Instantiate(lobbyItemPrefab, lobbyListRoot);
            if (!item) continue;

            var itemUI = item.GetComponent<LobbyItemUI>();
            if (!itemUI) { Destroy(item); continue; }

            itemUI.SetLobbyID(id);
            itemUI.LobbySelected += OnItemSelected;

            lobbyItems.Add(itemUI);
        }

        SetNoLobbiesVisible(lobbyItems.Count == 0);
        SetJoinInteractable(false);
    }
    public void SetBusy(bool busy) => refreshButton.interactable = !busy;

    public void ClearAll()
    {
        ClearItems();
        SetNoLobbiesVisible(false);
        SetJoinInteractable(false);
    }
    public void RemoveLobbyItem(CSteamID id)
    {
        if (!id.IsValid()) return;

        LobbyItemUI target = null;

        for (int i = 0; i < lobbyItems.Count; i++)
        {
            var ui = lobbyItems[i];
            if (ui != null && ui.LobbyID == id)
            {
                target = ui;
                break;
            }
        }

        if (target == null) return;

        target.LobbySelected -= OnItemSelected;

        if (selectedItem == target)
        {
            selectedItem.SetSelected(false);
            selectedItem = null;
            SetJoinInteractable(false);
        }

        if (target.gameObject) Destroy(target.gameObject);
        lobbyItems.Remove(target);

        SetNoLobbiesVisible(lobbyItems.Count == 0);
    }
    public void SetJoinInteractable(bool value)
    {
        if (!joinButton) return;

        bool globalBusy =
            (JoinCoordinator.Instance?.IsJoining ?? false) ||
            (SteamLobbyManager.Instance?.JoinInProgress ?? false);

        joinButton.interactable = value && !globalBusy; 
    }

    private void OnRefreshClick() => RefreshClicked?.Invoke();
    private void OnJoinClick() => JoinClicked?.Invoke();
    private void OnItemSelected(LobbyItemUI item)
    {
        if (selectedItem != null && selectedItem != item)
            selectedItem.SetSelected(false);

        selectedItem = item;
        selectedItem.SetSelected(true);

        SetJoinInteractable(true);
        SelectionChanged?.Invoke(selectedItem.LobbyID);
    }

    private void ClearItems(bool suppressUi = false)
    {
        if (selectedItem) selectedItem.SetSelected(false);
        selectedItem = null;

        for (int i = 0; i < lobbyItems.Count; i++)
        {
            var ui = lobbyItems[i];
            if (!ui) continue;
            ui.LobbySelected -= OnItemSelected;
            if (ui.gameObject) Destroy(ui.gameObject);
        }
        lobbyItems.Clear();

        if (!suppressUi) SetJoinInteractable(false);
    }
    private void SetNoLobbiesVisible(bool visible)
    {
        if (!noLobbyMessage) return;
        noLobbyMessage.gameObject.SetActive(visible);
    }
}
