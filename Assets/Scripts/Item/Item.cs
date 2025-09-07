// Item.cs
using UnityEngine;

// 物品类型枚举
public enum ItemType
{
    Generic,        // 通用物品类型，只能放入通用热键栏
    Consumable,     // 消耗品
    Tool,           // 工具
    BuildingMaterial,// 建筑材料
    Head,           // 头部装备
    Weapon,         // 武器
    Chest,          // 胸部装备
    Leg,            // 腿部装备
    Foot,           // 脚部装备
    Wand            // 魔杖
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    [Header("物品基本信息")]
    public string itemName = "新物品";
    public Sprite icon;
    [TextArea(3, 5)]
    public string description = "这是物品的描述。";

    [Header("价格设置")]
    public float price = 0f;

    [Header("冷却设置")]
    public float cooldownTime = 0f;

    [Header("物品类型 (可选，用于分类或特殊效果)")]
    public ItemType itemType = ItemType.Generic;

    //  装备提供的属性加成 
    [Header("装备属性加成")]
    [Tooltip("装备提供的额外移动速度。")]
    public float moveSpeedBonus = 0f;

    [Tooltip("装备提供的额外生命值上限。")]
    public float maxHealthBonus = 0f;

    [Tooltip("装备提供的额外魔力值上限。")]
    public float maxManaBonus = 0f;

    [Tooltip("装备提供的额外魔力回复速度。")]
    public float manaRegenBonus = 0f;

    [Tooltip("装备提供的额外伤害抗性（0.0到1.0之间）。")]
    [Range(0f, 1f)]
    public float resistanceBonus = 0f;

    [Tooltip("装备提供的额外攻击速度加成（会减少冷却时间）。")]
    public float attackSpeedBonus = 0f;

    [Tooltip("装备提供的额外攻击力。")]
    public float attackDamageBonus = 0f; //  攻击力加成

    /// <summary>
    /// 当物品被使用时调用 (例如，从快捷栏使用)。
    /// </summary>
    public virtual void Use()
    {
        Debug.Log($"使用了物品: {itemName}");
    }
}