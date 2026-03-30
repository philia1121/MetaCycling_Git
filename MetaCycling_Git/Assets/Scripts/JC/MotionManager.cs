using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class MotionManager : MonoBehaviour
{
    [Header("物件引用")]
    public Transform rightHand;       // 右手 Controller (追蹤對象)
    public LineRenderer lineRenderer; // 顯示路徑
    public AudioSource startAudio;

    [Header("自動判定參數")]
    public float startThreshold = 0.05f;    // 回到原點多少距離內算結束 (5cm)
    public float minMovementToStart = 0.15f; // 離開原點超過 15cm 才開始偵測「回原點」

    private List<MotionPoint> standardPath = new List<MotionPoint>();
    private Vector3 startPos;
    private Vector3 lastPos;
    private bool isRecording = false;
    private bool hasMovedAway = false;
    public bool autoEndRecord = true;
    private float recordStartTime;
    [Header("幽靈回放設置")]
    public Transform ghostModel;       // 拖入幽靈手把模型
    public bool loopPlayback = true;   // 是否循環播放
    private float playbackTimer = 0f;  // 播放計時器
    private float totalPathDuration = 0f; // 標準路徑總時長
    [Header("金幣遊戲設置")]
    public bool coinGame;
    public GameObject coinPrefab;      // 金幣 Prefab
    public float coinLifeSpan = 1.5f;  // 金幣消失時間（控制難度/速度要求）
    public float spawnInterval = 0.3f; // 每隔幾秒生成一顆金幣
    public AudioSource collectSound;   // 吃到金幣的聲音
    public GameObject effect; // 吃到金幣的特效
    private float coinTimer = 0f;
    private int currentCoinIndex = 0;

    [Header("體驗模式設置")]
    public bool isExperienceMode = false;
    public bool audiVisaulFeedback = true;
    public MeshRenderer feedbackIndicator; // 手把上的提示球
    public AudioSource beepSource;         // 嗶嗶聲來源
    public float maxDistanceTolerance = 0.2f; // 超過 20cm 算 0 分

    [Header("統計數據")]
    public int repCount = 0;
    private bool hasReachedHalfway = false; // 判斷是否已經拉到一半以上
    [Header("音效頻率設置")]
    public AudioClip beepClip;           // 拖入你的嗶嗶聲短音效
    private float beepTimer = 0f;        // 計時器
    private float currentScore = 0f;

    ControlMap controlMap;
    void Awake()
    {
        controlMap = new ControlMap();
        controlMap.Prototype.Enable();
        controlMap.Prototype.Record.started += ctx => StartRecordSetup();
        controlMap.Prototype.Delete.started += ctx => ClearRecording();
    }
    void Update()
    {
        if (isRecording)
        {
            RecordFrame();
        }

        if (!isRecording && standardPath.Count > 0 && ghostModel != null)
        {
            UpdateGhostPlayback();
        }
        else
        {
            ghostModel.gameObject.SetActive(standardPath.Count > 0);
        }

        if (isExperienceMode)
        {
            if (standardPath.Count > 0)
            {
                EvaluateCurrentMotion();
            }
            // 每一幀檢查是否該播放嗶嗶聲
            if (audiVisaulFeedback) HandleAudioBeep();
        }

        if (coinGame) HandleCoinGame();
    }
    void StartRecordSetup()
    {
        if (hasMovedAway)
        {
            StopRecording();
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(StartCountdown());
        }
    }

    IEnumerator StartCountdown()
    {
        startAudio.Play();
        Debug.Log("準備錄製！請將右手放到起點...倒數 3 秒");
        yield return new WaitForSeconds(3f);

        // 初始化錄製狀態
        standardPath.Clear();
        startPos = rightHand.position;
        lastPos = startPos;
        recordStartTime = Time.time;
        isRecording = true;
        hasMovedAway = false;

        lineRenderer.positionCount = 0;
        Debug.Log(">>> [錄製中] 請開始動作 <<<");
    }

    void RecordFrame()
    {
        Vector3 currentPos = rightHand.position;
        Vector3 currentVel = (currentPos - lastPos) / Time.deltaTime;

        // 建立詳細點位資料
        MotionPoint point = new MotionPoint
        {
            position = currentPos,
            rotation = rightHand.rotation,
            timestamp = Time.time - recordStartTime,
            velocity = currentVel
        };

        standardPath.Add(point);
        lastPos = currentPos;

        // 即時更新 LineRenderer 讓你看得到軌跡
        lineRenderer.positionCount = standardPath.Count;
        lineRenderer.SetPosition(standardPath.Count - 1, currentPos);

        // 判定：是否已離開起點一段距離
        float distFromStart = Vector3.Distance(currentPos, startPos);
        if (!hasMovedAway && distFromStart > minMovementToStart)
        {
            hasMovedAway = true;
            Debug.Log("已離開起始範圍，回程將自動觸發停止。");
        }

        // 判定：如果離開過起點，現在又回到了起點範圍
        if (autoEndRecord && hasMovedAway && distFromStart < startThreshold)
        {
            StopRecording();
        }
    }

    void StopRecording()
    {
        isRecording = false;
        float totalDuration = Time.time - recordStartTime;
        totalPathDuration = Time.time - recordStartTime;

        // 後處理：計算 Normalized Time (0~1)
        for (int i = 0; i < standardPath.Count; i++)
        {
            MotionPoint p = standardPath[i];
            p.normalizedTime = p.timestamp / totalDuration;
            standardPath[i] = p; // 寫回 List
        }

        playbackTimer = 0;
        Debug.Log($"錄製停止！共擷取 {standardPath.Count} 幀。總時長: {totalDuration:F2}s");
    }
    public void ClearRecording()
    {
        isRecording = false;
        hasMovedAway = false;
        standardPath.Clear();

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
        }

        Debug.Log("<color=red>紀錄已清除，可以重新開始錄製。</color>");
    }

    public void ToggleExperienceMode()
    {
        if (standardPath.Count == 0)
        {
            Debug.LogWarning("沒有標準路徑，無法開始體驗模式！");
            return;
        }
        isExperienceMode = !isExperienceMode;
        repCount = 0;
        hasReachedHalfway = false;
        Debug.Log(isExperienceMode ? "<color=green>體驗模式：開啟</color>" : "<color=red>體驗模式：關閉</color>");

        feedbackIndicator.enabled = isExperienceMode;
        if (!isExperienceMode) beepSource.Stop();
    }

    void EvaluateCurrentMotion()
    {
        Vector3 playerPos = rightHand.position;
        Quaternion playerRot = rightHand.rotation;

        // 1. 直接比對「當前」幽靈模型的位置與旋轉
        float dist = Vector3.Distance(playerPos, ghostModel.position);
        float angle = Quaternion.Angle(playerRot, ghostModel.rotation);

        // 2. 計算分數 (0.0 ~ 1.0)
        // 空間分數：假設 15cm 內算合格
        float spatialScore = Mathf.Clamp01(1 - (dist / 0.15f));

        // 旋轉分數：假設誤差 30 度內算合格
        float rotationScore = Mathf.Clamp01(1 - (angle / 30f));

        // 綜合分數 (你可以調整權重，這裡暫定各佔一半)
        float finalScore = (spatialScore + rotationScore) / 2f;

        // 3. 更新視覺與聽覺回饋 (就是你之前設定的 0.87Hz ~ 6.66Hz)
        if (audiVisaulFeedback) UpdateFeedback(finalScore);
    }

    void UpdateFeedback(float score)
    {
        // 顏色：紅(0) -> 綠(1)
        if (feedbackIndicator != null)
        {
            feedbackIndicator.material.color = Color.Lerp(Color.red, Color.green, score);
        }

        // 聲音：越正確(1.0) 頻率越低(1.0)，越錯誤(0.0) 頻率越高(3.0)
        currentScore = score;
    }
    void HandleAudioBeep()
    {
        if (!isExperienceMode || beepSource == null || beepClip == null) return;

        // 根據正確率計算播放間隔 (Interval)
        // 0% (score=0) -> 0.15s
        // 100% (score=1) -> 1.15s
        float targetInterval = Mathf.Lerp(0.15f, 1.15f, currentScore);

        beepTimer += Time.deltaTime;

        if (beepTimer >= targetInterval)
        {
            // 播放音效 (使用 PlayOneShot 避免音效被切斷)
            beepSource.PlayOneShot(beepClip);
            beepTimer = 0f; // 重置計時器
        }
    }
    void UpdateGhostPlayback()
    {
        // 1. 更新播放時間
        playbackTimer += Time.deltaTime;
        if (loopPlayback && playbackTimer > totalPathDuration)
        {
            playbackTimer = 0f; // 循環回到起點
        }

        float currentTime = Mathf.Clamp(playbackTimer, 0, totalPathDuration);
        float normTime = currentTime / totalPathDuration;

        // 2. 尋找對應時間點的 Pose 並更新模型
        SetGhostPose(normTime);
    }

    void SetGhostPose(float normTime)
    {
        // 這裡使用簡單的線性插值，讓動作看起來更平滑
        int index = 0;
        // 找到第一個比 normTime 大的點
        for (int i = 0; i < standardPath.Count - 1; i++)
        {
            if (standardPath[i + 1].normalizedTime > normTime)
            {
                index = i;
                break;
            }
        }

        MotionPoint p1 = standardPath[index];
        MotionPoint p2 = standardPath[index + 1];

        // 計算兩點間的局部比例
        float t = (normTime - p1.normalizedTime) / (p2.normalizedTime - p1.normalizedTime);

        // 插值位置與旋轉
        ghostModel.position = Vector3.Lerp(p1.position, p2.position, t);
        ghostModel.rotation = Quaternion.Slerp(p1.rotation, p2.rotation, t);
    }
    void HandleCoinGame()
    {
        if (!isExperienceMode || standardPath.Count == 0) return;

        // 根據播放進度（playbackTimer）來決定現在該在哪個點生金幣
        // 我們尋找 standardPath 中，最接近當前播放時間點的那個數據
        coinTimer += Time.deltaTime;

        if (coinTimer >= spawnInterval)
        {
            SpawnNextCoin();
            coinTimer = 0f;
        }
    }
    public void SetCoinGameValue(bool value) { coinGame = value; }
    public void SetAudiVisualValue(bool value) { audiVisaulFeedback = value; }
    void SpawnNextCoin()
    {
        // 取得幽靈手把當前的位置（因為幽靈手把已經幫我們做好了時間插值）
        GameObject newCoin = Instantiate(coinPrefab, ghostModel.position, ghostModel.rotation);
        Coin coinScript = newCoin.GetComponent<Coin>();
        coinScript.lifeSpan = coinLifeSpan; // 設定這顆金幣的消失時間
        coinScript.manager = this;
    }
    public void OnCoinCollected(Vector3 pos)
    {
        // 播放音效
        if (collectSound) collectSound.PlayOneShot(collectSound.clip);

        // 在金幣位置播放粒子特效
        if (effect)
        {
            var temp = Instantiate(effect, pos, quaternion.identity);
            temp.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
            Destroy(temp, 5f);
        }

        // 增加分數或顯示視覺 UI
        Debug.Log("吃到金幣！得分！");
    }
    public void SetCoinLifeSpan(float value) { coinLifeSpan = value; }

}
