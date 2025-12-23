using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Logs trial data from TrialController to CSV files.
/// Automatically captures trial data and performance metrics.
/// </summary>
public class TrialDataLogger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the TrialController")]
    public TrialController trialController;
    
    [Tooltip("Reference to the CSVWriter")]
    public CSVWriter csvWriter;

    [Header("Logging Configuration")]
    [Tooltip("Automatically initialize CSV file when first trial loads?")]
    public bool autoInitialize = true;
    
    [Tooltip("Additional custom columns to include in the CSV")]
    public List<string> customColumns = new List<string>();

    [Header("Automatic Timestamps")]
    [Tooltip("Log trial start time?")]
    public bool logTrialStartTime = true;
    
    [Tooltip("Log trial end time?")]
    public bool logTrialEndTime = true;
    
    [Tooltip("Log trial duration?")]
    public bool logTrialDuration = true;

    // Trial timing
    private DateTime trialStartTime;
    private Dictionary<string, string> currentRowData = new Dictionary<string, string>();
    private bool isLoggingTrial = false;

    void Start()
    {
        if (autoInitialize && trialController != null && trialController.IsTrialLoaded)
        {
            InitializeCSVFile();
        }
    }

    /// <summary>
    /// Initializes the CSV file with appropriate headers.
    /// Call this before logging any data.
    /// </summary>
    public void InitializeCSVFile()
    {
        if (csvWriter == null)
        {
            Debug.LogError("CSVWriter reference not set");
            return;
        }

        if (trialController == null)
        {
            Debug.LogError("TrialController reference not set");
            return;
        }

        // Build header list
        List<string> headers = new List<string>();

        // Add all trial data columns from CSV
        if (trialController.IsTrialLoaded)
        {
            foreach (var key in trialController.CurrentTrialData.Keys)
            {
                headers.Add(key);
            }
        }

        // Add timestamp columns
        if (logTrialStartTime)
            headers.Add("TrialStartTime");
        
        if (logTrialEndTime)
            headers.Add("TrialEndTime");
        
        if (logTrialDuration)
            headers.Add("TrialDuration_Seconds");

        // Add custom columns
        headers.AddRange(customColumns);

        // Initialize CSV file
        csvWriter.InitializeFile(headers, trialController.CurrentParticipantID);
        
        Debug.Log($"CSV file initialized at: {csvWriter.GetCurrentFilePath()}");
    }

    /// <summary>
    /// Starts logging a trial. Call this at the beginning of a trial.
    /// </summary>
    public void StartTrialLogging()
    {
        if (!csvWriter.IsInitialized())
        {
            InitializeCSVFile();
        }

        currentRowData.Clear();
        trialStartTime = DateTime.Now;
        isLoggingTrial = true;

        // Copy all current trial data
        if (trialController.IsTrialLoaded)
        {
            foreach (var kvp in trialController.CurrentTrialData)
            {
                currentRowData[kvp.Key] = kvp.Value;
            }
        }

        // Add start time
        if (logTrialStartTime)
        {
            currentRowData["TrialStartTime"] = trialStartTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        Debug.Log($"Started logging trial: PID={trialController.CurrentParticipantID}, Trial={trialController.CurrentTrialNumber}");
    }

    /// <summary>
    /// Ends trial logging and writes the data to CSV.
    /// </summary>
    public void EndTrialLogging()
    {
        if (!isLoggingTrial)
        {
            Debug.LogWarning("No trial logging in progress");
            return;
        }

        DateTime endTime = DateTime.Now;
        TimeSpan duration = endTime - trialStartTime;

        // Add end time and duration
        if (logTrialEndTime)
        {
            currentRowData["TrialEndTime"] = endTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        if (logTrialDuration)
        {
            currentRowData["TrialDuration_Seconds"] = duration.TotalSeconds.ToString("F3");
        }

        // Write to CSV
        csvWriter.WriteRow(currentRowData);
        
        isLoggingTrial = false;
        
        Debug.Log($"Trial data logged: Duration={duration.TotalSeconds:F3}s");
    }

    /// <summary>
    /// Adds or updates a custom field value for the current trial.
    /// Must be called between StartTrialLogging() and EndTrialLogging().
    /// </summary>
    public void SetCustomField(string fieldName, string value)
    {
        if (!isLoggingTrial)
        {
            Debug.LogWarning("No trial logging in progress. Call StartTrialLogging() first.");
            return;
        }

        currentRowData[fieldName] = value;
    }

    /// <summary>
    /// Sets multiple custom fields at once.
    /// </summary>
    public void SetCustomFields(Dictionary<string, string> fields)
    {
        foreach (var kvp in fields)
        {
            SetCustomField(kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    /// Convenience method: Sets a custom field with an integer value.
    /// </summary>
    public void SetCustomField(string fieldName, int value)
    {
        SetCustomField(fieldName, value.ToString());
    }

    /// <summary>
    /// Convenience method: Sets a custom field with a float value.
    /// </summary>
    public void SetCustomField(string fieldName, float value)
    {
        SetCustomField(fieldName, value.ToString("F3"));
    }

    /// <summary>
    /// Convenience method: Sets a custom field with a bool value.
    /// </summary>
    public void SetCustomField(string fieldName, bool value)
    {
        SetCustomField(fieldName, value.ToString());
    }

    /// <summary>
    /// Opens the directory where CSV files are saved.
    /// </summary>
    public void OpenSaveDirectory()
    {
        if (csvWriter != null)
        {
            csvWriter.OpenSaveDirectory();
        }
    }
}