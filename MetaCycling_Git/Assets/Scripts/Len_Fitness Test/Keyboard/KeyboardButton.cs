using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
//using Meta.XR.ImmersiveDebugger.UserInterface.Generic;
public class KeyboardButton : MonoBehaviour
{
    public Button btn;
    public TMP_Text textLabel;
    [SerializeField] private GameObject[] specialIcons;

    private string _originalKey;
    private FloatingKeyboard _manager;
    private bool _isSpecialKey = false;

    public void BtnSetup(string key, FloatingKeyboard manager)
    {
        _originalKey = key;
        _manager = manager;
        textLabel.text = key;

        _manager.OnShiftChanged += HandleShiftVisuals;
        CheckForSpecialIcon(_originalKey);
    }

    private void CheckForSpecialIcon(string keyName)
    {
        // Deactivate all icons by default
        foreach (GameObject icon in specialIcons) icon.SetActive(false);
        textLabel.gameObject.SetActive(true);
        textLabel.text = keyName;
        _isSpecialKey = true;

        switch (keyName)
        {
            case "backspace":
                ToggleIcon(0);
                break;
            case "enter":
                ToggleIcon(1);
                break;
            case "shift":
                // Default to Shift Off icon
                ToggleIcon(2);
                break;
            default:
                _isSpecialKey = false; // It's a standard character
                break;
        }
    }

    private void ToggleIcon(int index)
    {
        if (index >= 0 && index < specialIcons.Length && specialIcons[index] != null)
        {
            // Turn off other special icons first
            foreach (GameObject icon in specialIcons) icon.SetActive(false);

            specialIcons[index].SetActive(true);
            textLabel.gameObject.SetActive(false);
        }
    }

    private void HandleShiftVisuals(bool shiftActive)
    {
        if (char.IsLetter(_originalKey[0]))
        {
            textLabel.text = shiftActive ? _originalKey.ToUpper() : _originalKey.ToLower();
        }
        else if (_originalKey == "shift")
        {
            ToggleIcon(shiftActive ? 3 : 2);
        }
    }

    private void OnDestroy()
    {
        // Clean up subscription to prevent memory leaks/errors
        if (_manager != null)
        {
            _manager.OnShiftChanged -= HandleShiftVisuals;
        }
    }
}
