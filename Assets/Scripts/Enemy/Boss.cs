using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Collider))]
public class Boss : MonoBehaviour, IDamageable
{
    [System.Serializable]
    public class BossUIData
    {
        public Image hpFill;
        public TMP_Text hpText;
        public GameObject castUIContainer;
        public TMP_Text castNameText;
        public Image castFill;
    }



    [System.Serializable]
    public class SummonTimedObjectParams
    {
        [Tooltip("要生成的限时物体预制体。")]
        public GameObject prefab;
        [Tooltip("限时物体的生成位置。")]
        public Transform spawnPoint;
        [Tooltip("限时物体存在的持续时间。")]
        public float duration = 10f;
        [Tooltip("如果限时物体在持续时间结束后仍存在，Boss受到的惩罚伤害。")]
        public float failureDamage = 50f;
        [Tooltip("Boss施加惩罚伤害的范围。")]
        public float damageRange = 5f;
    }

    [System.Serializable]
    public class SummonPrefabParams
    {
        [Tooltip("要生成的预制体。")]
        public GameObject prefabToSummon;
        [Tooltip("预制体的生成位置。")]
        public Transform summonPoint;
    }

    [System.Serializable]
    public class SummonMultipleObjectsParams
    {
        [Tooltip("要在技能施放时激活的物体列表。")]
        public List<GameObject> objectsToActivate;
    }

    [System.Serializable]
    public class AppearTimedObjectParams
    {
        [Tooltip("要在一段时间后出现的物体。")]
        public GameObject targetObject;
        [Tooltip("物体从隐藏到出现所需的时间。")]
        public float duration = 3f;
    }

    [System.Serializable]
    public class ProjectileAttackParams
    {
        [Tooltip("投射物攻击技能的持续时间。")]
        public float skillDuration = 5f;
        [Tooltip("要发射的投射物预制体。")]
        public GameObject projectilePrefab;
        [Tooltip("发射投射物的位置。")]
        public Transform firePoint;
        [Tooltip("每次发射的投射物数量。")]
        public int shotsPerBurst = 6;
        [Tooltip("两波发射之间的间隔时间。")]
        public float fireRate = 0.5f;
        [Tooltip("投射物扇形的总角度。")]
        public float totalFanAngle = 30f;
        [Tooltip("Boss旋转的速度（每秒度数）。")]
        public float rotationSpeed = 60f;
        [Tooltip("投射物的初始飞行速度。")]
        public float projectileSpeed = 20f;
    }

    [System.Serializable]
    public class BarrageAttackParams
    {
        [Tooltip("Boss移动到的地点列表，将随机选择。")]
        public List<Transform> movementPoints;
        [Tooltip("弹幕攻击的目标物体。")]
        public Transform barrageTarget;
        [Tooltip("此技能期间的移动速度。")]
        public float movementSpeedOverride = 10f;
        [Tooltip("到达地点后，发射前的停顿时间。")]
        public float shotDelay = 0.5f;
        [Tooltip("要发射的投射物预制体。")]
        public GameObject projectilePrefab;
        [Tooltip("发射投射物的位置。")]
        public Transform firePoint;
        [Tooltip("投射物的初始飞行速度。")]
        public float projectileSpeed = 20f;
        [Tooltip("每次发射的投射物数量。")]
        public int numberOfProjectiles = 8;
        [Tooltip("弹幕扇形总角度。")]
        public float totalBarrageAngle = 120f;
    }

    [Header("Boss基础属性")]
    public float maxHP = 5000f;
    public float moveSpeed = 4f;

     public Transform playerTarget;

    [Header("近战攻击参数")]
    public float meleeDamage = 50f;
    public float meleeAttackCooldown = 2f;
    private float _lastMeleeAttackTime;
    public float meleeAttackRange = 3f;
   

    [Header("常规远程攻击")]
    public float rangedAttackRange = 15f; // 新增的远程攻击触发距离

    
    private float _lastRangedAttackTime;
    public float rangedAttackCooldown = 2f; // 远程攻击冷却时间
    private bool _isPerformingSkill = false;

    [SerializeField] private BossUIData uiSettings;

    private float _currentHP;
    public float currentHP { get { return _currentHP; } protected set { _currentHP = value; } }
    public NavMeshAgent NavMeshAgent { get; private set; }
    private Vector3 _initialPosition;
    private float _initialMoveSpeed;

    [Header("技能参数配置")]
    [Tooltip("定时出现物体技能的参数")]
    public AppearTimedObjectParams appearTimedObjectParams;
    [Tooltip("限时召唤技能的参数")]
    public SummonTimedObjectParams summonTimedObjectParams;
    [Tooltip("召唤单一预制体技能的参数")]
    public SummonPrefabParams summonPrefabParams;
    [Tooltip("多物体召唤技能的参数")]
    public SummonMultipleObjectsParams summonMultipleObjectsParams;
    [Tooltip("投射物攻击技能的参数")]
    public ProjectileAttackParams projectileAttackParams;
    [Tooltip("弹幕攻击技能的参数")]
    public BarrageAttackParams barrageAttackParams;

    private GameObject currentSkillObjectInstance;
    private Coroutine skillCoroutine;

    void Awake()
    {
        NavMeshAgent = GetComponent<NavMeshAgent>();
        _initialPosition = transform.position;
        _initialMoveSpeed = moveSpeed;

        if (NavMeshAgent != null)
        {
            NavMeshAgent.speed = moveSpeed;
            NavMeshAgent.stoppingDistance = meleeAttackRange * 0.9f;
        }

        currentHP = maxHP;

        if (uiSettings.castUIContainer != null)
        {
            uiSettings.castUIContainer.SetActive(false);
        }

        // 尝试在游戏开始时找到玩家对象
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            playerTarget = playerObject.transform;
        }
        else
        {
            Debug.LogError("没有找到带有 'Player' 标签的物体！请确保你的玩家物体有此标签。");
        }
    }

    void Update()
    {
        UpdateUI();

        // 如果 Boss 正在执行特殊技能或已死亡，则什么也不做
        if (_isPerformingSkill || currentHP <= 0)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            TakeDamage(maxHP * 0.025f);
        }

        if (playerTarget != null)
        {
            RotateTowards(playerTarget.position);

            float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

            // 如果玩家在近战攻击范围内
            if (distanceToPlayer <= meleeAttackRange)
            {
                // 停止移动并执行近战攻击
                if (NavMeshAgent != null)
                {
                    NavMeshAgent.isStopped = true;
                }
                PerformMeleeAttack(); // 调用近战攻击方法
            }
            // 如果玩家在远程攻击范围内（但不在近战范围内）
            else if (distanceToPlayer <= rangedAttackRange)
            {
                // 停止移动，只进行远程攻击，不追逐玩家
                if (NavMeshAgent != null)
                {
                    NavMeshAgent.isStopped = true;
                }
                
                // 检查冷却时间，执行远程攻击
                if (Time.time >= _lastRangedAttackTime + rangedAttackCooldown)
                {
                    Debug.Log("Boss进入远程攻击范围，停止移动并准备远程攻击。");
                    PerformSkill_SingleProjectileAttack();
                    _lastRangedAttackTime = Time.time;
                }
            }
            // 如果玩家在所有攻击范围之外
            else
            {
                // 继续追逐玩家
                if (NavMeshAgent != null)
                {
                    NavMeshAgent.isStopped = false;
                    NavMeshAgent.SetDestination(playerTarget.position);
                }
            }
        }
    }
    public void PerformMeleeAttack()
    {
        // 检查冷却时间，如果时间未到则直接返回
        if (Time.time < _lastMeleeAttackTime + meleeAttackCooldown)
        {
            return;
        }

        // 更新上次攻击时间
        _lastMeleeAttackTime = Time.time;

        Debug.Log("Boss执行近战攻击!");

        // 在 Boss 位置创建球形检测范围，寻找范围内的所有碰撞体
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, meleeAttackRange);
        foreach (var hitCollider in hitColliders)
        {
            // 检查碰撞体是否是玩家，并且实现了 IDamageable 接口
            IDamageable damageableObject = hitCollider.GetComponent<IDamageable>();
            if (damageableObject != null && hitCollider.CompareTag("Player"))
            {
                // 对玩家造成伤害
                damageableObject.TakeDamage(meleeDamage);
                Debug.Log($"Boss对玩家造成了 {meleeDamage} 点近战伤害。");
            }
        }
    }
    public void ResetBossState()
    {
        currentHP = maxHP;
        gameObject.SetActive(true);

        if (NavMeshAgent != null)
        {
            NavMeshAgent.enabled = false;
            transform.position = _initialPosition;
            NavMeshAgent.enabled = true;
            NavMeshAgent.isStopped = true;
        }

        if (uiSettings.castUIContainer != null)
        {
            uiSettings.castUIContainer.SetActive(false);
        }

        if (skillCoroutine != null)
        {
            StopCoroutine(skillCoroutine);
        }

        if (currentSkillObjectInstance != null)
        {
            Destroy(currentSkillObjectInstance);
        }

        Debug.Log("Boss状态已重置，准备开始新的回合。");
    }

    public void RotateTowards(Vector3 position)
    {
        Vector3 direction = (position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion rot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * moveSpeed * 2f);
        }
    }

    public void TakeDamage(float damage)
    {
        currentHP = Mathf.Max(0, currentHP - damage);
        UpdateUI();

        if (currentHP <= 0)
        {
            if (NavMeshAgent != null)
            {
                NavMeshAgent.isStopped = true;
                NavMeshAgent.enabled = false;
            }
            Debug.Log("Boss已被击败!");
        }
    }

    public IEnumerator StartCasting(float castTime, string skillName)
    {
        if (uiSettings.castUIContainer != null)
        {
            uiSettings.castUIContainer.SetActive(true);
            if (uiSettings.castNameText != null)
            {
                uiSettings.castNameText.text = skillName;
            }
        }

        float timer = 0f;
        while (timer < castTime)
        {
            timer += Time.deltaTime;
            if (uiSettings.castFill != null)
            {
                uiSettings.castFill.fillAmount = timer / castTime;
            }
            yield return null;
        }

        if (uiSettings.castUIContainer != null)
        {
            uiSettings.castUIContainer.SetActive(false);
        }
    }

    public void PerformSkill_AppearTimedObject()
    {
        if (appearTimedObjectParams.targetObject != null)
        {
            appearTimedObjectParams.targetObject.SetActive(false);
            Debug.Log($"Boss施放技能：“定时出现物体”，物体 {appearTimedObjectParams.targetObject.name} 暂时消失。");
            _isPerformingSkill = true; 
            StartCoroutine(TimedObjectAppearance());
        }
        else
        {
            Debug.LogWarning("“定时出现物体”技能的目标为空，无法执行。");
        }
    }

    private IEnumerator TimedObjectAppearance()
    {
        yield return new WaitForSeconds(appearTimedObjectParams.duration);

        if (appearTimedObjectParams.targetObject != null)
        {
            appearTimedObjectParams.targetObject.SetActive(true);
            Debug.Log($"物体 {appearTimedObjectParams.targetObject.name} 重新出现了！");
        }
        _isPerformingSkill = false; // 机制结束后重置状态
    }

    public void PerformSkill_SummonTimedObject()
    {
        
        GetComponent<Renderer>().enabled = false;
        GetComponent<Collider>().enabled = false;
        if (NavMeshAgent != null)
        {
            NavMeshAgent.isStopped = true;
        }

        if (summonTimedObjectParams.prefab != null && summonTimedObjectParams.spawnPoint != null)
        {
            currentSkillObjectInstance = Instantiate(summonTimedObjectParams.prefab, summonTimedObjectParams.spawnPoint.position, Quaternion.identity);
            Debug.Log($"第二个技能机制：限时物体 {summonTimedObjectParams.prefab.name} 在世界坐标 {summonTimedObjectParams.spawnPoint.position} 处生成。");
            _isPerformingSkill = true; 
            skillCoroutine = StartCoroutine(HandleTimedSkill());
        }
        else
        {
            Debug.LogWarning("技能2：限时物体预制体或生成点为空，无法执行召唤技能。");
            ReappearBoss(false);
        }
    }

    private IEnumerator HandleTimedSkill()
    {
        float timer = 0f;
        while (timer < summonTimedObjectParams.duration && currentSkillObjectInstance != null)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        bool skillSucceeded = (currentSkillObjectInstance == null);
        ReappearBoss(skillSucceeded);

        if (!skillSucceeded)
        {
            ApplyAoEDamage(summonTimedObjectParams.failureDamage, summonTimedObjectParams.damageRange);
            Destroy(currentSkillObjectInstance);
        }
    }

    private void ReappearBoss(bool skillSucceeded)
    {
        GetComponent<Renderer>().enabled = true;
        GetComponent<Collider>().enabled = true;
        if (NavMeshAgent != null)
        {
            NavMeshAgent.isStopped = false;
            NavMeshAgent.enabled = true; // 确保启用
        }
        
        //  Boss 重新出现时重置状态
        _isPerformingSkill = false;

        if (skillSucceeded)
        {
            Debug.Log("技能成功! Boss重新出现，没有惩罚。");
        }
        else
        {
            Debug.Log("技能失败! Boss重新出现。");
        }
    }

    private void ApplyAoEDamage(float damage, float range)
    {
        Debug.Log($"技能失败! Boss在 {range} 范围内造成了 {damage} 点伤害!");
    }

    public void PerformSkill_SummonPrefab()
    {
        if (summonPrefabParams.prefabToSummon != null && summonPrefabParams.summonPoint != null)
        {
            Instantiate(summonPrefabParams.prefabToSummon, summonPrefabParams.summonPoint.position, Quaternion.identity);
            Debug.Log($"Boss施放技能：在 {summonPrefabParams.summonPoint.position} 生成了预制体 {summonPrefabParams.prefabToSummon.name}！");
        }
        else
        {
            Debug.LogWarning("召唤单一预制体技能：预制体或生成点为空，无法执行。");
        }
    }

    public void PerformSkill_SummonMultipleObjects()
    {
        if (summonMultipleObjectsParams.objectsToActivate != null && summonMultipleObjectsParams.objectsToActivate.Count > 0)
        {
            Debug.Log($"Boss施放技能：“多物体激活”，开始激活 {summonMultipleObjectsParams.objectsToActivate.Count} 个物体。");

            foreach (var obj in summonMultipleObjectsParams.objectsToActivate)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                    Debug.Log($"   - 激活了物体: {obj.name}");
                }
            }
        }
        else
        {
            Debug.LogWarning("“多物体激活”技能的目标列表为空，无法执行。");
        }
    }

    public void PerformSkill_ProjectileAttack()
    {
        Debug.Log("开始执行投射物攻击技能。");

        if (projectileAttackParams.projectilePrefab == null)
        {
            Debug.LogError("投射物攻击技能失败：未指定“Projectile Prefab”！请在 Inspector 面板中配置。");
            return;
        }
        if (projectileAttackParams.firePoint == null)
        {
            Debug.LogError("投射物攻击技能失败：未指定“Fire Point”！请在 Inspector 面板中配置。");
            return;
        }

        if (NavMeshAgent != null)
        {
            NavMeshAgent.isStopped = true;
        }
        Debug.Log("Boss已停止移动。");
        _isPerformingSkill = true; 
        StartCoroutine(ShootProjectiles());
        Debug.Log("已启动 ShootProjectiles 协程。");
    }

    private IEnumerator ShootProjectiles()
    {
        Debug.Log($"ShootProjectiles 协程开始。技能持续时间: {projectileAttackParams.skillDuration}秒。");

        float angleStep = projectileAttackParams.totalFanAngle / (projectileAttackParams.shotsPerBurst - 1);
        float startAngle = -projectileAttackParams.totalFanAngle / 2;
        Debug.Log($"计算结果：angleStep = {angleStep}, startAngle = {startAngle}");

        float skillTimer = 0f;
        while (skillTimer < projectileAttackParams.skillDuration)
        {
            transform.Rotate(Vector3.up, projectileAttackParams.rotationSpeed * projectileAttackParams.fireRate);

            for (int i = 0; i < projectileAttackParams.shotsPerBurst; i++)
            {
                Quaternion forwardRotation = Quaternion.Euler(0, startAngle + i * angleStep, 0);
                Quaternion backwardRotation = Quaternion.Euler(0, 180 + startAngle + i * angleStep, 0);

                Quaternion finalForwardRotation = transform.rotation * forwardRotation;
                Quaternion finalBackwardRotation = transform.rotation * backwardRotation;

                GameObject forwardBullet = Instantiate(projectileAttackParams.projectilePrefab, projectileAttackParams.firePoint.position, finalForwardRotation);
                Rigidbody forwardRb = forwardBullet.GetComponent<Rigidbody>();
                if (forwardRb != null)
                {
                    forwardRb.velocity = forwardBullet.transform.forward * projectileAttackParams.projectileSpeed;
                }

                GameObject backwardBullet = Instantiate(projectileAttackParams.projectilePrefab, projectileAttackParams.firePoint.position, finalBackwardRotation);
                Rigidbody backwardRb = backwardBullet.GetComponent<Rigidbody>();
                if (backwardRb != null)
                {
                    backwardRb.velocity = backwardBullet.transform.forward * projectileAttackParams.projectileSpeed;
                }
            }
            Debug.Log($"发射了一波 {projectileAttackParams.shotsPerBurst} 个投射物。");

            yield return new WaitForSeconds(projectileAttackParams.fireRate);

            skillTimer += projectileAttackParams.fireRate;
        }

        if (NavMeshAgent != null)
        {
            NavMeshAgent.isStopped = false;
        }
        Debug.Log("投射物攻击技能完成，Boss继续移动。");
        _isPerformingSkill = false; // 机制结束后重置状态
    }

    public void PerformSkill_BarrageAttack()
    {
        Debug.Log("开始执行弹幕攻击技能。");

        if (barrageAttackParams.movementPoints == null || barrageAttackParams.movementPoints.Count == 0)
        {
            Debug.LogError("弹幕攻击技能失败：未指定移动地点！");
            return;
        }
        if (barrageAttackParams.barrageTarget == null)
        {
            Debug.LogError("弹幕攻击技能失败：未指定弹幕目标！");
            return;
        }
        if (barrageAttackParams.projectilePrefab == null || barrageAttackParams.firePoint == null)
        {
            Debug.LogError("弹幕攻击技能失败：未指定投射物预制体或发射点！");
            return;
        }
        _isPerformingSkill = true; 
        StartCoroutine(BarrageAttackSequence());
    }

    private IEnumerator BarrageAttackSequence()
    {
        if (NavMeshAgent != null)
        {
            _initialMoveSpeed = NavMeshAgent.speed;
            NavMeshAgent.speed = barrageAttackParams.movementSpeedOverride;
            NavMeshAgent.isStopped = false;
            NavMeshAgent.enabled = true;
        }

        Vector3 skillStartPosition = transform.position;

        List<Transform> shuffledPoints = new List<Transform>(barrageAttackParams.movementPoints);
        for (int i = 0; i < shuffledPoints.Count; i++)
        {
            Transform temp = shuffledPoints[i];
            int randomIndex = Random.Range(i, shuffledPoints.Count);
            shuffledPoints[i] = shuffledPoints[randomIndex];
            shuffledPoints[randomIndex] = temp;
        }

        foreach (Transform destination in shuffledPoints)
        {
            Debug.Log($"Boss开始移动到地点: {destination.name}");
            if (NavMeshAgent != null)
            {
                NavMeshAgent.SetDestination(destination.position);
            }

            while (Vector3.Distance(transform.position, destination.position) > NavMeshAgent.stoppingDistance)
            {
                yield return null;
            }
            Debug.Log($"Boss已到达目的地: {destination.name}，准备开始弹幕攻击。");

            if (NavMeshAgent != null)
            {
                NavMeshAgent.isStopped = true;
            }

            if (barrageAttackParams.barrageTarget != null)
            {
                // 只在水平面上旋转
                Vector3 directionToTarget = (barrageAttackParams.barrageTarget.position - transform.position).normalized;
                directionToTarget.y = 0; // 忽略Y轴

                if (directionToTarget != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                    //Slerp 平滑旋转
                    float rotationTimer = 0f;
                    while (rotationTimer < 1f)
                    {
                        rotationTimer += Time.deltaTime * 15f; //旋转速度
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationTimer);
                        yield return null;
                    }
                    transform.rotation = targetRotation;

                    Debug.Log("Boss已水平面向目标，角度：" + transform.rotation.eulerAngles);
                }
            }
            // 确保旋转完成
            yield return new WaitForSeconds(barrageAttackParams.shotDelay);

            float angleStep = barrageAttackParams.totalBarrageAngle / (barrageAttackParams.numberOfProjectiles - 1);
            float startAngle = -barrageAttackParams.totalBarrageAngle / 2;

            for (int i = 0; i < barrageAttackParams.numberOfProjectiles; i++)
            {
                Quaternion relativeRotation = Quaternion.Euler(0, startAngle + i * angleStep, 0);
                Quaternion finalRotation = transform.rotation * relativeRotation;

                GameObject bullet = Instantiate(barrageAttackParams.projectilePrefab, barrageAttackParams.firePoint.position, finalRotation);
                Rigidbody rb = bullet.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = bullet.transform.forward * barrageAttackParams.projectileSpeed;
                }
            }
            Debug.Log($"在地点 {destination.name} 发射了一波 {barrageAttackParams.numberOfProjectiles} 个投射物。");

            if (NavMeshAgent != null)
            {
                NavMeshAgent.isStopped = false;
            }
        }

        Debug.Log("所有地点遍历完毕，Boss返回初始位置。");
        if (NavMeshAgent != null)
        {
            NavMeshAgent.SetDestination(skillStartPosition);
            while (Vector3.Distance(transform.position, skillStartPosition) > NavMeshAgent.stoppingDistance)
            {
                yield return null;
            }
        }

        if (NavMeshAgent != null)
        {
            NavMeshAgent.speed = _initialMoveSpeed;
            NavMeshAgent.isStopped = false;
        }
        _isPerformingSkill = false; // 机制结束后重置状态
        Debug.Log("弹幕攻击技能完成。");
    }

    private void UpdateUI()
    {
        if (uiSettings.hpFill != null)
        {
            uiSettings.hpFill.fillAmount = currentHP / maxHP;
        }
        if (uiSettings.hpText != null)
        {
            uiSettings.hpText.text = $"{Mathf.RoundToInt((currentHP / maxHP) * 100)}%";
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeAttackRange);
    }
    
    public void PerformSkill_SingleProjectileAttack()
    {
        // 确保有投射物预制体和发射点
        if (projectileAttackParams.projectilePrefab == null || projectileAttackParams.firePoint == null)
        {
            Debug.LogError("单个投射物攻击失败：未指定“Projectile Prefab”或“Fire Point”！");
            return;
        }

        GameObject bullet = Instantiate(
            projectileAttackParams.projectilePrefab,
            projectileAttackParams.firePoint.position,
            Quaternion.LookRotation((playerTarget.position - projectileAttackParams.firePoint.position).normalized)
        );


        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = bullet.transform.forward * projectileAttackParams.projectileSpeed;
        }

        Debug.Log("Boss发射了一个投射物，目标是玩家。");
    }
}