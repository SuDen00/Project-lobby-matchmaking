using UnityEngine;
using TMPro;
using System.Collections;

public class LoadingScreenManager : MonoBehaviour
{
    private const float autoHideSeconds = Constants.TimeoutValue;
    public static LoadingScreenManager Instance { get; private set; }

    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text title;

    private Coroutine autoHideRoutine;

    public bool Visible => root && root.activeSelf;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Hide();
    }

    public void Show(string message = "Loading...")
    {
        if (title) title.text = message;
        if (root) root.SetActive(true);
        RestartAutoHide(autoHideSeconds);
    }

    public void Hide()
    {
        CancelAutoHide(); 
        if (root) root.SetActive(false);
    }

    private void RestartAutoHide(float seconds)
    {
        if (seconds <= 0f) { CancelAutoHide(); return; }
        if (autoHideRoutine != null) StopCoroutine(autoHideRoutine);
        autoHideRoutine = StartCoroutine(AutoHide(seconds));
    }

    private IEnumerator AutoHide(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        autoHideRoutine = null;
        if (Visible) Hide();
    }

    private void CancelAutoHide()
    {
        if (autoHideRoutine != null)
        {
            StopCoroutine(autoHideRoutine);
            autoHideRoutine = null;
        }
    }
}
