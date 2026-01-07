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

    [Tooltip("Automatically log trials when they load?")]
    public bool autoLogTrials = true;

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
    private bool hasInitialized = false;

    void Start()
    {
        // Subscribe to trial loaded event for automatic logging
        if (trialController != null && autoLogTrials)
        {
            trialController.OnTrialLoaded.AddListener(OnTrialLoaded);
            Debug.Log("[TrialDataLogger] Subscribed to OnTrialLoaded event");
        }
        else if (trialController == null)
        {
            Debug.LogError("[TrialDataLogger] TrialController reference not set!");
        }
    }

    void OnDestroy()
    {
        if (isLoggingTrial)
        {
            Debug.LogWarning("[TrialDataLogger] Ending trial logging on destroy");
            EndTrialLogging();
        }

        // Unsubscribe from events
        if (trialController != null)
        {
            trialController.OnTrialLoaded.RemoveListener(OnTrialLoaded);
        }
    }

    /// <summary>
    /// Called automatically when a trial is loaded via OnTrialLoaded event.
    /// </summary>
    private void OnTrialLoaded()
    {
        Debug.Log("[TrialDataLogger] OnTrialLoaded event received");

        // Initialize CSV on first trial
        if (!hasInitialized && autoInitialize)
        {
            InitializeCSVFile();
            hasInitialized = true;
        }

        // End previous trial if still logging
        if (isLoggingTrial)
        {
            Debug.LogWarning("[TrialDataLogger] Previous trial still logging - ending it now");
            EndTrialLogging();
        }

        // Start logging the new trial
        if (autoLogTrials)
        {
            StartTrialLogging();
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
            Debug.LogError("[TrialDataLogger] CSVWriter reference not set");
            return;
        }

        if (trialController == null)
        {
            Debug.LogError("[TrialDataLogger] TrialController reference not set");
            return;
        }

        if (!trialController.IsTrialLoaded)
        {
            Debug.LogError("[TrialDataLogger] No trial loaded - cannot initialize CSV");
            return;
        }

        List<string> headers = new List<string>();

        // Add all trial data columns from CSV
        foreach (var key in trialController.CurrentTrialData.Keys)
        {
            headers.Add(key);
        }

        // Add timestamp columns
        if (logTrialStartTime)
            headers.Add("TrialStartTime");

        if (logTrialEndTime)
            headers.Add("TrialEndTime");

        if (logTrialDuration)
            headers.Add("TrialDuration_Seconds");

        headers.AddRange(customColumns);

        csvWriter.InitializeFile(headers, trialController.CurrentParticipantID);

        Debug.Log($"[TrialDataLogger] CSV initialized: {csvWriter.GetCurrentFilePath()}");
        Debug.Log($"[TrialDataLogger] Headers: {string.Join(", ", headers)}");
    }

    /// <summary>
    /// Starts logging a trial. Call this at the beginning of a trial.
    /// </summary>
    public void StartTrialLogging()
    {
        if (!csvWriter.IsInitialized())
        {
            Debug.LogWarning("[TrialDataLogger] CSV not initialized - initializing now");
            InitializeCSVFile();
        }

        if (!csvWriter.IsInitialized())
        {
            Debug.LogError("[TrialDataLogger] Failed to initialize CSV - cannot start logging");
            return;
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

        Debug.Log($"[TrialDataLogger] Started logging - PID={trialController.CurrentParticipantID}, Trial={trialController.CurrentTrialNumber}");
        Debug.Log($"[TrialDataLogger] Data fields captured: {currentRowData.Count}");
    }

    /// <summary>
    /// Ends trial logging and writes the data to CSV.
    /// Call this from a button or when the trial completes.
    /// </summary>
    public void EndTrialLogging()
    {
        if (!isLoggingTrial)
        {
            Debug.LogWarning("[TrialDataLogger] No trial logging in progress");
            return;
        }

        if (!csvWriter.IsInitialized())
        {
            Debug.LogError("[TrialDataLogger] CSV not initialized - cannot write data");
            isLoggingTrial = false;
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

        Debug.Log($"[TrialDataLogger] Ending trial logging - Duration={duration.TotalSeconds:F3}s");
        Debug.Log($"[TrialDataLogger] Writing row with {currentRowData.Count} fields");

        // Write to CSV
        csvWriter.WriteRow(currentRowData);

        isLoggingTrial = false;

        Debug.Log($"[TrialDataLogger] ✓ Trial data written to: {csvWriter.GetCurrentFilePath()}");
    }

    /// <summary>
    /// Adds or updates a custom field value for the current trial.
    /// Must be called between StartTrialLogging() and EndTrialLogging().
    /// </summary>
    public void SetCustomField(string fieldName, string value)
    {
        if (!isLoggingTrial)
        {
            Debug.LogWarning("[TrialDataLogger] No trial logging in progress. Call StartTrialLogging() first.");
            return;
        }

        currentRowData[fieldName] = value;
        Debug.Log($"[TrialDataLogger] Set custom field: {fieldName} = {value}");
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
    /// Gets whether a trial is currently being logged.
    /// </summary>
    public bool IsLogging()
    {
        return isLoggingTrial;
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