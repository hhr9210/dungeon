using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class NpcSelect : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject NPCUIPanel;
    [SerializeField] private TextMeshProUGUI interactionPromptText;
    [SerializeField] private string interactionPrompt = "PRESS F TO SHOP";

    [Header("NPC Settings")]
    [SerializeField] private string npcName = "NPC NAME";
    [SerializeField] private LayerMask npcLayer;
    [SerializeField] private float nameTextHeightOffset = 2.5f;

    [Header("Highlight Settings")]
    [SerializeField] private Color highlightColor = Color.yellow;

    [Header("NPC Name Panel")]
    [SerializeField] private GameObject npcNamePanelPrefab;
    private TextMeshProUGUI npcNameTextInstance;

    private Renderer[] npcRenderers;
    private Color[] originalColors;

    private bool playerInTriggerRange = false;
    private bool shopUIisActive = false;

    private Camera mainCamera;

    private Shop shopManager;

    void Awake()
    {
        npcRenderers = GetComponentsInChildren<Renderer>();
        if (npcRenderers.Length == 0)
        {
            Debug.LogWarning("NPC没有Renderer组件! 确保NPC模型或其子物体上有Mesh Renderer。", this);
        }
        else
        {
            originalColors = new Color[npcRenderers.Length];
            for (int i = 0; i < npcRenderers.Length; i++)
            {
                if (npcRenderers[i].material.HasProperty("_Color"))
                {
                    originalColors[i] = npcRenderers[i].material.color;
                }
                else if (npcRenderers[i].material.HasProperty("_BaseColor"))
                {
                    originalColors[i] = npcRenderers[i].material.GetColor("_BaseColor");
                }
                else
                {
                    originalColors[i] = Color.white;
                    Debug.LogWarning($"材质 {npcRenderers[i].material.name} 没有 _Color 或 _BaseColor 属性，无法获取原始颜色。", npcRenderers[i].material);
                }
            }
        }
    }

    void Start()
    {
        mainCamera = Camera.main;

        shopManager = FindObjectOfType<Shop>();
        if (shopManager == null)
        {
            Debug.LogError("场景中没有找到Shop脚本! 确保它挂载在一个GameObject上。", this);
        }

        if (NPCUIPanel != null)
        {
            NPCUIPanel.SetActive(false);
        }

        if (interactionPromptText != null)
        {
            interactionPromptText.gameObject.SetActive(false);
            interactionPromptText.text = interactionPrompt;
        }

        if (npcNamePanelPrefab != null)
        {
            GameObject namePanelGO = Instantiate(npcNamePanelPrefab);
            namePanelGO.transform.SetParent(transform);
            namePanelGO.transform.localPosition = new Vector3(0, nameTextHeightOffset, 0);

            npcNameTextInstance = namePanelGO.GetComponentInChildren<TextMeshProUGUI>();
            if (npcNameTextInstance != null)
            {
                npcNameTextInstance.text = npcName;
                npcNameTextInstance.gameObject.SetActive(true);
            }
        }
    }

    void Update()
    {
        if (npcNameTextInstance != null && mainCamera != null)
        {
            Transform canvasTransform = npcNameTextInstance.transform.parent;
            if (canvasTransform != null)
            {
                canvasTransform.LookAt(canvasTransform.position + mainCamera.transform.rotation * Vector3.forward,
                                         mainCamera.transform.rotation * Vector3.up);
            }
        }

        if (playerInTriggerRange && !shopUIisActive)
        {
            if (interactionPromptText != null && !interactionPromptText.gameObject.activeSelf)
            {
                interactionPromptText.gameObject.SetActive(true);
            }

            ApplyHighlightColor();

            if (Input.GetKeyDown(KeyCode.F))
            {
                OpenShopUI();
            }
        }
        else
        {
            if (interactionPromptText != null && interactionPromptText.gameObject.activeSelf)
            {
                interactionPromptText.gameObject.SetActive(false);
            }

            if (!shopUIisActive && npcRenderers != null && originalColors != null)
            {
                RestoreOriginalColors();
            }
        }

        if (shopUIisActive)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseShopUI();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTriggerRange = true;
            Debug.Log("玩家进入NPC交互范围。");
            if (interactionPromptText != null)
            {
                interactionPromptText.gameObject.SetActive(true);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTriggerRange = false;
            Debug.Log("玩家离开NPC交互范围。");

            if (shopUIisActive)
            {
                CloseShopUI();
            }
            if (interactionPromptText != null)
            {
                interactionPromptText.gameObject.SetActive(false);
            }
            RestoreOriginalColors();
        }
    }

    private void ApplyHighlightColor()
    {
        if (npcRenderers == null) return;
        foreach (Renderer r in npcRenderers)
        {
            if (r != null)
            {
                if (r.material.HasProperty("_Color"))
                {
                    r.material.color = highlightColor;
                }
                else if (r.material.HasProperty("_BaseColor"))
                {
                    r.material.SetColor("_BaseColor", highlightColor);
                }
            }
        }
    }

    private void RestoreOriginalColors()
    {
        if (npcRenderers == null || originalColors == null) return;
        for (int i = 0; i < npcRenderers.Length; i++)
        {
            if (npcRenderers[i] != null && i < originalColors.Length)
            {
                if (npcRenderers[i].material.HasProperty("_Color"))
                {
                    npcRenderers[i].material.color = originalColors[i];
                }
                else if (npcRenderers[i].material.HasProperty("_BaseColor"))
                {
                    npcRenderers[i].material.SetColor("_BaseColor", originalColors[i]);
                }
            }
        }
    }

    private void OpenShopUI()
    {
        if (NPCUIPanel != null && !shopUIisActive)
        {
            if (PauseManager.Instance != null)
            {
                PauseManager.Instance.SetGamePaused(true);
                Debug.Log("暂停游戏并打开商店UI。");
            }
            else
            {
                Debug.LogError("PauseManager.Instance为空! 无法暂停游戏。");
            }

            NPCUIPanel.SetActive(true);
            shopUIisActive = true;
            Debug.Log("商店UI面板已显示。");

            if (interactionPromptText != null)
            {
                interactionPromptText.gameObject.SetActive(false);
            }

            if (shopManager != null)
            {
                shopManager.UpdateShopUI();
            }
        }
    }

    private void CloseShopUI()
    {
        if (NPCUIPanel != null && shopUIisActive)
        {
            if (PauseManager.Instance != null)
            {
                PauseManager.Instance.SetGamePaused(false);
                Debug.Log("恢复游戏并关闭商店UI。");
            }
            else
            {
                Debug.LogError("PauseManager.Instance为空! 无法恢复游戏。");
            }

            NPCUIPanel.SetActive(false);
            shopUIisActive = false;
            Debug.Log("商店UI面板已隐藏。");

            RestoreOriginalColors();

            if (playerInTriggerRange)
            {
                if (interactionPromptText != null)
                {
                    interactionPromptText.gameObject.SetActive(true);
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Collider triggerCollider = GetComponentInChildren<Collider>(true);
        if (triggerCollider != null && triggerCollider.isTrigger)
        {
            Gizmos.color = Color.cyan;
            if (triggerCollider is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(sphere.transform.position + sphere.center, sphere.radius);
            }
            else if (triggerCollider is BoxCollider box)
            {
                Gizmos.matrix = Matrix4x4.TRS(box.transform.position, box.transform.rotation, box.transform.lossyScale);
                Gizmos.DrawWireCube(box.center, box.size);
                Gizmos.matrix = Matrix4x4.identity;
            }
        }
    }
}
