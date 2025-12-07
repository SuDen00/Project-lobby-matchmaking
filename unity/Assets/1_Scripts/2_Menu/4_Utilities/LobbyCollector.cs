using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;

public enum LobbyMode { Public, Friends }

public sealed class LobbyCollector : MonoBehaviour
{
    private float publicRequestTimeout = Constants.TimeoutValue;
    private float friendsRequestTimeout = Mathf.Max(2f, Constants.TimeoutValue);
    private Coroutine activeRoutine;
    private int requestToken;

    private Action<List<CSteamID>> captureLobbyList;

    private void OnDisable() => CancelActiveRequest();

    public void CancelActiveRequest()
    {
        if (activeRoutine != null) { StopCoroutine(activeRoutine); activeRoutine = null; }

        SteamLobbyManager steamLobbyManager = SteamLobbyManager.Instance;
        if (captureLobbyList != null && steamLobbyManager != null)
        {
            steamLobbyManager.LobbyListReceived -= captureLobbyList;
            captureLobbyList = null;
        }

        requestToken++;
    }
    public void Request(LobbyMode source, Action<List<CSteamID>> callback)
    {
        CancelActiveRequest();

        int version = ++requestToken;
        activeRoutine = StartCoroutine(source == LobbyMode.Public
            ? CollectPublic(version, callback)
            : CollectFriends(version, callback));
    }

    private List<CSteamID> FilterLobbies(IEnumerable<CSteamID> ids, bool isFriendsTab)
    {
        var list = new List<CSteamID>();
        foreach (var id in ids)
        {
            if (!id.IsValid()) continue;

            if (!isFriendsTab)
            {
                int members = SteamMatchmaking.GetNumLobbyMembers(id);
                int limit   = SteamMatchmaking.GetLobbyMemberLimit(id);
                if (members <= 0) continue;
                if (limit > 0 && members >= limit) continue;
            }

            if (!TryGetHostSteamId(id, out _)) continue;

            string state = SteamMatchmaking.GetLobbyData(id, LobbyDataKeys.SessionState);
            if (!string.IsNullOrEmpty(state) && state != "lobby") continue;

            list.Add(id);
        }
        return list;
    }
    private IEnumerator CollectPublic(int version, Action<List<CSteamID>> callback)
    {
        SetupPublicFilters();

        List<CSteamID> result = null;

        captureLobbyList = (List<CSteamID> list) =>
        {
            result = list;
        };

        SteamLobbyManager.Instance.LobbyListReceived += captureLobbyList;
        SteamLobbyManager.Instance.RequestLobbyList();

        float time = 0f;
        while (result == null && time < publicRequestTimeout)
        {
            time += Time.unscaledDeltaTime;
            yield return null;
        }

        if (captureLobbyList != null)
        {
            SteamLobbyManager.Instance.LobbyListReceived -= captureLobbyList;
            captureLobbyList = null;
        }

        var rawList = result ?? new List<CSteamID>();
        var filteredList = FilterLobbies(rawList, isFriendsTab: false);

        if (version == requestToken) callback?.Invoke(filteredList);
        activeRoutine = null;
    }
    private IEnumerator CollectFriends(int version, Action<List<CSteamID>> callback)
    {
        SetupFriendsFilters();

        var friendIds = CollectFriendIds();

        List<CSteamID> result = null;

        captureLobbyList = (List<CSteamID> list) =>
        {
            result = list;
        };

        SteamLobbyManager.Instance.LobbyListReceived += captureLobbyList;
        SteamLobbyManager.Instance.RequestLobbyList();

        float t = 0f;
        while (result == null && t < friendsRequestTimeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (captureLobbyList != null)
        {
            SteamLobbyManager.Instance.LobbyListReceived -= captureLobbyList;
            captureLobbyList = null;
        }

        var rawList = result ?? new List<CSteamID>();
        var baseline = FilterLobbies(rawList, isFriendsTab: true);

        var filtered = new List<CSteamID>();
        foreach (var id in baseline)
        {
            string type = SteamMatchmaking.GetLobbyData(id, LobbyDataKeys.LobbyType);
            if (type == "private") continue;

            if (!TryGetHostSteamId(id, out ulong hostId)) continue;
            if (!friendIds.Contains(hostId)) continue;

            filtered.Add(id);
        }

        if (version == requestToken) callback?.Invoke(filtered);
        activeRoutine = null;
    }
    private void SetupPublicFilters()
    {
        SteamMatchmaking.AddRequestLobbyListResultCountFilter(Constants.MaxLobbyResults);
        SteamMatchmaking.AddRequestLobbyListStringFilter(LobbyDataKeys.LobbyType, "public", ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListStringFilter(LobbyDataKeys.SessionState, "lobby", ELobbyComparison.k_ELobbyComparisonEqual);
    }
    private void SetupFriendsFilters()
    {
        SteamMatchmaking.AddRequestLobbyListStringFilter(
            LobbyDataKeys.SessionState, "lobby", ELobbyComparison.k_ELobbyComparisonEqual);

        SteamMatchmaking.AddRequestLobbyListDistanceFilter(
            ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
    }
    

    private static bool TryGetHostSteamId(CSteamID lobbyId, out ulong hostId)
    {
        hostId = 0UL;
        string hostIdStr = SteamMatchmaking.GetLobbyData(lobbyId, LobbyDataKeys.HostSteamID);
        return !string.IsNullOrEmpty(hostIdStr) && ulong.TryParse(hostIdStr, out hostId);
    }
    private static HashSet<ulong> CollectFriendIds()
    {
        var set = new HashSet<ulong>();
        int n = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
        for (int i = 0; i < n; i++)
        {
            var f = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
            if (f.IsValid()) set.Add(f.m_SteamID);
        }
        return set;
    }
}
