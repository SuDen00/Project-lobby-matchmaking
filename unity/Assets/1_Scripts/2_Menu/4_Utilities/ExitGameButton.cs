using UnityEngine;
using UnityEngine.UI;

public class ExitGameButton : MonoBehaviour
{
    [SerializeField] private Button exitButton;

    private void OnEnable()
    {
        exitButton.onClick.AddListener(Quit);
    }

    private void OnDisable()
    {
        exitButton.onClick.RemoveListener(Quit);
    }

    private void Quit()
    {
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }
}
