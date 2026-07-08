using UnityEngine;
using UnityEngine.EventSystems;

public class UIResizable : MonoBehaviour, IDragHandler, IBeginDragHandler
{
    [Tooltip("要被縮放的目標，通常是父節點的視窗")]
    public RectTransform windowToResize, textureHolder;

    [Tooltip("限制視窗的最小尺寸")]
    public Vector2 minSize = new Vector2(200f, 150f);

    [Tooltip("限制視窗的最大尺寸")]
    public Vector2 maxSize = new Vector2(1200f, 900f);

    [Tooltip("是否強制保持原始長寬比，防止 RenderTexture 變形")]
    public bool keepAspectRatio = true;

    private Canvas canvas;
    private float aspectRatio = 1f; // 用來儲存長寬比

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        if (windowToResize == null)
        {
            windowToResize = transform.parent.GetComponent<RectTransform>();
        }

        // 在遊戲開始時，自動讀取這個視窗一開始的長寬比例 (寬 ÷ 高)
        if (windowToResize != null && windowToResize.rect.height != 0)
        {
            aspectRatio = windowToResize.rect.width / windowToResize.rect.height;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 縮放時也將視窗移到最上層
        windowToResize.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (windowToResize == null) return;

        float deltaWidth = eventData.delta.x / canvas.scaleFactor;
        float deltaHeight = -eventData.delta.y / canvas.scaleFactor;

        Vector2 newSize;

        if (keepAspectRatio)
        {
            // 【等比例縮放邏輯】
            // 我們統一以滑鼠「X軸」的移動量來決定整體大小，Y軸大小由比例推算
            float newWidth = windowToResize.sizeDelta.x + deltaWidth;

            // 限制寬度
            newWidth = Mathf.Clamp(newWidth, minSize.x, maxSize.x);

            // 根據寬度與長寬比，強制算出對應的高度
            float newHeight = newWidth / aspectRatio;

            // 確保推算出來的高度也沒有超出限制（雙重防護）
            if (newHeight > maxSize.y || newHeight < minSize.y)
            {
                newHeight = Mathf.Clamp(newHeight, minSize.y, maxSize.y);
                newWidth = newHeight * aspectRatio; // 反推回安全的寬度
            }

            newSize = new Vector2(newWidth, newHeight);
        }
        else
        {
            // 【原本的自由縮放邏輯】
            newSize = new Vector2(
                windowToResize.sizeDelta.x + deltaWidth,
                windowToResize.sizeDelta.y + deltaHeight
            );
            newSize.x = Mathf.Clamp(newSize.x, minSize.x, maxSize.x);
            newSize.y = Mathf.Clamp(newSize.y, minSize.y, maxSize.y);
        }

        windowToResize.sizeDelta = newSize;
        textureHolder.sizeDelta = newSize;
    }
}