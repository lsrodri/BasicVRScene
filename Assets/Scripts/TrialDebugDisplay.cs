using UnityEngine;
using TMPro;
using System.Text;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Debug component that displays current trial data from TrialController.
/// Shows all fields and values in a TextMeshPro component for testing.
/// </summary>
public class TrialDebugDisplay : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the TrialController")]
    public TrialController trialController;

    [Tooltip("TextMeshPro component to display debug info")]
    public TMP_Text debugDisplay;

    [Tooltip("TextMeshPro component to display status messages (completion, errors, etc.)")]
    public TMP_Text statusDisplay;

    [Tooltip("TextMeshPro component to display system logs (CSV loading, file operations, etc.)")]
    public TMP_Text logDisplay;

    [Header("Display Settings")]
    [Tooltip("Update display every frame?")]
    public bool updateContinuously = false;

    [Tooltip("Update display on Start?")]
    public bool updateOnStart = true;

    [Tooltip("Maximum number of log messages to keep")]
    public int maxLogMessages = 20;

    [Header("Formatting")]
    [Tooltip("Show field names in the display?")]
    public bool showFieldNames = true;

    [Tooltip("Character to use between field name and value")]
    public string separator = ": ";

    // Log message queue
    private Queue<string> logMessages = new Queue<string>();

    void Start()
    {
        // Subscribe to trial loaded event
        if (trialController != null)
        {
            trialController.OnTrialLoaded.AddListener(OnTrialDataLoaded);
            Debug.Log("[TrialDebugDisplay] Subscribed to OnTrialLoaded event");
        }
        else
        {
            Debug.LogError("[TrialDebugDisplay] TrialController reference is null!");
        }

        if (updateOnStart)
        {
            UpdateDisplay();
        }
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (trialController != null)
        {
            trialController.OnTrialLoaded.RemoveListener(OnTrialDataLoaded);
        }
    }

    /// <summary>
    /// Called when trial data is loaded via the OnTrialLoaded event.
    /// </summary>
    private void OnTrialDataLoaded()
    {
        Debug.Log("[TrialDebugDisplay] OnTrialDataLoaded() event triggered");
        UpdateDisplay();
    }

    void Update()
    {
        if (updateContinuously)
        {
            UpdateDisplay();
        }
    }

    /// <summary>
    /// Updates the debug display with current trial data.
    /// Call this manually to refresh the display.
    /// </summary>
    public void UpdateDisplay()
    {
        if (debugDisplay == null)
        {
            return;
        }

        if (trialController == null)
        {
            debugDisplay.text = "TrialController reference not set";
            UpdateStatus("ERROR: TrialController not assigned", Color.red);
            return;
        }

        if (!trialController.IsTrialLoaded)
        {
            debugDisplay.text = "No trial data loaded\n\nWaiting for trial data...";
            UpdateStatus("Waiting for trial data...", Color.yellow);
            return;
        }

        // Check if this is the last trial
        if (trialController.IsLastTrial())
        {
            UpdateStatus($"LAST TRIAL - {trialController.GetTrialProgress()}", Color.yellow);
        }
        else
        {
            UpdateStatus($"Progress: {trialController.GetTrialProgress()}", Color.white);
        }

        // Build the display text
        StringBuilder sb = new StringBuilder();
        
        // Header info
        sb.AppendLine($"=== TRIAL DEBUG INFO ===");
        sb.AppendLine($"Platform: {Application.platform}");
        sb.AppendLine($"PID: {trialController.CurrentParticipantID}");
        sb.AppendLine($"Trial: {trialController.CurrentTrialNumber}");
        sb.AppendLine($"Progress: {trialController.GetTrialProgress()}");
        sb.AppendLine();
        
        // SHOW ACTUAL SAVE LOCATION from CSVWriter
        sb.AppendLine("=== DATA LOCATION ===");
        
        // Get the ACTUAL save path from CSVWriter (respects platform-specific settings)
        CSVWriter csvWriter = FindObjectOfType<CSVWriter>();
        if (csvWriter != null)
        {
            string actualSavePath = csvWriter.GetSaveDirectory();
            sb.AppendLine($"Save Path:");
            sb.AppendLine($"{actualSavePath}");
            
            // Show which storage type is being used
#if UNITY_ANDROID && !UNITY_EDITOR
            if (csvWriter.useExternalStorageOnAndroid)
            {
                sb.AppendLine();
                sb.AppendLine($"Storage: External (USB accessible)");
                sb.AppendLine($"Access: Connect device via USB");
            }
            else
            {
                sb.AppendLine();
                sb.AppendLine($"Storage: Internal app storage");
                sb.AppendLine($"Access: Requires adb or file manager");
            }
#elif UNITY_EDITOR || UNITY_STANDALONE
            if (csvWriter.saveToDesktopInEditor)
            {
                sb.AppendLine();
                sb.AppendLine($"Storage: Desktop (development mode)");
            }
            else
            {
                sb.AppendLine();
                sb.AppendLine($"Storage: Persistent data path");
            }
#endif
        }
        else
        {
            // Fallback if CSVWriter not found
            sb.AppendLine($"Save Path:");
            sb.AppendLine($"{Application.persistentDataPath}");
            sb.AppendLine();
            sb.AppendLine($"[WARNING] CSVWriter not found!");
        }
        sb.AppendLine();
        
        sb.AppendLine("=== TRIAL DATA ===");
        
        // Display all fields
        foreach (var field in trialController.CurrentTrialData)
        {
            if (showFieldNames)
            {
                sb.AppendLine($"{field.Key}{separator}{field.Value}");
            }
            else
            {
                sb.AppendLine(field.Value);
            }
        }

        debugDisplay.text = sb.ToString();
    }

    /// <summary>
    /// Updates display showing only specific fields.
    /// </summary>
    public void UpdateDisplayWithFields(params string[] fieldNames)
    {
        if (debugDisplay == null || trialController == null || !trialController.IsTrialLoaded)
        {
            UpdateDisplay();
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"PID: {trialController.CurrentParticipantID} | Trial: {trialController.CurrentTrialNumber}");
        sb.AppendLine($"Progress: {trialController.GetTrialProgress()}");
        sb.AppendLine();

        foreach (string fieldName in fieldNames)
        {
            string value = trialController.GetFieldValue(fieldName);
            if (value != null)
            {
                sb.AppendLine($"{fieldName}{separator}{value}");
            }
        }

        debugDisplay.text = sb.ToString();
    }

    /// <summary>
    /// Displays a completion message when all trials are finished.
    /// Call this when there are no more trials left.
    /// </summary>
    public void ShowCompletionMessage()
    {
        if (trialController == null)
        {
            UpdateStatus("ERROR: Cannot show completion - TrialController not assigned", Color.red);
            return;
        }

        // Update main display
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== ALL TRIALS COMPLETED ===");
        sb.AppendLine();
        sb.AppendLine($"Participant ID: {trialController.CurrentParticipantID}");
        sb.AppendLine($"Total Trials Completed: {trialController.GetTotalTrialsForCurrentParticipant()}");
        sb.AppendLine();
        sb.AppendLine($"Data saved to:");
        sb.AppendLine($"{Application.persistentDataPath}");
        sb.AppendLine();
        sb.AppendLine("Thank you for participating!");

        if (debugDisplay != null)
        {
            debugDisplay.text = sb.ToString();
        }

        // Update status display
        UpdateStatus("ALL TRIALS COMPLETED!", Color.green);
    }

    /// <summary>
    /// Updates the status display with a message and color.
    /// </summary>
    public void UpdateStatus(string message, Color color)
    {
        if (statusDisplay != null)
        {
            statusDisplay.text = message;
            statusDisplay.color = color;
        }
    }

    /// <summary>
    /// Clears the status display.
    /// </summary>
    public void ClearStatus()
    {
        if (statusDisplay != null)
        {
            statusDisplay.text = "";
        }
    }

    /// <summary>
    /// Adds a log message to the log display.
    /// This is visible in VR and replaces Debug.Log for important messages.
    /// </summary>
    public void AddLog(string message, LogType logType = LogType.Log)
    {
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
        string prefix = GetLogPrefix(logType);
        string formattedMessage = $"[{timestamp}] {prefix}{message}";

        logMessages.Enqueue(formattedMessage);

        // Keep only the last N messages
        while (logMessages.Count > maxLogMessages)
        {
            logMessages.Dequeue();
        }

        UpdateLogDisplay();

        // Also log to Unity console for Editor debugging
        switch (logType)
        {
            case LogType.Error:
                Debug.LogError(message);
                break;
            case LogType.Warning:
                Debug.LogWarning(message);
                break;
            default:
                Debug.Log(message);
                break;
        }
    }

    /// <summary>
    /// Gets a prefix string based on log type.
    /// </summary>
    private string GetLogPrefix(LogType logType)
    {
        switch (logType)
        {
            case LogType.Error:
                return "[ERROR] ";
            case LogType.Warning:
                return "[WARN] ";
            default:
                return "";
        }
    }

    /// <summary>
    /// Updates the log display with all queued messages.
    /// </summary>
    private void UpdateLogDisplay()
    {
        if (logDisplay == null)
            return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== SYSTEM LOG ===");

        foreach (string message in logMessages)
        {
            sb.AppendLine(message);
        }

        logDisplay.text = sb.ToString();
    }

    /// <summary>
    /// Clears all log messages.
    /// </summary>
    public void ClearLogs()
    {
        logMessages.Clear();
        UpdateLogDisplay();
    }

    /// <summary>
    /// Shows all relevant paths for debugging.
    /// </summary>
    public void ShowDebugPaths()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== DEBUG PATHS ===");
        sb.AppendLine($"Platform: {Application.platform}");
        sb.AppendLine();
        sb.AppendLine($"Persistent Data Path:");
        sb.AppendLine($"{Application.persistentDataPath}");
        sb.AppendLine();
        sb.AppendLine($"Streaming Assets Path:");
        sb.AppendLine($"{Application.streamingAssetsPath}");
        sb.AppendLine();
        sb.AppendLine($"Data Path:");
        sb.AppendLine($"{Application.dataPath}");

        if (debugDisplay != null)
        {
            debugDisplay.text = sb.ToString();
        }

        AddLog("Debug paths displayed");
    }

    /// <summary>
    /// Verifies CSV files exist and shows their info.
    /// </summary>
    public void VerifyCSVFiles()
    {
        ClearLogs();
        AddLog("=== CSV FILE VERIFICATION ===", LogType.Log);

        // Check input CSV in StreamingAssets
        string inputPath = Path.Combine(Application.streamingAssetsPath, "Trials.csv");
        AddLog($"Input CSV: {inputPath}", LogType.Log);

#if !UNITY_ANDROID || UNITY_EDITOR
        if (File.Exists(inputPath))
        {
            FileInfo inputInfo = new FileInfo(inputPath);
            AddLog($"[OK] Input exists: {inputInfo.Length} bytes", LogType.Log);
        }
        else
        {
            AddLog("[FAIL] Input CSV not found!", LogType.Error);
        }
#else
        AddLog("Cannot check file existence on Android", LogType.Warning);
#endif

        // Check output directory
        string outputDir = "/sdcard/Documents/ExperimentData";
        AddLog($"Output dir: {outputDir}", LogType.Log);

        if (Directory.Exists(outputDir))
        {
            AddLog("[OK] Output directory exists", LogType.Log);

            string[] files = Directory.GetFiles(outputDir, "*.csv");
            AddLog($"Found {files.Length} CSV files:", LogType.Log);

            foreach (string file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
                AddLog($"  - {Path.GetFileName(file)} ({fileInfo.Length} bytes)", LogType.Log);
            }
        }
        else
        {
            AddLog("[FAIL] Output directory not found!", LogType.Error);
        }
    }
}

/// <summary>
/// Log type enum for categorizing messages.
/// </summary>
public enum LogType
{
    Log,
    Warning,
    Error
}