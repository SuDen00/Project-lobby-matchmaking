using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerCountSelector : MonoBehaviour
{
    [SerializeField] private Button leftArrowButton;
    [SerializeField] private Button rightArrowButton;
    [SerializeField] private TMP_Text countText;

    private int minPlayers;
    private int maxPlayers;
    private int currentCount;
    
    public int PlayerCount => currentCount;

    private void Awake()
    {
        minPlayers = Constants.MinPlayersCount;
        maxPlayers = Constants.MaxPlayersCount;
    }

    private void Start()
    {
        int saved = PlayerPrefs.GetInt(PlayerPrefsKeys.MaxPlayers, Constants.MaxPlayersCount);
        currentCount = Mathf.Clamp(saved, minPlayers, maxPlayers);

        UpdateDisplay();

        leftArrowButton.onClick.AddListener(OnLeftArrowClicked);
        rightArrowButton.onClick.AddListener(OnRightArrowClicked);
    }

    private void OnLeftArrowClicked()
    {
        currentCount = Mathf.Clamp(currentCount - 1, minPlayers, maxPlayers);
        UpdateDisplay();
    }

    private void OnRightArrowClicked()
    {
        currentCount = Mathf.Clamp(currentCount + 1, minPlayers, maxPlayers);
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        leftArrowButton.interactable = currentCount > minPlayers;
        rightArrowButton.interactable = currentCount < maxPlayers;
        countText.text = currentCount.ToString();
    }
}
