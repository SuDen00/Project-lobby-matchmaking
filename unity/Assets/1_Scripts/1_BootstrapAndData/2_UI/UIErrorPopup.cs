using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIErrorPopup : MonoBehaviour
{
    [SerializeField] private TMP_Text errorMessageText;
    [SerializeField] private Button closeButton;

    private void Awake() => closeButton.onClick.AddListener(OnCloseButtonPressed);

    public void SetError(string error)
    {
        errorMessageText.text = error;
    }
    public void OnCloseButtonPressed() => gameObject.SetActive(false);
}
