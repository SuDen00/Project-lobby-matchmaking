using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using Steamworks;

public class LobbyCreationController : MonoBehaviour
{
    [SerializeField] private TMP_InputField lobbyNameInputField;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private PlayerCountSelector playerCountSelector;

    private enum CreationState { Idle, Creating }
    private CreationState _state = CreationState.Idle;
    private Coroutine _creationTimeoutCoroutine;

    private void Awake()
    {
        lobbyNameInputField.characterLimit = Constants.MaxLobbyNameLength;
        lobbyNameInputField.onValidateInput = (text, idx, ch) =>
            (char.IsLetterOrDigit(ch) || ch == ' ' || ch == '-' || ch == '_' || ch == '!' || ch == '?') ? ch : '\0';

        ApplyUI();
    }

    private void OnEnable()
    {
        createLobbyButton.onClick.AddListener(OnCreateLobbyPressed);
        lobbyNameInputField.onValueChanged.AddListener(_ => ApplyUI());

        SteamLobbyManager.Instance.LobbyCreated += OnLobbyCreated;
        SteamLobbyManager.Instance.LobbyCreationFailed += OnLobbyCreationFailed;
    }

    private void OnDisable()
    {
        createLobbyButton.onClick.RemoveAllListeners();
        lobbyNameInputField.onValueChanged.RemoveAllListeners();

        SteamLobbyManager.Instance.LobbyCreated -= OnLobbyCreated;
        SteamLobbyManager.Instance.LobbyCreationFailed -= OnLobbyCreationFailed;

        StopCreationTimeoutCoroutine();
        _state = CreationState.Idle;
        ApplyUI();
    }

    private void OnCreateLobbyPressed()
    {
        if (_state == CreationState.Creating) return;
        _state = CreationState.Creating;

        string rawName = lobbyNameInputField.text;
        if (string.IsNullOrWhiteSpace(rawName)) return;

        ApplyUI();

        int maxPlayers = playerCountSelector.PlayerCount;

        PlayerPrefs.SetInt(PlayerPrefsKeys.MaxPlayers, maxPlayers);
        PlayerPrefs.SetString(PlayerPrefsKeys.LobbyName, rawName.Trim());
        PlayerPrefs.Save();

        NetworkManager.singleton.maxConnections = maxPlayers;

        SteamLobbyManager.Instance.CreateLobby(maxPlayers);

        StopCreationTimeoutCoroutine();
        _creationTimeoutCoroutine = StartCoroutine(CreationTimeoutCoroutine(Constants.TimeoutValue));
    }

    private void OnLobbyCreated(CSteamID _)
    {
        _state = CreationState.Idle;
        StopCreationTimeoutCoroutine();
        ApplyUI();
    }

    private void OnLobbyCreationFailed(EResult _)
    {
        _state = CreationState.Idle;
        StopCreationTimeoutCoroutine();
        ApplyUI();

        ErrorPopupManager.Instance.ShowError(ErrorType.LobbyCreationFailed);
    }

    private void ApplyUI()
    {
        bool hasName = !string.IsNullOrWhiteSpace(lobbyNameInputField.text);
        bool creating = _state == CreationState.Creating;

        createLobbyButton.interactable = hasName && !creating;
        lobbyNameInputField.interactable = !creating;
    }

    private void StopCreationTimeoutCoroutine()
    {
        if (_creationTimeoutCoroutine != null)
        {
            StopCoroutine(_creationTimeoutCoroutine);
            _creationTimeoutCoroutine = null;
        }
    }

    private IEnumerator CreationTimeoutCoroutine(float seconds)
    {
        float t = 0f;
        while (t < seconds && _state == CreationState.Creating)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (_state == CreationState.Creating)
        {
            _state = CreationState.Idle;
            ApplyUI();
            ErrorPopupManager.Instance.ShowError(ErrorType.Timeout);
        }

        _creationTimeoutCoroutine = null;
    }
}
