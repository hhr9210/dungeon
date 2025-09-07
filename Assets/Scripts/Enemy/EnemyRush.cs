using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class EnemyRush : MonoBehaviour
{
    [Header("冲锋属性")]
    public float chargeRange = 10f;
    public float chargeSpeed = 15f;
    public float chargeDamage = 20f;
    public float chargeCooldown = 3f;
    public float postChargePauseTime = 1f;

    [Header("冲锋持续时间")]
    public float initialChargeDuration = 1.0f;
    public float postHitChargeDuration = 0.5f;

    [Header("冲锋警示")]
    public float chargeWarningTime = 1f;

    private Enemy _enemy;
    private EnemyMovement _enemyMovement;
    private NavMeshAgent _navMeshAgent;
    private GameObject _player;

    private bool _isCharging = false;
    private bool _isCoolingDown = false;
    public bool IsCoolingDown => _isCoolingDown;
    private bool _hasDamagedPlayer = false;

    void Awake()
    {
        _enemy = GetComponentInParent<Enemy>();
        _enemyMovement = GetComponentInParent<EnemyMovement>();
        _navMeshAgent = GetComponentInParent<NavMeshAgent>();

        if (_enemy == null || _enemyMovement == null || _navMeshAgent == null)
        {
            Debug.LogError("EnemyRush 脚本需要 Enemy, EnemyMovement 和 NavMeshAgent 脚本。", this);
            enabled = false;
        }
    }

    public void HandleCharging(GameObject target)
    {
        if (_isCoolingDown || _isCharging)
        {
            return;
        }

        _player = target;
        StartCoroutine(ChargeSequence());
    }

    private IEnumerator ChargeSequence()
    {
        _enemyMovement.StopMovement();
        _enemyMovement.RotateTowards(_player.transform.position);
        yield return new WaitForSeconds(chargeWarningTime);

        _isCharging = true;
        _hasDamagedPlayer = false;

        Vector3 chargeDirection = (_player.transform.position - transform.position).normalized;
        _navMeshAgent.isStopped = false;
        _navMeshAgent.velocity = chargeDirection * chargeSpeed;

        yield return new WaitForSeconds(initialChargeDuration);

        if (_hasDamagedPlayer)
        {
            yield return new WaitForSeconds(postHitChargeDuration);
        }

        StopCharge();
    }

    private void StopCharge()
    {
        _isCharging = false;
        _navMeshAgent.isStopped = true;
        _navMeshAgent.velocity = Vector3.zero;

        StartCoroutine(PostChargePause());
    }

    private IEnumerator PostChargePause()
    {
        yield return new WaitForSeconds(postChargePauseTime);
        StartCooldown();
    }

    private void StartCooldown()
    {
        _isCoolingDown = true;
        Debug.Log($"{_enemy.name} 进入冲锋冷却...");
        Invoke(nameof(EndCooldown), chargeCooldown);
    }

    private void EndCooldown()
    {
        _isCoolingDown = false;
        Debug.Log($"{_enemy.name} 冲锋冷却结束。");
    }

    void OnTriggerEnter(Collider other)
    {
        if (_isCharging && !_hasDamagedPlayer && other.gameObject == _player)
        {
            IDamageable damageable = _player.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(chargeDamage);
                _hasDamagedPlayer = true;
            }
        }
    }
}
