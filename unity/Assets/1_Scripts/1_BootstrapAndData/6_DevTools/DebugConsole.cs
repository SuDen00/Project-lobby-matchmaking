using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DebugConsole : MonoBehaviour
{
    private static DebugConsole instance;
    private static string fullLog = "";

    [SerializeField] private TMP_Text logText;
    [SerializeField] private GameObject root;
    [SerializeField] private KeyCode toggleKey = KeyCode.BackQuote;
    [SerializeField] private ScrollRect scrollRect;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            root.SetActive(!root.activeSelf);
    }

    private void OnEnable() => Application.logMessageReceived += HandleUnityLog;
    private void OnDisable() => Application.logMessageReceived -= HandleUnityLog;

    private void HandleUnityLog(string message, string stackTrace, LogType type)
    {
        string prefix = type switch
        {
            LogType.Error => "[ERROR] ",
            LogType.Warning => "[WARNING] ",
            LogType.Exception => "[EXCEPTION] ",
            LogType.Assert => "[ASSERT] ",
            _ => "[LOG] "
        };

        string color = type switch
        {
            LogType.Error => "red",
            LogType.Warning => "yellow",
            LogType.Exception => "red",
            LogType.Assert => "orange",
            _ => "white"
        };

        Log(prefix + message, color);
        if (type == LogType.Exception)
            Log(stackTrace, color);
    }

    private static void Log(string msg, string color = "white")
    {
        string time = System.DateTime.Now.ToString("HH:mm:ss");
        string coloredMsg = $"<color={color}>[{time}] {msg}</color>\n";

        fullLog += coloredMsg;

        if (instance != null && instance.logText != null)
        {
            instance.logText.text = fullLog;
            instance.ScrollToBottom();
            instance.DisableSubMeshRaycasts();
        }
    }

    private void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    private void DisableSubMeshRaycasts()
    {
        var subMeshes = GetComponentsInChildren<TMPro.TMP_SubMeshUI>();
        foreach (var subMesh in subMeshes)
            subMesh.raycastTarget = false;
    }
}
