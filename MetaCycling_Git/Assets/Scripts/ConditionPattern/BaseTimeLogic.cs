using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class BaseTimeLogic
{
    public bool IsCurrentlyActive { get; protected set; } = false;
    public abstract bool Tick(bool isConditionMet, float deltaTime);
    public abstract void Reset();
}

// 範例 1：瞬間達成 (只要滿足就觸發)
[System.Serializable]
public class InstantLogic : BaseTimeLogic
{
    public override bool Tick(bool isConditionMet, float deltaTime)
    {
        bool wasActive = IsCurrentlyActive;
        IsCurrentlyActive = isConditionMet;

        return IsCurrentlyActive && !wasActive;
    }

    public override void Reset() { IsCurrentlyActive = false; }
}

// 範例 2：維持 N 秒
[System.Serializable]
public class MaintainForNSecondsLogic : BaseTimeLogic
{
    public float secondsToMaintain = 3.0f;
    private float currentTimer = 0f;

    public override bool Tick(bool isConditionMet, float deltaTime)
    {
        bool wasActive = IsCurrentlyActive; // 儲存上一幀的狀態
        if (isConditionMet)
        {
            currentTimer += deltaTime;
            if (currentTimer >= secondsToMaintain)
            {
                IsCurrentlyActive = true;
            }
        }
        else
        {
            currentTimer = 0f; // 條件中斷，計時器重置
            IsCurrentlyActive = false;
        }
        return IsCurrentlyActive && !wasActive;
    }

    public override void Reset()
    {
        currentTimer = 0f;
        IsCurrentlyActive = false;
    }
}

// 範例 3：N 秒內達成 M 次
[System.Serializable]
public class MTimesInNSecondsLogic : BaseTimeLogic
{
    public int timesToHappen = 5;
    public float timeWindow = 10.0f;

    // 使用佇列來儲存事件發生的時間戳
    private Queue<float> eventTimestamps = new Queue<float>();
    private bool wasMetLastFrame = false; // 用於偵測 "上升緣" (剛達成的那一幀)

    public override bool Tick(bool isConditionMet, float deltaTime)
    {
        float currentTime = Time.time;

        // 1. 移除佇列中超過 N 秒的舊時間戳
        while (eventTimestamps.Count > 0 && currentTime - eventTimestamps.Peek() > timeWindow)
        {
            eventTimestamps.Dequeue();
        }

        // 2. 偵測條件是否 "剛被滿足" (從 false 變 true)
        if (isConditionMet && !wasMetLastFrame)
        {
            eventTimestamps.Enqueue(currentTime);
        }
        wasMetLastFrame = isConditionMet;

        // 3. 檢查次數是否達標
        if (eventTimestamps.Count >= timesToHappen)
        {
            return true; // 達成！
        }

        return false;
    }

    public override void Reset()
    {
        eventTimestamps.Clear();
        wasMetLastFrame = false;
    }
}

[System.Serializable] // [重要] 記得加上
public class MaintainForNSecondsWithGraceLogic : BaseTimeLogic
{
    [Tooltip("需要維持的主要時間 (秒)")]
    public float secondsToMaintain = 3.0f;

    [Tooltip("當條件中斷時，允許的緩衝時間 (秒)")]
    public float gracePeriodSeconds = 0.3f; // 0.3秒的寬限期

    // --- 內部狀態變數 ---
    private float currentTimer = 0f;        // 主計時器
    private float currentGraceTimer = 0f; // 寬限期計時器

    public override bool Tick(bool isConditionMet, float deltaTime)
    {
        bool wasActive = IsCurrentlyActive; // 儲存上一幀的狀態

        if (isConditionMet)
        {
            // 1. 條件滿足：
            // 累加主計時器
            currentTimer += deltaTime;
            // 重置寬限期計時器 (因為我們不需要它)
            currentGraceTimer = 0f;

            if (currentTimer >= secondsToMaintain)
            {
                // 成功維持了 N 秒
                IsCurrentlyActive = true;
            }
        }
        else
        {
            // 2. 條件不滿足 (例如速度 < 2)：

            // 檢查我們是否「正在計時中」或「已經滿足條件」
            if (currentTimer > 0f || IsCurrentlyActive)
            {
                // 我們的主計時器有進度，或者已經達標了
                // 開始計算寬限期
                currentGraceTimer += deltaTime;

                if (currentGraceTimer > gracePeriodSeconds)
                {
                    // 寬限期用完了！
                    // 重置所有東西
                    currentTimer = 0f;
                    currentGraceTimer = 0f;
                    IsCurrentlyActive = false; // 規則狀態解除
                }
            }
            // (如果 currentTimer == 0 且 !IsCurrentlyActive，我們什麼都不做，保持在 0)
        }

        // "剛剛" 觸發 = 上一幀為 False，這一幀為 True
        return IsCurrentlyActive && !wasActive;
    }

    public override void Reset()
    {
        currentTimer = 0f;
        currentGraceTimer = 0f;
        IsCurrentlyActive = false;
    }
}