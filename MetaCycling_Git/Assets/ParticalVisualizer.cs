using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticalVisualizer : MonoBehaviour
{
    private ParticleSystem _particleSystem;
    public PetrollerObjectInfo info;

    [Header("顯示設定")]
    [Tooltip("輸入數據的最大範圍 (例如輸入值在 -10 到 10 之間，就設為 10)")]
    public bool autoRange = true;
    public float dataRange = 10.0f;

    [Tooltip("超出範圍是否要隱藏？ (True=不畫, False=限制在邊緣)")]
    public bool hideOutOfBounds = false;

    [Header("設定")]
    [Tooltip("點存在的時間 (秒)")]
    public float pointLifetime = 2.0f;

    [Tooltip("點的大小")]
    public float pointSize = 0.1f;

    [Tooltip("當Vector3長度達到此值時，顏色最紅")]
    public float maxMagnitudeForColor = 10.0f;
    public Color[] colors;

    void Awake()
    {
        _particleSystem = GetComponent<ParticleSystem>();
        if (autoRange) dataRange = transform.parent.localScale.x;
    }

    void Update()
    {
        DrawPoint(info.Velocity);
    }

    /// <summary>
    /// 畫出一個點
    /// </summary>
    /// <param name="input">Vector3 的值 (即點的位置)</param>
    public void DrawPoint(Vector3 input)
    {
        float magnitude = input.magnitude;
        float t = Mathf.Clamp01(magnitude / maxMagnitudeForColor);
        Color finalColor = Color.Lerp(colors[0], colors[1], t);

        Vector3 localPos = input / (dataRange * 2);
        bool isOutOfBounds =
            Mathf.Abs(localPos.x) > 0.5f ||
            Mathf.Abs(localPos.y) > 0.5f ||
            Mathf.Abs(localPos.z) > 0.5f;

        if (isOutOfBounds)
        {
            if (hideOutOfBounds) return;

            localPos.x = Mathf.Clamp(localPos.x, -0.5f, 0.5f);
            localPos.y = Mathf.Clamp(localPos.y, -0.5f, 0.5f);
            localPos.z = Mathf.Clamp(localPos.z, -0.5f, 0.5f);
        }

        var emitParams = new ParticleSystem.EmitParams();

        emitParams.position = localPos;
        emitParams.startColor = finalColor;
        emitParams.startSize = pointSize;
        emitParams.startLifetime = pointLifetime;
        emitParams.velocity = Vector3.zero;

        _particleSystem.Emit(emitParams, 1);
    }
}
