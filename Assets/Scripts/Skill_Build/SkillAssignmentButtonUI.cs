using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System; 

public class SkillAssignmentButtonUI : MonoBehaviour
{
    [Header("UI 引用")]
    public Image skillIcon; 
    public TextMeshProUGUI skillNameText;
    public Button selectSkillButton; 

    private Skill _skillData; 

    // 静态事件：当一个技能按钮被点击时触发
    public static event Action<Skill> OnSkillSelectedForAssignment;

    /// <summary>
    /// 使用给定的技能数据来设置按钮的显示。
    /// </summary>
    /// <param name="skill">要显示和绑定的技能数据。</param>
    public void Setup(Skill skill)
    {
        _skillData = skill;

        if (skillIcon != null) skillIcon.sprite = skill.icon;
        if (skillNameText != null) skillNameText.text = skill.skillName;

        // 清除旧监听器并添加新的点击事件
        if (selectSkillButton != null)
        {
            selectSkillButton.onClick.RemoveAllListeners();
            selectSkillButton.onClick.AddListener(OnSelectSkillClicked);
        }
    }

    // 按钮点击时调用的方法
    private void OnSelectSkillClicked()
    {
        if (_skillData != null)
        {
            OnSkillSelectedForAssignment?.Invoke(_skillData);
            Debug.Log($"技能 '{_skillData.skillName}' 被选中进行绑定。", this);
        }
    }
}
