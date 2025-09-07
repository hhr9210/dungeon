using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    // 角色移动速度
    public float moveSpeed = 5f; 
    
    // 角色旋转平滑速度，使用 Slerp 的插值系数
    public float rotationLerpSpeed = 10f; 
    
    // 输入平滑系数，值越小越平滑 (0表示无平滑，越大越僵硬)
    public float inputSmoothing = 0.1f; 

    [Header("相机设置")]
    // 拖拽主相机到这个位置！
    public Transform cameraTransform; 
    
    // 相机跟随玩家的速度 (Lerp系数)
    public float cameraFollowSpeed = 5f; 
    
    // 相机相对于玩家的初始位置偏移
    private Vector3 initialCameraRelativePosition; 

    private CharacterController controller;
    
    // 经过平滑处理的输入变量
    private Vector3 currentMoveInput; 
    
    // 最终的移动方向和角色旋转方向 (世界坐标)
    private Vector3 moveDirection; 

    void Start()
    {
        // 获取并检查 CharacterController 组件
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            Debug.LogError("TopDownMovement: 游戏对象上未找到 CharacterController 组件。请添加一个。");
            enabled = false;
            return;
        }

        // 检查相机引用，如果没有则自动查找主相机
        if (cameraTransform == null)
        {
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
                Debug.LogWarning("TopDownMovement: 相机引用未设置，已自动找到主相机。");
            }
            else
            {
                Debug.LogError("TopDownMovement: 相机引用未设置且未找到主相机。相机将不会跟随玩家。");
            }
        }

        // 记录相机相对于玩家的初始相对位置偏移
        if (cameraTransform != null)
        {
            initialCameraRelativePosition = cameraTransform.position - transform.position;
        }
    }

    void Update()
    {
        // --- 输入平滑处理 ---
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        // 原始输入向量 (用于键盘的局部坐标前/后/左/右)
        Vector3 rawInputVector = new Vector3(horizontalInput, 0f, verticalInput);

        // 对原始输入进行平滑插值
        currentMoveInput = Vector3.Lerp(currentMoveInput, rawInputVector, inputSmoothing * Time.deltaTime * 60f);

        // --- 根据相机方向计算移动方向 ---
        // 确保相机引用不为空
        if (cameraTransform != null)
        {
            // 获取相机的 Y 轴旋转 (用于确定相机的水平朝向)
            // 只需要 Y 轴，所以创建一个只包含 Y 轴旋转的 Quaternion
            Quaternion cameraYawRotation = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);

            // 将平滑后的输入向量通过相机的 Y 轴旋转转换到世界空间
            // 这样当按下 W 时，角色会朝着相机所朝向的方向前进
            moveDirection = cameraYawRotation * currentMoveInput;

            // 确保 moveDirection 只在 X 和 Z 轴有值，Y 轴为 0 (平面移动)
            moveDirection.y = 0f;

            // Normalize 归一化，确保移动速度一致，防止对角线移动更快
            moveDirection.Normalize();
        }
        else // 如果没有相机，则使用世界坐标系的移动 (按W始终向前)
        {
            moveDirection = currentMoveInput.normalized;
            moveDirection.y = 0f;
        }

        // --- 移动角色 ---
        // 如果输入很小，平滑插值会使移动方向也变小，从而实现减速
        controller.Move(moveDirection * moveSpeed * Time.deltaTime);

        // --- 旋转角色 ---
        // 只有当有实际移动方向时才旋转角色
        if (moveDirection.magnitude > 0.01f) // 避免在没有移动时角色抖动
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationLerpSpeed * Time.deltaTime);
        }

        // --- 相机跟随逻辑 ---
        if (cameraTransform != null)
        {
            // 目标相机位置 = 玩家位置 + 初始相机相对偏移
            Vector3 targetCamPosition = transform.position + initialCameraRelativePosition;
            cameraTransform.position = Vector3.Lerp(cameraTransform.position, targetCamPosition, cameraFollowSpeed * Time.deltaTime);
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // 碰撞逻辑...
    }
}