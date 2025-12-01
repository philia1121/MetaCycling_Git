using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleNote : MonoBehaviour
{
    [TextArea(5, 10)]
    public string Notes = "Comment Here.";
}
