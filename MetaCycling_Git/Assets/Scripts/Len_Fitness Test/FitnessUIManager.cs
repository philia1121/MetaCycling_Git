using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FitnessUIManager : MonoBehaviour
{
    [Header("UI Indicatiors")]
    [SerializeField] private Button startBtn;
    [SerializeField] private Button endBtn;
    [SerializeField] private Button changeDisplayBtn;
    [SerializeField] private TMP_Text changeDisplayBtnText;
    [SerializeField] private Button resetBtn;
    [SerializeField] private Slider densitySlider;

    [Header("Position Indicators")]
    [SerializeField] private GameObject startPrefab;
    [SerializeField] private GameObject endPrefab;
    [SerializeField] private TMP_Text moveDistText;
    [SerializeField] private TMP_Text heightTxt;

    private FitnessTestManager m_fitness;
    private PathVisualizer m_path;

    private bool isPlaying = false;
    private JumpResult jumpRes;
    private void Start()
    {
        m_fitness = FitnessTestManager.instance;
        m_path = PathVisualizer.instance;

        CheckDisplayType();
        endBtn.gameObject.SetActive(false);

        startBtn.onClick.AddListener(() => {
            //start moving if isplaying false
            if (isPlaying)
                m_fitness.StartMovement(success => {
                    if (!success)
                        return;
                    jumpRes = new JumpResult();
                    isPlaying = !isPlaying;

                    startBtn.gameObject.SetActive(false);
                    endBtn.gameObject.SetActive(true);
                });
            //or end moving if isplaying is true
        });
        endBtn.onClick.AddListener(() => {
            if (!isPlaying)
            {
                jumpRes = m_fitness.EndMovement();
                if (!jumpRes.success)
                    return;
                moveDistText.text = $"Moved {jumpRes.distance} cm";
                heightTxt.text = $"Highest point {jumpRes.height} cm";
                isPlaying = !isPlaying;

                startBtn.gameObject.SetActive(true);
                endBtn.gameObject.SetActive(false);
            }
        });
        densitySlider.onValueChanged.AddListener(m_path.OnDensityChanged);
        changeDisplayBtn.onClick.AddListener(()=> { m_path.DisplayTrailingPath();
            CheckDisplayType();
        });
        resetBtn.onClick.AddListener(m_fitness.ClearTrackingData);
    }

    private void Update()
    {
        startPrefab.SetActive(m_fitness.calibratedStartPos != Vector3.zero);
        endPrefab.SetActive(m_fitness.calibratedEndPos!= Vector3.zero);
    }

    private void CheckDisplayType()
    {
        switch (m_path.displayID)
        {
            case 1:
                changeDisplayBtnText.text = "Trailing";
                break;

            case 2:
                changeDisplayBtnText.text = "Show All";
                break;

            case 3:
                changeDisplayBtnText.text = "Show Line";
                break;
        }
    }

}
