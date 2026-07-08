using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
public class SceneViewCamera : MonoBehaviour
{
    [Header("視角控制感度")]
    public float lookSpeed = 0.2f;  // 數值通常會比舊版 Input 小，因為 delta 是實際像素
    public float panSpeed = 5f;
    public float zoomSpeed = 0.005f; // 滾輪數值較大，感度需調小

    private float pitch = 0f;
    private float yaw = 0f;

    [Header("聚焦設定")]
    [Tooltip("將 HMD, RC, LC, RH, LH 五個物件拖曳到這個陣列中")]
    public Transform[] targetObjects;
    public float focusDistance = 3f;

    private bool isBlockedByUI = false;

    void Start()
    {
        // 初始化視角為當前攝影機的角度
        pitch = transform.eulerAngles.x;
        yaw = transform.eulerAngles.y;
    }

    void Update()
    {
        // 確保目前系統中有偵測到滑鼠裝置
        if (Mouse.current == null) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // 讀取滑鼠每幀的位移量與滾輪量
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        Vector2 scrollDelta = Mouse.current.scroll.ReadValue();

        // 右鍵按住：調整視角 (FPS Look)
        if (Mouse.current.rightButton.isPressed)
        {
            yaw += mouseDelta.x * lookSpeed;
            pitch -= mouseDelta.y * lookSpeed;

            // 限制上下觀看的角度避免翻轉
            pitch = Mathf.Clamp(pitch, -90f, 90f);

            transform.eulerAngles = new Vector3(pitch, yaw, 0f);
        }

        // 中鍵：平移視角
        if (Mouse.current.middleButton.isPressed)
        {
            // 這裡乘上 Time.deltaTime 使平移較為平滑
            float panX = -mouseDelta.x * panSpeed * Time.deltaTime;
            float panY = -mouseDelta.y * panSpeed * Time.deltaTime;
            transform.Translate(new Vector3(panX, panY, 0f), Space.Self);
        }

        // 左鍵：以物件為中心移動視角
        if (Mouse.current.leftButton.isPressed)
        {
            Vector3 center = GetTargetsCenter();
            float distance = Vector3.Distance(transform.position, center);

            // 避免距離過近造成計算異常
            if (distance < 0.05f) distance = focusDistance;

            yaw += mouseDelta.x * lookSpeed;
            pitch -= mouseDelta.y * lookSpeed;
            pitch = Mathf.Clamp(pitch, -89f, 89f); // 限制角度避免翻轉

            // 計算新旋轉與位置
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = center - (rotation * Vector3.forward * distance);
            transform.rotation = rotation;
        }

        // 滾輪滾動：放大縮小 (Z軸移動)
        if (Mathf.Abs(scrollDelta.y) > 0.001f)
        {
            // scrollDelta.y 向前滾通常是正值，向後滾是負值
            transform.Translate(Vector3.forward * scrollDelta.y * zoomSpeed, Space.Self);
        }

    }

    private Vector3 GetTargetsCenter()
    {
        if (targetObjects == null || targetObjects.Length == 0)
            return transform.position + transform.forward * focusDistance;

        Vector3 center = Vector3.zero;
        int activeCount = 0;

        foreach (Transform t in targetObjects)
        {
            if (t != null && t.gameObject.activeInHierarchy)
            {
                center += t.position;
                activeCount++;
            }
        }

        if (activeCount > 0)
        {
            return center / activeCount;
        }

        // 若完全沒有物件顯示，則回傳相機前方的一點作為虛擬中心
        return transform.position + transform.forward * focusDistance;
    }
    public void FocusOnTargets()
    {
        Vector3 center = GetTargetsCenter();
        transform.position = center - transform.forward * focusDistance;
    }
}
