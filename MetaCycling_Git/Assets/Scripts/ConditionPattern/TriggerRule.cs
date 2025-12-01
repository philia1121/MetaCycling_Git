using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class TriggerRule
{
    public bool Ignore = false;
    public string ruleName; // 方便在 Inspector 中辨識

    [Tooltip("要檢測的條件 (ScriptableObject)")]
    public BaseCondition condition;

    // 關鍵：這會讓 Unity Inspector 顯示一個下拉選單，
    // 讓你選擇 BaseTimeLogic 的所有子類 (Instant, Maintain, MTimes)
    [SerializeReference]
    [SubclassSelector]
    public BaseTimeLogic timeLogic = new InstantLogic(); // 預設值

    [Tooltip("當規則被觸發時要執行的動作")]
    public UnityEvent OnRuleMet, OnRuleLost;

    public bool fireOnce = true; // 是否只觸發一次
    private bool hasFired = false;
    private bool hasFiredRule = false; // 用於 fireOnce 邏輯
    private bool wasConditionMetLastFrame = false; // 追蹤 "原始條件" 狀態
    private bool wasRuleActiveLastFrame = false;   // [新增] 追蹤 "完整規則" 狀態

    // 由 ComplexTrigger 在 Update() 中呼叫
    public void Tick(GameObject owner, Transform player, float deltaTime)
    {
        if (fireOnce && hasFired) return;

        bool isConditionMet = (condition == null) ? true : condition.IsMet(owner, player);
        wasConditionMetLastFrame = isConditionMet;

        timeLogic.Tick(isConditionMet, deltaTime);
        bool isRuleCurrentlyActive = timeLogic.IsCurrentlyActive;

        if (isRuleCurrentlyActive && !wasRuleActiveLastFrame)
        {
            // 規則剛剛從 False -> True
            if (!fireOnce || !hasFiredRule)
            {
                Debug.Log($"Trigger Rule '{ruleName}' MET!");
                OnRuleMet.Invoke();
                hasFiredRule = true; // 標記已觸發
            }
        }
        else if (!isRuleCurrentlyActive && wasRuleActiveLastFrame)
        {
            // 規則剛剛從 True -> False
            Debug.Log($"Trigger Rule '{ruleName}' LOST!");
            OnRuleLost.Invoke(); // [新增] 觸發 OnRuleLost

            // [關鍵] 重置 fireOnce 標記，這樣規則才能被下一次滿足時再次觸發
            if (fireOnce)
            {
                hasFiredRule = false;
            }
        }

        wasRuleActiveLastFrame = isRuleCurrentlyActive;
    }
}