using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;

/// <summary>
/// Writes CSV data to persistent storage.
/// Works across Editor, Standalone builds, and Android builds.
/// Files are saved to Application.persistentDataPath for write access on all platforms.
/// </summary>
public class CSVWriter : MonoBehaviour
{
    [Header("CSV Configuration")]
    [Tooltip("Base filename for the CSV (extension will be added automatically)")]
    public string baseFileName = "trial_data";

    [Tooltip("Include timestamp in filename?")]
    public bool includeTimestamp = true;

    [Tooltip("Include ParticipantID in filename?")]
    public bool includeParticipantID = true;

    [Header("Platform-Specific Settings")]
    [Tooltip("Save to Desktop when running in Editor or PC builds? (Easier access during testing)")]
    public bool saveToDesktopInEditor = true;

    [Tooltip("Android: Save to external storage (accessible via USB)? If false, uses internal storage.")]
    public bool useExternalStorageOnAndroid = true;

    [Header("Debug Display")]
    [Tooltip("Reference to TrialDebugDisplay for showing logs in VR")]
    public TrialDebugDisplay debugDisplay;

    // Current file path
    private string currentFilePath;
    private List<string> headers = new List<string>();
    private bool fileInitialized = false;

    void Awake()
    {
        Debug.Log("[CSVWriter] ========== AWAKE ==========");
        Debug.Log($"[CSVWriter] saveToDesktopInEditor: {saveToDesktopInEditor}");
        Debug.Log($"[CSVWriter] useExternalStorageOnAndroid: {useExternalStorageOnAndroid}");

        // Try to find debug display if not assigned
        if (debugDisplay == null)
        {
            debugDisplay = FindObjectOfType<TrialDebugDisplay>();
            if (debugDisplay == null)
            {
                Debug.LogWarning("[CSVWriter] TrialDebugDisplay not found. Logs will only show in Unity Console.");
            }
        }
    }

    void Start()
    {
        Debug.Log("[CSVWriter] ========== START ==========");
        LogMessage("=== CSV WRITER STARTED ===");
        LogMessage($"Platform: {Application.platform}");
        LogMessage($"Base filename: {baseFileName}");
        LogMessage($"Timestamp: {includeTimestamp}");
        LogMessage($"Include PID: {includeParticipantID}");
        LogMessage($"saveToDesktopInEditor: {saveToDesktopInEditor}");
        LogMessage($"useExternalStorageOnAndroid: {useExternalStorageOnAndroid}");

        string testPath = GetSaveDirectory();
        LogMessage($"Save directory: {testPath}");

        // Test write permissions
        TestWritePermissions();
    }

    /// <summary>
    /// Tests if we have write permissions to the target directory.
    /// </summary>
    private void TestWritePermissions()
    {
        try
        {
            string testDir = GetSaveDirectory();
            string testFile = Path.Combine(testDir, "_test_write.txt");

            LogMessage("Testing write permissions...");
            LogMessage($"Test file path: {testFile}");

            File.WriteAllText(testFile, "test");

            if (File.Exists(testFile))
            {
                File.Delete(testFile);
                LogMessage("[OK] Write permissions verified");
            }
            else
            {
                LogError("[FAIL] Test file not created");
            }
        }
        catch (Exception e)
        {
            LogError($"[FAIL] Write test failed: {e.Message}");
            LogError($"Exception type: {e.GetType().Name}");
            LogError($"Stack trace: {e.StackTrace}");
        }
    }

    /// <summary>
    /// Gets the directory where CSV files are saved.
    /// Platform-specific logic for Desktop, Editor, and Android.
    /// </summary>
    public string GetSaveDirectory()
    {
        string basePath;

#if UNITY_ANDROID && !UNITY_EDITOR
        // Android build (Quest/standalone Android app)
        Debug.Log("[CSVWriter] Platform: ANDROID BUILD");
        if (useExternalStorageOnAndroid)
        {
            // Use external storage - accessible via USB when device is connected
            // This is /storage/emulated/0/ or /sdcard/
            basePath = "/storage/emulated/0/Documents";
            Debug.Log($"[CSVWriter] Android: using external storage (USB accessible)");
        }
        else
        {
            // Use internal app storage - only accessible via adb or file manager apps
            basePath = Application.persistentDataPath;
            Debug.Log($"[CSVWriter] Android: using internal app storage");
        }
#elif UNITY_EDITOR || UNITY_STANDALONE
        // PC Editor or PC Standalone build
        Debug.Log("[CSVWriter] Platform: EDITOR or STANDALONE");
        Debug.Log($"[CSVWriter] saveToDesktopInEditor = {saveToDesktopInEditor}");

        if (saveToDesktopInEditor)
        {
            // Save to Desktop for easy access during development
            try
            {
                basePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                Debug.Log($"[CSVWriter] ✓ Desktop path: {basePath}");

                if (string.IsNullOrEmpty(basePath))
                {
                    Debug.LogError("[CSVWriter] Desktop path is NULL or empty! Falling back to persistentDataPath");
                    basePath = Application.persistentDataPath;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CSVWriter] Failed to get Desktop path: {e.Message}");
                basePath = Application.persistentDataPath;
            }
        }
        else
        {
            // Use persistent data path
            basePath = Application.persistentDataPath;
            Debug.Log($"[CSVWriter] PC/Editor: using persistent path");
        }
#else
        // Fallback for other platforms (iOS, WebGL, etc.)
        Debug.Log("[CSVWriter] Platform: OTHER");
        basePath = Application.persistentDataPath;
        Debug.Log($"[CSVWriter] Other platform: using persistent path");
#endif

        Debug.Log($"[CSVWriter] Base path resolved to: '{basePath}'");

        if (string.IsNullOrEmpty(basePath))
        {
            Debug.LogError("[CSVWriter] CRITICAL: Base path is NULL or empty!");
            basePath = Application.persistentDataPath;
            Debug.LogError($"[CSVWriter] Emergency fallback to: {basePath}");
        }

        // Create subdirectory for experiment data
        string experimentDataPath;

        try
        {
            experimentDataPath = Path.Combine(basePath, "ExperimentData");
            Debug.Log($"[CSVWriter] Attempting to create directory: {experimentDataPath}");

            if (!Directory.Exists(experimentDataPath))
            {
                DirectoryInfo dirInfo = Directory.CreateDirectory(experimentDataPath);
                Debug.Log($"[CSVWriter] ✓ Created directory: {dirInfo.FullName}");
                Debug.Log($"[CSVWriter] Directory exists check: {Directory.Exists(experimentDataPath)}");
            }
            else
            {
                Debug.Log($"[CSVWriter] ✓ Directory already exists: {experimentDataPath}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[CSVWriter] ✗ Failed to create ExperimentData directory!");
            Debug.LogError($"[CSVWriter] Error: {e.Message}");
            Debug.LogError($"[CSVWriter] Exception type: {e.GetType().Name}");
            Debug.LogError($"[CSVWriter] Stack trace: {e.StackTrace}");
            Debug.LogError($"[CSVWriter] Falling back to base path: {basePath}");
            return basePath; // Fall back to base path
        }

        Debug.Log($"[CSVWriter] Final save directory: {experimentDataPath}");
        return experimentDataPath;
    }

    /// <summary>
    /// Gets the full path of the current CSV file.
    /// </summary>
    public string GetCurrentFilePath()
    {
        return currentFilePath;
    }

    /// <summary>
    /// Initializes a new CSV file with headers.
    /// </summary>
    public void InitializeFile(List<string> columnHeaders, int participantID = -1)
    {
        Debug.Log("[CSVWriter] ========== INITIALIZE FILE ==========");
        LogMessage("=== INITIALIZING CSV FILE ===");

        if (columnHeaders == null || columnHeaders.Count == 0)
        {
            LogError("[FAIL] No headers provided!");
            return;
        }

        headers = new List<string>(columnHeaders);
        LogMessage($"Headers count: {headers.Count}");

        // Get save directory
        string saveDirectory = GetSaveDirectory();
        LogMessage($"Save dir: {saveDirectory}");

        // Generate filename
        string fileName = GenerateFileName(participantID);
        LogMessage($"Filename: {fileName}");

        currentFilePath = Path.Combine(saveDirectory, fileName);
        LogMessage($"Full path: {currentFilePath}");

        try
        {
            // Check if file already exists
            bool fileExists = File.Exists(currentFilePath);
            LogMessage($"File exists: {fileExists}");

            if (!fileExists)
            {
                // Write headers to new file
                string headerLine = FormatCSVLine(headers);
                LogMessage($"Writing headers: {headerLine}");

                File.WriteAllText(currentFilePath, headerLine + "\n");

                // Verify file was created
                if (File.Exists(currentFilePath))
                {
                    FileInfo fi = new FileInfo(currentFilePath);
                    LogMessage($"[OK] File created: {fi.Length} bytes");
                    LogMessage($"[OK] Full path: {fi.FullName}");
                }
                else
                {
                    LogError("[FAIL] File not created!");
                }
            }
            else
            {
                FileInfo fi = new FileInfo(currentFilePath);
                LogMessage($"[OK] File exists: {fi.Length} bytes");
            }

            fileInitialized = true;
            LogMessage("[OK] CSV ready for writing");
            Debug.Log($"[CSVWriter] ✓✓✓ CSV INITIALIZED SUCCESSFULLY ✓✓✓");
        }
        catch (UnauthorizedAccessException e)
        {
            LogError($"[FAIL] Permission denied: {e.Message}");
            fileInitialized = false;
        }
        catch (DirectoryNotFoundException e)
        {
            LogError($"[FAIL] Directory not found: {e.Message}");
            fileInitialized = false;
        }
        catch (IOException e)
        {
            LogError($"[FAIL] IO error: {e.Message}");
            fileInitialized = false;
        }
        catch (Exception e)
        {
            LogError($"[FAIL] Unexpected error: {e.Message}");
            LogError($"Type: {e.GetType().Name}");
            LogError($"Stack: {e.StackTrace}");
            fileInitialized = false;
        }
    }

    /// <summary>
    /// Writes a row of data to the CSV file.
    /// </summary>
    public void WriteRow(List<string> rowData)
    {
        if (!fileInitialized)
        {
            LogError("[FAIL] CSV not initialized! Call InitializeFile() first");
            return;
        }

        if (rowData == null)
        {
            LogError("[FAIL] Row data is null!");
            return;
        }

        if (rowData.Count != headers.Count)
        {
            LogWarning($"[WARN] Column count: {rowData.Count}/{headers.Count}");
        }

        try
        {
            string line = FormatCSVLine(rowData);
            File.AppendAllText(currentFilePath, line + "\n");

            // Verify write
            FileInfo fi = new FileInfo(currentFilePath);
            LogMessage($"[OK] Row written. File: {fi.Length} bytes");
        }
        catch (Exception e)
        {
            LogError($"[FAIL] Write failed: {e.Message}");
            LogError($"Type: {e.GetType().Name}");
        }
    }

    /// <summary>
    /// Writes a row using a dictionary (matches values to header names).
    /// </summary>
    public void WriteRow(Dictionary<string, string> rowData)
    {
        if (!fileInitialized)
        {
            LogError("[FAIL] CSV not initialized");
            return;
        }

        if (rowData == null)
        {
            LogError("[FAIL] Row data is null!");
            return;
        }

        List<string> orderedData = new List<string>();

        foreach (string header in headers)
        {
            if (rowData.TryGetValue(header, out string value))
            {
                orderedData.Add(value);
            }
            else
            {
                orderedData.Add("");
                LogWarning($"[WARN] Missing field: {header}");
            }
        }

        WriteRow(orderedData);
    }

    /// <summary>
    /// Appends multiple rows at once.
    /// </summary>
    public void WriteRows(List<List<string>> rows)
    {
        foreach (var row in rows)
        {
            WriteRow(row);
        }
    }

    /// <summary>
    /// Formats a list of values into a properly escaped CSV line.
    /// Handles quotes and commas within values.
    /// </summary>
    private string FormatCSVLine(List<string> values)
    {
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < values.Count; i++)
        {
            string value = values[i] ?? "";

            // Check if value needs quoting (contains comma, quote, or newline)
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                // Escape quotes by doubling them
                value = value.Replace("\"", "\"\"");
                value = $"\"{value}\"";
            }

            sb.Append(value);

            if (i < values.Count - 1)
            {
                sb.Append(",");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates a filename based on configuration.
    /// </summary>
    private string GenerateFileName(int participantID)
    {
        StringBuilder fileName = new StringBuilder(baseFileName);

        if (includeParticipantID && participantID != -1)
        {
            fileName.Append($"_P{participantID:D3}");
        }

        if (includeTimestamp)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            fileName.Append($"_{timestamp}");
        }

        fileName.Append(".csv");

        return fileName.ToString();
    }

    /// <summary>
    /// Opens the save directory in the system's file explorer.
    /// Only works on Desktop platforms.
    /// </summary>
    public void OpenSaveDirectory()
    {
        string path = GetSaveDirectory();

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        try
        {
            System.Diagnostics.Process.Start("explorer.exe", path.Replace("/", "\\"));
            Debug.Log($"[CSVWriter] Opened file explorer: {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[CSVWriter] Failed to open file explorer: {e.Message}");
        }
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        System.Diagnostics.Process.Start("open", path);
#elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
        System.Diagnostics.Process.Start("xdg-open", path);
#else
        LogMessage($"[Android/Other] Cannot open file explorer. Save directory: {path}");
        LogMessage($"On Android, connect device via USB and browse to this path");
#endif
    }

    /// <summary>
    /// Checks if a file is currently initialized and ready for writing.
    /// </summary>
    public bool IsInitialized()
    {
        return fileInitialized;
    }

    /// <summary>
    /// Resets the writer to allow initializing a new file.
    /// </summary>
    public void Reset()
    {
        fileInitialized = false;
        currentFilePath = null;
        headers.Clear();
        LogMessage("CSV writer reset");
    }

    // Logging helper methods
    private void LogMessage(string message)
    {
        string fullMessage = $"[CSV] {message}";

        if (debugDisplay != null)
        {
            debugDisplay.AddLog(fullMessage, LogType.Log);
        }

        Debug.Log($"[CSVWriter] {message}");
    }

    private void LogWarning(string message)
    {
        string fullMessage = $"[CSV] {message}";

        if (debugDisplay != null)
        {
            debugDisplay.AddLog(fullMessage, LogType.Warning);
        }

        Debug.LogWarning($"[CSVWriter] {message}");
    }

    private void LogError(string message)
    {
        string fullMessage = $"[CSV] {message}";

        if (debugDisplay != null)
        {
            debugDisplay.AddLog(fullMessage, LogType.Error);
        }

        Debug.LogError($"[CSVWriter] {message}");
    }
}