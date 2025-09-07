using UnityEngine;
using TMPro;
using System.Collections;

public class PlayerShoot : MonoBehaviour
{
    [Header("射击属性")]
    public GameObject projectilePrefab;
    public float shootForce = 10f;
    public Transform shootPoint;
    public float projectileDamage = 20f;

    [Header("子弹系统设置")]
    public Inventory playerInventory;
    public Item bulletItemData;
    public TextMeshProUGUI bulletCountText;
    public GameObject bulletUIPanel;

    [Header("音效设置")]
    public AudioClip shootSound;
    public AudioClip noAmmoSound;
    private AudioSource audioSource;

    private Coroutine hidePanelCoroutine;
    private const float PANEL_HIDE_DELAY = 2f;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
    }

    void Start()
    {
        if (playerInventory == null)
        {
            playerInventory = FindObjectOfType<Inventory>();
            if (playerInventory == null)
            {
                Debug.LogError("PlayerShoot: 未找到 Inventory 脚本。请确保场景中有一个带有 Inventory 脚本的 GameObject。", this);
                enabled = false;
                return;
            }
        }

        UpdateBulletUI();

        if (bulletUIPanel != null)
        {
            bulletUIPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("PlayerShoot: Bullet UI Panel 未赋值。子弹 UI 的可见性将无法控制。", this);
        }
    }

    void OnEnable()
    {
        if (playerInventory != null)
        {
            Inventory.OnInventoryContentChanged += UpdateBulletUI;
        }
    }

    void OnDisable()
    {
        if (playerInventory != null)
        {
            Inventory.OnInventoryContentChanged -= UpdateBulletUI;
        }
        if (hidePanelCoroutine != null)
        {
            StopCoroutine(hidePanelCoroutine);
            hidePanelCoroutine = null;
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Shoot();
            StopAndStartHidePanelCoroutine();
        }
    }

    void Shoot()
    {
        if (bulletItemData == null)
        {
            Debug.LogWarning("PlayerShoot: 未设置子弹 Item Data，无法判断子弹类型。", this);
            return;
        }

        if (playerInventory == null)
        {
            Debug.LogError("PlayerShoot: Inventory 引用为空。无法获取子弹数量。", this);
            return;
        }

        int currentBullets = playerInventory.GetItemQuantity(bulletItemData);
        if (currentBullets <= 0)
        {
            Debug.Log("没有足够的子弹，无法射击。", this);
            if (audioSource != null && noAmmoSound != null)
            {
                audioSource.PlayOneShot(noAmmoSound);
            }
            return;
        }

        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }

        playerInventory.RemoveItem(bulletItemData, 1);

        Vector3 shootDirection = transform.forward;
        shootDirection.y = 0;
        shootDirection.Normalize();

        GameObject projectileGO = Instantiate(projectilePrefab, shootPoint.position, Quaternion.LookRotation(shootDirection));

        Bullet balletScript = projectileGO.GetComponent<Bullet>();
        if (balletScript != null)
        {
            balletScript.damageAmount = projectileDamage;
        }
        else
        {
            Debug.LogWarning("投掷物预制体上没有 Ballet 脚本，伤害值将不会被应用。", projectileGO);
        }

        Rigidbody rb = projectileGO.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(shootDirection * shootForce, ForceMode.Impulse);
        }
        else
        {
            Debug.LogWarning("投掷物预制体上没有 Rigidbody 组件，无法施加物理力。", projectileGO);
        }
    }

    private void UpdateBulletUI()
    {
        if (bulletItemData == null || bulletCountText == null || playerInventory == null)
        {
            return;
        }

        int currentBullets = playerInventory.GetItemQuantity(bulletItemData);
        bulletCountText.text = currentBullets.ToString();
        Debug.Log($"子弹数量已更新为: {currentBullets}");
    }

    private void StopAndStartHidePanelCoroutine()
    {
        if (hidePanelCoroutine != null)
        {
            StopCoroutine(hidePanelCoroutine);
        }
        
        if (bulletUIPanel != null)
        {
            bulletUIPanel.SetActive(true);
            hidePanelCoroutine = StartCoroutine(HidePanelAfterDelay(PANEL_HIDE_DELAY));
        }
    }

    IEnumerator HidePanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (bulletUIPanel != null && bulletUIPanel.activeSelf)
        {
            bulletUIPanel.SetActive(false);
        }
        hidePanelCoroutine = null;
    }
}
