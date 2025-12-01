using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfoBoardManager : MonoBehaviour
{
    [Header("設定參考物件")]
    public Transform objectA; // 物件 A (基準點)
    public Transform objectB; // 物件 B (要移動/出現的物件)

    [Header("參數設定")]
    public float distanceX = 5.0f; // 距離 X
    public KeyCode triggerKey = KeyCode.A; // 觸發按鍵 (Button A)

    void Update()
    {
        // 偵測按下按鈕 A
        if (Input.GetKeyDown(triggerKey))
        {
            PlaceBInFrontOfA();
        }
    }

    void PlaceBInFrontOfA()
    {
        // 1. 計算位置：找出 A 正前方距離 X 的座標
        //公式：A的座標 + (A的前方方向 * 距離)
        Vector3 targetPosition = objectA.position + (objectA.forward * distanceX);

        // 設定 B 的位置
        objectB.position = targetPosition;

        // 2. 設定旋轉：讓 B 正面面向 A
        // 使用 LookAt API，讓 B 的 Z 軸 (藍色軸) 指向 A
        objectB.LookAt(objectA);

        // 如果物件 B 原本是隱藏的，可以在這裡開啟它
        // objectB.gameObject.SetActive(true);
    }
}
