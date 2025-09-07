using UnityEngine;

public class Teleporter : MonoBehaviour
{
    public Teleporter targetTeleporter;

    public GameObject designatedTriggerObject;

    private void OnTriggerEnter(Collider other)
    {
        if (designatedTriggerObject != null && other.gameObject == designatedTriggerObject)
        {
            Debug.Log($"指定对象 {other.name} 进入了此传送点 {this.name}。", this);
            if (TeleportManager.Instance != null)
            {
                TeleportManager.Instance.TeleportPlayer(other.gameObject, targetTeleporter);
            }
            else
            {
                Debug.LogError("场景中没有 TeleportManager 实例，无法执行传送。", this);
            }
        }
        else if (designatedTriggerObject == null && other.CompareTag("Player"))
        {
            Debug.Log($"玩家 {other.name} 进入了此传送点 {this.name}。", this);
            if (TeleportManager.Instance != null)
            {
                TeleportManager.Instance.TeleportPlayer(other.gameObject, targetTeleporter);
            }
            else
            {
                Debug.LogError("场景中没有 TeleportManager 实例，无法执行传送。", this);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (targetTeleporter != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, targetTeleporter.transform.position);
            Gizmos.DrawWireSphere(targetTeleporter.transform.position, 0.5f);
        }
    }
}
