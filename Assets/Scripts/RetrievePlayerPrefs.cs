using UnityEngine;
using TMPro;

public class RwtrievePlayerPrefs : MonoBehaviour
{
    public TMP_Text textDisplay;
    public string playerPrefKeyOne = "ParticipantID";
    public string playerPrefKeyTwo = "TrialNumber";

    void Start()
    {
        // Load and display the PlayerPrefs value
        string participantIDText = GetPlayerPrefValue(playerPrefKeyOne);
        string trialNumberText = GetPlayerPrefValue(playerPrefKeyTwo);

        textDisplay.text = $"PID: '{participantIDText}'. Trial: {trialNumberText}";
    }

    private string GetPlayerPrefValue(string key)
    {
        if (PlayerPrefs.HasKey(key))
        {
            int value = PlayerPrefs.GetInt(key);
            Debug.Log($"Retrieved {key}: {value}");
            return value.ToString();
        }
        else
        {
            Debug.LogWarning($"PlayerPrefs key '{key}' not found. Returning default value.");
            return "N/A";
        }
    }
}
