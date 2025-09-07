using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("玩家基本移动属性")]
    public float moveSpeed = 5f;
    public float gravity = -9.81f;

    [Header("跳跃属性")]
    public float jumpHeight = 1.5f;
    public KeyCode jumpInputKey = KeyCode.Space;
    private float jumpVelocity;

    [Header("引用")]
    [Tooltip("请将用于控制视角的摄像机拖拽到此处。")]
    public Transform playerCameraTransform;

    private CharacterController controller;
    private Vector3 playerVelocity;

    [Header("体力系统")]
    public float maxStamina = 100f;
    public float staminaRegenRate = 10f;
    public float runStaminaCost = 15f;
    public float walkStaminaRegenMultiplier = 1.5f;
    public float forceWalkSpeedMultiplier = 0.5f;
    private float currentStamina;
    private bool isRunning = false;
    private bool isMovingHorizontally = false;

    [Header("UI 显示 (体力)")]
    public Image staminaFillImage;
    public TextMeshProUGUI staminaValueText;

    [Header("脚步声设置")]
    public AudioClip[] walkFootstepSounds;
    public AudioClip[] runFootstepSounds;
    public float walkStepInterval = 0.5f;
    public float runStepInterval = 0.3f;
    public AudioSource footstepAudioSource;
    public float backwardFootstepIntervalMultiplier = 1.5f;
    private float nextFootstepTime;
    private bool _wasMovingLastFrameForFootsteps = false;

    [Header("动画属性")]
    private Animator animator;

    [Header("力量和能力提升")]
    [SerializeField] private int currentStrength = 0;
    [SerializeField] private float currentMeleeAttackDamage = 30f;
    [SerializeField] private float currentMeleeAttackCooldown = 0.8f;
    [SerializeField] private float currentMeleeAttackRange = 2f;

    [Header("UI 显示 (攻击)")]
    public TextMeshProUGUI attackDamageText;
    public TextMeshProUGUI attackSpeedText;
    public TextMeshProUGUI attackRangeText;

    private bool canControlPlayer = true;
    private bool canControlCamera = true;
    private bool canSprint = true;

    [Header("玩家后退属性")]
    public float backwardSpeedMultiplier = 0.7f;

    private float _originalMoveSpeed;
    private bool _isSpeedBuffActive = false;
    private Coroutine _speedBuffCoroutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            Debug.LogError("PlayerController: 未找到 CharacterController 组件。此脚本需要一个。", this);
            enabled = false;
        }

        if (footstepAudioSource == null)
        {
            footstepAudioSource = GetComponent<AudioSource>();
            if (footstepAudioSource == null)
            {
                footstepAudioSource = gameObject.AddComponent<AudioSource>();
                Debug.LogWarning("PlayerController: 自动添加了脚步声 AudioSource。建议手动分配一个。", this);
            }
        }
        footstepAudioSource.playOnAwake = false;
        footstepAudioSource.loop = false;
        footstepAudioSource.spatialBlend = 0f;

        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("PlayerController: 在玩家 GameObject 上找不到 Animator 组件。动画将无法工作！", this);
        }
    }

    void OnEnable()
    {
        if (PauseManager.Instance != null)
        {
            PauseManager.PauseStateChanged += OnPauseStateChanged;
        }
        else
        {
            Debug.LogWarning("PlayerController: 场景中没有找到 PauseManager 实例！玩家控制可能无法正确响应暂停。", this);
        }
    }

    void OnDisable()
    {
        if (PauseManager.Instance != null)
        {
            PauseManager.PauseStateChanged -= OnPauseStateChanged;
        }
    }

    void Start()
    {
        currentStamina = maxStamina;
        UpdateStaminaUI();
        UpdateAttackUI();
        _originalMoveSpeed = moveSpeed;
        jumpVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        if (playerCameraTransform == null)
        {
            Debug.LogError("PlayerController: 'playerCameraTransform' 未赋值！请将摄像机拖拽到此脚本的对应槽位。", this);
            enabled = false;
            return;
        }
    }

    void Update()
    {
        if (!canControlPlayer)
        {
            if (animator != null)
            {
                animator.SetBool("IsMoving", false);
                animator.SetFloat("Speed", 0f);
            }
            if (footstepAudioSource != null && footstepAudioSource.isPlaying)
            {
                footstepAudioSource.Stop();
            }
            playerVelocity.x = 0;
            playerVelocity.z = 0;
            isMovingHorizontally = false;
            isRunning = false;
            return;
        }

        float verticalInput = Input.GetAxis("Vertical");
        Vector3 horizontalMoveDirection = CalculateMovementVector(verticalInput);
        HandlePlayerSprintState(verticalInput);
        float currentEffectiveMoveSpeed = GetCurrentMoveSpeed(verticalInput);
        Vector3 horizontalMovement = horizontalMoveDirection * currentEffectiveMoveSpeed;

        if (controller.isGrounded && Input.GetKeyDown(jumpInputKey))
        {
            playerVelocity.y = jumpVelocity;
        }

        playerVelocity.y += gravity * Time.deltaTime;
        Vector3 finalMove = horizontalMovement;
        finalMove.y = playerVelocity.y;
        controller.Move(finalMove * Time.deltaTime);

        if (controller.isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -0.5f;
        }

        HandleStamina();
        UpdateStaminaUI();
        UpdateAnimations();
        HandleFootsteps(verticalInput);
    }

    private void OnPauseStateChanged(bool isPaused)
    {
        SetCanMove(!isPaused);
        SetCanControlCamera(!isPaused);
    }

    private Vector3 CalculateMovementVector(float verticalInput)
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        Vector3 cameraForward = playerCameraTransform.forward;
        Vector3 cameraRight = playerCameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = cameraForward * verticalInput + cameraRight * horizontalInput;
        if (moveDirection.magnitude > 1f)
        {
            moveDirection.Normalize();
        }

        isMovingHorizontally = moveDirection.magnitude > 0.01f;

        // 让玩家角色面向移动方向
        if (isMovingHorizontally)
        {
            Quaternion toRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, Time.deltaTime * 500f);
        }

        return moveDirection;
    }

    private void HandlePlayerSprintState(float verticalInput)
    {
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            canSprint = true;
        }
        // 移除 && verticalInput >= 0;
        isRunning = Input.GetKey(KeyCode.LeftShift) && currentStamina > 0 && isMovingHorizontally && canSprint;
    }

    private float GetCurrentMoveSpeed(float verticalInput)
    {
        float baseSpeed;
        if (isRunning)
        {
            baseSpeed = moveSpeed;
        }
        else
        {
            baseSpeed = moveSpeed * forceWalkSpeedMultiplier;
        }

        if (verticalInput < 0)
        {
            return baseSpeed * backwardSpeedMultiplier;
        }
        return baseSpeed;
    }

    private void HandleStamina()
    {
        if (isMovingHorizontally)
        {
            if (isRunning)
            {
                if (currentStamina > 0)
                {
                    currentStamina -= runStaminaCost * Time.deltaTime;
                    if (currentStamina <= 0)
                    {
                        currentStamina = 0;
                        Debug.Log("体力耗尽！强制切换为行走。");
                        isRunning = false;
                        canSprint = false;
                    }
                }
            }
            else
            {
                currentStamina = Mathf.Min(currentStamina + staminaRegenRate * walkStaminaRegenMultiplier * Time.deltaTime, maxStamina);
            }
        }
        else
        {
            currentStamina = Mathf.Min(currentStamina + staminaRegenRate * Time.deltaTime, maxStamina);
        }
    }

    void UpdateStaminaUI()
    {
        if (staminaFillImage != null)
        {
            staminaFillImage.fillAmount = currentStamina / maxStamina;
        }
        if (staminaValueText != null)
        {
            staminaValueText.text = $"{Mathf.FloorToInt(currentStamina)} / {Mathf.FloorToInt(maxStamina)}";
        }
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;
        Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z);
        bool isCurrentlyMoving = horizontalVelocity.magnitude > 0.05f;
        animator.SetBool("IsMoving", isCurrentlyMoving);
        float animSpeedRatio = isCurrentlyMoving ? horizontalVelocity.magnitude / moveSpeed : 0f;
        animator.SetFloat("Speed", animSpeedRatio);
    }

    private void HandleFootsteps(float verticalInput)
    {
        bool isCurrentlyMovingForFootsteps = isMovingHorizontally && controller.isGrounded;
        if (isCurrentlyMovingForFootsteps)
        {
            if (!_wasMovingLastFrameForFootsteps)
            {
                nextFootstepTime = Time.time;
            }
            if (Time.time >= nextFootstepTime)
            {
                PlayFootstepSound(isRunning);
                float speedRatioForFootsteps = controller.velocity.magnitude / moveSpeed;
                speedRatioForFootsteps = Mathf.Clamp01(speedRatioForFootsteps);
                float currentStepInterval = Mathf.Lerp(walkStepInterval, runStepInterval, speedRatioForFootsteps);
                if (verticalInput < 0)
                {
                    currentStepInterval *= backwardFootstepIntervalMultiplier;
                }
                nextFootstepTime = Time.time + currentStepInterval;
            }
        }
        else
        {
            if (footstepAudioSource != null && footstepAudioSource.isPlaying)
            {
                footstepAudioSource.Stop();
            }
            nextFootstepTime = Time.time + 0.1f;
        }
        _wasMovingLastFrameForFootsteps = isCurrentlyMovingForFootsteps;
    }

    private void PlayFootstepSound(bool isRunningSound)
    {
        if (footstepAudioSource == null) return;
        AudioClip[] soundsToPlay = isRunningSound ? runFootstepSounds : walkFootstepSounds;
        if (soundsToPlay == null || soundsToPlay.Length == 0) return;
        AudioClip footstepSound = soundsToPlay[Random.Range(0, soundsToPlay.Length)];
        if (footstepSound == null) return;
        footstepAudioSource.pitch = Random.Range(0.9f, 1.1f);
        footstepAudioSource.volume = Random.Range(0.8f, 1.0f);
        footstepAudioSource.PlayOneShot(footstepSound);
    }

    public void RotateTowards(Vector3 lookAtPosition)
    {
        Vector3 direction = (lookAtPosition - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (controller != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position + controller.center, controller.radius);
            Gizmos.DrawLine(transform.position + controller.center + Vector3.up * controller.height / 2f,
                            transform.position + controller.center - Vector3.up * controller.height / 2f);
        }
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }

    public void IncreaseMaxStamina(float amount)
    {
        if (amount <= 0) return;
        maxStamina += amount;
        currentStamina = Mathf.Min(currentStamina + amount, maxStamina);
        Debug.Log($"最大体力增加 {amount}。新最大体力：{maxStamina}。当前体力：{currentStamina}");
        UpdateStaminaUI();
    }

    public void IncreaseStrength(int amount)
    {
        if (amount < 0) return;
        currentStrength += amount;
        Debug.Log($"力量增加 {amount} 点。当前力量: {currentStrength}");
        IncreaseAttackDamage(amount * 1.5f);
    }

    public void IncreaseAttackDamage(float amount)
    {
        if (amount <= 0) return;
        currentMeleeAttackDamage += amount;
        Debug.Log($"攻击伤害增加 {amount} 点。新的近战伤害: {currentMeleeAttackDamage}");
        UpdateAttackUI();
    }

    public void IncreaseAttackSpeed(float amount)
    {
        if (amount <= 0) return;
        currentMeleeAttackCooldown = Mathf.Max(0.1f, currentMeleeAttackCooldown - amount);
        Debug.Log($"攻击速度提高。新的近战冷却: {currentMeleeAttackCooldown}s");
        UpdateAttackUI();
    }

    public void SetCanMove(bool allow)
    {
        bool oldCanControlPlayer = canControlPlayer;
        canControlPlayer = allow;
        if (!canControlPlayer && oldCanControlPlayer)
        {
            Debug.Log("玩家移动已锁定。");
            playerVelocity.x = 0;
            playerVelocity.z = 0;
            isMovingHorizontally = false;
            isRunning = false;
            if (footstepAudioSource != null && footstepAudioSource.isPlaying)
            {
                footstepAudioSource.Stop();
            }
        }
        else if (canControlPlayer && !oldCanControlPlayer)
        {
            Debug.Log("玩家移动已解锁。");
        }
    }

    public void SetCanControlCamera(bool allow)
    {
        bool oldCanControlCamera = canControlCamera;
        canControlCamera = allow;
        if (!canControlCamera && oldCanControlCamera)
        {
            Debug.Log("摄像头控制已锁定。");
        }
        else if (canControlCamera && !oldCanControlCamera)
        {
            Debug.Log("摄像头控制已解锁。");
        }
    }

    public void ApplySpeedBuff(float multiplier, float duration)
    {
        if (_isSpeedBuffActive && _speedBuffCoroutine != null)
        {
            StopCoroutine(_speedBuffCoroutine);
        }
        _speedBuffCoroutine = StartCoroutine(SpeedUpBuffCoroutine(multiplier, duration));
    }

    private IEnumerator SpeedUpBuffCoroutine(float multiplier, float duration)
    {
        _isSpeedBuffActive = true;
        Debug.Log($"玩家移动速度提升 {multiplier} 倍，持续 {duration} 秒！");
        float oldSpeed = moveSpeed;
        moveSpeed = _originalMoveSpeed * multiplier;
        yield return new WaitForSeconds(duration);
        moveSpeed = oldSpeed;
        _isSpeedBuffActive = false;
        Debug.Log("玩家移动速度恢复正常。");
    }
    void UpdateAttackUI()
    {
        if (attackDamageText != null)
            attackDamageText.text = $"近战伤害: {currentMeleeAttackDamage:F0}";
        if (attackSpeedText != null)
            attackSpeedText.text = $"近战冷却: {currentMeleeAttackCooldown:F2}s";
        if (attackRangeText != null)
            attackRangeText.text = $"近战范围: {currentMeleeAttackRange:F1}";
    }


}



