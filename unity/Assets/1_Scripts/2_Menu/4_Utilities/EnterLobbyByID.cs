using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnterLobbyByID : MonoBehaviour
{
    [SerializeField] private TMP_InputField codeInputField;
    [SerializeField] private Button joinButton;

    private bool _busy;

    private void Awake()
    {
        joinButton.interactable = false;

        codeInputField.contentType = TMP_InputField.ContentType.IntegerNumber;
        codeInputField.lineType = TMP_InputField.LineType.SingleLine;
        codeInputField.characterLimit = Constants.CodeLength;
    }
    private void OnEnable()
    {
        codeInputField.onValueChanged.AddListener(OnCodeChanged);
        joinButton.onClick.AddListener(OnJoinClicked);

        JoinCoordinator.Instance.JoinSucceeded += OnJoinSucceeded;
        JoinCoordinator.Instance.JoinFailed    += OnJoinFailed;

        _busy = false;
        UpdateJoinButton();
    }
    private void OnDisable()
    {
        codeInputField.onValueChanged.RemoveListener(OnCodeChanged);
        joinButton.onClick.RemoveListener(OnJoinClicked);

        if (JoinCoordinator.Instance != null)
        {
            JoinCoordinator.Instance.JoinSucceeded -= OnJoinSucceeded;
            JoinCoordinator.Instance.JoinFailed    -= OnJoinFailed;
        }

        _busy = false;
        codeInputField.interactable = true;
        joinButton.interactable = false;
    }

    private void OnCodeChanged(string _) => UpdateJoinButton();
    private void OnJoinClicked()
    {
        if (!CanJoinNow()) return;

        JoinCoordinator.Instance.JoinByCode(codeInputField.text.Trim());

        bool isStarted = JoinCoordinator.Instance.IsJoining ||
                  (SteamLobbyManager.Instance?.JoinInProgress ?? false);
                  
        SetBusy(isStarted);
    }
    private void OnJoinSucceeded(CSteamID _) => SetBusy(false);
    private void OnJoinFailed(CSteamID _, ErrorType __, bool ___) => SetBusy(false);

    private void SetBusy(bool busy)
    {
        _busy = busy;
        codeInputField.interactable = !busy;
        UpdateJoinButton();
    }
    private void UpdateJoinButton()
    {
        bool hasCode        = codeInputField.text.Length == Constants.CodeLength;
        bool globalBusy     = (JoinCoordinator.Instance?.IsJoining ?? false) ||
                              (SteamLobbyManager.Instance?.JoinInProgress ?? false);
        bool alreadyInLobby = SteamLobbyManager.Instance?.CurrentLobbyID.IsValid() ?? false;

        joinButton.interactable = !_busy && !globalBusy && !alreadyInLobby && hasCode;
    }
    private bool CanJoinNow()
    {
        bool hasCode        = codeInputField.text.Length == Constants.CodeLength;
        bool globalBusy     = (JoinCoordinator.Instance?.IsJoining ?? false) ||
                              (SteamLobbyManager.Instance?.JoinInProgress ?? false);
        bool alreadyInLobby = SteamLobbyManager.Instance?.CurrentLobbyID.IsValid() ?? false;

        return !_busy && hasCode && !globalBusy && !alreadyInLobby;
    }
}
