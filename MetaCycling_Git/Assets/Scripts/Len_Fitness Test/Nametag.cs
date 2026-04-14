using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class Nametag : MonoBehaviour
{
    [SerializeField] private string text;
    [SerializeField] private TMP_Text nameText;
    private Camera _cam;
    public bool isDisplayName;

    private void Start()
    {
        if (!isDisplayName)
            return;

        if (_cam == null)
            _cam = Camera.main;
        nameText.text = text;
    }

    public void DisableName()
    {
        nameText.text = "";
    }

    void Update()
    {
        if (_cam == null)
            return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(transform.position - _cam.transform.position), Time.deltaTime * 3);
    }
}
