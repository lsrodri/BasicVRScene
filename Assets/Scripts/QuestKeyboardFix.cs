using System.Collections;
using TMPro;
using UnityEngine;

public class QuestKeyboardFix : MonoBehaviour
{
    private TMP_InputField inputField;

    void Awake()
    {
        inputField = GetComponent<TMP_InputField>();
        inputField.onSelect.AddListener(OnInputFieldSelected);
    }

    void OnInputFieldSelected(string text)
    {
        StartCoroutine(ForceKeyboardOpen());
    }

    IEnumerator ForceKeyboardOpen()
    {
        // Wait for initial attempt
        yield return new WaitForSeconds(0.5f);

        // If keyboard didn't appear, force it
        if (!TouchScreenKeyboard.visible)
        {
            inputField.ActivateInputField();
            TouchScreenKeyboard.Open(inputField.text, TouchScreenKeyboardType.Default);
        }
    }
}
