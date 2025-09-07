using UnityEngine;
using TMPro; // 使用 TextMeshPro 命名空间
using System.Collections; // 需要导入 System.Collections 来使用 Coroutine
using UnityEngine.UI; // 用于 Image

public class NPCChat : MonoBehaviour
{
    public DialogueData npcDialogue;
    public GameObject interactionPrompt;
    public string promptText = "PRESS F TO CHAT";

    [Header("NPC Data")]
    public string npcCharacterName;
    public Sprite npcCharacterPortrait;

    [Header("NPC Display UI")]
    public TextMeshProUGUI npcNameDisplay;
    public Image npcIconDisplay;

    private bool playerInRange = false;
    private Coroutine exitCheckCoroutine;

    void Start()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.F))
        {
            if (DialogueManager.Instance != null && !DialogueManager.Instance.dialoguePanel.activeInHierarchy)
            {
                if (exitCheckCoroutine != null)
                {
                    StopCoroutine(exitCheckCoroutine);
                    exitCheckCoroutine = null;
                }
                DialogueManager.Instance.StartDialogue(npcDialogue, this);
                if (interactionPrompt != null)
                {
                    interactionPrompt.SetActive(false);
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            if (interactionPrompt != null)
            {
                TextMeshProUGUI textComponent = interactionPrompt.GetComponent<TextMeshProUGUI>();
                if (textComponent == null)
                {
                    textComponent = interactionPrompt.GetComponentInChildren<TextMeshProUGUI>();
                }
                if (textComponent != null)
                {
                    textComponent.text = promptText;
                    interactionPrompt.SetActive(true);
                }
            }
            if (exitCheckCoroutine != null)
            {
                StopCoroutine(exitCheckCoroutine);
                exitCheckCoroutine = null;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (DialogueManager.Instance != null && DialogueManager.Instance.dialoguePanel.activeInHierarchy)
            {
                if (exitCheckCoroutine != null)
                {
                    StopCoroutine(exitCheckCoroutine);
                }
                exitCheckCoroutine = StartCoroutine(CheckPlayerExitDelayed(0.1f));
            }
            else
            {
                playerInRange = false;
                if (interactionPrompt != null)
                {
                    interactionPrompt.SetActive(false);
                }
            }
        }
    }

    IEnumerator CheckPlayerExitDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!playerInRange && DialogueManager.Instance != null && DialogueManager.Instance.dialoguePanel.activeInHierarchy)
        {
            DialogueManager.Instance.EndDialogue();
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }
        }
        playerInRange = false;
        exitCheckCoroutine = null;
    }
}
