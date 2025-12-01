using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleMaterialController : MonoBehaviour
{
    public Material mat;
    public Color[] myColors;
    public void ChangeColor(int colorCode)
    {
        mat.color = myColors[colorCode];
    }
}
