using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using UnityEngine;

public class Test : MonoBehaviour
{
    async void Start()
    {
        var dependencyStatus = await FirebaseApp.CheckDependenciesAsync();
        if (dependencyStatus == DependencyStatus.Available)
        {
            Debug.Log("availble");
        }
        else
        {
            Debug.Log("not available");
        }
    }
}
