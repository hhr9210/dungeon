using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    [Header("World Generation")]
    [Tooltip("WorldBuilder脚本引用。")]
    public WorldBuilder worldBuilder;

    [Header("UI References")]
    [Tooltip("用于黑屏过渡的UI面板。")]
    public GameObject blackScreenPanel;
    [Tooltip("用于显示当前楼层的UI文本。")]
    public TextMeshProUGUI floorText;
    [Tooltip("用于显示加载进度的UI文本。")]
    public TextMeshProUGUI loadingProgressText;

    [Header("Typing Effect Settings")]
    [Tooltip("加载进度的基础信息。")]
    public string loadingMessageBase = "Loading next dungeon level......";
    [Tooltip("打字效果的字符间隔时间。")]
    public float typingSpeed = 0.05f;

    private int currentFloor = 0;

    void Start()
    {
        if (worldBuilder == null)
        {
            Debug.LogError("GameManager: WorldBuilder引用未设置！");
            return;
        }
        if (blackScreenPanel == null)
        {
            Debug.LogError("GameManager: 黑屏面板引用未设置！");
        }
        if (floorText == null)
        {
            Debug.LogError("GameManager: 楼层文本引用未设置！");
        }
        if (loadingProgressText == null)
        {
            Debug.LogError("GameManager: 加载进度文本引用未设置！");
        }

        // 初始化并生成第一个世界
        currentFloor = 1;
        UpdateFloorUI();
        GenerateNewWorld();
    }

    // 生成一个新的世界。
    void GenerateNewWorld()
    {
        StartCoroutine(GenerateWorldRoutine());
    }

    IEnumerator GenerateWorldRoutine()
    {
        // 显示黑屏和加载文本
        if (blackScreenPanel != null)
        {
            blackScreenPanel.SetActive(true);
            if (loadingProgressText != null)
            {
                loadingProgressText.gameObject.SetActive(true);
                loadingProgressText.text = "";
            }
            yield return null;
        }

        // 启动打字效果
        if (loadingProgressText != null && !string.IsNullOrEmpty(loadingMessageBase))
        {
            yield return StartCoroutine(TypeMessageRoutine(loadingMessageBase, loadingProgressText, typingSpeed));
        }

        // 模拟加载进度
        float duration = 1.5f;
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / duration);
            if (loadingProgressText != null)
            {
                loadingProgressText.text = loadingMessageBase + Mathf.RoundToInt(progress * 100) + "%";
            }
            yield return null;
        }

        if (loadingProgressText != null)
        {
            loadingProgressText.text = loadingMessageBase + "100%";
        }
        yield return new WaitForSeconds(0.2f);

        // 销毁旧世界
        if (worldBuilder.transform.childCount > 0)
        {
            Transform[] children = new Transform[worldBuilder.transform.childCount];
            for (int i = 0; i < worldBuilder.transform.childCount; i++)
            {
                children[i] = worldBuilder.transform.GetChild(i);
            }

            foreach (Transform child in children)
            {
                if (child != null && child.gameObject != worldBuilder.startPrefab)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        // 生成新世界
        worldBuilder.GenerateAndBuildWorld();

        // 隐藏黑屏和加载文本
        if (blackScreenPanel != null)
        {
            yield return new WaitForSeconds(0.2f);
            blackScreenPanel.SetActive(false);
            if (loadingProgressText != null)
            {
                loadingProgressText.gameObject.SetActive(false);
            }
        }
    }

    // 逐字协程
    IEnumerator TypeMessageRoutine(string message, TextMeshProUGUI targetText, float delayPerChar)
    {
        targetText.text = "";
        foreach (char c in message)
        {
            targetText.text += c;
            yield return new WaitForSeconds(delayPerChar);
        }
    }

    // 当玩家到达终点时调用。
    public void PlayerReachedEndpoint()
    {
        Debug.Log("玩家到达终点！正在生成下一个地牢...");
        currentFloor++;
        UpdateFloorUI();
        GenerateNewWorld();
    }

    // 更新UI上显示的楼层。
    private void UpdateFloorUI()
    {
        if (floorText != null)
        {
            floorText.text = "FLOOR " + currentFloor.ToString();
        }
    }
}
