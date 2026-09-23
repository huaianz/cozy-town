using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;


/// </summary>
public class DayNightManager : Singleton<DayNightManager>
{
    #region 时间配置
    [Header("=== 时间配置 ===")]
    [Tooltip("白天开始小时（默认6点）")]
    [Range(0, 23)]
    public int dayStartHour = 6;

    [Tooltip("夜晚开始小时（默认18点）")]
    [Range(0, 23)]
    public int nightStartHour = 18;

    [Tooltip("黎明过渡时长（小时）")]
    [Range(0.1f, 3f)]
    public float dawnTransitionHours = 1f;

    [Tooltip("黄昏过渡时长（小时）")]
    [Range(0.1f, 3f)]
    public float duskTransitionHours = 1f;
    #endregion

    #region 天气配置
    [Header("=== 天气配置 ===")]
    [Tooltip("下雨时启用的特效对象（比如雨滴粒子）")]
    public GameObject rainEffect;

    [Tooltip("下雨时全局光与太阳光的强度倍率")]
    [Range(0f, 1f)]
    public float rainLightScale = 0.6f;

    [Tooltip("阴天时全局光与太阳光的强度倍率")]
    [Range(0f, 1f)]
    public float cloudyLightScale = 0.85f;

    [Tooltip("下雨时的光色")]
    public Color rainLightColor = new Color(0.55f, 0.6f, 0.75f);

    [Tooltip("阴天时的光色")]
    public Color cloudyLightColor = new Color(0.8f, 0.82f, 0.85f);
    #endregion

    #region 全局光照配置 (Light2D Global Light)
    [Header("=== 全局光照配置 (Light2D Global Light) ===")]
    [Tooltip("Light2D全局光 - 控制整体场景亮度")]
    public Light2D globalLight;

    [Tooltip("白天全局光强度")]
    [Range(0f, 3f)]
    public float dayGlobalIntensity = 1.2f;

    [Tooltip("夜晚全局光强度")]
    [Range(0f, 1f)]
    public float nightGlobalIntensity = 0.15f;

    [Tooltip("白天全局光颜色")]
    public Color dayGlobalColor = new Color(1f, 0.98f, 0.9f);

    [Tooltip("夜晚全局光颜色")]
    public Color nightGlobalColor = new Color(0.2f, 0.25f, 0.4f);

    [Tooltip("黎明/黄昏全局光颜色")]
    public Color dawnDuskGlobalColor = new Color(1f, 0.65f, 0.4f);
    #endregion

    #region 太阳光配置 (Light2D Sun)
    [Header("=== 太阳光配置 (Light2D Sun) ===")]
    [Tooltip("太阳光Light2D组件 - 建议使用Freeform Light或Sprite Light")]
    public Light2D sunLight2D;

    [Tooltip("太阳光起始位置（左侧）")]
    public Vector3 sunStartPosition = new Vector3(-10f, 5f, 0f);

    [Tooltip("太阳光最高点位置（正午）")]
    public Vector3 sunMidPosition = new Vector3(0f, 8f, 0f);

    [Tooltip("太阳光结束位置（右侧）")]
    public Vector3 sunEndPosition = new Vector3(10f, 5f, 0f);

    [Tooltip("白天太阳光强度")]
    [Range(0f, 5f)]
    public float daySunIntensity = 2f;

    [Tooltip("夜晚太阳光强度")]
    [Range(0f, 1f)]
    public float nightSunIntensity = 0f;

    [Tooltip("白天太阳光颜色")]
    public Color daySunColor = new Color(1f, 0.95f, 0.8f);

    [Tooltip("夜晚太阳光颜色")]
    public Color nightSunColor = new Color(0.15f, 0.2f, 0.35f);

    [Tooltip("黎明/黄昏太阳光颜色（暖橙色）")]
    public Color dawnDuskSunColor = new Color(1f, 0.55f, 0.25f);

    [Tooltip("太阳光半径")]
    [Range(0.5f, 10f)]
    public float sunRadius = 3f;

    [Tooltip("太阳光边缘柔和度")]
    [Range(0f, 1f)]
    public float sunFalloffStrength = 0.5f;
    #endregion

    #region 环境光与色调配置
    [Header("=== 环境光与色调配置 ===")]
    [Tooltip("是否启用颜色渐变（影响整体场景色调）")]
    public bool enableColorTint = true;

    [Tooltip("白天场景色调")]
    public Color dayTintColor = Color.white;

    [Tooltip("夜晚场景色调（偏蓝紫）")]
    public Color nightTintColor = new Color(0.3f, 0.35f, 0.55f);

    [Tooltip("黎明/黄昏场景色调（偏橙红）")]
    public Color dawnDuskTintColor = new Color(1f, 0.7f, 0.5f);
    #endregion

    #region 2D阴影配置
    [Header("=== 2D阴影配置 ===")]
    [Tooltip("是否启用2D阴影控制（仅URP 10-版本支持动态控制）")]
    public bool enableShadowControl = false;

    [Tooltip("白天阴影强度")]
    [Range(0f, 1f)]
    public float dayShadowIntensity = 0.6f;

    [Tooltip("夜晚阴影强度")]
    [Range(0f, 1f)]
    public float nightShadowIntensity = 0.1f;

    [Tooltip("阴影柔和度（仅URP 10-版本支持）")]
    [Range(0f, 1f)]
    public float shadowSoftness = 0.3f;
    #endregion

    #region 夜晚灯光配置 (Light2D)
    [Header("=== 夜晚灯光配置 (Light2D) ===")]
    [Tooltip("是否启用灯光自动控制")]
    public bool enableLightControl = true;

    [Tooltip("夜晚自动开启的Light2D灯光列表")]
    [SerializeField]
    private List<Light2D> nightLights2D = new List<Light2D>();

    [Tooltip("夜晚灯光目标强度")]
    [Range(0.1f, 5f)]
    public float nightLightIntensity = 1.5f;

    [Tooltip("灯光过渡平滑度")]
    [Range(0.01f, 0.2f)]
    public float lightTransitionSmoothness = 0.05f;
    #endregion

    #region 调试信息
    [Header("=== 调试信息 ===")]
    [Tooltip("当前昼夜状态")]
    public DayNightState currentState;

    [Tooltip("当前时间进度（0-1）")]
    [Range(0f, 1f)]
    public float dayProgress;

    [Tooltip("当前小时")]
    public int currentHour;

    [Tooltip("当前分钟")]
    public int currentMinute;

    [Tooltip("光照过渡系数（0=夜晚，1=白天）")]
    [Range(0f, 1f)]
    public float lightTransitionLerp;
    #endregion

    #region 事件系统（与原有系统100%兼容）
    /// <summary>
    /// 昼夜状态变化事件
    /// 参数：新状态, 旧状态
    /// </summary>
    public static event Action<DayNightState, DayNightState> OnDayNightStateChanged;

    /// <summary>
    /// 白天开始事件
    /// </summary>
    public static event Action OnDayStart;

    /// <summary>
    /// 夜晚开始事件
    /// </summary>
    public static event Action OnNightStart;

    /// <summary>
    /// 昼夜过渡进度事件
    /// 参数：过渡进度(0-1)，true=白天到夜晚，false=夜晚到白天
    /// </summary>
    public static event Action<float, bool> OnDayNightTransition;
    #endregion

    #region 私有变量
    /// <summary>
    /// 上一帧的昼夜状态
    /// </summary>
    private DayNightState previousState;

    /// <summary>
    /// 当前灯光强度缓存（用于平滑过渡）
    /// </summary>
    private Dictionary<Light2D, float> lightIntensityCache = new Dictionary<Light2D, float>();

    private int lastLightHour = -1;

    private int lastLightMinute = -1;

    private WeatherType currentWeather = WeatherType.Sunny;

    private bool lightDirty;
    #endregion

    #region 初始化与生命周期
    /// <summary>
    /// 管理器初始化
    /// 保持与原有Singleton模式完全一致
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
        // 初始化灯光强度缓存
        InitializeLightCache();

        // 初始化太阳光参数
        InitializeSunLight();

        // 记录初始状态
        previousState = currentState;
    }

    /// <summary>
    /// 注册事件监听
    /// 与原有EventHandler系统完全兼容
    /// </summary>
    private void OnEnable()
    {
        // 监听现有时间系统的分钟事件
        EventHandler.GameMinuteEvent += OnGameMinuteEvent;
        EventHandler.GameDateEvent += OnGameDateEvent;
        EventHandler.WeatherChangedEvent += OnWeatherChangedEvent;
    }

    /// <summary>
    /// 取消事件监听
    /// </summary>
    private void OnDisable()
    {
        EventHandler.GameMinuteEvent -= OnGameMinuteEvent;
        EventHandler.GameDateEvent -= OnGameDateEvent;
        EventHandler.WeatherChangedEvent -= OnWeatherChangedEvent;
    }

    /// <summary>
    /// 每帧更新视觉效果
    /// </summary>
    private void Update()
    {
        bool timeChanged = currentHour != lastLightHour || currentMinute != lastLightMinute || lightDirty;

        if (timeChanged)
        {
            lastLightHour = currentHour;
            lastLightMinute = currentMinute;
            lightDirty = false;

            // 更新昼夜进度
            UpdateDayProgress();

            // 计算光照过渡系数
            lightTransitionLerp = CalculateLightIntensityLerp();

            // 更新全局光照
            UpdateGlobalLight();

            // 更新太阳光（位置+颜色+强度）
            UpdateSunLight2D();

            // 更新2D阴影
            UpdateShadows();

            // 检查状态变化并触发事件
            CheckStateChanged();

            // 触发过渡事件
            TriggerTransitionEvents();
        }

        // 更新夜晚灯光
        UpdateNightLights();
    }
    #endregion

    #region 初始化方法
    /// <summary>
    /// 初始化灯光强度缓存
    /// </summary>
    private void InitializeLightCache()
    {
        lightIntensityCache.Clear();
        foreach (var light in nightLights2D)
        {
            if (light != null)
            {
                lightIntensityCache[light] = light.intensity;
            }
        }
    }

    /// <summary>
    /// 初始化太阳光参数
    /// </summary>
    private void InitializeSunLight()
    {
        if (sunLight2D != null)
        {
            sunLight2D.pointLightOuterRadius = sunRadius;
            sunLight2D.falloffIntensity = sunFalloffStrength;
        }
    }
    #endregion

    #region 事件处理（与原有系统完全兼容）
    /// <summary>
    /// 游戏时间分钟变化事件处理
    /// </summary>
    /// <param name="minute">当前分钟</param>
    /// <param name="hour">当前小时</param>
    private void OnGameMinuteEvent(int minute, int hour)
    {
        currentMinute = minute;
        currentHour = hour;
    }

    /// <summary>
    /// 游戏日期变化事件处理
    /// </summary>
    private void OnGameDateEvent(int hour, int day, int month, int year, Season season)
    {
        currentHour = hour;
    }

    private void OnWeatherChangedEvent(WeatherType weather)
    {
        currentWeather = weather;
        lightDirty = true;

        if (rainEffect != null)
        {
            rainEffect.SetActive(WeatherEffects.IsRainy(weather));
        }
    }

    private float GetWeatherLightScale()
    {
        return WeatherEffects.GetLightScale(currentWeather);
    }

    private Color ApplyWeatherTint(Color color)
    {
        if (WeatherEffects.IsRainy(currentWeather))
        {
            return Color.Lerp(color, rainLightColor, 0.5f);
        }

        if (WeatherEffects.IsFoggy(currentWeather) || currentWeather == WeatherType.Overcast)
        {
            return Color.Lerp(color, cloudyLightColor, 0.45f);
        }

        if (WeatherEffects.IsSnowy(currentWeather))
        {
            return Color.Lerp(color, cloudyLightColor, 0.35f);
        }

        if (currentWeather == WeatherType.Cloudy)
        {
            return Color.Lerp(color, cloudyLightColor, 0.3f);
        }

        return color;
    }
    #endregion

    #region 时间进度计算
    /// <summary>
    /// 更新一天的时间进度
    /// </summary>
    private void UpdateDayProgress()
    {
        // 计算0-1的时间进度
        dayProgress = (currentHour * 60f + currentMinute) / 1440f;

        // 更新当前昼夜状态
        UpdateDayNightState();
    }

    /// <summary>
    /// 更新昼夜状态
    /// </summary>
    private void UpdateDayNightState()
    {
        float dawnEnd = dayStartHour + dawnTransitionHours;
        float duskStart = nightStartHour - duskTransitionHours;

        if (currentHour >= dayStartHour && currentHour < dawnEnd)
        {
            currentState = DayNightState.Dawn;
        }
        else if (currentHour >= dawnEnd && currentHour < duskStart)
        {
            currentState = DayNightState.Day;
        }
        else if (currentHour >= duskStart && currentHour < nightStartHour)
        {
            currentState = DayNightState.Dusk;
        }
        else
        {
            currentState = DayNightState.Night;
        }
    }
    #endregion

    #region 光照核心计算
    /// <summary>
    /// 计算光照强度插值系数
    /// </summary>
    /// <returns>0-1的插值系数（0=夜晚，1=白天）</returns>
    private float CalculateLightIntensityLerp()
    {
        float dawnEnd = dayStartHour + dawnTransitionHours;
        float duskStart = nightStartHour - duskTransitionHours;

        if (currentState == DayNightState.Dawn)
        {
            // 黎明：从夜晚过渡到白天
            float currentTime = currentHour + currentMinute / 60f;
            float progress = (currentTime - dayStartHour) / dawnTransitionHours;
            return Mathf.SmoothStep(0f, 1f, progress);
        }
        else if (currentState == DayNightState.Day)
        {
            return 1f;
        }
        else if (currentState == DayNightState.Dusk)
        {
            // 黄昏：从白天过渡到夜晚
            float currentTime = currentHour + currentMinute / 60f;
            float progress = (currentTime - duskStart) / duskTransitionHours;
            return Mathf.SmoothStep(1f, 0f, progress);
        }
        else // Night
        {
            return 0f;
        }
    }

    /// <summary>
    /// 计算颜色插值（支持黎明/黄昏的暖色调峰值）
    /// </summary>
    /// <param name="dayColor">白天颜色</param>
    /// <param name="nightColor">夜晚颜色</param>
    /// <param name="dawnDuskColor">黎明黄昏颜色</param>
    /// <param name="dawnDuskBlend">黎明黄昏混合强度</param>
    /// <returns>插值后的颜色</returns>
    private Color CalculateColorLerp(Color dayColor, Color nightColor, Color dawnDuskColor, float dawnDuskBlend = 0.5f)
    {
        // 基础昼夜颜色插值
        Color baseColor = Color.Lerp(nightColor, dayColor, lightTransitionLerp);

        // 黎明/黄昏时添加暖色调
        if (currentState == DayNightState.Dawn || currentState == DayNightState.Dusk)
        {
            float transitionProgress;
            if (currentState == DayNightState.Dawn)
            {
                float currentTime = currentHour + currentMinute / 60f;
                transitionProgress = (currentTime - dayStartHour) / dawnTransitionHours;
            }
            else
            {
                float currentTime = currentHour + currentMinute / 60f;
                float duskStart = nightStartHour - duskTransitionHours;
                transitionProgress = 1f - (currentTime - duskStart) / duskTransitionHours;
            }

            // 使用正弦函数创建峰值效果（过渡中间时暖色调最强）
            float peakFactor = Mathf.Sin(transitionProgress * Mathf.PI);
            return Color.Lerp(baseColor, dawnDuskColor, peakFactor * dawnDuskBlend);
        }

        return baseColor;
    }
    #endregion

    #region 全局光照更新
    /// <summary>
    /// 更新Light2D全局光照
    /// </summary>
    private void UpdateGlobalLight()
    {
        if (globalLight == null) return;

        // 计算目标强度
        float targetIntensity = Mathf.Lerp(nightGlobalIntensity, dayGlobalIntensity, lightTransitionLerp) * GetWeatherLightScale();
        globalLight.intensity = targetIntensity;

        // 计算目标颜色
        globalLight.color = ApplyWeatherTint(CalculateColorLerp(dayGlobalColor, nightGlobalColor, dawnDuskGlobalColor, 0.4f));
    }
    #endregion

    #region 太阳光更新 (Light2D)
    /// <summary>
    /// 更新太阳光2D（位置+颜色+强度）
    /// 实现太阳从左到右的抛物线运动轨迹
    /// </summary>
    private void UpdateSunLight2D()
    {
        if (sunLight2D == null) return;

        // 1. 更新太阳光位置 - 抛物线运动
        UpdateSunPosition();

        // 2. 更新太阳光强度
        float targetIntensity = Mathf.Lerp(nightSunIntensity, daySunIntensity, lightTransitionLerp) * GetWeatherLightScale();
        sunLight2D.intensity = targetIntensity;

        // 3. 更新太阳光颜色
        sunLight2D.color = CalculateColorLerp(daySunColor, nightSunColor, dawnDuskSunColor, 0.6f);

        // 4. 更新太阳光半径（夜晚缩小）
        sunLight2D.pointLightOuterRadius = Mathf.Lerp(sunRadius * 0.5f, sunRadius, lightTransitionLerp);
    }

    /// <summary>
    /// 更新太阳光位置 - 贝塞尔曲线实现平滑抛物线轨迹
    /// </summary>
    private void UpdateSunPosition()
    {
        // 计算白天时间进度（0-1）
        float dayTimeProgress = (float)(currentHour - dayStartHour) / (nightStartHour - dayStartHour);
        dayTimeProgress = Mathf.Clamp01(dayTimeProgress);

        // 使用二次贝塞尔曲线计算太阳位置
        // P0: 起始位置, P1: 最高点, P2: 结束位置
        Vector3 position = CalculateQuadraticBezier(
            sunStartPosition,
            sunMidPosition,
            sunEndPosition,
            dayTimeProgress
        );

        sunLight2D.transform.position = position;
    }

    /// <summary>
    /// 二次贝塞尔曲线计算
    /// </summary>
    private Vector3 CalculateQuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1 - t;
        return u * u * p0 + 2 * u * t * p1 + t * t * p2;
    }
    #endregion

    #region 2D阴影更新
    /// <summary>
    /// 更新2D阴影强度
    /// 
    /// 注意：URP 12+版本移除了Light2D.shadows和Light2D.shadowSoftness API
    /// 新版URP中阴影设置只能在Light2D组件的Inspector面板中静态配置
    /// 
    /// 如果您使用的是URP 10或更旧版本，请在Player Settings中添加编译符号：
    /// URP_10_OR_OLDER
    /// 即可启用完整的动态阴影控制功能
    /// </summary>
    private void UpdateShadows()
    {
        if (!enableShadowControl) return;

        // URP 10或更旧版本的阴影控制（条件编译）
#if URP_10_OR_OLDER
        // 计算目标阴影强度
        float targetIntensity = Mathf.Lerp(nightShadowIntensity, dayShadowIntensity, lightTransitionLerp);

        // 更新全局光阴影
        if (globalLight != null)
        {
            // 注意：以下API仅在URP 10或更旧版本中可用
            globalLight.shadowIntensity = targetIntensity;
            globalLight.shadowSoftness = shadowSoftness;
        }

        // 更新太阳光阴影
        if (sunLight2D != null)
        {
            // 注意：以下API仅在URP 10或更旧版本中可用
            sunLight2D.shadowIntensity = targetIntensity;
            sunLight2D.shadowSoftness = shadowSoftness;
        }
#else
        // URP 12+版本：动态阴影控制已被URP移除
        // 阴影设置请在Light2D组件的Inspector面板中静态配置
        // 如需阴影随昼夜变化效果，建议通过调整lightIntensity间接实现
#endif
    }
    #endregion

    #region 夜晚灯光更新
    /// <summary>
    /// 更新所有夜晚Light2D灯光
    /// 支持平滑过渡开启/关闭
    /// </summary>
    private void UpdateNightLights()
    {
        if (!enableLightControl) return;

        // 当过渡系数小于0.5时开启灯光
        bool shouldLightsBeOn = lightTransitionLerp < 0.5f;
        float targetIntensity = shouldLightsBeOn ? nightLightIntensity : 0f;

        foreach (var light in nightLights2D)
        {
            if (light == null) continue;

            // 获取当前强度（从缓存或组件）
            float currentIntensity;

            if (!lightIntensityCache.TryGetValue(light, out currentIntensity))
            {
                currentIntensity = light.intensity;
            }

            // 平滑过渡
            float newIntensity = Mathf.Lerp(currentIntensity, targetIntensity, lightTransitionSmoothness);

            lightIntensityCache[light] = newIntensity;

            // 应用新强度
            if (!Mathf.Approximately(light.intensity, newIntensity))
            {
                light.intensity = newIntensity;
            }

            // 完全熄灭时禁用组件（性能优化）
            bool shouldEnable = newIntensity > 0.01f;

            if (light.enabled != shouldEnable)
            {
                light.enabled = shouldEnable;
            }
        }
    }

    /// <summary>
    /// 添加夜晚灯光（运行时动态添加）
    /// </summary>
    public void AddNightLight(Light2D light)
    {
        if (light != null && !nightLights2D.Contains(light))
        {
            nightLights2D.Add(light);
            lightIntensityCache[light] = light.intensity;
        }
    }

    /// <summary>
    /// 移除夜晚灯光
    /// </summary>
    public void RemoveNightLight(Light2D light)
    {
        if (light != null)
        {
            nightLights2D.Remove(light);
            lightIntensityCache.Remove(light);
        }
    }
    #endregion

    #region 事件触发
    /// <summary>
    /// 检查昼夜状态是否变化并触发事件
    /// </summary>
    private void CheckStateChanged()
    {
        if (currentState != previousState)
        {
            // 触发状态变化事件
            OnDayNightStateChanged?.Invoke(currentState, previousState);

            // 触发特定状态开始事件
            if (currentState == DayNightState.Day && previousState != DayNightState.Day)
            {
                OnDayStart?.Invoke();
            }
            else if (currentState == DayNightState.Night && previousState != DayNightState.Night)
            {
                OnNightStart?.Invoke();
            }

            previousState = currentState;
        }
    }

    /// <summary>
    /// 触发昼夜过渡事件
    /// </summary>
    private void TriggerTransitionEvents()
    {
        bool isDayToNight = currentState == DayNightState.Dusk || currentState == DayNightState.Night;
        float transitionProgress = 1f - lightTransitionLerp;
        OnDayNightTransition?.Invoke(transitionProgress, isDayToNight);
    }
    #endregion

    #region 公共API（与原有系统兼容）
    /// <summary>
    /// 获取当前昼夜状态名称（用于UI显示）
    /// </summary>
    /// <returns>状态名称字符串</returns>
    public string GetStateName()
    {
        return currentState switch
        {
            DayNightState.Dawn => "黎明",
            DayNightState.Day => "白天",
            DayNightState.Dusk => "黄昏",
            DayNightState.Night => "夜晚",
            _ => "未知"
        };
    }

    /// <summary>
    /// 检查是否是白天
    /// </summary>
    /// <returns>是否为白天</returns>
    public bool IsDayTime()
    {
        return currentState == DayNightState.Dawn || currentState == DayNightState.Day;
    }

    /// <summary>
    /// 检查是否是夜晚
    /// </summary>
    /// <returns>是否为夜晚</returns>
    public bool IsNightTime()
    {
        return currentState == DayNightState.Dusk || currentState == DayNightState.Night;
    }

    /// <summary>
    /// 获取当前光照强度系数（0-1）
    /// </summary>
    public float GetLightIntensityFactor()
    {
        return lightTransitionLerp;
    }

    /// <summary>
    /// 获取当前场景色调颜色
    /// </summary>
    public Color GetCurrentTintColor()
    {
        return CalculateColorLerp(dayTintColor, nightTintColor, dawnDuskTintColor, 0.3f);
    }
    #endregion

    #region Gizmos调试
    /// <summary>
    /// 在Scene视图中绘制太阳运动轨迹
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // 绘制太阳轨迹点
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(sunStartPosition, 0.3f);
        Gizmos.DrawWireSphere(sunMidPosition, 0.3f);
        Gizmos.DrawWireSphere(sunEndPosition, 0.3f);

        // 绘制贝塞尔曲线路径
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.5f);
        for (int i = 0; i < 20; i++)
        {
            float t1 = i / 20f;
            float t2 = (i + 1) / 20f;
            Vector3 p1 = CalculateQuadraticBezier(sunStartPosition, sunMidPosition, sunEndPosition, t1);
            Vector3 p2 = CalculateQuadraticBezier(sunStartPosition, sunMidPosition, sunEndPosition, t2);
            Gizmos.DrawLine(p1, p2);
        }
    }
    #endregion
}