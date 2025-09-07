using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.AI;

public class TeleportManager : MonoBehaviour
{
    public static TeleportManager Instance { get; private set; }

    [Header("传送设置")]
    public float teleportOffset = 1f;
    public float teleportImmunityDuration = 0.5f;

    [SerializeField]
    private List<Teleporter> allTeleporters = new List<Teleporter>();

    private HashSet<GameObject> teleportImmuneObjects = new HashSet<GameObject>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void TeleportPlayer(GameObject objectToTeleport, Teleporter destinationTeleporter)
    {
        if (objectToTeleport == null)
        {
            Debug.LogWarning("尝试传送一个空的GameObject！", this);
            return;
        }

        if (teleportImmuneObjects.Contains(objectToTeleport))
        {
            Debug.Log($"物体 {objectToTeleport.name} 处于传送免疫状态，忽略本次传送请求。", objectToTeleport);
            return;
        }

        if (destinationTeleporter == null)
        {
            Debug.LogWarning($"传送点 {objectToTeleport.name} 的目标传送点未设置！无法传送。", this);
            return;
        }

        Vector3 targetPosition = destinationTeleporter.transform.position;
        targetPosition.y += teleportOffset;

        NavMeshAgent agent = objectToTeleport.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled)
        {
            agent.enabled = false;
            objectToTeleport.transform.position = targetPosition;
            agent.enabled = true;
            agent.Warp(targetPosition);
            Debug.Log($"已使用NavMeshAgent Warp将 {objectToTeleport.name} 传送至 {destinationTeleporter.name}。", this);
        }
        else
        {
            objectToTeleport.transform.position = targetPosition;
            Debug.Log($"已将 {objectToTeleport.name} 传送至 {destinationTeleporter.name}。", this);
        }

        StartCoroutine(TeleportImmunityCoroutine(objectToTeleport, teleportImmunityDuration));
    }

    private IEnumerator TeleportImmunityCoroutine(GameObject obj, float duration)
    {
        teleportImmuneObjects.Add(obj);
        Debug.Log($"{obj.name} 进入传送免疫状态，持续 {duration} 秒。");

        yield return new WaitForSeconds(duration);

        if (obj != null)
        {
            teleportImmuneObjects.Remove(obj);
            Debug.Log($"{obj.name} 退出传送免疫状态。");
        }
    }


    public void RegisterTeleporter(Teleporter teleporter)
    {
        if (!allTeleporters.Contains(teleporter))
        {
            allTeleporters.Add(teleporter);
            Debug.Log($"已注册传送点: {teleporter.name}");
        }
    }

    public void DeregisterTeleporter(Teleporter teleporter)
    {
        if (allTeleporters.Contains(teleporter))
        {
            allTeleporters.Remove(teleporter);
            Debug.Log($"已移除传送点: {teleporter.name}");
        }
    }

    void OnApplicationQuit()
    {
        Instance = null;
    }
}
