using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;

public class PlayerEXP : MonoBehaviour
{
    public static event Action<int> OnLevelUp;
    public static event Action<int, int> OnExperienceChanged;
    public static event Action<int> OnSkillPointsChanged;

    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int currentExperience = 0; 
    [SerializeField] private int skillPoints = 0; 

    [SerializeField] private int[] levelExpRequirements = { 100, 250, 500, 800, 1200, 1800, 2500, 3500, 5000 };

    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI expText;
    [SerializeField] private Image expFillImage;

    public int CurrentLevel => currentLevel;
    public int CurrentExperience => currentExperience;
    public int SkillPoints => skillPoints;

    public int CurrentSkillPoints => skillPoints;

    void Start()
    {
        UpdateLevelUI(currentLevel);
        UpdateExpUI(currentExperience, GetExperienceRequiredForNextLevel());
        OnSkillPointsChanged?.Invoke(skillPoints); 
    }

    public void AddExperience(int amount)
    {
        if (amount < 0) return;

        currentExperience += amount;
        Debug.Log($"获得了 {amount} 点经验。当前经验值: {currentExperience}");

        UpdateExpUI(currentExperience, GetExperienceRequiredForNextLevel());

        CheckForLevelUp();
    }

    private void CheckForLevelUp()
    {
        while (currentLevel < levelExpRequirements.Length + 1)
        {
            int expToNextLevel = GetExperienceRequiredForNextLevel();

            if (expToNextLevel == -1)
            {
                break;
            }

            if (currentExperience >= expToNextLevel)
            {
                currentExperience -= expToNextLevel;
                currentLevel++;
                skillPoints += 1;

                Debug.Log($"升到了 {currentLevel} 级,获得了1个技能点。");

                UpdateLevelUI(currentLevel);
                UpdateExpUI(currentExperience, GetExperienceRequiredForNextLevel());
                OnSkillPointsChanged?.Invoke(skillPoints); 
            }
            else
            {
                break;
            }
        }
        if (currentLevel >= levelExpRequirements.Length + 1)
        {
            Debug.Log($"达到最高等级 ({currentLevel})，经验值将继续累积。");
            UpdateExpUI(currentExperience, 0); 
        }
    }

    private int GetExperienceRequiredForNextLevel()
    {
        if (currentLevel - 1 < levelExpRequirements.Length)
        {
            return levelExpRequirements[currentLevel - 1];
        }
        else
        {
            return -1;
        }
    }

    public bool SpendSkillPoint(string skillName)
    {
        if (skillPoints > 0)
        {
            skillPoints--;
            Debug.Log($"已分配1个技能点给 {skillName}。剩余技能点: {skillPoints}");

            OnSkillPointsChanged?.Invoke(skillPoints); 
            return true;
        }
        else
        {
            Debug.Log("技能点不足！");
            return false;
        }
    }

    private void UpdateLevelUI(int newLevel)
    {
        if (levelText != null)
        {
            levelText.text = $"LV {newLevel}";
        }
        else
        {
            Debug.LogWarning("PlayerEXP: 等级文本 UI 引用未设置。无法更新等级显示。");
        }
    }

    private void UpdateExpUI(int currentExp, int expToNextLevel)
    {
        if (expText != null)
        {
            if (expToNextLevel == -1)
            {
                expText.text = $"经验: {currentExp} / 最高";
            }
            else
            {
                expText.text = $"{currentExp} / {expToNextLevel}";
            }
        }
        else
        {
            Debug.LogWarning("PlayerEXP: 经验值文本 UI 引用未设置。无法更新经验值显示。");
        }

        if (expFillImage != null)
        {
            if (expToNextLevel > 0)
            {
                expFillImage.fillAmount = (float)currentExp / expToNextLevel;
            }
            else
            {
                expFillImage.fillAmount = 1f;
            }
        }
        else
        {
            Debug.LogWarning("PlayerEXP: 经验值填充图像 UI 引用未设置。无法更新经验值进度条。");
        }
    }
}
