using UnityEngine;
using UnityEngine.UI; // ⭐ 引入 UnityEngine.UI 命名空间以使用 Image

public class DayNightCycle : MonoBehaviour
{
    [Header("时间设置")]
    [Tooltip("一天的持续时间（秒）")]
    public float secondsInFullDay = 120f; // 2分钟为一天
    [Range(0, 1)]
    [Tooltip("当前时间，0为午夜，0.5为正午")]
    public float currentTimeOfDay = 0.25f; // 初始设置为清晨

    [Header("光照设置")]
    [Tooltip("模拟太阳的定向光")]
    public Light sunLight;
    [Tooltip("模拟月亮的定向光（可选，可以不设置）")]
    public Light moonLight; // 可以不设置，只通过太阳光来控制
    [Tooltip("太阳光在白天的最大强度")]
    public float sunIntensity = 1f;
    [Tooltip("月光在夜晚的最大强度")]
    public float moonIntensity = 0.2f; // 月光通常较弱

    [Header("天空盒设置")]
    [Tooltip("白天天空的颜色")]
    public Color daySkyColor = new Color(0.53f, 0.81f, 0.92f); // 天蓝色
    [Tooltip("夜晚天空的颜色")]
    public Color nightSkyColor = new Color(0.04f, 0.05f, 0.15f); // 深蓝色/黑色
    [Tooltip("黄昏/黎明时的环境颜色")]
    public Color twilightAmbientColor = new Color(0.3f, 0.25f, 0.2f); // 暖色调

    [Header("环境光设置")]
    [Tooltip("白天环境光的强度")]
    public float dayAmbientIntensity = 1f;
    [Tooltip("夜晚环境光的强度")]
    public float nightAmbientIntensity = 0.1f;

    // ⭐ 新增：UI 引用
    [Header("UI 进度圈设置")]
    [Tooltip("用于显示昼夜进度的 UI Image。请将其 Image Type 设置为 Filled，Fill Method 设置为 Radial 360。")]
    public Image dayNightProgressBar;

    private float sunRotationSpeed;

    void Start()
    {
        // 计算太阳每秒旋转的速度
        sunRotationSpeed = 360f / secondsInFullDay;

        // 如果没有指定太阳光，尝试获取场景中的定向光
        if (sunLight == null)
        {
            sunLight = GameObject.Find("Directional Light")?.GetComponent<Light>();
            if (sunLight == null)
            {
                Debug.LogError("场景中没有找到名为 'Directional Light' 的定向光，请手动设置 Sun Light！", this);
            }
        }

        // 确保使用代码控制环境光
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
    }

    void Update()
    {
        // 更新时间， currentTimeOfDay 在 0 到 1 之间循环
        // currentTimeOfDay = Mathf.Repeat(currentTimeOfDay + Time.deltaTime / secondsInFullDay, 1f); // 更简洁的写法
        currentTimeOfDay += Time.deltaTime / secondsInFullDay;
        if (currentTimeOfDay >= 1f)
        {
            currentTimeOfDay = 0f; // 一天结束，回到起点
        }

        UpdateLight();
        UpdateSkyboxAndAmbient();
        UpdateUIProgressBar(); // ⭐ 调用更新 UI 的方法
    }

    void UpdateLight()
    {
        // 根据时间更新太阳光旋转
        if (sunLight != null)
        {
            // -90度表示太阳从地平线升起（0度是正上方，-90是水平线，-180是正下方）
            // currentTimeOfDay * 360f 将 0-1 的进度映射到 0-360 度的旋转
            sunLight.transform.rotation = Quaternion.Euler((currentTimeOfDay * 360f) - 90f, 170f, 0); // 170是Y轴旋转，可以调整

            // 根据时间调整太阳光强度
            // 计算一个在 0.25 (黎明) 和 0.75 (黄昏) 之间为 1，在其他时间接近 0 的乘数
            float sunLerpFactor = 0f;
            if (currentTimeOfDay < 0.25f) // 午夜到黎明
            {
                sunLerpFactor = Mathf.InverseLerp(0f, 0.25f, currentTimeOfDay);
            }
            else if (currentTimeOfDay > 0.75f) // 黄昏到午夜
            {
                sunLerpFactor = Mathf.InverseLerp(1f, 0.75f, currentTimeOfDay);
            }
            else // 黎明到黄昏（白天）
            {
                sunLerpFactor = 1f; // 保持最大强度
            }

            // 使用一个更平滑的曲线来控制强度，避免突兀的开关
            // sunIntensityMultiplier = 1.0f - Mathf.Abs(currentTimeOfDay - 0.5f) * 2f;
            // sunLight.intensity = Mathf.Lerp(0, sunIntensity, sunIntensityMultiplier * 2f); // 调整乘数使过渡更明显

            // 更精细的强度控制，例如在0.2-0.3和0.7-0.8之间逐渐增强/减弱
            float intensityCurve = 0f;
            if (currentTimeOfDay >= 0.2f && currentTimeOfDay <= 0.8f) // 假设白天主要区间
            {
                // 从 0.2 到 0.5 逐渐增强，从 0.5 到 0.8 逐渐减弱
                float peakTime = 0.5f;
                float transitionStart = 0.2f;
                float transitionEnd = 0.8f;

                if (currentTimeOfDay < peakTime)
                {
                    intensityCurve = Mathf.InverseLerp(transitionStart, peakTime, currentTimeOfDay);
                }
                else
                {
                    intensityCurve = Mathf.InverseLerp(transitionEnd, peakTime, currentTimeOfDay);
                }
                intensityCurve = Mathf.Clamp01(intensityCurve); // 确保在0-1之间
            }
            sunLight.intensity = Mathf.Lerp(0, sunIntensity, intensityCurve);
            sunLight.enabled = sunLight.intensity > 0.01f; // 当强度非常低时禁用光照


            // 可以根据需要调整颜色
            // sunLight.color = Color.Lerp(Color.white, Color.red, sunIntensityMultiplier); // 黄昏时偏红
        }

        // 如果有月亮光，在夜晚激活
        if (moonLight != null)
        {
            // 假设月亮在太阳的对面，旋转角度比太阳多180度
            moonLight.transform.rotation = Quaternion.Euler((currentTimeOfDay * 360f) + 90f, 170f, 0);

            // 月光在夜晚时可见
            float moonIntensityCurve = 0f;
            if (currentTimeOfDay < 0.2f || currentTimeOfDay > 0.8f) // 假设夜晚主要区间
            {
                // 从 0.8 到 0 逐渐增强，从 0 到 0.2 逐渐减弱
                if (currentTimeOfDay > 0.8f)
                {
                    moonIntensityCurve = Mathf.InverseLerp(0.8f, 1f, currentTimeOfDay);
                }
                else
                {
                    moonIntensityCurve = Mathf.InverseLerp(0f, 0.2f, currentTimeOfDay);
                }
                moonIntensityCurve = Mathf.Clamp01(moonIntensityCurve); // 确保在0-1之间
            }
            moonLight.intensity = Mathf.Lerp(0, moonIntensity, moonIntensityCurve);
            moonLight.enabled = moonLight.intensity > 0.01f; // 当强度非常低时禁用光照
        }
    }

    void UpdateSkyboxAndAmbient()
    {
        // 根据时间更新天空盒颜色和环境光
        // 定义白天和夜晚的阈值
        float dawnTime = 0.2f;  // 黎明开始
        float dayTime = 0.3f;   // 白天完全亮
        float duskTime = 0.7f;  // 黄昏开始
        float nightTime = 0.8f; // 夜晚完全黑

        Color currentSkyColor;
        float currentAmbientIntensity;

        if (currentTimeOfDay >= dayTime && currentTimeOfDay <= duskTime)
        {
            // 完全白天
            currentSkyColor = daySkyColor;
            currentAmbientIntensity = dayAmbientIntensity;
        }
        else if (currentTimeOfDay > duskTime && currentTimeOfDay < nightTime)
        {
            // 黄昏过渡
            float lerpFactor = Mathf.InverseLerp(duskTime, nightTime, currentTimeOfDay);
            currentSkyColor = Color.Lerp(daySkyColor, nightSkyColor, lerpFactor);
            currentAmbientIntensity = Mathf.Lerp(dayAmbientIntensity, nightAmbientIntensity, lerpFactor);
        }
        else if (currentTimeOfDay >= dawnTime && currentTimeOfDay < dayTime)
        {
            // 黎明过渡
            float lerpFactor = Mathf.InverseLerp(dawnTime, dayTime, currentTimeOfDay);
            currentSkyColor = Color.Lerp(nightSkyColor, daySkyColor, lerpFactor);
            currentAmbientIntensity = Mathf.Lerp(nightAmbientIntensity, dayAmbientIntensity, lerpFactor);
        }
        else
        {
            // 完全夜晚
            currentSkyColor = nightSkyColor;
            currentAmbientIntensity = nightAmbientIntensity;
        }

        RenderSettings.ambientLight = currentSkyColor;
        RenderSettings.ambientIntensity = currentAmbientIntensity;

        // 如果你使用的是 Unity 默认的天空盒，它会根据定向光的颜色和强度自动变化。
        // 对于自定义的天空盒材质，可能需要在这里通过 SetColor 或 SetVector 来更新颜色参数。
        // 例如：RenderSettings.skybox?.SetColor("_Tint", currentSkyColor); // 假设天空盒材质有_Tint属性
    }

    // ⭐ 新增：更新 UI 进度圈的方法
    void UpdateUIProgressBar()
    {
        if (dayNightProgressBar != null)
        {
            // 直接将 currentTimeOfDay 赋值给 fillAmount
            // 因为 currentTimeOfDay 已经在 0 到 1 之间循环
            dayNightProgressBar.fillAmount = currentTimeOfDay;
        }
        else
        {
            // 避免在 Start() 中检查时出现大量警告，只在第一次检测到 null 时警告
            // 并且不在每帧都警告
            Debug.LogWarning("DayNightCycle: dayNightProgressBar 未赋值！请拖拽 UI Image 到 Inspector。", this);
            // 警告一次后，如果仍然为 null，则将此脚本设置为禁用，防止持续报错
            this.enabled = false;
        }
    }
}