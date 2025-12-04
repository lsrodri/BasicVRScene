using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InputHandler : MonoBehaviour
{
    [Header("UI Component References")]
    public Slider slider;
    public TMP_InputField textInput;
    
    // This method gets called by your button's OnClick event
    public void OnSubmitButtonClicked()
    {
        // Get all values at once when button is clicked
        float sliderValue = slider.value;
        string textValue = textInput.text;
        
        // Use the values
        Debug.Log($"Slider: {sliderValue}");
        Debug.Log($"Text Input: {textValue}");
        
    }

}
