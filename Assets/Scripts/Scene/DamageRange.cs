using UnityEngine;
using System.Collections;

public class MovingHazard : MonoBehaviour
{
    [Header("移动参数")]
    public float moveSpeed = 2f;       // 移动速度
    public float moveRange = 2f;       // 上下移动范围
    public bool isMovingUpwards = true; // 初始方向：true 向上，false 向下

    private Vector3 _startPosition;

    [Header("伤害参数")]
    public float damagePerSecond = 10f;
    public float damageInterval = 1f;

    private bool _isDamagingPlayer = false;
    private IDamageable _playerDamageable;

    void Start()
    {
        _startPosition = transform.position;
    }

    void Update()
    {
        // PingPong 返回 0 ~ moveRange 之间的值
        float pingPongValue = Mathf.PingPong(Time.time * moveSpeed, moveRange);

        // 根据 isMovingUpwards 决定偏移方向
        float newY = isMovingUpwards 
            ? _startPosition.y + pingPongValue 
            : _startPosition.y - pingPongValue;

        // 防止 NaN
        if (float.IsNaN(newY))
        {
            return;
        }

        transform.position = new Vector3(_startPosition.x, newY, _startPosition.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent(out IDamageable damageable))
            {
                _playerDamageable = damageable;
                if (!_isDamagingPlayer)
                {
                    _isDamagingPlayer = true;
                    StartCoroutine(DamageOverTimeRoutine());
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _isDamagingPlayer = false;
            _playerDamageable = null;
        }
    }

    private IEnumerator DamageOverTimeRoutine()
    {
        while (_isDamagingPlayer && _playerDamageable != null)
        {
            _playerDamageable.TakeDamage(damagePerSecond);
            yield return new WaitForSeconds(damageInterval);
        }
    }
}
