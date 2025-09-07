using UnityEngine;
using UnityEngine.UI;
using TMPro;         
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [System.Serializable]
    public struct UIPanelConfig
    {
        public string panelName;
        public GameObject panelGameObject;
        public Button toggleButton;
        public KeyCode hotkey;
    }

    [Header("UI 面板设置")]
    public List<UIPanelConfig> uiPanelConfigs;

    [Header("游戏内日志显示")]
    public TextMeshProUGUI logOutputText;
    public int maxLogCharacters = 2000;

    private string currentLogs = "";

    void OnEnable()
    {
        Application.logMessageReceived += HandleLogMessage;
        Debug.Log("[UIManager] 日志处理程序已启用。");
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLogMessage;
        Debug.Log("[UIManager] 日志处理程序已禁用。");
    }

    void Start()
    {
        Debug.Log("[UIManager] Start 方法开始执行。");

        if (uiPanelConfigs == null || uiPanelConfigs.Count == 0)
        {
            Debug.LogWarning("[UIManager.Start] uiPanelConfigs 列表为空或未赋值！脚本已禁用。");
            enabled = false;
            return;
        }

        foreach (var config in uiPanelConfigs)
        {
            if (config.panelGameObject != null)
            {
                config.panelGameObject.SetActive(false);
                Debug.Log($"[UIManager.Start] 强制面板 '{config.panelName}' 设为非激活状态。当前 activeSelf: {config.panelGameObject.activeSelf}");

                // 为按钮注册点击事件监听器
                if (config.toggleButton != null)
                {
                    config.toggleButton.onClick.AddListener(() => TogglePanel(config.panelGameObject));
                    Debug.Log($"[UIManager.Start] 面板 '{config.panelName}' 的开关按钮已注册。");
                }
                else
                {
                    Debug.LogWarning($"[UIManager.Start] 警告：面板 '{config.panelName}' 的按钮未在 Inspector 中赋值。");
                }
            }
            else
            {
                Debug.LogWarning($"[UIManager.Start] 警告：'{config.panelName}' 的 Panel GameObject 未赋值。");
            }
        }

        if (logOutputText == null)
        {
            Debug.LogWarning("[UIManager.Start] 警告：logOutputText 未在 Inspector 中赋值。游戏内日志将无法显示。");
        }

        Debug.Log("[UIManager] Start 方法执行完毕。");
    }

    void Update()
    {
        // 检查所有面板的热键
        foreach (var config in uiPanelConfigs)
        {
            if (config.panelGameObject != null && Input.GetKeyDown(config.hotkey))
            {
                TogglePanel(config.panelGameObject);
            }
        }

        //ESC 键关闭所有已打开的面板
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("[UIManager.Update] 检测到 ESC 按键。正在尝试关闭已打开的 UI 面板。");
            bool panelWasClosed = false;

            foreach (var config in uiPanelConfigs)
            {
                if (config.panelGameObject != null && config.panelGameObject.activeSelf)
                {
                    config.panelGameObject.SetActive(false);
                    Debug.Log($"[UIManager.Update] 面板 '{config.panelName}' 已通过 ESC 键关闭。");
                    panelWasClosed = true;
                }
            }

            if (!panelWasClosed)
            {
                Debug.Log("[UIManager.Update] 按下 ESC 键，但没有找到任何已打开的 UI 面板可关闭。");
            }
        }
    }

    public void TogglePanel(GameObject panel)
    {
        if (panel == null)
        {
            Debug.LogWarning("[UIManager.TogglePanel] 警告：尝试切换一个空面板。");
            return;
        }

        bool currentState = panel.activeSelf;
        panel.SetActive(!currentState);

        Debug.Log($"[UIManager.TogglePanel] 面板 '{panel.name}' 状态已切换。切换前: {currentState}, 切换后: {panel.activeSelf}");
    }

    // 捕获所有 Debug.Log 的消息，并将其显示到 UI 文本中
    void HandleLogMessage(string logString, string stackTrace, LogType type)
    {
        // 只处理 Log 类型的消息（不包括 Warning 和 Error）
        if (type == LogType.Log)
        {
            if (logOutputText == null)
            {
                // 如果 UI 文本组件未赋值，则取消注册处理程序，以防再次触发警告
                Application.logMessageReceived -= HandleLogMessage;
                Debug.LogWarning("[UIManager.HandleLogMessage] logOutputText 未赋值。已停止日志捕获以防止进一步警告。");
                return;
            }

            string formattedLog = $"{System.DateTime.Now:HH:mm:ss} : {logString}\n";
            currentLogs = formattedLog + currentLogs;

            // 限制日志文本的长度，以避免性能问题
            if (currentLogs.Length > maxLogCharacters)
            {
                currentLogs = currentLogs.Substring(0, maxLogCharacters);
            }
            logOutputText.text = currentLogs;
        }
    }

    void OnDestroy()
    {
        // 在对象被销毁时，确保移除日志处理程序
        Application.logMessageReceived -= HandleLogMessage;
        Debug.Log("[UIManager] 脚本正在被销毁。日志处理程序已移除。");
    }
}