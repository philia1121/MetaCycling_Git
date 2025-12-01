using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyboardMover : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("移動速度 (單位/秒)")]
    public float moveSpeed = 3.0f;

    [Tooltip("旋轉速度 (度/秒)")]
    public float rotateSpeed = 60.0f;

    [Header("Control Toggle")]
    [Tooltip("按下此鍵來啟用/禁用鍵盤控制")]
    public KeyCode toggleKey = KeyCode.P;

    // 內部狀態，追蹤鍵盤控制是否已啟用
    private bool isControlEnabled = false;

    // 更新：每幀被呼叫一次
    void Update()
    {
        // 步驟 1: 檢查是否按下了總開關
        if (Input.GetKeyDown(toggleKey))
        {
            isControlEnabled = !isControlEnabled; // 反轉啟用狀態
            Debug.Log("Keyboard control " + (isControlEnabled ? "ENABLED" : "DISABLED"));
        }

        // 步驟 2: 如果控制未啟用，則不執行任何操作
        if (!isControlEnabled)
        {
            return;
        }

        // --- 步驟 3: 位置移動 (WASD + Q/E) ---

        Vector3 moveDirection = Vector3.zero;

        // X 軸 (左右) - 相對於物件的 "右邊"
        if (Input.GetKey(KeyCode.D))
        {
            moveDirection += transform.right;
        }
        if (Input.GetKey(KeyCode.A))
        {
            moveDirection -= transform.right;
        }

        // Z 軸 (前後) - 相對於物件的 "前面"
        if (Input.GetKey(KeyCode.W))
        {
            moveDirection += transform.forward;
        }
        if (Input.GetKey(KeyCode.S))
        {
            moveDirection -= transform.forward;
        }

        // Y 軸 (上下) - 相對於物件的 "上面"
        if (Input.GetKey(KeyCode.E))
        {
            moveDirection += transform.up;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            moveDirection -= transform.up;
        }

        // 套用移動
        // .normalized 確保斜向移動不會比較快
        // Time.deltaTime 確保移動速度與幀率無關
        transform.position += moveDirection.normalized * moveSpeed * Time.deltaTime;


        // --- 步驟 4: 旋轉 (方向鍵 + Z/C) ---

        float yaw = 0;   // Y Axis
        float pitch = 0; // X Axis
        float roll = 0;  // Z Axis

        // Yaw
        if (Input.GetKey(KeyCode.RightArrow))
        {
            yaw = 1;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            yaw = -1;
        }

        // Pitch
        if (Input.GetKey(KeyCode.DownArrow)) 
        {
            pitch = 1;
        }
        if (Input.GetKey(KeyCode.UpArrow)) 
        {
            pitch = -1;
        }

        // Roll
        if (Input.GetKey(KeyCode.C))
        {
            roll = 1;
        }
        if (Input.GetKey(KeyCode.Z))
        {
            roll = -1;
        }

        transform.Rotate(Vector3.up, yaw * rotateSpeed * Time.deltaTime, Space.World);
        
        transform.Rotate(transform.right, pitch * rotateSpeed * Time.deltaTime, Space.Self);
        transform.Rotate(transform.forward, roll * rotateSpeed * Time.deltaTime, Space.Self);
    }
}
