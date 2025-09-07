using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI 引用")]
    public Image icon;       // 物品图标
    public TextMeshProUGUI quantityText; // 物品数量文本
    public GameObject highlightPanel; // 选中高亮面板 (可选)

    // 新增冷却UI引用
    [Header("冷却UI")]
    public Image cooldownOverlay; // 用于显示冷却进度的Image (Type: Filled)
    public TextMeshProUGUI cooldownText; // 用于显示冷却倒计时的文本

    // 物品数据
    [HideInInspector] public InventoryItem currentItem; // 当前槽位持有的物品

    private CanvasGroup canvasGroup; // 用于拖拽时的透明度控制
    private InventoryUI inventoryUI;

    // 存储当前正在被拖拽的 DragIcon 实例
    private DragIconUI _currentDragIconInstance;

    // Inventory 脚本的引用
    private Inventory inventory;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        inventoryUI = FindObjectOfType<InventoryUI>();
        if (inventoryUI == null)
        {
            Debug.LogError("InventorySlotUI: 未找到 InventoryUI 脚本！请确保场景中存在InventoryUI。", this);
            enabled = false;
            return;
        }

        inventory = FindObjectOfType<Inventory>();
        if (inventory == null)
        {
            Debug.LogError("InventorySlotUI: 未找到 Inventory 脚本！请确保场景中存在Inventory。", this);
            enabled = false;
            return;
        }
    }

    void Update()
    {
        if (currentItem != null && currentItem.IsOnCooldown())
        {
            float remaining = currentItem.GetRemainingCooldown();
            if (cooldownOverlay != null)
            {
                cooldownOverlay.fillAmount = remaining / currentItem.itemData.cooldownTime;
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
            if (cooldownOverlay != null) cooldownOverlay.enabled = false;
            if (cooldownText != null) cooldownText.enabled = false;
        }
    }

    /// <summary>
    /// 设置槽位的物品图标和数量。
    /// </summary>
    public void SetItem(InventoryItem item)
    {
        currentItem = item;
        if (currentItem != null && currentItem.itemData != null)
        {
            if (icon != null)
            {
                icon.sprite = currentItem.itemData.icon;
                icon.color = Color.white;
                icon.enabled = true;
            }
            if (quantityText != null)
            {
                if (currentItem.quantity > 1)
                {
                    quantityText.text = currentItem.quantity.ToString();
                    quantityText.enabled = true;
                }
                else
                {
                    quantityText.text = "";
                    quantityText.enabled = false;
                }
            }
            Update();
        }
        else
        {
            ClearSlot();
        }
    }

    /// <summary>
    /// 清空槽位显示。
    /// </summary>
    public void ClearSlot()
    {
        currentItem = null;
        if (icon != null) icon.enabled = false;
        if (quantityText != null) quantityText.enabled = false;
        if (highlightPanel != null) highlightPanel.SetActive(false);
        if (cooldownOverlay != null) cooldownOverlay.enabled = false;
        if (cooldownText != null) cooldownText.enabled = false;
    }

    /// <summary>
    /// 设置高亮显示。
    /// </summary>
    public void SetHighlight(bool isHighlighted)
    {
        if (highlightPanel != null)
        {
            highlightPanel.SetActive(isHighlighted);
        }
    }

    // --- 拖拽事件处理 ---

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem == null || currentItem.itemData == null) return;
        TooltipManager.Instance.HideTooltip();
        _currentDragIconInstance = Instantiate(inventoryUI.dragIconPrefab, inventoryUI.GetComponentInParent<Canvas>().transform).GetComponent<DragIconUI>();
        _currentDragIconInstance.SetIconAndQuantity(currentItem);
        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
        Debug.Log("开始拖拽物品: " + currentItem.itemData.itemName);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_currentDragIconInstance != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)inventoryUI.GetComponentInParent<Canvas>().transform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint);
            ((RectTransform)_currentDragIconInstance.transform).anchoredPosition = localPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_currentDragIconInstance != null)
        {
            Destroy(_currentDragIconInstance.gameObject);
            _currentDragIconInstance = null;
        }



        // ⭐ 关键修改：移除此行，不主动复原源槽位的图标 ⭐
        // 物品状态的改变由 OnDrop 方法处理
        // SetItem(currentItem);

        // 恢复 CanvasGroup 的 Blocks Raycasts 为 true
        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
        Debug.Log("拖拽结束。");
    }

    // --- 拖放事件处理 (修改以处理多来源) ---

    public void OnDrop(PointerEventData eventData)
    {
        InventorySlotUI draggedInventorySlot = eventData.pointerDrag.GetComponent<InventorySlotUI>();
        HotkeySlotUI draggedHotkeySlot = eventData.pointerDrag.GetComponent<HotkeySlotUI>();

        // 情况1: 从背包拖到背包
        if (draggedInventorySlot != null && draggedInventorySlot.currentItem != null)
        {
            if (draggedInventorySlot == this) return;

            // 交换背包中的物品引用
            int fromIndex = inventory.inventoryItems.IndexOf(draggedInventorySlot.currentItem);
            int toIndex = inventory.inventoryItems.IndexOf(currentItem);

            if (fromIndex >= 0 && toIndex >= 0)
            {
                (inventory.inventoryItems[fromIndex], inventory.inventoryItems[toIndex]) =
                    (inventory.inventoryItems[toIndex], inventory.inventoryItems[fromIndex]);
            }

            // 更新UI显示
            InventoryItem tempItem = currentItem;
            SetItem(draggedInventorySlot.currentItem);
            draggedInventorySlot.SetItem(tempItem);
        }
        // 情况2: 从快捷栏拖到背包
        else if (draggedHotkeySlot != null && draggedHotkeySlot.assignedItem != null)
        {
            // 添加到背包
            inventory.inventoryItems.Add(draggedHotkeySlot.assignedItem);
            SetItem(draggedHotkeySlot.assignedItem);
            draggedHotkeySlot.ClearSlot();
        }

        inventory.NotifyInventoryContentChanged();
    }

    // --- 鼠标悬停事件处理 ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentItem != null && currentItem.itemData != null)
        {
            if (!eventData.dragging)
            {
                TooltipManager.Instance.ShowTooltip(currentItem.itemData);
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Instance.HideTooltip();
    }
}
