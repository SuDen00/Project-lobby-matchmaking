using System;
using UnityEngine;
using UnityEngine.UI;

public class LobbyModeFilter : MonoBehaviour
{
    [SerializeField] private Button publicFilterButton;
    [SerializeField] private Button friendsFilterButton;

    public bool IsPublicFilter { get; private set; } = true;

    public event Action<bool> FilterChanged;
    
    private void OnEnable()
    {
        var saved = PlayerPrefs.GetString(PlayerPrefsKeys.LobbyTypeFilter, "public");
        IsPublicFilter = saved == "public";

        UpdateUI();

        publicFilterButton.onClick.AddListener(OnPublicClicked);
        friendsFilterButton.onClick.AddListener(OnFriendsClicked);

        FilterChanged?.Invoke(IsPublicFilter);
    }

    private void OnDisable()
    {
        publicFilterButton.onClick.RemoveListener(OnPublicClicked);
        friendsFilterButton.onClick.RemoveListener(OnFriendsClicked);
    }

    private void OnPublicClicked() => ApplyFilter(true);
    private void OnFriendsClicked() => ApplyFilter(false);

    private void ApplyFilter(bool isPublic)
    {
        if (IsPublicFilter == isPublic) return;
        
        IsPublicFilter = isPublic;

        UpdateUI();

        PlayerPrefs.SetString(PlayerPrefsKeys.LobbyTypeFilter, isPublic ? "public" : "friends");
        PlayerPrefs.Save();

        FilterChanged?.Invoke(IsPublicFilter);
    }

    private void UpdateUI()
    {
        publicFilterButton.interactable = !IsPublicFilter;
        friendsFilterButton.interactable = IsPublicFilter;
    }
}
