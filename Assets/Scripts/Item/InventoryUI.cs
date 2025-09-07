using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// 管理背包UI的显示和交互。
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("UI 引用")]
    public GameObject inventoryPanel; // 背包UI面板
    public Transform inventorySlotsParent; // 物品槽位父对象
    public GameObject inventorySlotPrefab; // 物品槽位预制体
    public GameObject dragIconPrefab; // 拖拽图标预制体

    private Inventory inventory;
    private List<InventorySlotUI> currentInventorySlots = new List<InventorySlotUI>();

    void Awake()
    {
        inventory = FindObjectOfType<Inventory>();
        if (inventory == null)
        {
            Debug.LogError("InventoryUI: 未找到 Inventory 脚本！请确保玩家GameObject上挂载了Inventory脚本。", this);
            enabled = false;
            return;
        }

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false); // 确保背包UI在游戏开始时是隐藏的
        }

        InitializeInventorySlots();
    }

    void OnEnable()
    {
        Inventory.OnInventoryContentChanged += UpdateInventoryUI;
    }

    void OnDisable()
    {
        Inventory.OnInventoryContentChanged -= UpdateInventoryUI;
    }

    void Update()
    {
        // 按下 Tab 键时切换背包UI显示
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInventoryUI();
        }
    }

    /// <summary>
    /// 初始化背包槽位。
    /// </summary>
    private void InitializeInventorySlots()
    {
        if (inventorySlotsParent == null || inventorySlotPrefab == null)
        {
            Debug.LogError("InventoryUI: 未设置 inventorySlotsParent 或 inventorySlotPrefab！", this);
            return;
        }

        // 清除现有槽位
        foreach (Transform child in inventorySlotsParent)
        {
            Destroy(child.gameObject);
        }
        currentInventorySlots.Clear();

        // 根据背包容量创建UI槽位
        for (int i = 0; i < inventory.inventoryCapacity; i++)
        {
            GameObject slotObj = Instantiate(inventorySlotPrefab, inventorySlotsParent);
            InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();

            if (slotUI != null)
            {
                currentInventorySlots.Add(slotUI);
                slotUI.ClearSlot(); // 确保初始槽位是空的
            }
            else
            {
                Debug.LogError($"InventoryUI: 物品槽位预制体 '{inventorySlotPrefab.name}' 未找到 InventorySlotUI 组件！", inventorySlotPrefab);
            }
        }
    }

    /// <summary>
    /// 切换背包UI显示状态并暂停/恢复游戏。
    /// </summary>
    public void ToggleInventoryUI()
    {
        if (inventoryPanel != null)
        {
            bool willBeActive = !inventoryPanel.activeSelf;
            inventoryPanel.SetActive(willBeActive);

            // 通知游戏暂停/恢复
            if (PauseManager.Instance != null)
            {
                PauseManager.Instance.SetGamePaused(willBeActive);
            }
            else
            {
                Debug.LogWarning("InventoryUI: 场景中没有找到 PauseManager 实例！暂停/恢复功能可能无法正常工作。", this);
            }

            if (willBeActive)
            {
                UpdateInventoryUI();
            }
        }
    }

    /// <summary>
    /// 根据数据更新UI内容。
    /// </summary>
    public void UpdateInventoryUI()
    {
        if (inventory == null || currentInventorySlots.Count == 0) return;

        for (int i = 0; i < currentInventorySlots.Count; i++)
        {
            if (i < inventory.inventoryItems.Count && inventory.inventoryItems[i] != null)
            {
                currentInventorySlots[i].SetItem(inventory.inventoryItems[i]);
            }
            else
            {
                currentInventorySlots[i].ClearSlot();
            }
        }
    }

    // 通过公共方法控制背包UI
    public void OpenInventory()
    {
        if (!inventoryPanel.activeSelf)
        {
            ToggleInventoryUI();
        }
    }

    public void CloseInventory()
    {
        if (inventoryPanel.activeSelf)
        {
            ToggleInventoryUI();
        }
    }
}
