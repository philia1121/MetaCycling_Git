using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIRotVisualizer : MonoBehaviour
{
    [SerializeField] private RectTransform pointer;

    private Slider slider;
    private Image sliderFill;

    private float deviationValue;
    private bool isReset;

    private void Awake()
    {
        //recall all this just in case
        slider = GetComponent<Slider>();
        if (slider.maxValue < 360) slider.maxValue = 360;
        slider.wholeNumbers = false;

        sliderFill = slider.fillRect.GetComponent<Image>();
        sliderFill.type = Image.Type.Filled;
        sliderFill.fillMethod = Image.FillMethod.Radial360;
    }

    public void SetDeviationValue(float value)
    {
        deviationValue = value;
        isReset = false;
    }

    public void SetFillamount(float fillamount)
    {
        float _i = fillamount + deviationValue;

        //reset rotation logic if player presses something
        if (!isReset)
        {
            isReset = true;
            slider.value = 0f;
            return;
        }

        sliderFill.fillClockwise = _i > 0f;
        slider.value = Mathf.Abs(_i % 360);
        pointer.localRotation = Quaternion.Euler(0f, 0f, (_i > 0) ? -Mathf.Abs(_i % 360) : Mathf.Abs(_i % 360));

        //float delta = Mathf.DeltaAngle(previousAngle, _i);
        //sliderFill.fillClockwise = delta < 0f;
        //slider.value = Mathf.Clamp(Mathf.Abs(delta), 0f, 360f);
        //pointer.localRotation = Quaternion.Euler(0f,0f,delta);
    }
}
