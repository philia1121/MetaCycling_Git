using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
public class FeatureToggle : MonoBehaviour
{
    public Button button;
    TextMeshProUGUI textMesh;
    Image image;
    public bool active = false;
    public string[] buttonTxts;
    public Color[] colors;
    public UnityEvent ToggleOnEvent, ToggleOffEvent;
    void Awake()
    {
        button.onClick.AddListener(FireToggleEvent);
        textMesh = button.GetComponentInChildren<TextMeshProUGUI>();
        image = button.GetComponentInChildren<Image>();
    }
    void Start()
    {
        active = !active;
        FireToggleEvent();
    }
    public void FireToggleEvent()
    {
        active = !active;
        if (active)
        {
            ToggleOnEvent?.Invoke();
            if (buttonTxts.Length > 0 & textMesh) textMesh.text = buttonTxts[1];
            if (colors.Length > 0 & image) image.color = colors[1];
        }
        else
        {
            ToggleOffEvent?.Invoke();
            if (buttonTxts.Length > 0 & textMesh) textMesh.text = buttonTxts[0];
            if (colors.Length > 0 & image) image.color = colors[0];
        }
    }
}
