using System.Collections.Generic;
using UnityEngine;
using Steamworks;

public sealed class LobbyListController : MonoBehaviour
{
    [SerializeField] private LobbyCollector lobbyCollector;
    [SerializeField] private RoomListView roomListView;
    [SerializeField] private LobbyModeFilter lobbyModeFilter;

    private bool isBusy;
    private CSteamID selectedID;

    private System.Action<bool> filterChanged;

    private void OnEnable()
    {
        roomListView.RefreshClicked += RequestNow;
        roomListView.JoinClicked += OnJoinClicked;
        roomListView.SelectionChanged += OnSelectionChanged;

        filterChanged = _ => RequestNow();
        lobbyModeFilter.FilterChanged += filterChanged;

        JoinCoordinator.Instance.JoinFailed += OnGlobalJoinFailed;
        JoinCoordinator.Instance.JoinStarted += OnJoinStarted;

        RequestNow();
    }
    private void OnDisable()
    {
        roomListView.RefreshClicked -= RequestNow;
        roomListView.JoinClicked -= OnJoinClicked;
        roomListView.SelectionChanged -= OnSelectionChanged;

        lobbyModeFilter.FilterChanged -= filterChanged;

        if (JoinCoordinator.Instance != null)
        {
            JoinCoordinator.Instance.JoinFailed -= OnGlobalJoinFailed;
            JoinCoordinator.Instance.JoinStarted -= OnJoinStarted; 
        }

        isBusy = false;
        selectedID = CSteamID.Nil;
        lobbyCollector.CancelActiveRequest();
    }

    private void OnSelectionChanged(CSteamID id)
    {
        selectedID = id;
        UpdateJoinButton();
    }
    private void OnJoinClicked()
    {
        if (!selectedID.IsValid()) return;
        if (JoinCoordinator.Instance.IsJoining) return;

        roomListView.SetJoinInteractable(false);

        JoinCoordinator.Instance.JoinByLobbyId(selectedID);
        UpdateJoinButton();
    }
    private void OnGlobalJoinFailed(CSteamID id, ErrorType _, bool hard)
    {
        if (hard && id.IsValid())
        {
            roomListView.RemoveLobbyItem(id);
            if (selectedID == id) selectedID = CSteamID.Nil;
        }
        UpdateJoinButton();
    }
    private void OnCollectorResult(List<CSteamID> ids)
    {
        isBusy = false;
        roomListView.SetBusy(false);
        roomListView.ShowLobbies(ids);
    }
    private void OnJoinStarted(CSteamID _)
    {
        roomListView.SetJoinInteractable(false);
    }

    private void RequestNow()
    {
        if (isBusy) CancelVisualBusy();
        isBusy = true;
        selectedID = CSteamID.Nil;

        roomListView.SetBusy(true);
        roomListView.ClearAll();
        UpdateJoinButton();

        LobbyMode source = lobbyModeFilter.IsPublicFilter ? LobbyMode.Public : LobbyMode.Friends;
        lobbyCollector.Request(source, OnCollectorResult);
    }
    private void CancelVisualBusy()
    {
        isBusy = false;
        roomListView.SetBusy(false);
    }

    private void UpdateJoinButton()
    {
        bool canJoin =
            selectedID.IsValid() &&
            !isBusy &&
            !JoinCoordinator.Instance.IsJoining &&
            !(SteamLobbyManager.Instance?.JoinInProgress ?? false);

        roomListView.SetJoinInteractable(canJoin);
    }
}
