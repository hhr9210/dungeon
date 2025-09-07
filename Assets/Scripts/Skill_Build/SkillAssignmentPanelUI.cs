using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; // For Image and Button
using TMPro; // For TextMeshProUGUI

public class SkillAssignmentPanelUI : MonoBehaviour
{
    [Header("UI 引用")]
    public GameObject skillPanel; // 整个技能绑定主面板 (包含技能列表和确认菜单)
    public Transform skillButtonsParent; // 放置技能按钮的父对象 (例如 GridLayoutGroup)
    public GameObject skillButtonPrefab; // 单个技能按钮的预制体

    [Header("二级绑定确认菜单引用")]
    public GameObject assignmentConfirmationPanel; // 二级确认面板 (初始隐藏)
    public TextMeshProUGUI selectedSkillNameText; // 显示当前选中的技能名称
    // 技能描述文本引用 
    public TextMeshProUGUI skillDescriptionText; // 显示当前选中的技能描述
    public Button confirmBindQButton; // 确认绑定到 Q 键的按钮
    public Button confirmBindEButton; // 确认绑定到 E 键的按钮
    public Button cancelButton;       // 取消绑定并返回的按钮

    [Header("可用的所有技能")]
    [Tooltip("将所有 Skill ScriptableObject 拖拽到这里。")]
    public List<Skill> allAvailableSkills = new List<Skill>(); // 所有你希望玩家能绑定的技能

    private Skill _currentSelectedSkill; // 存储当前选中的技能

    void Awake()
    {
        // 确保主面板和二级面板初始都是隐藏的
        if (skillPanel != null) skillPanel.SetActive(false);
        if (assignmentConfirmationPanel != null) assignmentConfirmationPanel.SetActive(false);
    }

    void OnEnable()
    {
        //  订阅技能按钮的选中事件 
        SkillAssignmentButtonUI.OnSkillSelectedForAssignment += OnSkillSelected;
        PopulateSkillButtons(); // 每次显示面板时刷新按钮
    }

    void OnDisable()
    {
        //  取消订阅事件，防止内存泄漏 
        SkillAssignmentButtonUI.OnSkillSelectedForAssignment -= OnSkillSelected;
    }

    /// <summary>
    /// 填充技能按钮列表。
    /// </summary>
    public void PopulateSkillButtons()
    {
        // 清除旧按钮（如果存在）
        foreach (Transform child in skillButtonsParent)
        {
            Destroy(child.gameObject);
        }

        // 实例化新按钮
        foreach (Skill skill in allAvailableSkills)
        {
            if (skill == null) continue;

            GameObject skillButtonObj = Instantiate(skillButtonPrefab, skillButtonsParent);
            SkillAssignmentButtonUI buttonUI = skillButtonObj.GetComponent<SkillAssignmentButtonUI>();

            if (buttonUI != null)
            {
                buttonUI.Setup(skill);
            }
            else
            {
                Debug.LogError($"SkillAssignmentPanelUI: 技能按钮预制体 '{skillButtonPrefab.name}' 未找到 SkillAssignmentButtonUI 组件！", skillButtonPrefab);
            }
        }
    }

    /// <summary>
    /// 当有技能按钮被点击（选中）时调用。
    /// </summary>
    /// <param name="selectedSkill">被选中的技能数据。</param>
    private void OnSkillSelected(Skill selectedSkill)
    {
        _currentSelectedSkill = selectedSkill; // 存储被选中的技能
        ShowAssignmentConfirmationPanel(); // 显示二级确认面板
    }

    /// <summary>
    /// 显示二级绑定确认面板。
    /// </summary>
    private void ShowAssignmentConfirmationPanel()
    {
        if (assignmentConfirmationPanel == null)
        {
            Debug.LogError("Assignment Confirmation Panel is not assigned!", this);
            return;
        }

        // 更新确认面板上的文本
        if (selectedSkillNameText != null && _currentSelectedSkill != null)
        {
            selectedSkillNameText.text = $"{_currentSelectedSkill.skillName}";
        }

        // 更新技能描述文本 
        if (skillDescriptionText != null && _currentSelectedSkill != null)
        {
            skillDescriptionText.text = _currentSelectedSkill.description;
        }

        // 绑定二级菜单按钮的点击事件
        if (confirmBindQButton != null)
        {
            confirmBindQButton.onClick.RemoveAllListeners();
            confirmBindQButton.onClick.AddListener(OnConfirmBindQ);
        }
        if (confirmBindEButton != null)
        {
            confirmBindEButton.onClick.RemoveAllListeners();
            confirmBindEButton.onClick.AddListener(OnConfirmBindE);
        }
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(OnCancelAssignment);
        }

        // 激活确认面板，并隐藏技能列表 (如果需要)
        assignmentConfirmationPanel.SetActive(true);
        // 如果你的技能列表和确认面板是兄弟节点，你可能需要隐藏技能列表容器
        // skillButtonsParent.gameObject.SetActive(false); // 示例
    }

    /// <summary>
    /// 隐藏二级绑定确认面板。
    /// </summary>
    private void HideAssignmentConfirmationPanel()
    {
        if (assignmentConfirmationPanel != null)
        {
            assignmentConfirmationPanel.SetActive(false);
            // 重新显示技能列表 (如果之前隐藏了)
            // skillButtonsParent.gameObject.SetActive(true); // 示例
        }
        _currentSelectedSkill = null; // 清除选中的技能
    }

    private void OnConfirmBindQ()
    {
        if (_currentSelectedSkill != null && PlayerSkill.Instance != null)
        {
            PlayerSkill.Instance.AssignQSkill(_currentSelectedSkill);
            Debug.Log($"成功将 {_currentSelectedSkill.skillName} 绑定到 Q 键。", this);
        }
        HideAssignmentConfirmationPanel(); // 绑定后隐藏确认面板
    }

    private void OnConfirmBindE()
    {
        if (_currentSelectedSkill != null && PlayerSkill.Instance != null)
        {
            PlayerSkill.Instance.AssignESkill(_currentSelectedSkill);
            Debug.Log($"成功将 {_currentSelectedSkill.skillName} 绑定到 E 键。", this);
        }
        HideAssignmentConfirmationPanel(); // 绑定后隐藏确认面板
    }

    private void OnCancelAssignment()
    {
        Debug.Log("取消技能绑定。", this);
        HideAssignmentConfirmationPanel(); // 取消时隐藏确认面板
    }


    /// <summary>
    /// 切换整个技能绑定面板的显示/隐藏。
    /// </summary>
    public void ToggleSkillPanel()
    {
        if (skillPanel != null)
        {
            bool isActive = !skillPanel.activeSelf;
            skillPanel.SetActive(isActive);

            if (isActive)
            {
                PopulateSkillButtons(); // 每次显示时刷新，确保按钮最新
                HideAssignmentConfirmationPanel(); // 确保打开时，二级菜单是隐藏的
            }
        }
    }
}