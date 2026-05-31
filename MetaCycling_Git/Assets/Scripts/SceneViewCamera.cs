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

        // 中鍵/滾輪按住：平移視角
        if (Mouse.current.leftButton.isPressed)
        {
            // 這裡乘上 Time.deltaTime 使平移較為平滑
            float panX = -mouseDelta.x * panSpeed * Time.deltaTime;
            float panY = -mouseDelta.y * panSpeed * Time.deltaTime;
            transform.Translate(new Vector3(panX, panY, 0f), Space.Self);
        }

        // 滾輪滾動：放大縮小 (Z軸移動)
        if (Mathf.Abs(scrollDelta.y) > 0.001f)
        {
            // scrollDelta.y 向前滾通常是正值，向後滾是負值
            transform.Translate(Vector3.forward * scrollDelta.y * zoomSpeed, Space.Self);
        }

    }

    public void FocusOnTargets()
    {
        if (targetObjects == null || targetObjects.Length == 0) return;

        Vector3 center = Vector3.zero;
        int activeCount = 0;

        // 算出所有「目前有顯示 (Active)」的物件的平均中心位置
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
            center /= activeCount;
            // 保持相機當前的視角方向，單純把位置移動到中心點的後方
            transform.position = center - transform.forward * focusDistance;
        }
    }
}
