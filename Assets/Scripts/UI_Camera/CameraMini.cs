using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Camera))]
public class CameraMini : MonoBehaviour
{
    private const string LOG_PREFIX = "[MinimapDebug] ";

    private Camera minimapCamera;
    private UniversalAdditionalCameraData urpCameraData;

    [Header("渲染设置")]
    public bool ignoreShadows = true;

    void Awake()
    {
        Debug.Log(LOG_PREFIX + "Awake Called.");
        minimapCamera = GetComponent<Camera>();
        if (minimapCamera == null)
        {
            Debug.LogError(LOG_PREFIX + "找不到摄像机组件！脚本将禁用。");
            enabled = false;
            return;
        }

        urpCameraData = minimapCamera.GetUniversalAdditionalCameraData();
        if (urpCameraData == null)
        {
            Debug.LogWarning(LOG_PREFIX + "找不到 UniversalAdditionalCameraData。请确保摄像机在 URP 环境下。");
        }

        ApplyCameraSettings();
        Debug.Log(LOG_PREFIX + "Awake 完成，摄像机设置已应用。");
    }

    void ApplyCameraSettings()
    {
        // 确保是正交投影，适用于俯视小地图
        if (!minimapCamera.orthographic)
        {
            minimapCamera.orthographic = true;
        }

        // 禁用后处理
        if (urpCameraData != null)
        {
            urpCameraData.renderPostProcessing = false;

            if (ignoreShadows)
            {
                // 禁用URP摄像机的阴影渲染
                urpCameraData.renderShadows = false;
            }
            else
            {
                // 启用URP摄像机的阴影渲染
                urpCameraData.renderShadows = true;
            }
        }
    }
}