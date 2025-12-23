using UnityEngine;
using System.Collections.Generic;
using System;

public class ExperimentManager : MonoBehaviour
{
    public TrialController trialController;
    public TrialDataLogger dataLogger;
    
    void Start()
    {
        // Load first trial
        trialController.LoadTrialFromPlayerPrefs();
        
        // Start logging
        StartTrial();
    }
    
    void StartTrial()
    {
        dataLogger.StartTrialLogging();
        
        // Your trial logic here...
    }
    
    void OnUserResponse(int responseValue, bool correct)
    {
        // Log custom data during trial
        dataLogger.SetCustomField("UserResponse", responseValue);
        dataLogger.SetCustomField("Correct", correct);
        dataLogger.SetCustomField("ReactionTime", Time.time);
        
        // End logging
        dataLogger.EndTrialLogging();
        
        // Move to next trial
        if (trialController.LoadNextTrial())
        {
            StartTrial();
        }
        else
        {
            Debug.Log("All trials completed!");
            dataLogger.OpenSaveDirectory();
        }
    }
}