using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages trial data by loading CSV and matching ParticipantID and TrialNumber.
/// Provides public access to current trial data for other systems.
/// </summary>
public class TrialController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the CSVReader component")]
    public CSVReader csvReader;

    [Header("Configuration")]
    [Tooltip("Name of the ParticipantID column in CSV")]
    public string participantIDColumn = "ParticipantID";

    [Tooltip("Name of the TrialNumber column in CSV")]
    public string trialNumberColumn = "TrialNumber";

    [Tooltip("Load trial data on Start?")]
    public bool loadOnStart = true;

    [Header("Debug Override")]
    [Tooltip("Override ParticipantID (set to -1 to use PlayerPrefs)")]
    public int overrideParticipantID = -1;

    [Tooltip("Override TrialNumber (set to -1 to use PlayerPrefs)")]
    public int overrideTrialNumber = -1;

    [Header("Stimulus Management")]
    [Tooltip("Parent GameObject containing all stimulus objects")]
    public GameObject stimulusContainer;

    [Tooltip("Name of the StimulusID column in CSV")]
    public string stimulusIDColumn = "StimulusID";

    // Event triggered when trial data is loaded
    [System.Serializable]
    public class TrialLoadedEvent : UnityEvent { }

    [Header("Events")]
    public TrialLoadedEvent OnTrialLoaded = new TrialLoadedEvent();

    // Public properties for accessing current trial data
    public bool IsTrialLoaded { get; private set; } = false;
    public int CurrentParticipantID { get; private set; } = -1;
    public int CurrentTrialNumber { get; private set; } = -1;
    public Dictionary<string, string> CurrentTrialData { get; private set; } = new Dictionary<string, string>();

    // Track currently active stimulus
    private GameObject currentActiveStimulus;

    // Trial progression tracking
    private List<int> availableTrialNumbers = new List<int>();
    private int currentTrialIndex = -1;

    void Start()
    {
        Debug.Log("[TrialController] Start() called");

        // Check for debug overrides
        if (overrideParticipantID != -1)
        {
            Debug.LogWarning($"[TrialController] DEBUG OVERRIDE: ParticipantID = {overrideParticipantID}");
            PlayerPrefs.SetInt("ParticipantID", overrideParticipantID);
        }
        else if (!PlayerPrefs.HasKey("ParticipantID"))
        {
            Debug.LogWarning("[TrialController] Setting TEST ParticipantID = 1");
            PlayerPrefs.SetInt("ParticipantID", 1);
        }

        if (overrideTrialNumber != -1)
        {
            Debug.LogWarning($"[TrialController] DEBUG OVERRIDE: TrialNumber = {overrideTrialNumber}");
            PlayerPrefs.SetInt("TrialNumber", overrideTrialNumber);
        }
        else if (!PlayerPrefs.HasKey("TrialNumber"))
        {
            Debug.LogWarning("[TrialController] Setting TEST TrialNumber = 1");
            PlayerPrefs.SetInt("TrialNumber", 1);
        }

        if (loadOnStart)
        {
            StartCoroutine(WaitForCSVAndLoadTrial());
        }
        else
        {
            Debug.Log("[TrialController] loadOnStart is FALSE - trial will not auto-load");
        }
    }

    private System.Collections.IEnumerator WaitForCSVAndLoadTrial()
    {
        Debug.Log("[TrialController] Waiting for CSV to load...");

        // Debug CSVReader reference
        if (csvReader == null)
        {
            Debug.LogError("[TrialController] CSVReader reference is NULL! Check Inspector.");
            yield break;
        }

        Debug.Log($"[TrialController] CSVReader found: {csvReader.name}");
        Debug.Log($"[TrialController] CSVReader.csvFileName = '{csvReader.csvFileName}'");
        Debug.Log($"[TrialController] CSVReader.IsLoaded = {csvReader.IsLoaded}");

        int waitFrames = 0;
        // Wait until CSV is loaded
        while (csvReader == null || !csvReader.IsLoaded)
        {
            waitFrames++;
            if (waitFrames % 60 == 0) // Log every 60 frames (~1 second)
            {
                Debug.Log($"[TrialController] Still waiting for CSV... ({waitFrames} frames)");
            }

            if (waitFrames > 300) // Timeout after ~5 seconds
            {
                Debug.LogError("[TrialController] TIMEOUT: CSV never loaded. Check CSVReader component.");
                yield break;
            }

            yield return null;
        }

        Debug.Log($"[TrialController] CSV loaded after {waitFrames} frames!");
        Debug.Log($"[TrialController] CSV has {csvReader.Rows.Count} rows");
        Debug.Log($"[TrialController] CSV headers: {string.Join(", ", csvReader.Headers)}");

        // Now load the trial
        LoadTrialFromPlayerPrefs();
    }

    /// <summary>
    /// Loads trial data using ParticipantID and TrialNumber from PlayerPrefs.
    /// </summary>
    public void LoadTrialFromPlayerPrefs()
    {
        Debug.Log("[TrialController] LoadTrialFromPlayerPrefs() called");

        if (!PlayerPrefs.HasKey("ParticipantID") || !PlayerPrefs.HasKey("TrialNumber"))
        {
            Debug.LogWarning("[TrialController] ParticipantID or TrialNumber not found in PlayerPrefs");
            Debug.LogWarning($"[TrialController] Has ParticipantID: {PlayerPrefs.HasKey("ParticipantID")}");
            Debug.LogWarning($"[TrialController] Has TrialNumber: {PlayerPrefs.HasKey("TrialNumber")}");
            IsTrialLoaded = false;
            return;
        }

        int participantID = PlayerPrefs.GetInt("ParticipantID");
        int trialNumber = PlayerPrefs.GetInt("TrialNumber");

        Debug.Log($"[TrialController] Loading: ParticipantID={participantID}, TrialNumber={trialNumber}");

        LoadTrial(participantID, trialNumber);
    }

    /// <summary>
    /// Loads trial data for specific ParticipantID and TrialNumber.
    /// </summary>
    public void LoadTrial(int participantID, int trialNumber)
    {
        if (csvReader == null)
        {
            Debug.LogError("CSVReader reference not set in TrialController");
            IsTrialLoaded = false;
            return;
        }

        if (!csvReader.IsLoaded)
        {
            Debug.LogError("CSV data not loaded. Make sure CSVReader has loaded the data first.");
            IsTrialLoaded = false;
            return;
        }

        // Update available trials for this participant
        UpdateAvailableTrials(participantID);

        // Find matching row
        int matchingRowIndex = FindMatchingRow(participantID, trialNumber);

        if (matchingRowIndex == -1)
        {
            Debug.LogWarning($"No matching trial found for ParticipantID: {participantID}, TrialNumber: {trialNumber}");
            IsTrialLoaded = false;
            return;
        }

        // Update current trial index in the available trials list
        currentTrialIndex = availableTrialNumbers.IndexOf(trialNumber);

        // Load the trial data
        LoadTrialDataFromRow(matchingRowIndex, participantID, trialNumber);
    }

    /// <summary>
    /// Updates the list of available trial numbers for the given participant.
    /// </summary>
    private void UpdateAvailableTrials(int participantID)
    {
        availableTrialNumbers.Clear();

        if (!csvReader.hasHeader)
        {
            Debug.LogError("CSV must have headers to use TrialController");
            return;
        }

        int pidColumnIndex = csvReader.Headers.IndexOf(participantIDColumn);
        int trialColumnIndex = csvReader.Headers.IndexOf(trialNumberColumn);

        if (pidColumnIndex == -1 || trialColumnIndex == -1)
            return;

        // Find all trial numbers for this participant
        for (int i = 0; i < csvReader.Rows.Count; i++)
        {
            List<string> row = csvReader.Rows[i];

            if (row.Count <= pidColumnIndex || row.Count <= trialColumnIndex)
                continue;

            if (int.TryParse(row[pidColumnIndex], out int rowPID) &&
                int.TryParse(row[trialColumnIndex], out int rowTrial))
            {
                if (rowPID == participantID)
                {
                    availableTrialNumbers.Add(rowTrial);
                }
            }
        }

        // Sort trial numbers for sequential progression
        availableTrialNumbers.Sort();

        Debug.Log($"Found {availableTrialNumbers.Count} trials for ParticipantID {participantID}");
    }

    /// <summary>
    /// Loads the next trial for the current participant.
    /// Returns true if successful, false if no more trials available.
    /// </summary>
    public bool LoadNextTrial()
    {
        if (!IsTrialLoaded)
        {
            Debug.LogWarning("No current trial loaded. Use LoadTrial() first.");
            return false;
        }

        if (!HasNextTrial())
        {
            Debug.LogWarning($"No more trials available for ParticipantID: {CurrentParticipantID}");
            return false;
        }

        int nextTrialNumber = availableTrialNumbers[currentTrialIndex + 1];
        LoadTrial(CurrentParticipantID, nextTrialNumber);

        // Update PlayerPrefs to persist the new trial number
        PlayerPrefs.SetInt("TrialNumber", nextTrialNumber);
        PlayerPrefs.Save();

        return true;
    }

    /// <summary>
    /// Wrapper method for UI buttons to load the next trial.
    /// Call this from Button OnClick events.
    /// </summary>
    public void LoadNextTrialButton()
    {
        // CRITICAL: End logging for current trial BEFORE loading next
        TrialDataLogger logger = FindObjectOfType<TrialDataLogger>();
        if (logger != null && logger.IsLogging())
        {
            Debug.Log("[TrialController] Ending trial logging before loading next trial");
            logger.EndTrialLogging();
        }

        bool success = LoadNextTrial();

        if (!success && IsLastTrial())
        {
            Debug.Log("All trials completed for this participant!");

            // CRITICAL: Save the LAST trial
            if (logger != null && logger.IsLogging())
            {
                Debug.Log("[TrialController] Saving final trial");
                logger.EndTrialLogging();
            }

            // Update debug display if available
            TrialDebugDisplay debugDisplay = FindObjectOfType<TrialDebugDisplay>();
            if (debugDisplay != null)
            {
                debugDisplay.ShowCompletionMessage();
            }
        }
    }

    /// <summary>
    /// Checks if there is a next trial available for the current participant.
    /// </summary>
    public bool HasNextTrial()
    {
        if (!IsTrialLoaded)
            return false;

        return currentTrialIndex >= 0 && currentTrialIndex < availableTrialNumbers.Count - 1;
    }

    /// <summary>
    /// Gets the total number of trials for the current participant.
    /// </summary>
    public int GetTotalTrialsForCurrentParticipant()
    {
        return availableTrialNumbers.Count;
    }

    /// <summary>
    /// Gets the current trial index (1-based) out of total trials.
    /// Returns "3 of 10" format string.
    /// </summary>
    public string GetTrialProgress()
    {
        if (!IsTrialLoaded)
            return "No trial loaded";

        int currentPosition = currentTrialIndex + 1;
        int total = availableTrialNumbers.Count;
        return $"{currentPosition} of {total}";
    }

    /// <summary>
    /// Checks if this is the last trial for the current participant.
    /// </summary>
    public bool IsLastTrial()
    {
        if (!IsTrialLoaded)
            return false;

        return currentTrialIndex == availableTrialNumbers.Count - 1;
    }

    /// <summary>
    /// Gets all trial numbers available for the current participant.
    /// </summary>
    public List<int> GetAvailableTrialNumbers()
    {
        return new List<int>(availableTrialNumbers);
    }

    /// <summary>
    /// Finds the row index matching the ParticipantID and TrialNumber.
    /// </summary>
    private int FindMatchingRow(int participantID, int trialNumber)
    {
        if (!csvReader.hasHeader)
        {
            Debug.LogError("CSV must have headers to use TrialController");
            return -1;
        }

        // Get column indices
        int pidColumnIndex = csvReader.Headers.IndexOf(participantIDColumn);
        int trialColumnIndex = csvReader.Headers.IndexOf(trialNumberColumn);

        if (pidColumnIndex == -1)
        {
            Debug.LogError($"Column '{participantIDColumn}' not found in CSV headers");
            return -1;
        }

        if (trialColumnIndex == -1)
        {
            Debug.LogError($"Column '{trialNumberColumn}' not found in CSV headers");
            return -1;
        }

        // Search for matching row
        for (int i = 0; i < csvReader.Rows.Count; i++)
        {
            List<string> row = csvReader.Rows[i];

            if (row.Count <= pidColumnIndex || row.Count <= trialColumnIndex)
                continue;

            // Try to parse both values
            if (int.TryParse(row[pidColumnIndex], out int rowPID) &&
                int.TryParse(row[trialColumnIndex], out int rowTrial))
            {
                if (rowPID == participantID && rowTrial == trialNumber)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    /// <summary>
    /// Loads all data from the matching row into CurrentTrialData dictionary.
    /// </summary>
    private void LoadTrialDataFromRow(int rowIndex, int participantID, int trialNumber)
    {
        CurrentTrialData.Clear();
        List<string> row = csvReader.Rows[rowIndex];

        // Store all column data
        for (int i = 0; i < csvReader.Headers.Count && i < row.Count; i++)
        {
            string header = csvReader.Headers[i];
            string value = row[i];
            CurrentTrialData[header] = value;
        }

        CurrentParticipantID = participantID;
        CurrentTrialNumber = trialNumber;
        IsTrialLoaded = true;

        Debug.Log($"Trial loaded successfully: PID={participantID}, Trial={trialNumber} ({CurrentTrialData.Count} fields) - {GetTrialProgress()}");

        // Trigger the event to notify listeners
        OnTrialLoaded.Invoke();

        // Automatically activate the stimulus for this trial
        ActivateCurrentStimulus();
    }

    /// <summary>
    /// Gets a specific field value from the current trial data.
    /// </summary>
    public string GetFieldValue(string fieldName)
    {
        if (!IsTrialLoaded)
        {
            Debug.LogWarning("No trial data loaded");
            return null;
        }

        if (CurrentTrialData.TryGetValue(fieldName, out string value))
        {
            return value;
        }

        Debug.LogWarning($"Field '{fieldName}' not found in current trial data");
        return null;
    }

    /// <summary>
    /// Gets a specific field value as an integer.
    /// </summary>
    public bool TryGetFieldValueAsInt(string fieldName, out int result)
    {
        string value = GetFieldValue(fieldName);
        if (value != null && int.TryParse(value, out result))
        {
            return true;
        }
        result = 0;
        return false;
    }

    /// <summary>
    /// Gets a specific field value as a float.
    /// </summary>
    public bool TryGetFieldValueAsFloat(string fieldName, out float result)
    {
        string value = GetFieldValue(fieldName);
        if (value != null && float.TryParse(value, out result))
        {
            return true;
        }
        result = 0f;
        return false;
    }

    /// <summary>
    /// Activates the stimulus GameObject matching the current trial's StimulusID.
    /// Deactivates the previously active stimulus.
    /// </summary>
    public void ActivateCurrentStimulus()
    {
        if (!IsTrialLoaded)
        {
            Debug.LogWarning("[TrialController] No trial loaded - cannot activate stimulus");
            return;
        }

        // Get StimulusID from current trial data
        string stimulusID = GetFieldValue(stimulusIDColumn);

        if (string.IsNullOrEmpty(stimulusID))
        {
            Debug.LogWarning($"[TrialController] StimulusID not found in trial data");
            return;
        }

        // ENHANCED DEBUG: Show exact value and length
        Debug.Log($"[TrialController] Looking for StimulusID: '{stimulusID}' (Length: {stimulusID.Length})");
        Debug.Log($"[TrialController] StimulusID bytes: {System.BitConverter.ToString(System.Text.Encoding.UTF8.GetBytes(stimulusID))}");

        ActivateStimulus(stimulusID);
    }

    /// <summary>
    /// Activates a specific stimulus by name/ID.
    /// </summary>
    public void ActivateStimulus(string stimulusID)
    {
        // Deactivate previous stimulus
        if (currentActiveStimulus != null)
        {
            currentActiveStimulus.SetActive(false);
            Debug.Log($"[TrialController] Deactivated previous stimulus: {currentActiveStimulus.name}");
        }

        // Find stimulus by name
        GameObject stimulus = FindStimulusGameObject(stimulusID);

        if (stimulus == null)
        {
            Debug.LogError($"[TrialController] Stimulus GameObject '{stimulusID}' not found!");
            return;
        }

        // Activate new stimulus
        stimulus.SetActive(true);
        currentActiveStimulus = stimulus;

        Debug.Log($"[TrialController] Activated stimulus: {stimulusID}");
    }

    /// <summary>
    /// Finds a stimulus GameObject by its name/ID.
    /// Searches in stimulusContainer if assigned, otherwise searches entire scene.
    /// </summary>
    private GameObject FindStimulusGameObject(string stimulusID)
    {
        // Option 1: Search within a specific container (more efficient)
        if (stimulusContainer != null)
        {
            Debug.Log($"[TrialController] Searching for '{stimulusID}' in container '{stimulusContainer.name}'");
            Debug.Log($"[TrialController] Container has {stimulusContainer.transform.childCount} children");

            // List all children for debugging
            foreach (Transform child in stimulusContainer.transform)
            {
                Debug.Log($"[TrialController] Found child: '{child.name}' (Length: {child.name.Length})");

                // Try exact match
                if (child.name == stimulusID)
                {
                    Debug.Log($"[TrialController] EXACT MATCH found: {child.name}");
                    return child.gameObject;
                }

                // Try trimmed match
                if (child.name.Trim() == stimulusID.Trim())
                {
                    Debug.LogWarning($"[TrialController] TRIMMED MATCH found: '{child.name}' vs '{stimulusID}'");
                    return child.gameObject;
                }
            }

            Debug.LogError($"[TrialController] Stimulus '{stimulusID}' not found in container '{stimulusContainer.name}'");
            return null;
        }

        // Option 2: Search entire scene (less efficient but more flexible)
        GameObject stimulus = GameObject.Find(stimulusID);

        if (stimulus == null)
        {
            Debug.LogWarning($"[TrialController] Stimulus '{stimulusID}' not found in scene");
        }

        return stimulus;
    }

    /// <summary>
    /// Deactivates all stimulus GameObjects in the container.
    /// Useful for initialization or reset.
    /// </summary>
    public void DeactivateAllStimuli()
    {
        if (stimulusContainer == null)
        {
            Debug.LogWarning("[TrialController] No stimulus container assigned");
            return;
        }

        foreach (Transform child in stimulusContainer.transform)
        {
            child.gameObject.SetActive(false);
        }

        currentActiveStimulus = null;
        Debug.Log($"[TrialController] Deactivated all stimuli in {stimulusContainer.name}");
    }

    /// <summary>
    /// Gets the currently active stimulus GameObject.
    /// </summary>
    public GameObject GetCurrentStimulus()
    {
        return currentActiveStimulus;
    }
}