using UnityEngine;
using UnityEngine.EventSystems;

public class UIDraggable : MonoBehaviour, IDragHandler, IBeginDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;
    private RectTransform canvasRectTransform; // 新增：用來取得 Canvas 的邊界

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        // 取得最上層 Canvas 的 RectTransform
        if (canvas != null)
        {
            canvasRectTransform = canvas.GetComponent<RectTransform>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 拖曳時移到最上層
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 1. 先處理滑鼠位移
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;

        // 2. 處理完位移後，檢查是否超出邊界並強制拉回
        ClampToScreen();
    }

    // 核心邏輯：限制視窗不超出螢幕
    private void ClampToScreen()
    {
        if (canvasRectTransform == null) return;

        // 取得 Canvas 的四個角落座標
        Vector3[] canvasCorners = new Vector3[4];
        canvasRectTransform.GetWorldCorners(canvasCorners);

        // 取得視窗的四個角落座標
        Vector3[] uiCorners = new Vector3[4];
        rectTransform.GetWorldCorners(uiCorners);

        /* * GetWorldCorners 的陣列順序固定為：
         * [0]: 左下角 (Bottom-Left)
         * [1]: 左上角 (Top-Left)
         * [2]: 右上角 (Top-Right)
         * [3]: 右下角 (Bottom-Right)
         */

        Vector3 currentPos = rectTransform.position;

        // --- 水平方向限制 ---
        // 檢查左邊界 (視窗左下角的 X < Canvas左下角的 X)
        if (uiCorners[0].x < canvasCorners[0].x)
        {
            currentPos.x += canvasCorners[0].x - uiCorners[0].x; // 補回差值
        }
        // 檢查右邊界 (視窗右上角的 X > Canvas右上角的 X)
        else if (uiCorners[2].x > canvasCorners[2].x)
        {
            currentPos.x -= uiCorners[2].x - canvasCorners[2].x; // 扣除差值
        }

        // --- 垂直方向限制 ---
        // 檢查下邊界 (視窗左下角的 Y < Canvas左下角的 Y)
        if (uiCorners[0].y < canvasCorners[0].y)
        {
            currentPos.y += canvasCorners[0].y - uiCorners[0].y;
        }
        // 檢查上邊界 (視窗右上角的 Y > Canvas右上角的 Y)
        else if (uiCorners[2].y > canvasCorners[2].y)
        {
            currentPos.y -= uiCorners[2].y - canvasCorners[2].y;
        }

        // 將修正後、保證在螢幕內的座標套用回去
        rectTransform.position = currentPos;
    }
}