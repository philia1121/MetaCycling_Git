using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SFB;
using TMPro;
using System.IO;
[RequireComponent(typeof(Button))]
public class CanvasSampleOpenFileJson : MonoBehaviour, IPointerDownHandler
{
    public TextMeshProUGUI tmpText;
    public DataReplayer dataReplayer;

    public void OnPointerDown(PointerEventData eventData) { }

    void Start()
    {
        var button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        // var paths = StandaloneFileBrowser.OpenFilePanel("Title", "", "txt", true);
        var paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", "", true);
        if (paths.Length > 0)
        {
            string fullPath = paths[0];
            string fileName = Path.GetFileName(fullPath);

            tmpText.text = fileName;
            if (dataReplayer) dataReplayer.LoadJson(fullPath);
        }
    }
}