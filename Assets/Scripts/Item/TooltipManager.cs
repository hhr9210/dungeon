using UnityEngine;
using TMPro;

public class TooltipManager : MonoBehaviour
{
    // 单例模式
    public static TooltipManager Instance { get; private set; }

    public GameObject tooltipPanel;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescriptionText;
    // 更多信息文本

    private Canvas tooltipCanvas;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        tooltipCanvas = tooltipPanel.GetComponentInParent<Canvas>();
        if (tooltipCanvas == null)
        {
            Debug.LogError("TooltipPanel 必须在 Canvas 下方！");
            enabled = false;
        }

        HideTooltip(); 
    }

    public void ShowTooltip(Item itemData)
    {
        if (itemData == null)
        {
            HideTooltip();
            return;
        }

        if (itemNameText != null)
        {
            itemNameText.text = itemData.itemName;
        }

        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = itemData.description;
        }

        tooltipPanel.SetActive(true);
        UpdateTooltipPosition(); // 显示时更新位置
    }

    public void HideTooltip()
    {
        tooltipPanel.SetActive(false);
    }

    private void Update()
    {
        if (tooltipPanel.activeSelf)
        {
            UpdateTooltipPosition(); // 每一帧更新位置以跟随鼠标
        }
    }

    private void UpdateTooltipPosition()
    {
        if (tooltipPanel != null && tooltipCanvas != null)
        {
            Vector3 mousePositionScreen = Input.mousePosition;

            // 将屏幕坐标转换为本地坐标
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)tooltipCanvas.transform,
                mousePositionScreen,
                tooltipCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : tooltipCanvas.worldCamera,
                out Vector2 localMousePosition
            );

            // 调整位置以避免UI遮挡
            ((RectTransform)tooltipPanel.transform).anchoredPosition = localMousePosition + new Vector2(110f, -40f);
        }
    }
}
