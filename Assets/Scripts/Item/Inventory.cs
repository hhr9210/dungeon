using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using TMPro;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;

    [Header("玩家金钱设置")]
    [Tooltip("玩家当前的金钱数量。")]
    public float currentMoney = 500f;
    [Tooltip("金钱UI")]
    public TextMeshProUGUI moneyText;
    public static event Action<float> OnMoneyChanged;

    [Header("背包设置")]
    [Tooltip("背包的最大容量。")]
    public int inventoryCapacity = 20;
    [Tooltip("当前玩家背包中的物品列表。")]
    public List<InventoryItem> inventoryItems = new List<InventoryItem>();
    public static event Action OnInventoryContentChanged;

    // 玩家属性脚本引用，用于装备属性加成
    [Header("玩家属性引用")]
    [Tooltip("请将玩家身上的 PlayerHealth 脚本拖拽到此处。")]
    public PlayerHealth playerHealth;
    [Tooltip("请将玩家身上的 PlayerAttack 脚本拖拽到此处。")]
    public PlayerAttack playerAttack;
    [Tooltip("请将玩家身上的 PlayerController 脚本拖拽到此处。")]
    public PlayerController playerController;
    [Tooltip("请将玩家身上的 PlayerMana 脚本拖拽到此处。")]
    public PlayerMana playerMana;

    // 装备槽位，用于跟踪当前穿戴的装备
    [Header("装备槽位")]
    public InventoryItem headSlot;
    public InventoryItem weaponSlot;
    public InventoryItem chestSlot;
    public InventoryItem legSlot;
    public InventoryItem footSlot;
    public InventoryItem wandSlot;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        UpdateMoneyUI();

        foreach (var item in inventoryItems)
        {
            if (item != null)
            {
                item.InitializeCooldown();
            }
        }
    }

    /// <summary>
    /// 当背包内容改变时，通知 UI 进行更新。
    /// </summary>
    public void NotifyInventoryContentChanged()
    {
        OnInventoryContentChanged?.Invoke();
    }

    /// <summary>
    /// 增加玩家的金钱。
    /// </summary>
    public void AddMoney(float amount)
    {
        currentMoney += amount;
        UpdateMoneyUI();
    }

    /// <summary>
    /// 减少玩家的金钱。
    /// </summary>
    public bool RemoveMoney(float amount)
    {
        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            UpdateMoneyUI();
            return true;
        }
        Debug.LogWarning("金钱不足！无法支付。");
        return false;
    }

    //  SpendMoney 方法
    /// <summary>
    /// 减少玩家的金钱，功能等同于 RemoveMoney。
    /// </summary>
    public bool SpendMoney(float amount)
    {
        return RemoveMoney(amount);
    }

    /// <summary>
    /// 更新金钱UI。
    /// </summary>
    void UpdateMoneyUI()
    {
        if (moneyText != null)
        {
            moneyText.text = currentMoney.ToString("F2");
        }
        OnMoneyChanged?.Invoke(currentMoney);
    }

    /// <summary>
    /// 检查玩家是否有足够的金钱。
    /// </summary>
    public bool CanAfford(float amount)
    {
        return currentMoney >= amount;
    }

    //  HasFreeSpace 方法
    /// <summary>
    /// 检查背包是否还有空位。
    /// </summary>
    public bool HasFreeSpace()
    {
        return inventoryItems.Count < inventoryCapacity;
    }

    /// <summary>
    /// 添加一个物品到背包。
    /// </summary>
    public bool AddItem(Item item, int quantity = 1)
    {
        if (item == null) return false;

        if (!HasFreeSpace() && !inventoryItems.Exists(i => i.itemData == item))
        {
            Debug.LogWarning("背包已满，无法添加新物品！");
            return false;
        }

        InventoryItem existingItem = inventoryItems.Find(x => x.itemData == item);
        if (existingItem != null)
        {
            existingItem.quantity += quantity;
        }
        else
        {
            inventoryItems.Add(new InventoryItem(item, quantity));
        }

        NotifyInventoryContentChanged();
        return true;
    }

    /// <summary>
    /// 从背包移除一个物品。
    /// </summary>
    public bool RemoveItem(Item itemToRemove, int quantity = 1)
    {
        InventoryItem existingItem = inventoryItems.Find(x => x.itemData == itemToRemove);
        if (existingItem != null)
        {
            existingItem.quantity -= quantity;
            if (existingItem.quantity <= 0)
            {
                inventoryItems.Remove(existingItem);
            }
            NotifyInventoryContentChanged();
            return true;
        }
        Debug.LogWarning($"Inventory: 背包中没有找到物品 {itemToRemove.itemName} 或数量不足。");
        return false;
    }

    /// <summary>
    /// 获取背包中某个物品的数量。
    /// </summary>
    public int GetItemQuantity(Item item)
    {
        InventoryItem existingItem = inventoryItems.Find(x => x.itemData == item);
        return existingItem != null ? existingItem.quantity : 0;
    }

    /// <summary>
    /// 使用背包中的某个物品。
    /// </summary>
    public void UseItem(Item itemToUse)
    {
        if (itemToUse == null) return;

        InventoryItem foundItem = inventoryItems.Find(x => x.itemData == itemToUse);

        if (foundItem != null)
        {
            foundItem.Use();
            if (itemToUse.itemType == ItemType.Consumable)
            {
                RemoveItem(itemToUse);
            }
        }
    }

    //  穿戴装备
    public void EquipItem(Item itemToEquip, ItemType slotType)
    {
        InventoryItem oldItem = GetEquippedItem(slotType);
        if (oldItem != null)
        {
            RemoveBonuses(oldItem.itemData);
        }

        SetEquippedItem(itemToEquip, slotType);

        if (itemToEquip != null)
        {
            ApplyBonuses(itemToEquip);
            Debug.Log($"Inventory: 装备 {itemToEquip.itemName} 已穿戴，属性加成已应用。");
        }
    }

    //  脱下装备
    public void UnequipItem(ItemType slotType)
    {
        InventoryItem equippedItem = GetEquippedItem(slotType);
        if (equippedItem != null)
        {
            RemoveBonuses(equippedItem.itemData);
            SetEquippedItem(null, slotType);
            Debug.Log($"Inventory: 装备 {equippedItem.itemData.itemName} 已脱下，属性加成已移除。");
        }
    }

    private InventoryItem GetEquippedItem(ItemType slotType)
    {
        switch (slotType)
        {
            case ItemType.Head: return headSlot;
            case ItemType.Weapon: return weaponSlot;
            case ItemType.Chest: return chestSlot;
            case ItemType.Leg: return legSlot;
            case ItemType.Foot: return footSlot;
            case ItemType.Wand: return wandSlot;
        }
        return null;
    }

    private void SetEquippedItem(Item item, ItemType slotType)
    {
        InventoryItem newItem = item != null ? new InventoryItem(item, 1) : null;
        switch (slotType)
        {
            case ItemType.Head: headSlot = newItem; break;
            case ItemType.Weapon: weaponSlot = newItem; break;
            case ItemType.Chest: chestSlot = newItem; break;
            case ItemType.Leg: legSlot = newItem; break;
            case ItemType.Foot: footSlot = newItem; break;
            case ItemType.Wand: wandSlot = newItem; break;
        }
    }

    //  应用属性加成
    public void ApplyBonuses(Item item)
    {
        if (playerController != null)
        {
            playerController.moveSpeed += item.moveSpeedBonus;
        }
        if (playerHealth != null)
        {
            playerHealth.maxHealth += item.maxHealthBonus;
            playerHealth.currentHealth += item.maxHealthBonus;
            playerHealth.damageResistance += item.resistanceBonus;
        }
        if (playerMana != null)
        {
            playerMana.maxMana += item.maxManaBonus;
            playerMana.currentMana += item.maxManaBonus;
            playerMana.manaRegenRate += item.manaRegenBonus;
        }
        if (playerAttack != null)
        {
            playerAttack.meleeAttackDamage += item.attackDamageBonus;
            playerAttack.meleeAttackCooldown -= item.attackSpeedBonus;
        }
    }

    //  移除属性加成
    public void RemoveBonuses(Item item)
    {
        if (playerController != null)
        {
            playerController.moveSpeed -= item.moveSpeedBonus;
        }
        if (playerHealth != null)
        {
            playerHealth.maxHealth -= item.maxHealthBonus;
            playerHealth.currentHealth -= item.maxHealthBonus;
            playerHealth.damageResistance -= item.resistanceBonus;
        }
        if (playerMana != null)
        {
            playerMana.maxMana -= item.maxManaBonus;
            playerMana.currentMana -= item.maxManaBonus;
            playerMana.manaRegenRate -= item.manaRegenBonus;
        }
        if (playerAttack != null)
        {
            playerAttack.meleeAttackDamage -= item.attackDamageBonus;
            playerAttack.meleeAttackCooldown += item.attackSpeedBonus;
        }
    }
}