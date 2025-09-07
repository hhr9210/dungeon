using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

/// <summary>
/// 热键槽位UI的逻辑控制。处理物品的分配、使用、冷却显示以及拖拽交互。
/// </summary>
public class HotkeySlotUI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI 引用")]
    [Tooltip("此槽位中物品的图标。")]
    public Image icon;
    [Tooltip("此槽位中物品的数量文本。")]
    public TextMeshProUGUI quantityText;

    [Header("冷却UI")]
    [Tooltip("用于显示冷却进度的Image（类型：Filled）。")]
    public Image cooldownOverlay;
    [Tooltip("用于显示冷却倒计时的文本。")]
    public TextMeshProUGUI cooldownText;

    [HideInInspector] public InventoryItem assignedItem;

    [Header("槽位类型设置")]
    [Tooltip("勾选此项表示这是一个装备槽位。装备槽位只接受指定类型的装备，且不接受通用物品。")]
    public bool isEquipmentSlot = false;

    [Tooltip("此槽位允许的物品类型。如果 isEquipmentSlot 为 true，则此类型必须是 head, weapon 等装备类型。如果 isEquipmentSlot 为 false，则此类型应为 Generic。")]
    public ItemType allowedItemType = ItemType.Generic; // 默认值保持为 Generic

    private Inventory inventory;
    private CanvasGroup canvasGroup;
    private DragIconUI _currentDragIconInstance;
    private InventoryUI inventoryUI;
    private bool shouldClearSlotAfterDrag = false;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // 查找并引用场景中的Inventory脚本
        inventory = FindObjectOfType<Inventory>();
        if (inventory == null)
        {
            Debug.LogError("HotkeySlotUI: 未找到 Inventory 脚本！", this);
            enabled = false;
            return;
        }

        // 查找并引用场景中的InventoryUI脚本
        inventoryUI = FindObjectOfType<InventoryUI>();
        if (inventoryUI == null)
        {
            Debug.LogError("HotkeySlotUI: 未找到 InventoryUI 脚本！请确保场景中存在InventoryUI。", this);
            enabled = false;
            return;
        }

        // 确保设置正确：如果设置为装备槽位，allowedItemType不能是Generic
        if (isEquipmentSlot && allowedItemType == ItemType.Generic)
        {
            Debug.LogWarning($"槽位 {gameObject.name} 被标记为装备槽位，但其 'Allowed Item Type' 仍为 'Generic'。这可能导致非预期行为。请将其设置为具体的装备类型 (head, weapon等)。", this);
        }
        // 确保设置正确：如果不是装备槽位，allowedItemType应该为Generic
        if (!isEquipmentSlot && allowedItemType != ItemType.Generic)
        {
            Debug.LogWarning($"槽位 {gameObject.name} 未被标记为装备槽位，但其 'Allowed Item Type' 不是 'Generic'。普通热键栏通常只接受通用物品。这可能导致非预期行为。", this);
        }
    }

    /// <summary>
    /// Update函数用于实时更新冷却UI。
    /// </summary>
    void Update()
    {
        // 检查是否有物品，并且物品是否处于冷却中
        if (assignedItem != null && assignedItem.IsOnCooldown())
        {
            float remaining = assignedItem.GetRemainingCooldown();
            if (cooldownOverlay != null)
            {
                cooldownOverlay.fillAmount = remaining / assignedItem.itemData.cooldownTime;
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
            // 如果不在冷却中，则隐藏冷却UI
            if (cooldownOverlay != null) cooldownOverlay.enabled = false;
            if (cooldownText != null) cooldownText.enabled = false;
        }
    }

    /// <summary>
    /// 将一个物品分配给此槽位，并更新UI显示。
    /// </summary>
    /// <param name="item">要分配的物品。</param>
    // 在 HotkeySlotUI 中
    public void AssignItem(InventoryItem item)
    {
        assignedItem = item; // 直接引用，不创建新实例

        if (assignedItem != null && assignedItem.itemData != null)
        {
            icon.sprite = assignedItem.itemData.icon;
            icon.color = Color.white;
            icon.enabled = true;

            quantityText.text = assignedItem.quantity > 1 ? assignedItem.quantity.ToString() : "";
            quantityText.enabled = assignedItem.quantity > 1;
        }
        else
        {
            ClearSlot();
        }

        Update(); // 更新冷却状态
    }

    /// <summary>
    /// 清空此槽位的物品及UI显示。
    /// </summary>
    public void ClearSlot()
    {
        assignedItem = null;
        if (icon != null)
        {
            icon.sprite = null;
            icon.enabled = false; // 确保图标被禁用
        }
        if (quantityText != null)
        {
            quantityText.text = "";
            quantityText.enabled = false;
        }
        if (cooldownOverlay != null) cooldownOverlay.enabled = false;
        if (cooldownText != null) cooldownText.enabled = false;
    }

    /// <summary>
    /// 尝试使用当前槽位分配的物品。装备槽位不会通过此方法使用。
    /// </summary>
    public void UseAssignedItem()
    {
        if (isEquipmentSlot) return; // 装备槽位不通过快捷键使用

        if (assignedItem == null || assignedItem.itemData == null) return;
        if (assignedItem.IsOnCooldown()) return;

        Debug.Log($"使用快捷栏物品: {assignedItem.itemData.itemName}");

        // 使用物品效果
        assignedItem.Use();

        // 如果是消耗品，只减少1个数量
        if (assignedItem.itemData.itemType == ItemType.Consumable)
        {
            assignedItem.quantity--;

            // 更新背包中的数量
            Inventory.Instance.RemoveItem(assignedItem.itemData, 1);

            // 更新快捷栏显示
            if (assignedItem.quantity <= 0)
            {
                ClearSlot();
            }
            else
            {
                quantityText.text = assignedItem.quantity > 1 ? assignedItem.quantity.ToString() : "";
                quantityText.enabled = assignedItem.quantity > 1;
            }
        }

        // 更新冷却显示
        Update();
    }

    /// <summary>
    /// 当开始拖拽此槽位时调用。
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (assignedItem == null || assignedItem.itemData == null) return;

        TooltipManager.Instance.HideTooltip();

        // 实例化一个拖拽图标并设置其内容
        _currentDragIconInstance = Instantiate(inventoryUI.dragIconPrefab, inventoryUI.GetComponentInParent<Canvas>().transform).GetComponent<DragIconUI>();
        _currentDragIconInstance.SetIconAndQuantity(assignedItem);

        // 拖拽时，禁用射线检测，让下方的槽位能够接收OnDrop事件
        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;

        shouldClearSlotAfterDrag = false; // 默认不清除，除非在 OnDrop 中设置
        Debug.Log("开始从快捷栏拖拽物品: " + assignedItem.itemData.itemName, this);
    }

    /// <summary>
    /// 拖拽过程中持续调用。
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (_currentDragIconInstance != null)
        {
            // 将拖拽图标的位置更新为鼠标位置
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)inventoryUI.GetComponentInParent<Canvas>().transform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint);

            ((RectTransform)_currentDragIconInstance.transform).anchoredPosition = localPoint;
        }
    }

    /// <summary>
    /// 拖拽结束时调用。
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        // 销毁拖拽图标
        if (_currentDragIconInstance != null)
        {
            Destroy(_currentDragIconInstance.gameObject);
            _currentDragIconInstance = null;
        }

        // 根据OnDrop中的判断，决定是否清除此槽位的物品
        if (shouldClearSlotAfterDrag)
        {
            ClearSlot();
            shouldClearSlotAfterDrag = false;
        }
        else
        {
            // 恢复图标显示
            if (assignedItem != null && assignedItem.itemData != null)
            {
                if (icon != null) icon.enabled = true;
                if (quantityText != null) quantityText.enabled = assignedItem.quantity > 1;
                Update();
            }
        }

        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;



        Debug.Log("快捷栏拖拽结束。", this);
    }


    /// <summary>
    /// 当有可拖拽物品在此槽位上释放时调用。
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {

        InventorySlotUI draggedInventorySlot = eventData.pointerDrag.GetComponent<InventorySlotUI>();
        HotkeySlotUI draggedHotkeySlot = eventData.pointerDrag.GetComponent<HotkeySlotUI>();


        // 获取源物品（直接使用引用，不创建新实例）
        InventoryItem sourceItem = draggedInventorySlot?.currentItem ?? draggedHotkeySlot?.assignedItem;

        if (sourceItem == null || sourceItem.itemData == null)
        {
            if (draggedHotkeySlot != null) draggedHotkeySlot.shouldClearSlotAfterDrag = true;
            ClearSlot();
            return;
        }

        // 类型验证保持不变
        if (isEquipmentSlot && sourceItem.itemData.itemType != allowedItemType)
        {
            Debug.LogWarning($"装备槽位类型不匹配: {allowedItemType}", this);
            return;
        }
        else if (!isEquipmentSlot && sourceItem.itemData.itemType != ItemType.Generic)
        {
            Debug.LogWarning("普通快捷栏只能放置通用物品", this);
            return;
        }

        // 处理从背包拖到快捷栏
        if (draggedInventorySlot != null)
        {
            // 保存当前快捷栏的物品（可能为null）
            InventoryItem oldItem = assignedItem;

            // 直接引用背包中的物品
            AssignItem(sourceItem);

            if (oldItem != null)
            {
                // 如果快捷栏原有物品，放回背包
                inventory.inventoryItems.Add(oldItem);
                draggedInventorySlot.SetItem(oldItem);
            }
            else
            {
                // 清空源槽位
                draggedInventorySlot.ClearSlot();
            }

            // 关键修改：从背包移除被拖拽的物品
            inventory.inventoryItems.Remove(sourceItem);
        }
        // 处理快捷栏之间的交换
        else if (draggedHotkeySlot != null && draggedHotkeySlot != this)
        {
            InventoryItem temp = assignedItem;
            AssignItem(sourceItem);
            draggedHotkeySlot.AssignItem(temp);
        }

        inventory.NotifyInventoryContentChanged();
    }

    /// <summary>
    /// 鼠标进入此槽位时调用，显示提示。
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (assignedItem != null && assignedItem.itemData != null)
        {
            if (!eventData.dragging)
            {
                TooltipManager.Instance.ShowTooltip(assignedItem.itemData);
            }
        }
    }

    /// <summary>
    /// 鼠标离开此槽位时调用，隐藏提示。
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Instance.HideTooltip();
    }
}