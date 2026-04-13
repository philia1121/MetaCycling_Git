using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Meta.XR.ImmersiveDebugger.UserInterface.Generic;
using Unity.VisualScripting;
public class ConfigUIManager : MonoBehaviour
{
    public TextMeshProUGUI prefixText;
    public void CheckPrefix()
    {
        prefixText.text = TrajectoryRecorder.instance.GetFilePrefix();
    }
}
