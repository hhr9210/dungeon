using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro; 
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;

    public GameObject optionsPanel; 
    public Button optionButtonPrefab;

    // **** 新增：历史记录文本UI引用 ****
    [Header("Dialogue History")]
    public TextMeshProUGUI historyText; //历史文本UI
    public int maxHistoryLines = 50; // 最大历史行数，防止无限增长
    private List<string> dialogueHistory = new List<string>(); // 存储历史对话行
    // **********************************

    [Header("Settings")]
    public float typingSpeed = 0.05f; // 文本逐字显示的速度

    private DialogueData currentDialogue;
    private int currentLineIndex;
    private Coroutine typingCoroutine;
    private bool isTyping;

    private TextMeshProUGUI currentNpcNameDisplay;
    private Image currentNpcIconDisplay;
    private NPCChat _currentActiveNPCChatInstance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        dialoguePanel.SetActive(false); 
    }

    void Start()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);

        if (historyText != null)
        {
            historyText.text = "";
            dialogueHistory.Clear();
        }
    }

    void Update()
    {
        // 在对话进行中，按键或点击鼠标推进对话
        // 只有当对话面板激活，没有正在打字，并且玩家点击了鼠标左键时才响应
        if (dialoguePanel.activeInHierarchy && !isTyping && Input.GetMouseButtonDown(0))
        {
            if (currentDialogue.dialogueLines[currentLineIndex].playerOptions == null ||
                currentDialogue.dialogueLines[currentLineIndex].playerOptions.Count == 0)
            {
                if (currentLineIndex >= currentDialogue.dialogueLines.Count - 1)
                {
                    EndDialogue();
                }
                else 
                {
                    currentLineIndex++;
                    DisplayNextLine(); 
                }
            }
        }
    }

    public void StartDialogue(DialogueData dialogue, NPCChat npcChatInstance)
    {
        //  新增: 在对话开始时暂停游戏
        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.SetGamePaused(true);
        }

        _currentActiveNPCChatInstance = npcChatInstance;

        currentDialogue = dialogue;
        currentLineIndex = 0;
        dialoguePanel.SetActive(true);
        optionsPanel.SetActive(false); 

        currentNpcNameDisplay = npcChatInstance.npcNameDisplay;
        currentNpcIconDisplay = npcChatInstance.npcIconDisplay;

        if (currentNpcNameDisplay != null)
        {
            currentNpcNameDisplay.text = npcChatInstance.npcCharacterName;
        }
        else
        {
            Debug.LogWarning("DialogueManager: currentNpcNameDisplay is null. Please assign it in NPCChat Inspector.");
        }

        if (currentNpcIconDisplay != null)
        {
            currentNpcIconDisplay.sprite = npcChatInstance.npcCharacterPortrait;
        }
        else
        {
            Debug.LogWarning("DialogueManager: currentNpcIconDisplay is null. Please assign it in NPCChat Inspector.");
        }

        DisplayNextLine();
    }

    void DisplayNextLine()
    {
        if (currentLineIndex < currentDialogue.dialogueLines.Count)
        {
            DialogueLine line = currentDialogue.dialogueLines[currentLineIndex];

            foreach (Transform child in optionsPanel.transform)
            {
                Destroy(child.gameObject);
            }
            optionsPanel.SetActive(false); 

            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }

            if (_currentActiveNPCChatInstance != null)
            {
                AddLineToHistory(_currentActiveNPCChatInstance.npcCharacterName, line.dialogueText); //
            }
            else
            {
                AddLineToHistory("Unknown NPC", line.dialogueText); //
            }
            // *******************************
            typingCoroutine = StartCoroutine(TypeSentence(line.dialogueText));

            // 语音
            // if (line.voiceClip != null)
            // {
            //     GetComponent<AudioSource>().PlayOneShot(line.voiceClip);
            // }
        }
    }

    IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueText.text = "";
        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }
        isTyping = false;

        // 文本打字完成后，检查是否有玩家选项
        DialogueLine currentLine = currentDialogue.dialogueLines[currentLineIndex];
        if (currentLine.playerOptions != null && currentLine.playerOptions.Count > 0)
        {
            DisplayPlayerOptions(currentLine.playerOptions);
        }
    }

    void DisplayPlayerOptions(List<PlayerOption> options)
    {
        optionsPanel.SetActive(true);
        foreach (PlayerOption option in options)
        {
            Button optionBtn = Instantiate(optionButtonPrefab, optionsPanel.transform);
            optionBtn.GetComponentInChildren<TextMeshProUGUI>().text = option.optionText;
            optionBtn.onClick.AddListener(() => OnOptionSelected(option));
        }
    }

    void OnOptionSelected(PlayerOption selectedOption)
    {
        optionsPanel.SetActive(false); // 隐藏选项
        // 停止任何正在进行的打字协程，防止在新对话开始时仍在旧文本上操作
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            isTyping = false; 
        }

        AddLineToHistory("you", selectedOption.optionText);
        if (selectedOption.nextDialogue != null)
        {
            StartDialogue(selectedOption.nextDialogue, _currentActiveNPCChatInstance); // 开始新的对话，传递当前NPC实例
        }
        else
        {
            EndDialogue();
        }
    }

    public void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        currentNpcNameDisplay = null;
        currentNpcIconDisplay = null;
        _currentActiveNPCChatInstance = null; // 在对话结束时清除引用

        Debug.Log("对话结束！");

        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.SetGamePaused(false);
        }
    }

    private void AddLineToHistory(string characterName, string text) //
    {
        if (historyText == null) return; //

        string formattedLine = $"{characterName}: {text}"; // 格式化一行文本，例如 "NPC名字: 对话内容"
        dialogueHistory.Add(formattedLine); //

        // 限制历史记录的行数，移除最旧的行
        while (dialogueHistory.Count > maxHistoryLines) //
        {
            dialogueHistory.RemoveAt(0); // 移除最旧的（第一个）行
        }

        UpdateHistoryTextDisplay(); // 更新UI显示
    }

    // **** 更新历史文本UI的显示 ****
    private void UpdateHistoryTextDisplay() //
    {
        if (historyText == null) return; 
        historyText.text = string.Join("\n", dialogueHistory); // 将列表中的所有行用换行符连接起来
    }
}