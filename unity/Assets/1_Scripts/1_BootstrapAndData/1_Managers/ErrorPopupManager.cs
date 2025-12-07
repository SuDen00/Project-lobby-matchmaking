using UnityEngine;

public class ErrorPopupManager : MonoBehaviour
{
    public static ErrorPopupManager Instance { get; private set; }

    [SerializeField] private UIErrorPopup popupPrefab;

    private UIErrorPopup currentPopup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (currentPopup == null)
        {
            currentPopup = Instantiate(popupPrefab, gameObject.transform);
            currentPopup.gameObject.SetActive(false);
        }
    }

    public void ShowError(ErrorType type)
    {
        string message = type switch
        {
            ErrorType.ConnectionFailed => "Не удалось подключиться к серверу.",
            ErrorType.LobbyCreationFailed => "Не удалось создать лобби.",
            ErrorType.Kicked => "Вас выгнали из лобби.",
            ErrorType.HostExit => "Хост покинул лобби.",
            ErrorType.LobbyFull => "Сервер переполнен.",
            ErrorType.Timeout => "Время ожидания истекло.",
            ErrorType.ConnectionLost => "Потеряно соединение с сервером.",
            ErrorType.InvalidIP => "Некорректный IP-адрес.",
            ErrorType.LobbyClosed => "Лобби было закрыто.",
            ErrorType.LobbyNotFound => "Лобби не найдено.",
            ErrorType.HostMissing => "Хост отсутствует в лобби.",
            ErrorType.AlreadyConnected => "Вы уже подключены.",
            ErrorType.UnauthorizedAction => "Недопустимое действие.",
            ErrorType.AccessDenied => "Доступ к лобби запрещён.",
            ErrorType.Banned => "Вы заблокированы в этом лобби.",
            ErrorType.CommunityBanned => "Ваш аккаунт заблокирован сообществом.",
            ErrorType.MemberBlocked => "Вас заблокировал хост лобби.",
            ErrorType.YouBlockedMember => "Вы заблокировали хоста лобби.",
            ErrorType.RateLimitExceeded => "Слишком много запросов — попробуйте позже.",
            ErrorType.LimitedAccount => "Ваш аккаунт ограничен и не может войти.",
            ErrorType.ClanDisabled => "Чаты для сообщества отключены.",
            ErrorType.GenericJoinError => "Не удалось зайти в лобби.",
            ErrorType.AlreadyInLobby => "Сначала покиньте текущее лобби.",
            _ => "Произошла неизвестная ошибка."
        };

        ShowPopup(message);
    }
    private void ShowPopup(string message)
    {
        Debug.Log($"[ErrorPopupManager] Показ попапа с ошибкой: {message}");
        
        currentPopup.SetError(message);
        currentPopup.gameObject.SetActive(true);
        currentPopup.transform.SetAsLastSibling();
    }
}
