using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 能见度管理器 - 根据高度动态调整雾效和能见度
/// 实现从云层（低能见度）到清晰场景（高能见度）的过渡
/// </summary>
public class VisibilityManager : MonoBehaviour
{
    #region Singleton
    private static VisibilityManager _instance;
    public static VisibilityManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("VisibilityManager");
                _instance = go.AddComponent<VisibilityManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
    #endregion

    #region 配置参数
    [Header("高度分段配置")]
    [Tooltip("云层区域高度（米），高于此高度时能见度最低")]
    [SerializeField] private float cloudLayerHeight = 2500f;
    
    [Tooltip("云层过渡区域高度（米），在此高度以下开始逐渐清晰")]
    [SerializeField] private float cloudTransitionHeight = 2000f;
    
    [Tooltip("清晰区域高度（米），低于此高度时能见度最高")]
    [SerializeField] private float clearHeight = 1000f;
    
    [Tooltip("地面清晰区域高度（米），低于此高度时完全清晰")]
    [SerializeField] private float groundClearHeight = 500f;

    [Header("雾效配置")]
    [Tooltip("云层区域的最大雾效密度")]
    [SerializeField] private float maxFogDensity = 0.15f;
    
    [Tooltip("清晰区域的最小雾效密度")]
    [SerializeField] private float minFogDensity = 0.01f;
    
    [Tooltip("雾效颜色（云层白色）")]
    [SerializeField] private Color cloudFogColor = new Color(0.9f, 0.9f, 0.95f, 1f);
    
    [Tooltip("地面雾效颜色（淡蓝色）")]
    [SerializeField] private Color groundFogColor = new Color(0.7f, 0.8f, 0.9f, 1f);

    [Header("雾效模式")]
    [Tooltip("雾效模式")]
    [SerializeField] private FogMode fogMode = FogMode.ExponentialSquared;

    [Header("后处理效果（可选）")]
    [Tooltip("是否启用后处理调整")]
    [SerializeField] private bool enablePostProcessing = false;
    
    [Tooltip("云层区域的亮度（0-1）")]
    [SerializeField] private float cloudBrightness = 0.8f;
    
    [Tooltip("清晰区域的亮度（0-1）")]
    [SerializeField] private float clearBrightness = 1.0f;

    [Header("平滑过渡")]
    [Tooltip("雾效变化平滑度（值越大变化越平滑）")]
    [SerializeField] private float fogSmoothness = 2f;

    [Header("相机设置（可选）")]
    [Tooltip("是否自动调整相机远裁剪平面")]
    [SerializeField] private bool adjustCameraFarPlane = true;
    
    [Tooltip("云层区域的远裁剪距离（米）")]
    [SerializeField] private float cloudFarPlane = 500f;
    
    [Tooltip("清晰区域的远裁剪距离（米）")]
    [SerializeField] private float clearFarPlane = 5000f;
    #endregion

    #region 私有变量
    private float currentFogDensity = 0f;
    private Color currentFogColor = Color.white;
    private float currentBrightness = 1f;
    private Camera mainCamera = null;
    private float originalFarPlane = 1000f;
    #endregion

    #region Unity生命周期
    private void Start()
    {
        InitializeFog();
        InitializeCamera();
        Debug.Log("✅ VisibilityManager 初始化完成");
    }

    /// <summary>
    /// 初始化相机设置
    /// </summary>
    private void InitializeCamera()
    {
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            originalFarPlane = mainCamera.farClipPlane;
            Debug.Log($"✅ 相机初始化: 原始远裁剪平面={originalFarPlane}");
        }
        else
        {
            Debug.LogWarning("⚠️ VisibilityManager: 未找到主相机");
        }
    }

    private void Update()
    {
        if (PhysicsManager.Instance != null)
        {
            UpdateVisibility(PhysicsManager.Instance.CurrentHeight);
        }
    }
    #endregion

    #region 初始化
    /// <summary>
    /// 初始化雾效设置
    /// </summary>
    private void InitializeFog()
    {
        // 启用Unity的雾效系统
        RenderSettings.fog = true;
        RenderSettings.fogMode = fogMode;
        
        // 设置初始雾效（云层状态）
        currentFogDensity = maxFogDensity;
        currentFogColor = cloudFogColor;
        
        RenderSettings.fogDensity = currentFogDensity;
        RenderSettings.fogColor = currentFogColor;
        
        Debug.Log($"✅ 雾效初始化: 密度={currentFogDensity}, 颜色={currentFogColor}");
    }
    #endregion

    #region 能见度更新
    /// <summary>
    /// 根据高度更新能见度
    /// </summary>
    /// <param name="height">当前高度（米）</param>
    private void UpdateVisibility(float height)
    {
        // 计算雾效密度（根据高度分段）
        float targetFogDensity = CalculateFogDensity(height);
        
        // 平滑过渡雾效密度
        currentFogDensity = Mathf.Lerp(currentFogDensity, targetFogDensity, Time.deltaTime * fogSmoothness);
        
        // 计算雾效颜色（根据高度混合）
        Color targetFogColor = CalculateFogColor(height);
        currentFogColor = Color.Lerp(currentFogColor, targetFogColor, Time.deltaTime * fogSmoothness);
        
        // 应用雾效设置
        RenderSettings.fogDensity = currentFogDensity;
        RenderSettings.fogColor = currentFogColor;
        
        // 更新后处理效果（如果启用）
        if (enablePostProcessing)
        {
            UpdatePostProcessing(height);
        }
        
        // 更新相机远裁剪平面（如果启用）
        if (adjustCameraFarPlane && mainCamera != null)
        {
            UpdateCameraFarPlane(height);
        }
    }

    /// <summary>
    /// 根据高度计算目标雾效密度
    /// </summary>
    private float CalculateFogDensity(float height)
    {
        if (height >= cloudLayerHeight)
        {
            // 云层区域：最大雾效密度
            return maxFogDensity;
        }
        else if (height >= cloudTransitionHeight)
        {
            // 云层过渡区域：从最大密度逐渐降低
            float t = Mathf.InverseLerp(cloudLayerHeight, cloudTransitionHeight, height);
            return Mathf.Lerp(maxFogDensity, maxFogDensity * 0.6f, t);
        }
        else if (height >= clearHeight)
        {
            // 过渡到清晰区域：继续降低雾效密度
            float t = Mathf.InverseLerp(cloudTransitionHeight, clearHeight, height);
            return Mathf.Lerp(maxFogDensity * 0.6f, minFogDensity * 2f, t);
        }
        else if (height >= groundClearHeight)
        {
            // 清晰区域：进一步降低雾效密度
            float t = Mathf.InverseLerp(clearHeight, groundClearHeight, height);
            return Mathf.Lerp(minFogDensity * 2f, minFogDensity, t);
        }
        else
        {
            // 地面区域：最小雾效密度
            return minFogDensity;
        }
    }

    /// <summary>
    /// 根据高度计算雾效颜色
    /// </summary>
    private Color CalculateFogColor(float height)
    {
        if (height >= cloudTransitionHeight)
        {
            // 云层区域：白色雾效
            return cloudFogColor;
        }
        else if (height >= clearHeight)
        {
            // 过渡区域：从白色逐渐过渡到淡蓝色
            float t = Mathf.InverseLerp(cloudTransitionHeight, clearHeight, height);
            return Color.Lerp(cloudFogColor, groundFogColor, t);
        }
        else
        {
            // 清晰区域：淡蓝色雾效
            return groundFogColor;
        }
    }

    /// <summary>
    /// 更新后处理效果（如果需要）
    /// </summary>
    private void UpdatePostProcessing(float height)
    {
        // 计算目标亮度
        float targetBrightness = 1f;
        
        if (height >= cloudLayerHeight)
        {
            targetBrightness = cloudBrightness;
        }
        else if (height >= clearHeight)
        {
            float t = Mathf.InverseLerp(cloudLayerHeight, clearHeight, height);
            targetBrightness = Mathf.Lerp(cloudBrightness, clearBrightness, t);
        }
        else
        {
            targetBrightness = clearBrightness;
        }
        
        // 平滑过渡亮度
        currentBrightness = Mathf.Lerp(currentBrightness, targetBrightness, Time.deltaTime * fogSmoothness);
        
        // 这里可以添加后处理效果的调整
        // 例如：调整曝光度、对比度等
        // 注意：需要Unity的Post Processing Stack才能使用
    }

    /// <summary>
    /// 更新相机远裁剪平面
    /// </summary>
    private void UpdateCameraFarPlane(float height)
    {
        float targetFarPlane = clearFarPlane;
        
        if (height >= cloudLayerHeight)
        {
            // 云层区域：近距离裁剪
            targetFarPlane = cloudFarPlane;
        }
        else if (height >= clearHeight)
        {
            // 过渡区域：逐渐增加远裁剪距离
            float t = Mathf.InverseLerp(cloudLayerHeight, clearHeight, height);
            targetFarPlane = Mathf.Lerp(cloudFarPlane, clearFarPlane, t);
        }
        else
        {
            // 清晰区域：最大远裁剪距离
            targetFarPlane = clearFarPlane;
        }
        
        // 平滑过渡
        mainCamera.farClipPlane = Mathf.Lerp(mainCamera.farClipPlane, targetFarPlane, Time.deltaTime * fogSmoothness);
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 手动设置雾效密度（用于调试）
    /// </summary>
    public void SetFogDensity(float density)
    {
        currentFogDensity = Mathf.Clamp01(density);
        RenderSettings.fogDensity = currentFogDensity;
    }

    /// <summary>
    /// 手动设置雾效颜色（用于调试）
    /// </summary>
    public void SetFogColor(Color color)
    {
        currentFogColor = color;
        RenderSettings.fogColor = currentFogColor;
    }

    /// <summary>
    /// 获取当前雾效密度
    /// </summary>
    public float CurrentFogDensity => currentFogDensity;

    /// <summary>
    /// 获取当前雾效颜色
    /// </summary>
    public Color CurrentFogColor => currentFogColor;
    #endregion

    #region 调试
    private void OnGUI()
    {
        if (PhysicsManager.Instance == null) return;
        
        float height = PhysicsManager.Instance.CurrentHeight;
        
        GUIStyle style = new GUIStyle();
        style.fontSize = 14;
        style.normal.textColor = Color.yellow;
        
        float yPos = Screen.height - 100;
        GUI.Label(new Rect(10, yPos, 400, 20), $"能见度系统", style);
        yPos += 20;
        GUI.Label(new Rect(10, yPos, 400, 20), $"当前高度: {height:F1} m", style);
        yPos += 20;
        GUI.Label(new Rect(10, yPos, 400, 20), $"雾效密度: {currentFogDensity:F3}", style);
        yPos += 20;
        GUI.Label(new Rect(10, yPos, 400, 20), $"雾效颜色: R={currentFogColor.r:F2} G={currentFogColor.g:F2} B={currentFogColor.b:F2}", style);
    }
    #endregion
}

