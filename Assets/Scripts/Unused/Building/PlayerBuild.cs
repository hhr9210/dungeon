using UnityEngine;
using UnityEngine.UI; // For UI panel management
using System.Collections.Generic; // For List

// 定义一个可序列化的类，用于存储每个可建造建筑的信息
[System.Serializable]
public class BuildableBuilding
{
    [Tooltip("要建造的建筑预制体。")]
    public GameObject prefab;
    [Tooltip("建造此建筑所需的金钱。")]
    public float cost = 100f;
    [Tooltip("此建筑的名称，用于UI或日志。")]
    public string buildingName = "新建筑";
}

public class PlayerBuild : MonoBehaviour
{
    [Header("建造功能")]
    [Tooltip("请拖拽你的建造UI面板（通常是一个Canvas下的Panel）到这里。")]
    public GameObject buildingUIPanel;      // 建造UI面板的引用

    [Tooltip("用于建筑放置的地面层。确保你的地面GameObject在此层。")]
    public LayerMask groundLayerForBuilding; // 用于建筑放置的地面层

    [Header("可建造建筑列表")]
    [Tooltip("在这里配置所有可建造的建筑，包括它们的预制体和建造价格。")]
    public List<BuildableBuilding> buildableBuildings = new List<BuildableBuilding>();

    private bool isBuildingMode = false;        // 是否处于建造模式
    private GameObject currentBuildingPrefab;   // 当前选择的建筑预制体
    private float currentBuildingCost;          // 当前选择建筑的建造价格
    private GameObject buildingPreviewInstance; // 建筑预览实例 (可选)

    private Inventory inventory; // 引用 Inventory 脚本

    void Awake()
    {
        // 获取 Inventory 脚本引用
        inventory = GetComponent<Inventory>();
        if (inventory == null)
        {
            Debug.LogError("PlayerBuilding: Inventory 脚本未找到！请确保玩家GameObject上挂载了Inventory脚本。", this);
        }

        // 确保建造UI面板在开始时是隐藏的
        if (buildingUIPanel != null)
        {
            buildingUIPanel.SetActive(false);
        }

        // 调试：检查UI引用是否正确赋值
        Debug.Log("--- PlayerBuilding UI引用调试 ---");
        CheckUIReference(buildingUIPanel, "Building UI Panel");
        // 检查 buildableBuildings 列表中的每个预制体是否已赋值
        for (int i = 0; i < buildableBuildings.Count; i++)
        {
            CheckUIReference(buildableBuildings[i].prefab, $"Buildable Building {i} Prefab");
        }
        Debug.Log("--- PlayerBuilding UI引用调试结束 ---");
    }

    void Update()
    {
        // 建造模式切换 (F 键)
        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleBuildingMode();
        }

        // 如果处于建造模式且已选择建筑
        if (isBuildingMode && currentBuildingPrefab != null)
        {
            HandleBuildingPlacement();
        }
    }

    /// <summary>
    /// 切换建造模式的开启/关闭。
    /// </summary>
    void ToggleBuildingMode()
    {
        isBuildingMode = !isBuildingMode;
        if (buildingUIPanel != null)
        {
            buildingUIPanel.SetActive(isBuildingMode);
        }
        Debug.Log($"PlayerBuilding: 建造模式: {(isBuildingMode ? "开启" : "关闭")}");

        // 如果关闭建造模式，清理当前选择和预览
        if (!isBuildingMode)
        {
            currentBuildingPrefab = null;
            currentBuildingCost = 0f; // 清理成本
            DestroyBuildingPreview();
        }
    }

    /// <summary>
    /// 选择一个建筑预制体进行建造。
    /// 这个方法会绑定到建造UI按钮的OnClick事件。
    /// </summary>
    /// <param name="prefabToSelect">要建造的建筑预制体。</param>
    public void SelectBuilding(GameObject prefabToSelect)
    {
        Debug.Log($"PlayerBuilding: SelectBuilding 方法被调用。传入的预制体: {(prefabToSelect != null ? prefabToSelect.name : "NULL")}");

        if (!isBuildingMode)
        {
            Debug.LogWarning("PlayerBuilding: 未处于建造模式，无法选择建筑。");
            return; // 必须在建造模式下才能选择
        }

        // 从 buildableBuildings 列表中查找匹配的建筑信息
        BuildableBuilding selectedBuildingInfo = null;
        foreach (var building in buildableBuildings)
        {
            if (building.prefab == prefabToSelect)
            {
                selectedBuildingInfo = building;
                break;
            }
        }

        if (selectedBuildingInfo == null)
        {
            Debug.LogError($"PlayerBuilding: 传入的预制体 '{prefabToSelect?.name}' 未在 'Buildable Buildings' 列表中找到！请检查 Inspector 配置。", prefabToSelect);
            return;
        }

        currentBuildingPrefab = selectedBuildingInfo.prefab;
        currentBuildingCost = selectedBuildingInfo.cost; // 存储当前建筑的成本
        Debug.Log($"PlayerBuilding: 已选择建筑: {selectedBuildingInfo.buildingName}，建造价格: {currentBuildingCost}");

        // 创建建筑预览 (可选)
        CreateBuildingPreview();
    }

    /// <summary>
    /// 处理建筑的放置逻辑。
    /// </summary>
    void HandleBuildingPlacement()
    {
        // 更新建筑预览位置
        UpdateBuildingPreview();

        // 鼠标左键点击放置建筑
        if (Input.GetMouseButtonDown(0)) // 鼠标左键
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // 射线检测地面
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayerForBuilding))
            {
                // --- 检查金钱是否足够 ---
                if (inventory != null && inventory.CanAfford(currentBuildingCost))
                {
                    // 扣除金钱
                    inventory.SpendMoney(currentBuildingCost);

                    // 在点击的地面位置实例化建筑
                    Instantiate(currentBuildingPrefab, hit.point, Quaternion.identity);
                    Debug.Log($"PlayerBuilding: 成功建造 {currentBuildingPrefab.name} 在 {hit.point}，花费 {currentBuildingCost} 金钱。");

                    // 放置后可以清除当前选择，以便继续选择其他建筑或退出建造模式
                    currentBuildingPrefab = null; // 清除当前选择
                    currentBuildingCost = 0f;     // 清除成本
                    DestroyBuildingPreview();     // 销毁预览
                    // 如果希望放置一个后立即退出建造模式，可以取消注释下面这行：
                    // ToggleBuildingMode();
                }
                else if (inventory != null)
                {
                    Debug.LogWarning($"PlayerBuilding: 金钱不足！无法建造 {currentBuildingPrefab.name}。需要 {currentBuildingCost}，当前只有 {inventory.currentMoney}。");
                }
                else
                {
                    Debug.LogError("PlayerBuilding: Inventory 脚本未初始化，无法检查金钱。");
                }
            }
            else
            {
                Debug.Log("PlayerBuilding: 未点击到有效地面，无法放置建筑。");
            }
        }
    }

    /// <summary>
    /// 创建建筑预览实例。
    /// </summary>
    void CreateBuildingPreview()
    {
        DestroyBuildingPreview(); // 先销毁旧的预览

        if (currentBuildingPrefab != null)
        {
            buildingPreviewInstance = Instantiate(currentBuildingPrefab);

            // --- 核心修改：为预览模型设置半透明材质并禁用碰撞体 ---
            Renderer previewRenderer = buildingPreviewInstance.GetComponent<Renderer>();
            if (previewRenderer == null) // 如果预制体本身没有Renderer，尝试在子对象中查找
            {
                previewRenderer = buildingPreviewInstance.GetComponentInChildren<Renderer>();
            }

            if (previewRenderer != null)
            {
                // URP 兼容的着色器
                Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
                if (urpLitShader == null)
                {
                    Debug.LogError("PlayerBuilding: 无法找到 'Universal Render Pipeline/Lit' Shader！预览模型将显示为粉红色。请确保项目已正确配置 URP。");
                    // 尝试使用 Unlit Shader 作为回退
                    urpLitShader = Shader.Find("Universal Render Pipeline/Unlit");
                    if (urpLitShader == null)
                    {
                        Debug.LogError("PlayerBuilding: 无法找到 'Universal Render Pipeline/Unlit' Shader！预览模型将继续显示为粉红色。");
                        return; // 无法找到任何可用Shader，直接返回
                    }
                }

                Material previewMaterial = new Material(urpLitShader);
                previewMaterial.color = new Color(0, 1, 0, 0.5f); // 绿色半透明，方便识别

                // 针对 URP/Lit 或 URP/Unlit Shader 的透明设置
                // URP Shader 通常通过 _Surface 属性控制渲染模式 (Opaque, Transparent, etc.)
                // _Surface 1 for Transparent (Fade)
                previewMaterial.SetFloat("_Surface", 1); // Set to Transparent
                previewMaterial.SetOverrideTag("RenderType", "Transparent");
                previewMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                previewMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                previewMaterial.SetInt("_ZWrite", 0);
                previewMaterial.DisableKeyword("_ALPHATEST_ON");
                previewMaterial.EnableKeyword("_ALPHABLEND_ON");
                previewMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                previewMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                // For URP Lit shader, you might also need to set _BlendMode to 0 (Alpha)
                previewMaterial.SetFloat("_Blend", 0); // Set to Alpha Blend mode for URP Lit

                previewRenderer.material = previewMaterial;
            }
            else
            {
                Debug.LogWarning($"PlayerBuilding: 预览预制体 '{currentBuildingPrefab.name}' 上没有找到 Renderer 组件，无法设置预览材质。", buildingPreviewInstance);
            }

            // 禁用预览模型的碰撞体，防止与场景中的其他对象发生不必要的交互
            Collider previewCollider = buildingPreviewInstance.GetComponent<Collider>();
            if (previewCollider == null) // 如果预制体本身没有Collider，尝试在子对象中查找
            {
                previewCollider = buildingPreviewInstance.GetComponentInChildren<Collider>();
            }
            if (previewCollider != null)
            {
                previewCollider.enabled = false;
            }
            // --- 核心修改结束 ---
        }
    }

    /// <summary>
    /// 更新建筑预览实例的位置。
    /// </summary>
    void UpdateBuildingPreview()
    {
        if (buildingPreviewInstance == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayerForBuilding))
        {
            buildingPreviewInstance.transform.position = hit.point;
            // 可以添加旋转逻辑，例如：
            // buildingPreviewInstance.transform.rotation = Quaternion.Euler(0, Mathf.Round(transform.eulerAngles.y / 90) * 90, 0);
        }
    }

    /// <summary>
    /// 销毁建筑预览实例。
    /// </summary>
    void DestroyBuildingPreview()
    {
        if (buildingPreviewInstance != null)
        {
            Destroy(buildingPreviewInstance);
            buildingPreviewInstance = null;
        }
    }

    // 辅助方法，用于检查UI引用并打印日志
    private void CheckUIReference<T>(T uiReference, string referenceName) where T : Object
    {
        if (uiReference == null)
        {
            Debug.LogError($"PlayerBuilding: UI引用 '{referenceName}' 未赋值！请确保在 Inspector 中正确拖拽了对应的UI元素。", this);
        }
        else
        {
            Debug.Log($"PlayerBuilding: UI引用 '{referenceName}' 已成功赋值。");
        }
    }

    void OnDestroy()
    {
        // 确保在脚本销毁时清理预览实例
        DestroyBuildingPreview();
    }
}
