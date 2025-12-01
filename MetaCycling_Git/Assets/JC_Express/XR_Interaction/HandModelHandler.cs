using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class HandModelHandler : MonoBehaviour
{
    public OVRMeshRenderer oVRMeshRenderer;
    public SkinnedMeshRenderer skinnedMeshRenderer;
    public Material mat;
    public bool isTrackingGood;
    public Transform PalmTransform { get; private set; }
    [Header("核心組件 (Core Components)")]
    public OVRHand hand;
    public OVRSkeleton skeleton;
    public Renderer handRenderer;

    [Header("延遲處理 (Hysteresis for Blinking)")]
    [Tooltip("手部必須持續穩定多長時間才重新顯示 (秒)")]
    public float showDelay = 0.2f;

    [Tooltip("手部必須持續不穩定多長時間才隱藏 (秒)")]
    public float hideDelay = 0.1f;

    // --- 私有變數 ---
    private float timeSinceLastStable = 0.0f;
    private float timeSinceLastUnstable = 0.0f;

    void Start()
    {
        if (hand == null) hand = GetComponent<OVRHand>();
        if (skeleton == null) skeleton = GetComponent<OVRSkeleton>();

        if (skeleton == null || skeleton.Bones == null || skeleton.Bones.Count == 0)
        {
            Debug.LogError("HandConfidenceMonitor: 找不到 OVRSkeleton 或骨骼！");
            this.enabled = false;
            return;
        }

        if (handRenderer == null)
        {
            handRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            if (handRenderer == null)
            {
                Debug.LogError("HandConfidenceMonitor: 找不到手部渲染器！");
                this.enabled = false;
            }
        }

        PalmTransform = skeleton.Bones[(int)OVRSkeleton.BoneId.XRHand_Palm].Transform;
    }

    void LateUpdate()
    {
        if (skeleton == null || handRenderer == null) return;
        // mat.color = skeleton.IsDataHighConfidence ? Color.white : Color.red;

        // 1. 基礎 API 檢測
        // 我們仍然需要 IsDataHighConfidence，因為當它為 false 時，骨骼信心也為 0
        if (hand.IsTracked && hand.IsDataHighConfidence)
        {
            int boneCount = skeleton.Bones.Count;
            isTrackingGood = boneCount == 0 ? false : skeleton.IsDataHighConfidence;
        }
        else
        {
            isTrackingGood = false;
        }

        // 3. 狀態更新與延遲處理 (防止閃爍)
        if (isTrackingGood)
        {
            timeSinceLastUnstable = 0;
            timeSinceLastStable += Time.deltaTime;

            if (timeSinceLastStable > showDelay)
            {
                handRenderer.enabled = true;
                oVRMeshRenderer.enabled = true;
                skinnedMeshRenderer.enabled = true;
            }
        }
        else
        {
            // 追蹤不好 (API 說不好 OR 雖然 API 說好，但平均信心太低)
            timeSinceLastStable = 0;
            timeSinceLastUnstable += Time.deltaTime;

            if (timeSinceLastUnstable > hideDelay)
            {
                handRenderer.enabled = false;
                oVRMeshRenderer.enabled = false;
                skinnedMeshRenderer.enabled = false;
            }
        }
    }
}
