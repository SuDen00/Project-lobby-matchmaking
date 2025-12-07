using UnityEngine;
using UnityEngine.UI;

public class LobbyModeSelector : MonoBehaviour
{
    [SerializeField] private Button publicButton;
    [SerializeField] private Button friendsButton;
    [SerializeField] private Button privateButton;

    private string currentMode;

    private void OnEnable()
    {
        currentMode = PlayerPrefs.GetString(PlayerPrefsKeys.CreatedLobbyType, "public");
        ApplyButtons();

        publicButton.onClick.AddListener(OnPublicClicked);
        friendsButton.onClick.AddListener(OnFriendsClicked);
        privateButton.onClick.AddListener(OnPrivateClicked);
    }

    private void OnDisable()
    {
        publicButton.onClick.RemoveListener(OnPublicClicked);
        friendsButton.onClick.RemoveListener(OnFriendsClicked);
        privateButton.onClick.RemoveListener(OnPrivateClicked);
    }

    private void OnPublicClicked()  => SetMode("public");
    private void OnFriendsClicked() => SetMode("friends");
    private void OnPrivateClicked() => SetMode("private");

    private void SetMode(string mode)
    {
        if (currentMode == mode) return;

        currentMode = mode;
        PlayerPrefs.SetString(PlayerPrefsKeys.CreatedLobbyType, mode);
        PlayerPrefs.Save();

        ApplyButtons();
    }

    private void ApplyButtons()
    {
        publicButton.interactable  = currentMode != "public";
        friendsButton.interactable = currentMode != "friends";
        privateButton.interactable = currentMode != "private";
    }
}
