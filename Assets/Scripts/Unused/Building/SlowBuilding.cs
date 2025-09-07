using UnityEngine;
using UnityEngine.UI; // ���� UI Image
using System.Collections.Generic; // ��Ȼ��Ҫ��������Ϊ�˼���Ч�����Է�������չ

public class SlowBuilding : MonoBehaviour
{
    // --- ��������ֵ��UI ---
    [Header("����������UI")]
    public float maxHealth = 300f; // �����������ֵ
    private float currentHealth;
    [Tooltip("����ק���HPѪ��CanvasԤ���嵽�����CanvasӦ����һ����Ϊ'Fill'��Image�������Ѫ����䡣")]
    public GameObject hpBarPrefab;
    private Image hpFill;
    private Transform hpBarTransform;

    // --- �ݻٱ�ըЧ�� ---
    [Header("�ݻٱ�ը")]
    public GameObject destructionEffectPrefab; // ����ʱ������Ч���򶯻�Ԥ����
    [Tooltip("������ըʱ�Ը���������ɵ��˺�ֵ��")]
    public float explosionDamage = 50f;
    [Tooltip("��ը�˺�Ӱ��İ뾶��")]
    public float explosionRadius = 7f;

    // �����������ײ�壬���ڱ����˹�����������Ҫ Is Trigger ������������
    private Collider buildingCollider;

    void Awake()
    {
        currentHealth = maxHealth; // ��ʼ������ֵ
    }

    void Start()
    {
        SetupHPBar(); // ����Ѫ��UI

        // ȷ��������һ�� Collider�����ڱ����˼��͹���
        buildingCollider = GetComponent<Collider>();
        if (buildingCollider == null)
        {
            Debug.LogError($"SlowBuilding: {gameObject.name} ��Ҫһ�� Collider ������ܱ����˹�����", this);
        }
        // ������ Collider ����Ϊʵ����ڣ���Ӧ�ù�ѡ Is Trigger
        if (buildingCollider != null && buildingCollider.isTrigger)
        {
            Debug.LogWarning($"SlowBuilding: {gameObject.name}'s Collider �� Is Trigger�����˿��ܴ����������ǹ�������", this);
        }

        // ע�⣺����û�м��ٹ��ܣ����ﲻ����Ҫ���� OnTriggerEnter/Exit �� Physics.OverlapSphere �ĳ�ʼ��⡣
    }

    void Update()
    {
        // ʵʱ����Ѫ��λ�úͳ���
        UpdateHPBarPosition();

        // ����û�м�������Ҳ����Ҫ���� enemiesInSlowZone �ֵ��ˡ�
    }

    /// <summary>
    /// ����Ѫ��UI������ռ��е�λ�úͳ���
    /// </summary>
    void UpdateHPBarPosition()
    {
        if (hpBarTransform != null)
        {
            hpBarTransform.position = transform.position + Vector3.up * 3f; // ����Ѫ���ڽ����Ϸ���ƫ����
            if (Camera.main != null)
            {
                // ȷ��Ѫ��ʼ�������ŵĲ������������
                Vector3 lookDirection = Camera.main.transform.forward;
                lookDirection.y = 0; // ����Y�ᣬֻ��ˮƽ�����������������ֹ��б
                if (lookDirection == Vector3.zero) lookDirection = transform.forward; // ��ֹ�������������ͬһֱ����

                hpBarTransform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }
    }

    /// <summary>
    /// ʵ����������Ѫ��UI��
    /// </summary>
    void SetupHPBar()
    {
        if (hpBarPrefab != null)
        {
            GameObject hpBarInstance = Instantiate(hpBarPrefab, transform.position + Vector3.up * 3f, Quaternion.identity);
            hpBarTransform = hpBarInstance.transform;

            // ����Ѫ�����Image���
            hpFill = hpBarTransform.Find("Background/Fill")?.GetComponent<Image>();
            if (hpFill == null)
            {
                Debug.LogError($"SlowBuilding: �� {hpBarInstance.name} ���Ҳ�����Ϊ 'Background/Fill' �� Image ��������� HP_UI Ԥ����Ľṹ��", hpBarInstance);
            }

            hpBarTransform.SetParent(null); // ��Ѫ������Ϊ���������󣬲��潨��������ת����
            UpdateHealthUI(); // ��ʼ����Ѫ����ʾ
        }
        else
        {
            Debug.LogWarning("SlowBuilding: hpBarPrefab δ��ֵ��������������ʾѪ����", this);
        }
    }

    /// <summary>
    /// �����ܵ��˺���
    /// </summary>
    /// <param name="amount">�ܵ����˺�ֵ��</param>
    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0) return; // �Ѿ��ݻ٣��������˺�

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0); // ȷ������ֵ��Ϊ��

        Debug.Log($"{gameObject.name} �ܵ��� {amount} ���˺�����ǰ����ֵ: {currentHealth}");
        UpdateHealthUI(); // ����Ѫ��UI

        if (currentHealth <= 0)
        {
            Explode(); // ����ֵ�ľ���������ը
        }
    }

    /// <summary>
    /// ����Ѫ��UI���������
    /// </summary>
    void UpdateHealthUI()
    {
        if (hpFill != null)
        {
            hpFill.fillAmount = currentHealth / maxHealth;
        }
    }

    /// <summary>
    /// ������ը�߼���
    /// </summary>
    void Explode()
    {
        Debug.Log($"{gameObject.name} �ѱ��ݻٲ���ը��");

        // 1. ʵ������ըЧ��
        if (destructionEffectPrefab != null)
        {
            Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);
        }

        // 2. �Ա�ը��Χ�ڵĵ�������˺�
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var hitCol in hitColliders)
        {
            Enemy enemy = hitCol.GetComponent<Enemy>();
            if (enemy != null && enemy.gameObject.activeInHierarchy) // ȷ���ǵ�������Ȼ���
            {
                enemy.TakeDamage(explosionDamage);
                Debug.Log($"SlowBuilding ��ը: {enemy.name} �ܵ��� {explosionDamage} ���˺���");
            }
        }

        // 3. ���ٽ�������
        Destroy(gameObject);
    }

    /// <summary>
    /// ��������ʱ��ͬʱ����Ѫ��UI��
    /// </summary>
    void OnDestroy()
    {
        if (hpBarTransform != null)
        {
            Destroy(hpBarTransform.gameObject);
        }
    }

    // �ڱ༭���л��Ʊ�ը��ΧGizmos
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f); // ��ը��Χ (��͸����ɫ)
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}