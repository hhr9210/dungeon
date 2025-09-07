using UnityEngine;
using UnityEngine.UI; // ���� UI Image
using System.Collections.Generic;

public class RoomHP : MonoBehaviour
{
    // --- ��������������ֵ��UI���� ---
    [Header("��������")]
    public float maxHealth = 250f; // RoomHP ���������ֵ�����Ը�����Ҫ����
    private float currentHealth; // ������ǰ����ֵ

    [Header("UI���� (��ѡ)")]
    [Tooltip("����ק���HPѪ��CanvasԤ���嵽�����CanvasӦ����һ��һ����Ϊ'Fill'��Image�������Ѫ����䡣")]
    public GameObject hpBarPrefab; // ������ק��� HP_UI Canvas Ԥ����
    private Image hpFill;
    private Transform hpBarTransform; // ����ʵ��������Canvas��Transform

    [Header("Ч������ (��ѡ)")]
    public GameObject destructionEffectPrefab; // ����ʱ������Ч���򶯻�
    // --- �������� ---


    [Header("�ָ�����")]
    [Tooltip("��Ҫ�ָ�Ѫ���Ķ���Tag")]
    public string targetTag = "Player"; // Ĭ������ΪPlayer��ǩ
    [Tooltip("ÿ��ָ���Ѫ��ֵ")]
    public float healthPerSecond = 5f;
    [Tooltip("�Ƿ���ʾ������Ϣ")]
    public bool showDebug = false;
    [Tooltip("�Ƿ��������Ѫ���ָ�")]
    public bool limitToMaxHealth = true;

    // �洢�����ڵ����пɻָ�����
    private Dictionary<GameObject, PlayerHealth> targetsInZone = new Dictionary<GameObject, PlayerHealth>();

    void Start()
    {
        currentHealth = maxHealth; // ��ʼ������ֵ
        SetupHPBar(); // ����Ѫ��

        // RoomHP ������Ҫһ�� Collider ������ OnTriggerEnter/Exit
        Collider col = GetComponent<Collider>();
        if (col == null || !col.isTrigger)
        {
            Debug.LogWarning($"RoomHP: {gameObject.name} ��Ҫһ�������� Collider ������ܹ�����", this);
        }
    }

    void Update()
    {
        // ����Ѫ��λ���Ը��潨��
        if (hpBarTransform != null)
        {
            hpBarTransform.position = transform.position + Vector3.up * 3f; // ����ƫ����
            if (Camera.main != null)
            {
                hpBarTransform.forward = Camera.main.transform.forward;
            }
        }

        // ÿ��ָ�Ѫ�� (ԭ RoomHP �߼�)
        // ʹ�� ToList ������ foreach ���޸ļ���
        List<GameObject> currentTargets = new List<GameObject>(targetsInZone.Keys);
        foreach (var go in currentTargets)
        {
            // ���¼���Ƿ���������������Ч���Է� OnTriggerExit �ӳ�
            if (targetsInZone.TryGetValue(go, out PlayerHealth playerHealth))
            {
                if (playerHealth != null && go != null && go.activeInHierarchy)
                {
                    // ����Ƿ���Ҫ�ָ���δ��Ѫ�������ƣ�
                    if (!limitToMaxHealth || playerHealth.GetCurrentHealth() < playerHealth.maxHealth)
                    {
                        playerHealth.Heal(healthPerSecond * Time.deltaTime);
                        if (showDebug) Debug.Log($"{go.name} �ָ� {healthPerSecond * Time.deltaTime:F2} ��Ѫ��");
                    }
                }
                else
                {
                    // Ŀ����Ч��������Ϊ�Ƴ�
                    targetsInZone.Remove(go);
                    if (showDebug) Debug.Log($"RoomHP: �Ƴ���ЧĿ�� {go?.name ?? "null"}");
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
                Debug.LogError($"RoomHP: �� {hpBarInstance.name} ���Ҳ�����Ϊ 'Background/Fill' �� Image ��������� HP_UI Ԥ����Ľṹ����ȷ��Canvas���д��Ӷ����Image�����", hpBarInstance);
            }

            hpBarTransform.SetParent(null); // ����Ѫ��Ϊ��������
            UpdateHealthUI(); // ��ʼ����Ѫ��UI
        }
        else
        {
            Debug.LogWarning("RoomHP: hpBarPrefab δ��ֵ��������������ʾѪ����", this);
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

        // ���� RoomHP ����
        Destroy(gameObject);
    }

    /// <summary>
    /// �� RoomHP ������ʱ��ͬʱ������Ѫ��UI��
    /// </summary>
    void OnDestroy()
    {
        if (hpBarTransform != null)
        {
            Destroy(hpBarTransform.gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // ������Ķ����Ƿ���ָ��Tag
        if (other.CompareTag(targetTag))
        {
            // ��ȡPlayerHealth���
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // ��ӵ��ֵ���
                if (!targetsInZone.ContainsKey(other.gameObject))
                {
                    targetsInZone.Add(other.gameObject, playerHealth);
                    if (showDebug) Debug.Log($"{other.name} ����ָ�����");
                }
            }
            else if (showDebug)
            {
                Debug.LogWarning($"{other.name} �� {targetTag} ��ǩ��û�� PlayerHealth ���", other.gameObject);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // ����뿪�Ķ����Ƿ���ָ��Tag
        if (other.CompareTag(targetTag))
        {
            // ���ֵ����Ƴ�
            if (targetsInZone.ContainsKey(other.gameObject))
            {
                targetsInZone.Remove(other.gameObject);
                if (showDebug) Debug.Log($"{other.name} �뿪�ָ�����");
            }
        }
    }

    // ���������ٵĶ��� (ԭ RoomHP �߼����������� Update ѭ���еļ���Ż�)
    // �����������ɾ������Ϊ Update �ڲ��Ѿ��и�ʵʱ�ļ����Ƴ��ˡ�
    /*
    private void CleanUpDestroyedObjects()
    {
        List<GameObject> toRemove = new List<GameObject>();

        foreach (var target in targetsInZone)
        {
            if (target.Key == null || target.Value == null)
            {
                toRemove.Add(target.Key);
            }
        }

        foreach (var key in toRemove)
        {
            targetsInZone.Remove(key);
            if (showDebug) Debug.Log("�����������ٵĶ���");
        }
    }
    */

    // �ڱ༭�����ӻ�����
    private void OnDrawGizmos()
    {
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f); // ��͸����ɫ
            if (collider is BoxCollider boxCollider)
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.position + boxCollider.center, transform.rotation, transform.lossyScale);
                Gizmos.DrawCube(Vector3.zero, boxCollider.size);
            }
            else if (collider is SphereCollider sphereCollider)
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.position + sphereCollider.center, transform.rotation, transform.lossyScale);
                Gizmos.DrawSphere(Vector3.zero, sphereCollider.radius);
            }
            else if (collider is CapsuleCollider capsuleCollider)
            {
                // ����������Ը��ӣ������ΪԲ��
                Gizmos.matrix = Matrix4x4.TRS(transform.position + capsuleCollider.center, transform.rotation, transform.lossyScale);
                Gizmos.DrawSphere(Vector3.zero, capsuleCollider.radius); // ʹ���������
            }
        }
    }
}