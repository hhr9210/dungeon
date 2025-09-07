using UnityEngine;

public class FloatingCamera : MonoBehaviour
{
    [Header("目标 & 偏移")]
    [Tooltip("要跟随的目标的 Transform（例如：玩家的头部）。")]
    public Transform target;
    [Tooltip("摄像机与目标的距离。")]
    public float distance = 5f;
    [Tooltip("摄像机与目标的偏移量（例如：可以向上或向右偏移）。")]
    public Vector3 cameraOffset = Vector3.zero;

    [Header("视角设置")]
    [Tooltip("鼠标水平旋转灵敏度。")]
    public float mouseSensitivityX = 2f;
    [Tooltip("鼠标垂直旋转灵敏度。")]
    public float mouseSensitivityY = 2f;
    [Tooltip("最小俯仰角度。")]
    [Range(-90, 90)]
    public float minYAngle = 10f;
    [Tooltip("最大俯仰角度。")]
    [Range(-90, 90)]
    public float maxYAngle = 80f;

    [Header("平滑度设置")]
    [Tooltip("摄像机跟随目标位置的平滑速度。")]
    public float followSpeed = 5f;
    [Tooltip("鼠标滚轮调整距离的灵敏度。")]
    public float scrollSensitivity = 1f;
    [Tooltip("滚轮调整距离的最小边界。")]
    public float minDistance = 2f;
    [Tooltip("滚轮调整距离的最大边界。")]
    public float maxDistance = 10f;

    [Header("功能开关")]
    [Tooltip("是否启用摄像机控制功能（如旋转和滚轮调整）。")]
    public bool enableCameraControl = true;

    private float currentYaw = 0f;      
    private float currentPitch = 0f;    
    private float currentDistance;      

    void Start()
    {
        currentDistance = distance;
    }

    void LateUpdate()
    {
        if (target == null)
        {
            Debug.LogWarning("FloatingCamera: Target not assigned!");
            return;
        }

        if (!enableCameraControl)
        {
            transform.position = Vector3.Lerp(transform.position, target.position + cameraOffset, followSpeed * Time.deltaTime);
            transform.LookAt(target.position + cameraOffset);
            return;
        }

        currentYaw += Input.GetAxis("Mouse X") * mouseSensitivityX;
        currentPitch -= Input.GetAxis("Mouse Y") * mouseSensitivityY;
        
        // 限制垂直旋转角度
        currentPitch = Mathf.Clamp(currentPitch, minYAngle, maxYAngle);


        currentDistance -= Input.GetAxis("Mouse ScrollWheel") * scrollSensitivity;

        currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

        Quaternion desiredRotation = Quaternion.Euler(currentPitch, currentYaw, 0);

        Vector3 desiredPosition = target.position + cameraOffset - desiredRotation * Vector3.forward * currentDistance;

        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        
        transform.LookAt(target.position + cameraOffset);
    }
}