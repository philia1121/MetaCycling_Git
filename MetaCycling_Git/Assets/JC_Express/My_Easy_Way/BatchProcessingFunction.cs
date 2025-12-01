using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BatchProcessingFunction : MonoBehaviour
{
    [SerializeField]private GameObject[] gameObjects;
    public void SetActiveAll(bool value)
    {
        foreach(var item in gameObjects)
        {
            item.SetActive(value);
        }
    }
}
