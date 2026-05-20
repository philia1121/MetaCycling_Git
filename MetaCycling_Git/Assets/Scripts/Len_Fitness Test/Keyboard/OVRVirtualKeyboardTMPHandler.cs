using UnityEngine;
using TMPro;
using System;

public class OVRVirtualKeyboardTMPHandler : OVRVirtualKeyboard.AbstractTextHandler
{
    [SerializeField] private TMP_InputField inputField;

    // The keyboard uses this to display the current text
    public override string Text => inputField.text;

    // Required: This tells the keyboard when the text has changed externally
    public override Action<string> OnTextChanged { get; set; }

    // Required: Return true if you want the "Enter" key to trigger Submit()
    public override bool SubmitOnEnter => true;

    // Required: Tells the keyboard if the input field is currently active
    public override bool IsFocused => inputField.isFocused;

    public override void AppendText(string text)
    {
        inputField.text += text;
        // Move the cursor to the end after typing
        inputField.caretPosition = inputField.text.Length;
        OnTextChanged?.Invoke(inputField.text);
    }

    public override void ApplyBackspace()
    {
        if (inputField.text.Length > 0)
        {
            inputField.text = inputField.text.Substring(0, inputField.text.Length - 1);
            inputField.caretPosition = inputField.text.Length;
            OnTextChanged?.Invoke(inputField.text);
        }
    }

    public override void MoveTextEnd()
    {
        inputField.caretPosition = inputField.text.Length;
    }

    public override void Submit()
    {
        // Triggers your "Start Record" logic if it's tied to onEndEdit
        inputField.onEndEdit.Invoke(inputField.text);

        // Deactivate focus so the keyboard knows we are done
        inputField.DeactivateInputField();
    }

    // Optional: Sync text if the user types via a physical keyboard too
    private void OnEnable()
    {
        inputField.onValueChanged.AddListener(HandleExternalValueChanged);
    }

    private void OnDisable()
    {
        inputField.onValueChanged.RemoveListener(HandleExternalValueChanged);
    }

    private void HandleExternalValueChanged(string arg0)
    {
        OnTextChanged?.Invoke(arg0);
    }
}