using System;
using System.IO;
using UnityEngine;

public class FileLogger : MonoBehaviour
{
    public static FileLogger Instance { get; private set; }

    private string logFilePath;
    private StreamWriter writer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        string logDir = Path.Combine(Application.persistentDataPath, "Logs");
        if (!Directory.Exists(logDir))
            Directory.CreateDirectory(logDir);

        logFilePath = Path.Combine(logDir, "latest_log.txt");
        writer = new StreamWriter(logFilePath, false); // false = перезаписать
        writer.AutoFlush = true;

        Application.logMessageReceived += HandleLog;

        Debug.Log($"[FileLogger] Лог записывается в: {logFilePath}");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Application.logMessageReceived -= HandleLog;
            writer?.Close();
        }
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        string time = DateTime.Now.ToString("HH:mm:ss");
        string prefix = type.ToString().ToUpper();

        writer.WriteLine($"[{time}] [{prefix}] {logString}");

        if (type == LogType.Exception || type == LogType.Error)
        {
            writer.WriteLine(stackTrace);
        }
    }
}
