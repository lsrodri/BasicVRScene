using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Reads CSV data from StreamingAssets folder and makes it publicly accessible.
/// Works across Editor, Standalone builds, and Android builds.
/// </summary>
public class CSVReader : MonoBehaviour
{
    [Header("CSV Configuration")]
    [Tooltip("Name of the CSV file in StreamingAssets folder (including .csv extension)")]
    public string csvFileName = "data.csv";
    
    [Tooltip("Does the CSV file have a header row?")]
    public bool hasHeader = true;
    
    [Tooltip("Load CSV automatically on Start?")]
    public bool loadOnStart = true;

    [Header("Debug Display")]
    [Tooltip("Reference to TrialDebugDisplay for showing logs in VR")]
    public TrialDebugDisplay debugDisplay;

    // Public properties for accessing CSV data
    public List<string> Headers { get; private set; } = new List<string>();
    public List<List<string>> Rows { get; private set; } = new List<List<string>>();
    public bool IsLoaded { get; private set; } = false;

    void Start()
    {
        if (loadOnStart)
        {
            LoadCSV();
        }
    }

    /// <summary>
    /// Loads the CSV file from StreamingAssets folder.
    /// Call this manually if loadOnStart is false.
    /// </summary>
    public void LoadCSV()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, csvFileName);
        
        LogMessage($"=== CSV LOADING ===");
        LogMessage($"Platform: {Application.platform}");
        LogMessage($"StreamingAssets: {Application.streamingAssetsPath}");
        LogMessage($"File: {csvFileName}");
        LogMessage($"Full path: {filePath}");
        
        #if UNITY_ANDROID && !UNITY_EDITOR
        // Android requires UnityWebRequest to read from StreamingAssets
        LogMessage("Using Android loading (UnityWebRequest)");
        StartCoroutine(LoadCSVFromAndroid(filePath));
        #else
        // Editor and other platforms can use direct file access
        LogMessage("Using file system loading");
        LoadCSVFromFile(filePath);
        #endif
    }

    /// <summary>
    /// Loads CSV from file path (Editor and most platforms).
    /// </summary>
    private void LoadCSVFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            LogError($"[FAIL] CSV not found: {filePath}");
            IsLoaded = false;
            return;
        }

        LogMessage($"[OK] File exists");

        try
        {
            string csvContent = File.ReadAllText(filePath);
            LogMessage($"[OK] Read {csvContent.Length} bytes");
            ParseCSV(csvContent);
            LogMessage($"[OK] CSV loaded: {Rows.Count} rows");
        }
        catch (System.Exception e)
        {
            LogError($"[FAIL] Read error: {e.Message}");
            IsLoaded = false;
        }
    }

    /// <summary>
    /// Loads CSV from StreamingAssets on Android using UnityWebRequest.
    /// </summary>
    private System.Collections.IEnumerator LoadCSVFromAndroid(string filePath)
    {
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(filePath))
        {
            LogMessage("Sending web request...");
            yield return www.SendWebRequest();

            if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                LogError($"[FAIL] Request failed: {www.error}");
                LogError($"Response code: {www.responseCode}");
                LogError($"URI: {www.uri}");
                IsLoaded = false;
            }
            else
            {
                LogMessage($"[OK] Downloaded {www.downloadHandler.data.Length} bytes");
                ParseCSV(www.downloadHandler.text);
                LogMessage($"[OK] CSV loaded: {Rows.Count} rows");
            }
        }
    }

    /// <summary>
    /// Parses CSV content into Headers and Rows.
    /// Handles quoted fields and commas within quotes.
    /// </summary>
    private void ParseCSV(string csvContent)
    {
        Headers.Clear();
        Rows.Clear();

        if (string.IsNullOrEmpty(csvContent))
        {
            LogWarning("CSV content is empty");
            IsLoaded = false;
            return;
        }

        // Split into lines, handling different line endings
        string[] lines = csvContent.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
        
        LogMessage($"Found {lines.Length} lines");
        
        int startIndex = 0;

        // Parse header if present
        if (hasHeader && lines.Length > 0)
        {
            Headers = ParseCSVLine(lines[0]);
            LogMessage($"Headers: {string.Join(", ", Headers)}");
            startIndex = 1;
        }

        // Parse data rows
        for (int i = startIndex; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            List<string> row = ParseCSVLine(lines[i]);
            Rows.Add(row);
        }

        IsLoaded = true;
        LogMessage($"[OK] Parsed {Rows.Count} data rows");
    }

    /// <summary>
    /// Parses a single CSV line, handling quoted fields and commas within quotes.
    /// </summary>
    private List<string> ParseCSVLine(string line)
    {
        List<string> fields = new List<string>();
        bool inQuotes = false;
        string currentField = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                // Handle escaped quotes (two consecutive quotes)
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    currentField += '"';
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(currentField.Trim());
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }

        // Add the last field
        fields.Add(currentField.Trim());

        return fields;
    }

    /// <summary>
    /// Gets a specific cell value by row and column index.
    /// </summary>
    public string GetCell(int rowIndex, int columnIndex)
    {
        if (rowIndex < 0 || rowIndex >= Rows.Count)
        {
            LogWarning($"Row index {rowIndex} out of range");
            return null;
        }

        if (columnIndex < 0 || columnIndex >= Rows[rowIndex].Count)
        {
            LogWarning($"Column index {columnIndex} out of range");
            return null;
        }

        return Rows[rowIndex][columnIndex];
    }

    /// <summary>
    /// Gets a column by header name (only works if hasHeader is true).
    /// </summary>
    public List<string> GetColumn(string headerName)
    {
        if (!hasHeader)
        {
            LogWarning("Cannot get column by name when hasHeader is false");
            return null;
        }

        int columnIndex = Headers.IndexOf(headerName);
        if (columnIndex == -1)
        {
            LogWarning($"Header '{headerName}' not found");
            return null;
        }

        return Rows.Select(row => row.Count > columnIndex ? row[columnIndex] : "").ToList();
    }

    /// <summary>
    /// Gets a specific row by index.
    /// </summary>
    public List<string> GetRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= Rows.Count)
        {
            LogWarning($"Row index {rowIndex} out of range");
            return null;
        }

        return Rows[rowIndex];
    }

    private void LogMessage(string message)
    {
        if (debugDisplay != null)
        {
            debugDisplay.AddLog($"[CSVRead] {message}", LogType.Log);
        }
        Debug.Log($"[CSVReader] {message}");
    }

    private void LogWarning(string message)
    {
        if (debugDisplay != null)
        {
            debugDisplay.AddLog($"[CSVRead] {message}", LogType.Warning);
        }
        Debug.LogWarning($"[CSVReader] {message}");
    }

    private void LogError(string message)
    {
        if (debugDisplay != null)
        {
            debugDisplay.AddLog($"[CSVRead] {message}", LogType.Error);
        }
        Debug.LogError($"[CSVReader] {message}");
    }
}