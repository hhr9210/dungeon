using UnityEngine;
using UnityEngine.UI; // ���� UI Image
using System.Collections;
using System.Collections.Generic;

public class RoomSoldier : MonoBehaviour
{
    [Header("��������")]
    public float maxHealth = 200f; // �������������ֵ
    private float currentHealth; // ������ǰ����ֵ

    [Header("UI���� (��ѡ)")]
    [Tooltip("����ק���HPѪ��CanvasԤ���嵽�����CanvasӦ����һ����Ϊ'Fill'��Image�������Ѫ����䡣")]
    public GameObject hpBarPrefab; // ������ק��� HP_UI Canvas Ԥ����
    private Image hpFill;
    private Transform hpBarTransform; // ����ʵ��������Canvas��Transform

    [Header("Ч������ (��ѡ)")]
    public GameObject destructionEffectPrefab; // ����ʱ������Ч���򶯻�

    // --- ʿ��Ԥ�������� ---
    [Header("ʿ��Ԥ��������")]
    [Tooltip("Ҫ���ɵ�ʿ��Ԥ����")]
    public GameObject soldierPrefab;

    [Header("��ʼ��������")]
    [Tooltip("��Ϸ��ʼʱһ�������ɵ�ʿ������")]
    public int initialSoldierCount = 5;

    [Header("��ʱ��������")]
    [Tooltip("�Ƿ�����ʱ����")]
    public bool enableTimedSpawning = true;
    [Tooltip("ÿ�ζ�ʱ���ɵ�ʿ������")]
    public int timedSpawnCount = 2;
    [Tooltip("��������֮���ʱ�������룩")]
    public float spawnInterval = 10f;

    [Header("����λ������")]
    [Tooltip("ʿ����������İ뾶��Χ�Ʊ�Ӫ����")]
    public float spawnRadius = 5f;
    [Tooltip("ÿ�γ��Բ�������λ�õ�����������������ѭ��")]
    public int maxSpawnAttempts = 10;
    [Tooltip("ʿ��Ԥ����İ뾶����ײ���С�����ڱ����ص���ȷ��ʿ��Ԥ��������ײ�壡")]
    public float soldierDetectionRadius = 0.8f;

    private List<GameObject> spawnedSoldiers = new List<GameObject>();


    void Start()
    {
        currentHealth = maxHealth; // ��ʼ������ֵ
        SetupHPBar(); // �����µ�����Ѫ������

        // 1. ��Ϸ��ʼʱһ��������ʿ��
        SpawnSoldiers(initialSoldierCount);

        // 2. ��������˶�ʱ���ɣ�������Э��
        if (enableTimedSpawning)
        {
            StartCoroutine(TimedSoldierSpawn());
        }
    }

    void Update()
    {
        // ����Ѫ��λ���Ը��潨��
        if (hpBarTransform != null)
        {
            hpBarTransform.position = transform.position + Vector3.up * 3f; // ����ƫ����������Ѫ���ڽ����Ϸ�

            // ȷ��Ѫ��ʼ�����������
            if (Camera.main != null)
            {
                hpBarTransform.forward = Camera.main.transform.forward;
            }
        }
    }

    /// <summary>
    /// ���ò�ʵ����������Ѫ����
    /// </summary>
    void SetupHPBar()
    {
        if (hpBarPrefab != null)
        {
            // ʵ����Ѫ��Ԥ���壬�����䶨λ�ڽ����Ϸ�
            GameObject hpBarInstance = Instantiate(hpBarPrefab, transform.position + Vector3.up * 3f, Quaternion.identity);
            hpBarTransform = hpBarInstance.transform;

            // ���Բ���Ѫ����� Image ���������ٶ���·���� "Background/Fill"
            hpFill = hpBarTransform.Find("Background/Fill")?.GetComponent<Image>();

            if (hpFill == null)
            {
                Debug.LogError($"Barracks: �� {hpBarInstance.name} ���Ҳ�����Ϊ 'Background/Fill' �� Image ��������� HP_UI Ԥ����Ľṹ����ȷ��Canvas���д��Ӷ����Image�����", hpBarInstance);
            }

            // ��Ѫ������Ϊ���������κθ�����ʹ���ܶ������潨��λ��
            hpBarTransform.SetParent(null);
            UpdateHealthUI(); // ��ʼ����Ѫ��UI
        }
        else
        {
            Debug.LogWarning("Barracks: hpBarPrefab δ��ֵ��������������ʾѪ����", this);
        }
    }

    /// <summary>
    /// ���ٽ�����ǰ����ֵ��
    /// </summary>
    /// <param name="amount">�ܵ����˺�����</param>
    public void TakeDamage(float amount)
    {
        Debug.Log($"{gameObject.name} �� TakeDamage ���������ã��˺���: {amount}");
        if (currentHealth <= 0)
        {
            Debug.Log($"{gameObject.name} �Ѿ��ݻ٣������ܵ��˺���");
            return;
        }

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0); // ȷ������ֵ������0

        Debug.Log($"{gameObject.name} �ܵ��� {amount} ���˺�����ǰ����ֵ: {currentHealth}");

        UpdateHealthUI(); // ����Ѫ��UI

        if (currentHealth <= 0)
        {
            DestroyBuilding(); // ����ֵ���㣬���ٽ���
        }
    }

    /// <summary>
    /// ���½�������ֵUI�����磬Ѫ����䣩��
    /// </summary>
    void UpdateHealthUI()
    {
        if (hpFill != null)
        {
            hpFill.fillAmount = currentHealth / maxHealth;
        }
    }

    /// <summary>
    /// ���������ٵ��߼���
    /// </summary>
    void DestroyBuilding()
    {
        Debug.Log($"{gameObject.name} �ѱ��ݻ٣�");

        // ʵ��������Ч��
        if (destructionEffectPrefab != null)
        {
            Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);
        }

        // ���ٱ�Ӫ����
        Destroy(gameObject);
    }

    /// <summary>
    /// ����Ӫ������ʱ��ͬʱ������Ѫ��UI��
    /// </summary>
    void OnDestroy()
    {
        if (hpBarTransform != null)
        {
            // ȷ�����ٵ���Ѫ���� GameObject�������ǽ����� Transform
            Destroy(hpBarTransform.gameObject);
        }
        // ��ѡ�����������ɵ�ʿ���б���֪ͨ���Ǳ�Ӫ�ѱ�����
        // for (int i = spawnedSoldiers.Count - 1; i >= 0; i--)
        // {
        //     if (spawnedSoldiers[i] != null)
        //     {
        //         // spawnedSoldiers[i].GetComponent<SoldierAI>()?.SetBarracksDestroyed();
        //     }
        // }
        // spawnedSoldiers.Clear();
    }


    /// <summary>
    /// ����ָ��������ʿ��
    /// </summary>
    /// <param name="count">Ҫ���ɵ�ʿ������</param>
    private void SpawnSoldiers(int count)
    {
        if (soldierPrefab == null)
        {
            Debug.LogError("ʿ��Ԥ����δָ�������� Inspector ����ק Soldier Prefab��", this);
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPosition = FindValidSpawnPosition();

            if (spawnPosition != Vector3.zero)
            {
                GameObject newSoldier = Instantiate(soldierPrefab, spawnPosition, Quaternion.identity);
                spawnedSoldiers.Add(newSoldier);

                Debug.Log($"������ʿ����{newSoldier.name} ��λ�� {newSoldier.transform.position}", newSoldier);
            }
            else
            {
                Debug.LogWarning("δ���ҵ�һ�����ص���ʿ������λ�ã����� spawnRadius �� maxSpawnAttempts��", this);
            }
        }
    }

    /// <summary>
    /// ����һ�����ص�����Ч����λ��
    /// </summary>
    /// <returns>һ�����ص���Vector3λ�ã����ʧ���򷵻�Vector3.zero</returns>
    private Vector3 FindValidSpawnPosition()
    {
        Vector3 basePosition = transform.position;

        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            // �ڱ�Ӫ��Χ��Բ���������������һ����
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 potentialSpawnPosition = new Vector3(basePosition.x + randomCircle.x, basePosition.y, basePosition.z + randomCircle.y);

            // ������λ���Ƿ�����������巢���ص�
            // Physics.CheckSphere ����true����������κ���ײ���ཻ
            if (!Physics.CheckSphere(potentialSpawnPosition, soldierDetectionRadius))
            {
                return potentialSpawnPosition; // �ҵ���Чλ��
            }
        }

        return Vector3.zero; // δ���ҵ���Чλ��
    }

    /// <summary>
    /// ��ʱ����ʿ����Э��
    /// </summary>
    private IEnumerator TimedSoldierSpawn()
    {
        // ֻ�е���Ӫ�����ҿ�����ʱ����ʱ�ż���
        while (enableTimedSpawning && currentHealth > 0)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnSoldiers(timedSpawnCount);
        }
    }

    // ��ѡ���ڱ༭������ʾ���ɷ�Χ�ͼ�����壬�������
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, soldierDetectionRadius);
    }
}