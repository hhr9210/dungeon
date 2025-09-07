using UnityEngine;

public class DestructibleObject : MonoBehaviour, IDamageable
{
    [Header("Destructible Properties")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Destruction Replacement (Prefab)")]
    public GameObject replacementPrefab;

    [Header("Effects (Optional)")]
    public GameObject destructionEffectPrefab;
    public AudioClip destructionSound;
    private AudioSource audioSource;

    void Awake()
    {
        currentHealth = maxHealth;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (currentHealth <= 0)
        {
            return;
        }

        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"{gameObject.name} 受到 {damageAmount} 伤害。当前生命值: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            DestroyObject();
        }
    }

    private void DestroyObject()
    {
        Debug.Log($"{gameObject.name} 正在被摧毁！");

        if (audioSource != null && destructionSound != null)
        {
            audioSource.PlayOneShot(destructionSound);
        }
        else if (destructionSound != null)
        {
            AudioSource.PlayClipAtPoint(destructionSound, transform.position);
        }

        if (destructionEffectPrefab != null)
        {
            GameObject effect = Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);
        }

        if (replacementPrefab != null)
        {
            GameObject newObject = Instantiate(replacementPrefab, transform.position, transform.rotation);
            Debug.Log($"在 {gameObject.name} 的位置和旋转生成了替代预制体: {newObject.name}", newObject);
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: 没有指定替代预制体，将直接销毁此对象而没有任何替换物", this);
        }

        Destroy(gameObject);
    }
}
