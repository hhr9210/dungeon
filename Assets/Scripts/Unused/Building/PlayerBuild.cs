using UnityEngine;
using UnityEngine.UI; // For UI panel management
using System.Collections.Generic; // For List

// ����һ�������л����࣬���ڴ洢ÿ���ɽ��콨������Ϣ
[System.Serializable]
public class BuildableBuilding
{
    [Tooltip("Ҫ����Ľ���Ԥ���塣")]
    public GameObject prefab;
    [Tooltip("����˽�������Ľ�Ǯ��")]
    public float cost = 100f;
    [Tooltip("�˽��������ƣ�����UI����־��")]
    public string buildingName = "�½���";
}

public class PlayerBuild : MonoBehaviour
{
    [Header("���칦��")]
    [Tooltip("����ק��Ľ���UI��壨ͨ����һ��Canvas�µ�Panel�������")]
    public GameObject buildingUIPanel;      // ����UI��������

    [Tooltip("���ڽ������õĵ���㡣ȷ����ĵ���GameObject�ڴ˲㡣")]
    public LayerMask groundLayerForBuilding; // ���ڽ������õĵ����

    [Header("�ɽ��콨���б�")]
    [Tooltip("�������������пɽ���Ľ������������ǵ�Ԥ����ͽ���۸�")]
    public List<BuildableBuilding> buildableBuildings = new List<BuildableBuilding>();

    private bool isBuildingMode = false;        // �Ƿ��ڽ���ģʽ
    private GameObject currentBuildingPrefab;   // ��ǰѡ��Ľ���Ԥ����
    private float currentBuildingCost;          // ��ǰѡ�����Ľ���۸�
    private GameObject buildingPreviewInstance; // ����Ԥ��ʵ�� (��ѡ)

    private Inventory inventory; // ���� Inventory �ű�

    void Awake()
    {
        // ��ȡ Inventory �ű�����
        inventory = GetComponent<Inventory>();
        if (inventory == null)
        {
            Debug.LogError("PlayerBuilding: Inventory �ű�δ�ҵ�����ȷ�����GameObject�Ϲ�����Inventory�ű���", this);
        }

        // ȷ������UI����ڿ�ʼʱ�����ص�
        if (buildingUIPanel != null)
        {
            buildingUIPanel.SetActive(false);
        }

        // ���ԣ����UI�����Ƿ���ȷ��ֵ
        Debug.Log("--- PlayerBuilding UI���õ��� ---");
        CheckUIReference(buildingUIPanel, "Building UI Panel");
        // ��� buildableBuildings �б��е�ÿ��Ԥ�����Ƿ��Ѹ�ֵ
        for (int i = 0; i < buildableBuildings.Count; i++)
        {
            CheckUIReference(buildableBuildings[i].prefab, $"Buildable Building {i} Prefab");
        }
        Debug.Log("--- PlayerBuilding UI���õ��Խ��� ---");
    }

    void Update()
    {
        // ����ģʽ�л� (F ��)
        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleBuildingMode();
        }

        // ������ڽ���ģʽ����ѡ����
        if (isBuildingMode && currentBuildingPrefab != null)
        {
            HandleBuildingPlacement();
        }
    }

    /// <summary>
    /// �л�����ģʽ�Ŀ���/�رա�
    /// </summary>
    void ToggleBuildingMode()
    {
        isBuildingMode = !isBuildingMode;
        if (buildingUIPanel != null)
        {
            buildingUIPanel.SetActive(isBuildingMode);
        }
        Debug.Log($"PlayerBuilding: ����ģʽ: {(isBuildingMode ? "����" : "�ر�")}");

        // ����رս���ģʽ������ǰѡ���Ԥ��
        if (!isBuildingMode)
        {
            currentBuildingPrefab = null;
            currentBuildingCost = 0f; // ����ɱ�
            DestroyBuildingPreview();
        }
    }

    /// <summary>
    /// ѡ��һ������Ԥ������н��졣
    /// ���������󶨵�����UI��ť��OnClick�¼���
    /// </summary>
    /// <param name="prefabToSelect">Ҫ����Ľ���Ԥ���塣</param>
    public void SelectBuilding(GameObject prefabToSelect)
    {
        Debug.Log($"PlayerBuilding: SelectBuilding ���������á������Ԥ����: {(prefabToSelect != null ? prefabToSelect.name : "NULL")}");

        if (!isBuildingMode)
        {
            Debug.LogWarning("PlayerBuilding: δ���ڽ���ģʽ���޷�ѡ������");
            return; // �����ڽ���ģʽ�²���ѡ��
        }

        // �� buildableBuildings �б��в���ƥ��Ľ�����Ϣ
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
            Debug.LogError($"PlayerBuilding: �����Ԥ���� '{prefabToSelect?.name}' δ�� 'Buildable Buildings' �б����ҵ������� Inspector ���á�", prefabToSelect);
            return;
        }

        currentBuildingPrefab = selectedBuildingInfo.prefab;
        currentBuildingCost = selectedBuildingInfo.cost; // �洢��ǰ�����ĳɱ�
        Debug.Log($"PlayerBuilding: ��ѡ����: {selectedBuildingInfo.buildingName}������۸�: {currentBuildingCost}");

        // ��������Ԥ�� (��ѡ)
        CreateBuildingPreview();
    }

    /// <summary>
    /// �������ķ����߼���
    /// </summary>
    void HandleBuildingPlacement()
    {
        // ���½���Ԥ��λ��
        UpdateBuildingPreview();

        // ������������ý���
        if (Input.GetMouseButtonDown(0)) // ������
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // ���߼�����
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayerForBuilding))
            {
                // --- ����Ǯ�Ƿ��㹻 ---
                if (inventory != null && inventory.CanAfford(currentBuildingCost))
                {
                    // �۳���Ǯ
                    inventory.SpendMoney(currentBuildingCost);

                    // �ڵ���ĵ���λ��ʵ��������
                    Instantiate(currentBuildingPrefab, hit.point, Quaternion.identity);
                    Debug.Log($"PlayerBuilding: �ɹ����� {currentBuildingPrefab.name} �� {hit.point}������ {currentBuildingCost} ��Ǯ��");

                    // ���ú���������ǰѡ���Ա����ѡ�������������˳�����ģʽ
                    currentBuildingPrefab = null; // �����ǰѡ��
                    currentBuildingCost = 0f;     // ����ɱ�
                    DestroyBuildingPreview();     // ����Ԥ��
                    // ���ϣ������һ���������˳�����ģʽ������ȡ��ע���������У�
                    // ToggleBuildingMode();
                }
                else if (inventory != null)
                {
                    Debug.LogWarning($"PlayerBuilding: ��Ǯ���㣡�޷����� {currentBuildingPrefab.name}����Ҫ {currentBuildingCost}����ǰֻ�� {inventory.currentMoney}��");
                }
                else
                {
                    Debug.LogError("PlayerBuilding: Inventory �ű�δ��ʼ�����޷�����Ǯ��");
                }
            }
            else
            {
                Debug.Log("PlayerBuilding: δ�������Ч���棬�޷����ý�����");
            }
        }
    }

    /// <summary>
    /// ��������Ԥ��ʵ����
    /// </summary>
    void CreateBuildingPreview()
    {
        DestroyBuildingPreview(); // �����پɵ�Ԥ��

        if (currentBuildingPrefab != null)
        {
            buildingPreviewInstance = Instantiate(currentBuildingPrefab);

            // --- �����޸ģ�ΪԤ��ģ�����ð�͸�����ʲ�������ײ�� ---
            Renderer previewRenderer = buildingPreviewInstance.GetComponent<Renderer>();
            if (previewRenderer == null) // ���Ԥ���屾��û��Renderer���������Ӷ����в���
            {
                previewRenderer = buildingPreviewInstance.GetComponentInChildren<Renderer>();
            }

            if (previewRenderer != null)
            {
                // URP ���ݵ���ɫ��
                Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
                if (urpLitShader == null)
                {
                    Debug.LogError("PlayerBuilding: �޷��ҵ� 'Universal Render Pipeline/Lit' Shader��Ԥ��ģ�ͽ���ʾΪ�ۺ�ɫ����ȷ����Ŀ����ȷ���� URP��");
                    // ����ʹ�� Unlit Shader ��Ϊ����
                    urpLitShader = Shader.Find("Universal Render Pipeline/Unlit");
                    if (urpLitShader == null)
                    {
                        Debug.LogError("PlayerBuilding: �޷��ҵ� 'Universal Render Pipeline/Unlit' Shader��Ԥ��ģ�ͽ�������ʾΪ�ۺ�ɫ��");
                        return; // �޷��ҵ��κο���Shader��ֱ�ӷ���
                    }
                }

                Material previewMaterial = new Material(urpLitShader);
                previewMaterial.color = new Color(0, 1, 0, 0.5f); // ��ɫ��͸��������ʶ��

                // ��� URP/Lit �� URP/Unlit Shader ��͸������
                // URP Shader ͨ��ͨ�� _Surface ���Կ�����Ⱦģʽ (Opaque, Transparent, etc.)
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
                Debug.LogWarning($"PlayerBuilding: Ԥ��Ԥ���� '{currentBuildingPrefab.name}' ��û���ҵ� Renderer ������޷�����Ԥ�����ʡ�", buildingPreviewInstance);
            }

            // ����Ԥ��ģ�͵���ײ�壬��ֹ�볡���е���������������Ҫ�Ľ���
            Collider previewCollider = buildingPreviewInstance.GetComponent<Collider>();
            if (previewCollider == null) // ���Ԥ���屾��û��Collider���������Ӷ����в���
            {
                previewCollider = buildingPreviewInstance.GetComponentInChildren<Collider>();
            }
            if (previewCollider != null)
            {
                previewCollider.enabled = false;
            }
            // --- �����޸Ľ��� ---
        }
    }

    /// <summary>
    /// ���½���Ԥ��ʵ����λ�á�
    /// </summary>
    void UpdateBuildingPreview()
    {
        if (buildingPreviewInstance == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayerForBuilding))
        {
            buildingPreviewInstance.transform.position = hit.point;
            // ���������ת�߼������磺
            // buildingPreviewInstance.transform.rotation = Quaternion.Euler(0, Mathf.Round(transform.eulerAngles.y / 90) * 90, 0);
        }
    }

    /// <summary>
    /// ���ٽ���Ԥ��ʵ����
    /// </summary>
    void DestroyBuildingPreview()
    {
        if (buildingPreviewInstance != null)
        {
            Destroy(buildingPreviewInstance);
            buildingPreviewInstance = null;
        }
    }

    // �������������ڼ��UI���ò���ӡ��־
    private void CheckUIReference<T>(T uiReference, string referenceName) where T : Object
    {
        if (uiReference == null)
        {
            Debug.LogError($"PlayerBuilding: UI���� '{referenceName}' δ��ֵ����ȷ���� Inspector ����ȷ��ק�˶�Ӧ��UIԪ�ء�", this);
        }
        else
        {
            Debug.Log($"PlayerBuilding: UI���� '{referenceName}' �ѳɹ���ֵ��");
        }
    }

    void OnDestroy()
    {
        // ȷ���ڽű�����ʱ����Ԥ��ʵ��
        DestroyBuildingPreview();
    }
}
