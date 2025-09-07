using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems; 

public class PauseMenuManager : MonoBehaviour
{
    // 单例模式实例
    public static PauseMenuManager Instance { get; private set; }

    [Header("UI Panels to Hide on Pause")]
    [Tooltip("当暂停菜单激活时，需要隐藏的玩家HUD或其他UI面板。")]
    public GameObject[] uiPanelsToHide;

    [Header("Core UI References")]
    [Tooltip("主暂停菜单面板。")]
    public GameObject pausePanel;

    [Header("Sub-Menu Panels")]
    public GameObject logPanel;
    public GameObject itemPanel;
    public GameObject optionPanel;

    [Header("Buttons")]
    public Button resumeButton;
    public Button logButton;
    public Button itemButton;
    public Button optionsButton;
    public Button menuButton; // 用于“回到主菜单”或“退出游戏”的按钮

    [Header("Audio Settings")]
    public AudioClip menuOpenSound;
    public AudioClip menuCloseSound;
    private AudioSource audioSource;

    // 用于管理子菜单的堆栈
    private Stack<GameObject> menuStack = new Stack<GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    void Start()
    {
        // 确保所有UI面板初始状态为隐藏
        if (pausePanel != null) pausePanel.SetActive(false);
        if (logPanel != null) logPanel.SetActive(false);
        if (itemPanel != null) itemPanel.SetActive(false);
        if (optionPanel != null) optionPanel.SetActive(false);

        // 绑定按钮点击事件
        if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeButtonClicked);
        if (logButton != null) logButton.onClick.AddListener(OnLogButtonClicked);
        if (itemButton != null) itemButton.onClick.AddListener(OnItemButtonClicked);
        if (optionsButton != null) optionsButton.onClick.AddListener(OnOptionsButtonClicked);
        if (menuButton != null) menuButton.onClick.AddListener(OnMenuButtonClicked);
    }

    void Update()
    {
        // 监听F1键
        if (Input.GetKeyDown(KeyCode.F1))
        {
            if (PauseManager.Instance != null && PauseManager.Instance.IsGamePaused)
            {
                if (menuStack.Count > 0)
                {
                    // 如果堆栈中有子菜单，则弹出它们
                    if (menuStack.Count > 1)
                    {
                        PopMenu();
                    }
                    // 如果只剩下主菜单，则恢复游戏
                    else if (menuStack.Peek() == pausePanel)
                    {
                        ResumeGame();
                    }
                }
            }
            // 如果游戏未暂停
            else if (PauseManager.Instance != null && !PauseManager.Instance.IsGamePaused)
            {
                // 仅在主面板隐藏时才打开
                if (pausePanel != null && !pausePanel.activeSelf)
                {
                    PauseGame();
                }
            }
            // 如果PauseManager实例不可用，显示警告
            else if (PauseManager.Instance == null)
            {
                Debug.LogWarning("PauseMenuManager: PauseManager 实例不可用，无法处理 Escape 键。", this);
            }
        }
    }


    /// <summary>
    /// 暂停游戏并显示暂停菜单。
    /// </summary>
    public void PauseGame()
    {
        // 仅在游戏未暂停时执行暂停操作
        if (PauseManager.Instance != null && !PauseManager.Instance.IsGamePaused)
        {
            // 通知PauseManager暂停游戏
            PauseManager.Instance.SetGamePaused(true);

            // 将主暂停菜单推入堆栈
            PushMenu(pausePanel);

            Debug.Log("PauseMenuManager: 游戏已暂停，主菜单打开。");
            if (audioSource != null && menuOpenSound != null)
            {
                audioSource.PlayOneShot(menuOpenSound);
            }

            // 隐藏游戏中的其他UI面板
            if (uiPanelsToHide != null)
            {
                foreach (GameObject panel in uiPanelsToHide)
                {
                    if (panel != null)
                    {
                        panel.SetActive(false);
                    }
                }
            }
        }
        else if (PauseManager.Instance == null)
        {
            Debug.LogWarning("PauseMenuManager: PauseManager 实例不可用，无法暂停游戏。", this);
        }
    }

    /// <summary>
    /// 恢复游戏并隐藏所有暂停菜单。
    /// </summary>
    public void ResumeGame()
    {
        // 仅在游戏已暂停时执行恢复操作
        if (PauseManager.Instance != null && PauseManager.Instance.IsGamePaused)
        {
            // 清空菜单堆栈并隐藏所有菜单
            while (menuStack.Count > 0)
            {
                GameObject poppedMenu = menuStack.Pop();
                if (poppedMenu != null)
                {
                    poppedMenu.SetActive(false);
                }
            }

            // 通知PauseManager恢复游戏
            PauseManager.Instance.SetGamePaused(false);

            Debug.Log("PauseMenuManager: 游戏已恢复，所有菜单关闭。");

            // 清除EventSystem的选中状态
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            if (audioSource != null && menuCloseSound != null)
            {
                audioSource.PlayOneShot(menuCloseSound);
            }

            // 显示之前隐藏的UI面板
            if (uiPanelsToHide != null)
            {
                foreach (GameObject panel in uiPanelsToHide)
                {
                    if (panel != null)
                    {
                        panel.SetActive(true);
                    }
                }
            }
        }
        else if (PauseManager.Instance == null)
        {
            Debug.LogWarning("PauseMenuManager: PauseManager 实例不可用，无法恢复游戏。", this);
        }
    }

    /// <summary>
    /// 将新菜单推入堆栈并显示，同时隐藏前一个菜单。
    /// </summary>
    public void PushMenu(GameObject newMenuPanel)
    {
        if (newMenuPanel == null) return;

        // 如果要推送的菜单已在栈顶，则不做任何操作
        if (menuStack.Count > 0 && menuStack.Peek() == newMenuPanel)
        {
            Debug.LogWarning($"Attempted to push {newMenuPanel.name} but it's already on top of the stack.", newMenuPanel);
            return;
        }

        if (menuStack.Count > 0)
        {
            // 隐藏当前在堆栈顶部的菜单
            menuStack.Peek().SetActive(false);
        }

        // 显示新菜单
        newMenuPanel.SetActive(true);
        // 将新菜单推入堆栈
        menuStack.Push(newMenuPanel);
        Debug.Log($"Pushing menu: {newMenuPanel.name}, Stack count: {menuStack.Count}");

        // 设置新菜单的默认选中项
        if (EventSystem.current != null)
        {
            // 首先，清除当前选中
            EventSystem.current.SetSelectedGameObject(null);
            GameObject targetSelectable = null;

            if (newMenuPanel == pausePanel && resumeButton != null)
            {
                targetSelectable = resumeButton.gameObject;
            }
            else if (newMenuPanel == logPanel)
            {
                Button firstLogButton = logPanel.GetComponentInChildren<Button>();
                if (firstLogButton != null) targetSelectable = firstLogButton.gameObject;
            }
            else if (newMenuPanel == itemPanel)
            {
                Button firstItemButton = itemPanel.GetComponentInChildren<Button>();
                if (firstItemButton != null) targetSelectable = firstItemButton.gameObject;
            }
            else if (newMenuPanel == optionPanel)
            {
                Selectable firstOptionSelectable = optionPanel.GetComponentInChildren<Selectable>();
                if (firstOptionSelectable != null) targetSelectable = firstOptionSelectable.gameObject;
            }

            if (targetSelectable != null)
            {
                EventSystem.current.SetSelectedGameObject(targetSelectable);
                Debug.Log($"EventSystem: Set selected object to {targetSelectable.name} on menu push.");

                // 强制重新选择以刷新UI
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(targetSelectable);
                Debug.Log($"EventSystem: Performed quick re-selection for immediate hover refresh on {targetSelectable.name}.");
            }
        }
    }

    /// <summary>
    /// 从堆栈中弹出当前菜单并隐藏，然后显示堆栈中的上一个菜单。
    /// </summary>
    public void PopMenu()
    {
        if (menuStack.Count > 0)
        {
            // 弹出并隐藏当前菜单
            GameObject currentMenu = menuStack.Pop();
            if (currentMenu != null)
            {
                currentMenu.SetActive(false);
            }
            Debug.Log($"Popping menu: {currentMenu?.name ?? "Null Menu"}, Stack count: {menuStack.Count}");

            // 如果堆栈中还有其他菜单
            if (menuStack.Count > 0)
            {
                // 获取上一个菜单
                GameObject previousMenu = menuStack.Peek();
                if (previousMenu != null)
                {
                    // 显示上一个菜单
                    previousMenu.SetActive(true);

                    // 重新设置默认选中项
                    if (EventSystem.current != null)
                    {
                        EventSystem.current.SetSelectedGameObject(null);
                        GameObject targetSelectable = null;

                        // 根据面板选择默认按钮
                        if (previousMenu == pausePanel && resumeButton != null)
                        {
                            targetSelectable = resumeButton.gameObject;
                        }
                        else if (previousMenu.GetComponentInChildren<Button>() != null)
                        {
                            targetSelectable = previousMenu.GetComponentInChildren<Button>().gameObject;
                        }

                        if (targetSelectable != null)
                        {
                            EventSystem.current.SetSelectedGameObject(targetSelectable);
                            Debug.Log($"EventSystem: Re-selected {targetSelectable.name} after popping menu.");

                            // 同样在这里进行刷新
                            EventSystem.current.SetSelectedGameObject(null);
                            EventSystem.current.SetSelectedGameObject(targetSelectable);
                            Debug.Log($"EventSystem: Performed quick re-selection for immediate hover refresh on {targetSelectable.name} after pop.");
                        }
                    }
                }
            }
        }
    }

    // 按钮点击事件处理函数
    public void OnResumeButtonClicked()
    {
        // 调用ResumeGame方法来恢复游戏
        ResumeGame();
        Debug.Log("继续游戏按钮被点击。");
    }

    public void OnLogButtonClicked()
    {
        if (logPanel != null)
        {
            PushMenu(logPanel);
            Debug.Log("Log按钮被点击，Log Panel打开。");
        }
        else
        {
            Debug.LogWarning("Log Panel 未赋值！", this);
        }
    }

    public void OnItemButtonClicked()
    {
        if (itemPanel != null)
        {
            PushMenu(itemPanel);
            Debug.Log("Item按钮被点击，Item Panel打开。");
        }
        else
        {
            Debug.LogWarning("Item Panel 未赋值！", this);
        }
    }

    public void OnOptionsButtonClicked()
    {
        if (optionPanel != null)
        {
            PushMenu(optionPanel);
            Debug.Log("选项按钮被点击，Option Panel打开。");
        }
        else
        {
            Debug.LogWarning("Option Panel 未赋值！", this);
        }
    }

    public void OnMenuButtonClicked()
    {
        Debug.Log("Menu（退出/主菜单）按钮被点击。（功能待实现，例如加载主菜单场景）");
    }
}
