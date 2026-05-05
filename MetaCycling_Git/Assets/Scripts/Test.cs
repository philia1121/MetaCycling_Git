using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Storage;
using System.Text;

public class Test : MonoBehaviour
{
    private FirebaseStorage storage;
    private StorageReference storageRef;
    ControlMap controlMap;

    void Awake()
    {
        controlMap = new ControlMap();
    }
    void Start()
    {
        InitializeFirebase();
    }
    void OnEnable()
    {
        controlMap.Prototype.Enable();
        controlMap.Prototype.RecordButton.started += ctx => TestUploadObject();
    }
    void InitializeFirebase()
    {
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                storage = FirebaseStorage.DefaultInstance;
                storageRef = storage.GetReferenceFromUrl("gs://metacycling-d034c.firebasestorage.app");
                Debug.Log("Firebase Firestore 已成功初始化。");
            }
            else
            {
                Debug.LogError($"無法解析 Firebase 依賴項: {dependencyStatus}");
            }
        });
    }

    public async void UploadCSharpObject(MyData data, string pathInStorage)
    {
        // 1. 將 C# 物件序列化為 JSON 字串
        string jsonString = JsonUtility.ToJson(data);

        // 2. 將 JSON 字串轉換為位元組陣列
        byte[] bytes = Encoding.UTF8.GetBytes(jsonString);

        // 3. 創建一個文件參考
        StorageReference fileRef = storageRef.Child(pathInStorage);

        try
        {
            // 4. 上傳位元組陣列
            StorageMetadata metadata = await fileRef.PutBytesAsync(bytes);
            Debug.Log($"上傳成功！檔案名稱: {metadata.Name}, 大小: {metadata.SizeBytes}");

            // 你也可以取得下載URL
            // Uri downloadUrl = await fileRef.GetDownloadUrlAsync();
            // Debug.Log($"下載 URL: {downloadUrl}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"上傳失敗: {e.Message}");
        }
    }

    // 呼叫範例
    public void TestUploadObject()
    {
        Debug.Log("triggerd");
        MyData myObject = new MyData("PlayerOne", 100, 1.0f);
        UploadCSharpObject(myObject, "user_data/playerone_profile.json");
    }
}
public class MyData
{
    public string Name;
    public int Score;
    public float Version;

    public MyData(string name, int score, float version)
    {
        Name = name;
        Score = score;
        Version = version;
    }
}
