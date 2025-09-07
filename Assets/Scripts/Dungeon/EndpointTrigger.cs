using UnityEngine;

public class EndpointTrigger : MonoBehaviour
{

    private GameManager gameManager;

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("EndpointTrigger: 场景中未找到 GameManager。请确保场景中有一个带有 GameManager 脚本的对象。");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && gameManager != null) 
        {
            gameManager.PlayerReachedEndpoint(); // 通知 GameManager 玩家已到达终点
        }
    }
}