using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MotionTracker : MonoBehaviour
{
    [Header("Settings")]
    [Range(0.01f, 1.0f)]
    public float smoothing = 0.1f; // 數值平滑系數，越小越平滑但延遲越高

    [Header("Outputs (Read Only)")]
    public float Speed;
    public float AngularSpeed;
    public Vector3 Velocity;
    public Vector3 Acceleration;
    public Vector3 AngularVelocity;
    public Vector3 AngularAcceleration;

    private Vector3 lastPosition;
    private Vector3 lastVelocity;
    private Quaternion lastRotation;
    private Vector3 lastAngularVelocity;

    void Start()
    {
        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        if (dt <= 0) return;

        // 1. 計算位移速度與加速度
        Vector3 currentVelocity = (transform.position - lastPosition) / dt;
        Acceleration = Vector3.Lerp(Acceleration, (currentVelocity - lastVelocity) / dt, smoothing);
        Velocity = Vector3.Lerp(Velocity, currentVelocity, smoothing);
        Speed = Velocity.magnitude;

        // 2. 計算角速度 (Quaternion to Angular Velocity)
        // 使用 Quaternion.ToAngleAxis 獲取旋轉增量
        Quaternion deltaRotation = transform.rotation * Quaternion.Inverse(lastRotation);
        deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);

        // 確保角度在 -180 到 180 之間
        if (angle > 180f) angle -= 360f;

        // 角度轉為弧度 (Radian) 如果需要，或是保持角度 (Degree)
        // 這裡建議用 Degree/s，因為 Unity 常見標準如此
        Vector3 currentAngularVelocity = (axis * angle) / dt;

        // 3. 計算角加速度
        AngularAcceleration = Vector3.Lerp(AngularAcceleration, (currentAngularVelocity - lastAngularVelocity) / dt, smoothing);
        AngularVelocity = Vector3.Lerp(AngularVelocity, currentAngularVelocity, smoothing);
        AngularSpeed = AngularVelocity.magnitude;

        // 4. 更新暫存值
        lastPosition = transform.position;
        lastVelocity = Velocity;
        lastRotation = transform.rotation;
        lastAngularVelocity = AngularVelocity;
    }
}
