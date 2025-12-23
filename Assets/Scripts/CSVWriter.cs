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

    [Header("Android/Quest Settings")]
    [Tooltip("Use external storage (SDCard) on Android? Makes files accessible via SideQuest")]
    public bool useExternalStorage = true;

    [Header("Debug Display")]
    [Tooltip("Reference to TrialDebugDisplay for showing logs in VR")]
    public TrialDebugDisplay debugDisplay;

    // Current file path
    private string currentFilePath;
    private List<string> headers = new List<string>();
    private bool fileInitialized = false;

    /// <summary>
    /// Gets the directory where CSV files are saved.
    /// </summary>
    public string GetSaveDirectory()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        if (useExternalStorage)
        {
            // Use external storage (SDCard) - accessible via SideQuest
            string externalPath = "/sdcard/Documents/ExperimentData";
            
            // Try to create directory if it doesn't exist
            try
            {
                if (!Directory.Exists(externalPath))
                {
                    Directory.CreateDirectory(externalPath);
                    LogMessage($"Created directory: {externalPath}");
                }
                return externalPath;
            }
            catch (Exception e)
            {
                LogError($"Failed to create external directory: {e.Message}");
                LogMessage("Falling back to internal storage");
                return Application.persistentDataPath;
            }
        }
        #endif
        
        return Application.persistentDataPath;
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
        headers = new List<string>(columnHeaders);
        
        // Get save directory
        string saveDirectory = GetSaveDirectory();
        
        // Generate filename
        string fileName = GenerateFileName(participantID);
        currentFilePath = Path.Combine(saveDirectory, fileName);

        LogMessage($"=== CSV INITIALIZATION ===");
        LogMessage($"Platform: {Application.platform}");
        LogMessage($"Save Directory: {saveDirectory}");
        LogMessage($"File Name: {fileName}");
        LogMessage($"Full Path: {currentFilePath}");

        try
        {
            // Ensure directory exists
            string directory = Path.GetDirectoryName(currentFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                LogMessage($"Created directory: {directory}");
            }

            // Check if file already exists
            bool fileExists = File.Exists(currentFilePath);
            LogMessage($"File exists: {fileExists}");

            if (!fileExists)
            {
                // Write headers to new file
                string headerLine = FormatCSVLine(headers);
                File.WriteAllText(currentFilePath, headerLine + "\n");
                LogMessage($"[OK] CSV initialized: {headers.Count} columns");
                LogMessage($"Headers: {string.Join(", ", headers)}");
            }
            else
            {
                LogMessage("[OK] CSV exists, will append data");
            }

            fileInitialized = true;
            LogMessage("[OK] CSV ready for writing");
        }
        catch (Exception e)
        {
            LogError($"[FAIL] CSV init failed: {e.Message}");
            LogError($"Stack trace: {e.StackTrace}");
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
            LogError("[FAIL] CSV not initialized");
            return;
        }

        if (rowData.Count != headers.Count)
        {
            LogWarning($"[WARN] Column mismatch: {rowData.Count}/{headers.Count}");
        }

        try
        {
            string line = FormatCSVLine(rowData);
            File.AppendAllText(currentFilePath, line + "\n");
            LogMessage($"[OK] Row written ({rowData.Count} fields)");
        }
        catch (Exception e)
        {
            LogError($"[FAIL] Write failed: {e.Message}");
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
    /// Useful for debugging and accessing saved files.
    /// </summary>
    public void OpenSaveDirectory()
    {
        string path = GetSaveDirectory();
        
        #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        System.Diagnostics.Process.Start("explorer.exe", path.Replace("/", "\\"));
        #elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        System.Diagnostics.Process.Start("open", path);
        #elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
        System.Diagnostics.Process.Start("xdg-open", path);
        #else
        LogMessage($"Save directory: {path}");
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
        if (debugDisplay != null)
        {
            debugDisplay.AddLog($"[CSV] {message}", LogType.Log);
        }
        Debug.Log($"[CSVWriter] {message}");
    }

    private void LogWarning(string message)
    {
        if (debugDisplay != null)
        {
            debugDisplay.AddLog($"[CSV] {message}", LogType.Warning);
        }
        Debug.LogWarning($"[CSVWriter] {message}");
    }

    private void LogError(string message)
    {
        if (debugDisplay != null)
        {
            debugDisplay.AddLog($"[CSV] {message}", LogType.Error);
        }
        Debug.LogError($"[CSVWriter] {message}");
    }
}