using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// 控制敌人移动行为的脚本。
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Movement Properties")]
    public float moveSpeed = 2f; 

    [Header("Patrol Settings")]
    public float patrolRadius = 10f;
    public float idleDuration = 2f;


    public NavMeshAgent NavMeshAgent { get; private set; }

    // 巡逻相关变量
    private Vector3 _currentPatrolPoint;
    private float _idleAtPatrolPointTimer;

    void Awake()
    {
        NavMeshAgent = GetComponent<NavMeshAgent>();
        if (NavMeshAgent == null)
        {
            Debug.LogError("NavMeshAgent component not found on the enemy! The enemy will not be able to move.", this);
            enabled = false;
        }

        NavMeshAgent.speed = moveSpeed;
    }

    void Start()
    {
        SetNewPatrolPoint();
    }

    /// <summary>
    /// 处理巡逻行为。
    /// </summary>
    public void HandlePatrolling()
    {
        // 检查是否在等待
        if (NavMeshAgent.isStopped)
        {
            _idleAtPatrolPointTimer += Time.deltaTime;
            if (_idleAtPatrolPointTimer >= idleDuration)
            {
                SetNewPatrolPoint();
                _idleAtPatrolPointTimer = 0f;
            }
        }
        // 到达目标点，停止并计时
        else if (NavMeshAgent.remainingDistance <= NavMeshAgent.stoppingDistance)
        {
            NavMeshAgent.isStopped = true;
            NavMeshAgent.velocity = Vector3.zero;
        }
    }

    /// <summary>
    /// 处理追逐行为。
    /// </summary>
    /// <param name="targetPosition">追逐目标的位置。</param>
    public void HandleChasing(Vector3 targetPosition)
    {
        NavMeshAgent.isStopped = false;
        NavMeshAgent.SetDestination(targetPosition);
    }

    /// <summary>
    /// 停止移动。
    /// </summary>
    public void StopMovement()
    {
        NavMeshAgent.isStopped = true;
        NavMeshAgent.velocity = Vector3.zero;
    }

    /// <summary>
    /// 朝向目标旋转。
    /// </summary>
    /// <param name="position">目标位置。</param>
    public void RotateTowards(Vector3 position)
    {
        Vector3 direction = (position - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * moveSpeed * 2f);
        }
    }

    /// <summary>
    /// 设置新的巡逻点。
    /// </summary>
    private void SetNewPatrolPoint()
    {
        _currentPatrolPoint = GetRandomPatrolPoint();
        NavMeshAgent.SetDestination(_currentPatrolPoint);
        NavMeshAgent.isStopped = false;
    }

    /// <summary>
    /// 获取随机巡逻点。
    /// </summary>
    /// <returns>NavMesh 上一个有效点。</returns>
    private Vector3 GetRandomPatrolPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += transform.position;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return transform.position; 
    }
}
