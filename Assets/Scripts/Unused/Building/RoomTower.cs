using UnityEngine;
using UnityEngine.UI;

public class RoomTower : MonoBehaviour
{
    [Header("��������")]
    public float maxHealth = 200f; // �������������ֵ

    private float currentHealth; // ������ǰ����ֵ

    [Header("UI���� (��ѡ)")]
    [Tooltip("����ק���HPѪ��CanvasԤ���嵽�����CanvasӦ����һ����Ϊ'Fill'��Image�������Ѫ����䡣")]
    public GameObject hpBarPrefab; // **������ק��� HP_UI Canvas Ԥ����**
    private Image hpFill;
    private Transform hpBarTransform; // ����ʵ��������Canvas��Transform

    [Header("Ч������ (��ѡ)")]
    public GameObject destructionEffectPrefab; // ����ʱ������Ч���򶯻�

    // --- ������������������ ---
    [Header("������������")]
    [Tooltip("�Ƿ���������������")]
    public bool enableAttack = true; // ��������������/�رտ���
    public float attackDamage = 15f;    // ���������˺�
    public float attackRange = 7f;      // ����������Χ
    public float attackRate = 1.5f;     // ����ÿ�빥������
    public LayerMask targetLayers;      // �������Թ����Ĳ� (���� Enemy)

    [Header("���������Ӿ�����Ч")]
    public GameObject attackEffectPrefab; // ����ʱ����ЧԤ���� (���磺�ڵ�������)
    public AudioClip attackSound;       // ������Ч

    private float nextAttackTime;       // �����´ο��Թ�����ʱ���
    private AudioSource audioSource;    // ���ڲ�����Ч

    void Awake() // ȷ�� AudioSource �� Start ֮ǰ����
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Start()
    {
        currentHealth = maxHealth;
        nextAttackTime = Time.time; // ��ʼ���´ι���ʱ��

        SetupHPBar(); // �����µ�����Ѫ������
    }

    void Update()
    {
        // ����Ѫ��λ���Ը��潨��
        if (hpBarTransform != null)
        {
            hpBarTransform.position = transform.position + Vector3.up * 3f; // ����ƫ����

            // ȷ��Ѫ��ʼ�����������
            if (Camera.main != null)
            {
                hpBarTransform.forward = Camera.main.transform.forward;
            }
        }

        // --- ���������߼� ---
        // ֻ�е� enableAttack Ϊ true ʱ��ִ�й����߼�
        if (enableAttack)
        {
            // 1. Ѱ�ҹ���Ŀ��
            GameObject currentTarget = FindAttackTarget();

            // 2. ����ҵ�Ŀ�꣬���Թ���
            if (currentTarget != null)
            {
                // ȷ��Ŀ����Ȼ�������ڹ�����Χ��
                float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
                if (distanceToTarget <= attackRange)
                {
                    RotateTowards(currentTarget.transform.position); // ����ת��Ŀ�� (��ѡ��ȡ���ڽ�������)
                    AttackTarget(currentTarget); // ����Ŀ��
                }
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
            GameObject hpBarInstance = Instantiate(hpBarPrefab, transform.position + Vector3.up * 3f, Quaternion.identity);
            hpBarTransform = hpBarInstance.transform;

            hpFill = hpBarTransform.Find("Background/Fill")?.GetComponent<Image>();

            if (hpFill == null)
            {
                Debug.LogError($"HouseManager: �� {hpBarInstance.name} ���Ҳ�����Ϊ 'Background/Fill' �� Image ��������� HP_UI Ԥ����Ľṹ����ȷ��Canvas���д��Ӷ����Image�����", hpBarInstance);
            }

            hpBarTransform.SetParent(null);
            UpdateHealthUI(); // ��ʼ����Ѫ��UI
        }
        else
        {
            Debug.LogWarning("HouseManager: hpBarPrefab δ��ֵ��������������ʾѪ����", this);
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
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"{gameObject.name} �ܵ��� {amount} ���˺�����ǰ����ֵ: {currentHealth}");

        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            DestroyBuilding();
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

        if (destructionEffectPrefab != null)
        {
            Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (hpBarTransform != null)
            Destroy(hpBarTransform.gameObject);
    }

    /// <summary>
    /// �����ڹ�����Χ��Ѱ������ĵ�����Ϊ����Ŀ�ꡣ
    /// </summary>
    /// <returns>����ĵ���GameObject�����û���򷵻�null��</returns>
    GameObject FindAttackTarget()
    {
        GameObject closestEnemy = null;
        float shortestDistance = attackRange + 1f;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange, targetLayers);

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.gameObject.activeInHierarchy)
            {
                Enemy enemy = hitCollider.GetComponent<Enemy>();
                if (enemy != null)
                {
                    float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
                    if (distance < shortestDistance)
                    {
                        shortestDistance = distance;
                        closestEnemy = hitCollider.gameObject;
                    }
                }
            }
        }
        return closestEnemy;
    }

    /// <summary>
    /// ��������ָ��Ŀ�ꡣ
    /// </summary>
    /// <param name="target">Ҫ������Ŀ��GameObject��</param>
    void AttackTarget(GameObject target)
    {
        if (target == null || !target.activeInHierarchy)
        {
            return;
        }

        if (Time.time >= nextAttackTime)
        {
            Debug.Log($"{gameObject.name} ������ {target.name}����� {attackDamage} �˺���");

            Enemy enemy = target.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(attackDamage);
            }

            if (attackEffectPrefab != null)
            {
                Instantiate(attackEffectPrefab, target.transform.position, Quaternion.identity);
            }
            if (audioSource != null && attackSound != null)
            {
                audioSource.PlayOneShot(attackSound);
            }

            nextAttackTime = Time.time + 1f / attackRate;
        }
    }

    /// <summary>
    /// ʹ��������ָ��λ�á�
    /// </summary>
    /// <param name="lookAtPosition">Ҫ�����λ�á�</param>
    void RotateTowards(Vector3 lookAtPosition)
    {
        Vector3 direction = (lookAtPosition - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}