using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TrialParameterSetter : MonoBehaviour
{
    [Header("UI Component References")]
    public TMP_InputField ParticipantIDText;
    public TMP_InputField TrialNumberText;
    public TMP_Text WarningText;
    public string ExperimentSceneName;

    void Start()
    {
        // Pre-populate with current PlayerPrefs values if they exist
        if (PlayerPrefs.HasKey("ParticipantID"))
        {
            ParticipantIDText.text = PlayerPrefs.GetInt("ParticipantID").ToString();
        }
        
        // Always default trial number to 1 unless explicitly set
        TrialNumberText.text = "1";
    }

    // This method gets called by your button's OnClick event
    public void OnSubmitButtonClicked()
    {
        // Clear previous warnings
        WarningText.text = "";

        bool isValid = true;

        // Get all values at once when button is clicked
        if (int.TryParse(ParticipantIDText.text, out int participantID))
        {
            Debug.Log($"Participant ID: {participantID}");
        }
        else
        {
            WarningText.text += $"Invalid Participant: '{ParticipantIDText.text}'. ";
            isValid = false;
        }

        if (int.TryParse(TrialNumberText.text, out int trialNumber))
        {
            Debug.Log($"Trial Number: {trialNumber}");
        }
        else
        {
            WarningText.text += $"Invalid Trial: '{TrialNumberText.text}'. ";
            isValid = false;
        }

        if (isValid)
        {
            WarningText.text = "Set";

            // IMPORTANT: Force save the trial number (overwrites any old value)
            PlayerPrefs.SetInt("ParticipantID", participantID);
            PlayerPrefs.SetInt("TrialNumber", trialNumber);
            PlayerPrefs.Save(); // Explicitly save to ensure it's written
            
            Debug.Log($"[TrialParameterSetter] Saved: PID={participantID}, Trial={trialNumber}");

            // Check if ExperimentSceneName is set
            if (string.IsNullOrEmpty(ExperimentSceneName))
            {
                WarningText.text += " Experiment scene name is not set.";
                return;
            }
            else
            {
                // Load the experiment scene
                UnityEngine.SceneManagement.SceneManager.LoadScene(ExperimentSceneName);
            }
        }
    }
}
