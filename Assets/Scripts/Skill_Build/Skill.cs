using UnityEngine;

[CreateAssetMenu(fileName = "NewSkill", menuName = "Skill/Skill Data")]
public class Skill : ScriptableObject
{
    [Header("技能基本信息")]
    public string skillName = "新技能";
    public Sprite icon; // 技能图标
    [TextArea(3, 5)]
    public string description = "技能描述。";

    [Header("技能数值")]
    public float cooldown = 5f;    
    public float manaCost = 0f;    
    public float healthCost = 0f;  


    [Header("范围伤害技能属性 (可选)")]
    public float damageAmount = 0f;
    public float effectRadius = 0f;

    [Header("治疗技能属性 (可选)")]
    public float healAmount = 0f;

    [Header("速度提升技能属性 (可选)")]
    [Tooltip("速度提升的乘数，例如 1.5f 表示提升 50%。")]
    public float speedMultiplier = 1.5f;
    [Tooltip("速度提升效果的持续时间，单位为秒。")]
    public float effectDuration = 5f;

    [Header("技能视觉/听觉效果 (可选)")]
    public GameObject effectPrefab; // 特效预制体
    public AudioClip castSound;     // 音效
    public AudioClip hitSound;      // 命中音效

    [Header("抗性提升技能属性 (可选)")]
    [Tooltip("提升的伤害抗性值，例如 0.3 表示减少 30% 伤害。")]
    [Range(0f, 0.9f)]
    public float resistanceIncrease = 0.3f;


    // 技能的使用逻辑在 PlayerSkill 中处理。
}