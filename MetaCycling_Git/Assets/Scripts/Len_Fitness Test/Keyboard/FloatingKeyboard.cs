using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
public class FloatingKeyboard : MonoBehaviour
{
    [SerializeField] private GameObject KeyboardGO;
    [SerializeField] private TMP_InputField targetInputField;
    [SerializeField] private TMP_Text previewText;  //basically change this and just reload the target = this

    [SerializeField] private Transform keyboardGrid;
    [SerializeField] private HorizontalLayoutGroup rowPrefab;
    [SerializeField] private KeyboardButton keyBtnFab;

    [SerializeField]
    private string[] buttonTexts = {
        "1","2","3","4","5","6","7","8","9","0",
        "q","w","e","r","t","y","u","i","o","p","backspace",
        "a","s","d","f","g","h","j","k","l","enter",
        "shift","z","x","c","v","b","n","m",",",".","shift", "space"};

    private bool isShifted = false; //is it big or no
    private List<KeyboardButton> instantiatedButtons = new List<KeyboardButton>();
    public Action<bool> OnShiftChanged;

    private void Start()
    {
        GenerateKeyboard();
    }

    public void KeyboardOpen()
    {
        previewText.text = targetInputField.text;
        KeyboardGO.SetActive(true);
    }
    public void KeyboardClose()
    {
        KeyboardGO.SetActive(false);
    }
    private void GenerateKeyboard()
    {
        int[] rowCounts = { 10, 11, 10, 11 , 1};
        int currentKeyIndex = 0;

        for (int i = 0; i < rowCounts.Length; i++)
        {
            // Create a new row container
            HorizontalLayoutGroup rowObj = Instantiate(rowPrefab, keyboardGrid);
            rowObj.padding.left = (i % 2 == 0) ? 0 : -5;

            Transform currentRow = rowObj.transform;

            for (int j = 0; j < rowCounts[i]; j++)
            {
                if (currentKeyIndex >= buttonTexts.Length) break;

                string label = buttonTexts[currentKeyIndex];
                KeyboardButton _k = Instantiate(keyBtnFab, currentRow);

                // Initialize button visuals and subscription
                _k.BtnSetup(label, this);

                // Initialize button functionality
                SetupButtonLogic(_k, label);

                currentKeyIndex++;
            }
        }
    }

    private void SetupButtonLogic(KeyboardButton _k, string label)
    {
        switch (label.ToLower())
        {
            case "backspace":
                _k.btn.onClick.AddListener(Backspace);
                break;
            case "shift":
                _k.btn.onClick.AddListener(ToggleShift);
                break;
            case "enter":
                _k.btn.onClick.AddListener(Submit);
                ApplyExtraWidth(_k, 2);
                break;
            case "space":
                _k.btn.onClick.AddListener(Space);
                ApplyExtraWidth(_k, 10);
                break;
            default:
                string capturedLabel = label;
                _k.btn.onClick.AddListener(() => AddChar(capturedLabel));
                break;
        }
    }

    private void ApplyExtraWidth(KeyboardButton key, int multiplier)
    {
        // Ensure the prefab has a LayoutElement or add one
        LayoutElement le = key.GetComponent<LayoutElement>();
        if (le == null) le = key.gameObject.AddComponent<LayoutElement>();

        le.minWidth = 20;       // 2x width
        le.preferredWidth = 20;
        // Assuming your GridLayoutGroup uses preferred sizes or flexible sizes
        // In a Grid Layout Group, you can't easily span columns without a 
        // 'Custom Grid' or 'Flexible Layout Group'. 
        // If using a GridLayoutGroup, this button will just look bigger 
        // but might break the alignment unless you use a 'Layout Group' + 'Content Size Fitter'.
    }

    public void AddChar(string letter)
    {
        if (targetInputField == null)
            return;

        string charToAdd = isShifted ? letter.ToUpper() : letter.ToLower();
        previewText.text += charToAdd;

        UpdateText();
    }
    public void Space()
    {
        previewText.text += " ";
        UpdateText();
    }
    public void Backspace()
    {
        if (targetInputField == null || targetInputField.text.Length == 0) 
            return;

        previewText.text = previewText.text.Substring(0, previewText.text.Length - 1);
        UpdateText();
    }

    public void ToggleShift()
    {
        isShifted = !isShifted;
        OnShiftChanged?.Invoke(isShifted);
    }
    public void Submit()
    {
        KeyboardClose();
    }
    private void UpdateText()
    {
        targetInputField.text = previewText.text;
        targetInputField.MoveTextEnd(false);
    }


}
