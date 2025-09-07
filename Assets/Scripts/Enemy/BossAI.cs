using UnityEngine;
using System.Collections;

public class BossAI : MonoBehaviour
{
    public Boss boss;

    [Header("技能列表与配置")]
    [Tooltip("Boss的所有技能配置，按优先级排列。")]
    public SkillConfig[] skillConfigs;

    // 技能类型枚举
    public enum SkillType
    {
        AppearTimedObject,
        SummonTimedObject,
        SummonPrefab,
        SummonMultipleObjects,
        ProjectileAttack,
        BarrageAttack 
    }

    [System.Serializable]
    public class SkillConfig
    {
        [Tooltip("技能类型，决定要执行哪种逻辑。")]
        public SkillType skillType;
        [Tooltip("技能名称，用于咏唱UI显示。")]
        public string skillName;
        [Tooltip("触发此技能所需的Boss血量百分比 (0.0 - 1.0)。")]
        [Range(0.0f, 1.0f)]
        public float triggerHPPercentage = 0.5f;
        [Tooltip("技能咏唱所需时间。")]
        public float castDuration = 3.0f;
        
        [HideInInspector]
        public bool hasTriggered = false;
    }

    void Update()
    {
        CheckAndTriggerSkills();
    }

    private void CheckAndTriggerSkills()
    {
        foreach (var skill in skillConfigs)
        {
            if (!skill.hasTriggered && boss.currentHP <= boss.maxHP * skill.triggerHPPercentage)
            {
                skill.hasTriggered = true;
                StartCoroutine(CastAndPerformSkill(skill));
                return;
            }
        }
    }

    private IEnumerator CastAndPerformSkill(SkillConfig skill)
    {
        yield return StartCoroutine(boss.StartCasting(skill.castDuration, skill.skillName));

        switch (skill.skillType)
        {
            case SkillType.AppearTimedObject:
                boss.PerformSkill_AppearTimedObject();
                break;
            case SkillType.SummonTimedObject:
                boss.PerformSkill_SummonTimedObject();
                break;
            case SkillType.SummonPrefab:
                boss.PerformSkill_SummonPrefab();
                break;
            case SkillType.SummonMultipleObjects:
                boss.PerformSkill_SummonMultipleObjects();
                break;
            case SkillType.ProjectileAttack:
                boss.PerformSkill_ProjectileAttack();
                break;
            case SkillType.BarrageAttack: 
                boss.PerformSkill_BarrageAttack();
                break;
        }
    }
}