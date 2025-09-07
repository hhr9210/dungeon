using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class DragIconUI : MonoBehaviour // 移除 IDragHandler，因为 OnDrag 逻辑已移到 InventorySlotUI
{
    public Image iconImage;
    public TextMeshProUGUI quantityText;

    // <<< 新增冷却UI引用 >>>
    [Header("冷却UI")]
    public Image cooldownOverlay; // 用于显示冷却进度的Image (Type: Filled)
    public TextMeshProUGUI cooldownText; // 用于显示冷却倒计时的文本

    [HideInInspector] public InventoryItem draggedItem;

    private Canvas canvas;
    private RectTransform rectTransform;

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("DragIconUI: 找不到父级Canvas！拖拽图标可能无法正确显示。", this);
        }
        rectTransform = GetComponent<RectTransform>();

        // 确保数量文本、图标和冷却UI一开始是隐藏的
        if (iconImage != null) iconImage.enabled = false;
        if (quantityText != null) quantityText.enabled = false;
        if (cooldownOverlay != null) cooldownOverlay.enabled = false;
        if (cooldownText != null) cooldownText.enabled = false;
    }

    /// <summary>
    /// 设置拖拽图标的显示内容（图标、数量和冷却）。
    /// </summary>
    public void SetIconAndQuantity(InventoryItem item)
    {
        draggedItem = item;
        if (item != null && item.itemData != null)
        {
            if (iconImage != null)
            {
                iconImage.sprite = item.itemData.icon;
                iconImage.enabled = true;
                iconImage.color = Color.white;
            }
            else
            {
                Debug.LogWarning("DragIconUI: iconImage 未赋值！");
            }

            if (quantityText != null)
            {
                if (item.quantity > 1)
                {
                    quantityText.text = item.quantity.ToString();
                    quantityText.enabled = true;
                }
                else
                {
                    quantityText.text = "";
                    quantityText.enabled = false;
                }
            }
            else
            {
                Debug.LogWarning("DragIconUI: quantityText 未赋值！");
            }

            // <<< 根据物品的冷却状态设置拖拽图标的冷却UI >>>
            if (item.IsOnCooldown())
            {
                float remaining = item.GetRemainingCooldown();
                if (cooldownOverlay != null)
                {
                    cooldownOverlay.fillAmount = remaining / item.itemData.cooldownTime;
                    cooldownOverlay.enabled = true;
                }
                if (cooldownText != null)
                {
                    cooldownText.text = Mathf.CeilToInt(remaining).ToString();
                    cooldownText.enabled = true;
                }
            }
            else
            {
                // 不在冷却中则隐藏冷却UI
                if (cooldownOverlay != null) cooldownOverlay.enabled = false;
                if (cooldownText != null) cooldownText.enabled = false;
            }
        }
        else
        {
            // 如果物品为空，则清空所有显示
            if (iconImage != null) iconImage.enabled = false;
            if (quantityText != null) quantityText.enabled = false;
            if (cooldownOverlay != null) cooldownOverlay.enabled = false;
            if (cooldownText != null) cooldownText.enabled = false;
        }
    }
}