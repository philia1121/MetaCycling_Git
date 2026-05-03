using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Firebase;
using UnityEngine;

public class Test : MonoBehaviour
{
    public string folder;
    async void Start()
    {
        var num = Random.Range(0, 101);
        string json = JsonUtility.ToJson(num, true);
        string name = $"{System.DateTime.Now:yyyy_MM_dd_HH_mm_ss_}.json";
        string path = Path.Combine(Application.persistentDataPath, folder, name);
        Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, folder));
        File.WriteAllText(path, json);
        Debug.Log("[RecordCSVWriter] File PATH: " + path);
    }
}
