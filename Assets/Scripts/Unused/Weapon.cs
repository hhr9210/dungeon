using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Weapons/New Weapon")]
public class Weapon : ScriptableObject
{
    [Header("基本信息")]
    public string itemName = "New Weapon";     // 武器名称
    [TextArea(3, 5)]
    public string description = "Weapon description."; // 武器描述
    public Sprite icon = null;                 // UI显示图标

    [Header("武器佩戴条件")]
    [Tooltip("佩戴该武器所需的魔法分配点。")]
    public int manaRequirement = 0;
    [Tooltip("佩戴该武器所需的攻击分配点。")]
    public int attackRequirement = 0;
    [Tooltip("佩戴该武器所需的力量分配点。")]
    public int strengthRequirement = 0;

    // 移除了所有攻击属性修正，因为暂时不考虑具体效果
    // public float attackDamageModifier = 0f;
    // public float attackSpeedModifier = 0f;
}