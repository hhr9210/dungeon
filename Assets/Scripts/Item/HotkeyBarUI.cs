using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 管理快捷键栏UI和物品使用逻辑。
/// </summary>
public class HotkeyBarUI : MonoBehaviour
{
    [Header("UI 设置")]
    public GameObject hotkeyBarPanel; 
    public List<HotkeySlotUI> hotkeySlots = new List<HotkeySlotUI>(); // 热键槽位列表

    [Header("按键设置")]
    public KeyCode[] hotkeys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4 }; // 快捷键列表

    private Inventory inventory;

    void Awake()
    {
        inventory = FindObjectOfType<Inventory>();
        if (inventory == null)
        {
            Debug.LogError("HotkeyBarUI: 未找到 Inventory 脚本。", this);
            enabled = false;
            return;
        }

        if (hotkeys.Length != hotkeySlots.Count)
        {
            Debug.LogError("HotkeyBarUI: 热键数量与槽位数量不匹配！请检查Inspector。", this);
            enabled = false;
        }
    }

    void Update()
    {
        for (int i = 0; i < hotkeys.Length; i++)
        {
            if (Input.GetKeyDown(hotkeys[i]))
            {
                if (i < hotkeySlots.Count && hotkeySlots[i] != null)
                {
                    hotkeySlots[i].UseAssignedItem(); // 使用指定槽位的物品
                }
            }
        }
    }
}
