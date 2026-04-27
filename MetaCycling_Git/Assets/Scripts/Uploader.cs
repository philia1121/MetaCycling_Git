using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using UnityEngine.InputSystem;
using System.Net;
using Firebase;
using Firebase.Firestore;
using System.Threading.Tasks;
using Firebase.Extensions;
using System;
public class Uploader : MonoBehaviour
{
    private FirebaseFirestore firestore;

    void Awake()
    {
        // 檢查 Firebase App 的初始化狀態
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                // Firebase 依賴項已成功解決
                FirebaseApp app = FirebaseApp.DefaultInstance;

                // ****** 在這裡設定 Firebase 的日誌級別為 Debug 或 Verbose ******
                // Debug 會提供足夠的資訊，Verbose 會提供更多細節
                FirebaseApp.LogLevel = LogLevel.Verbose; // 或者 LogLevel.Verbose;

                firestore = FirebaseFirestore.DefaultInstance;
                Debug.Log("Firebase App 已成功初始化，Firestore 實例已獲取。");
            }
            else
            {
                Debug.LogError($"未能解決所有 Firebase 依賴項: {dependencyStatus}");
                // Firebase 不可用，不應繼續使用 Firebase 服務
            }
        });
    }

    public void SaveToBase()
    {
        if (firestore == null)
        {
            Debug.LogError("Firestore 實例尚未初始化。請檢查 Firebase App 初始化是否成功。");
            return;
        }

        TestData testData = new TestData
        {
            num = 11,
        };
        DocumentReference docRef = firestore.Document($"save_data/0");

        docRef.SetAsync(testData).ContinueWithOnMainThread(task => {
            if (task.IsFaulted)
            {
                Debug.LogError("寫入 Firestore 文件時發生錯誤: " + task.Exception);
                if (task.Exception is AggregateException aggregateException)
                {
                    foreach (var innerException in aggregateException.InnerExceptions)
                    {
                        Debug.LogError($"內部錯誤: {innerException.Message}");
                    }
                }
            }
            else if (task.IsCompleted)
            {
                Debug.Log("文件已成功寫入雲端，路徑為: " + docRef.Path);
            }
        });
        Debug.Log("嘗試寫入雲端...");
    }
}

[FirestoreData]
[System.Serializable]
public class TestData
{
    public TestData() { }
    [FirestoreProperty] public int num { get; set; }
}
