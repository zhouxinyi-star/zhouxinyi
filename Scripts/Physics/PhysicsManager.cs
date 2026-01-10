using System;
using UnityEngine;

/// <summary>
/// 核心物理模拟系统 - 负责计算自由落体、浮力、空气阻力等物理效果
/// 支持通过控制气球大小来调节降落速度
/// </summary>
public class PhysicsManager : MonoBehaviour
{
    #region Singleton
    private static PhysicsManager _instance;
    public static PhysicsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("PhysicsManager");
                _instance = go.AddComponent<PhysicsManager>();
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
        
        // 自动开始物理模拟
        StartSimulation();
    }
    #endregion

    #region 物理常量
    private const float GRAVITY = 9.81f;                    // 重力加速度 (m/s²)
    private const float AIR_DENSITY_SEA_LEVEL = 1.225f;     // 海平面空气密度 (kg/m³)
    private const float SCALE_HEIGHT = 8500f;               // 大气标度高度 (m)
    private const float HELIUM_DENSITY = 0.1786f;          // 氦气密度 (kg/m³)
    private const float DRAG_COEFFICIENT = 0.85f;          // 阻力系数 - 提升阻力以强化减速感
    private const float PLAYER_MASS = 70f;                  // 玩家质量 (kg)
    private const float TANK_MASS = 0.5f;                   // 单个氦气罐质量 (kg)
    private const float GROUND_LEVEL = 0f;                  // 地面高度 (m)
    #endregion

    #region 物理状态变量
    [Header("物理状态")]
    [SerializeField] private float currentHeight = 3000f;          // 当前高度 (m) - 从5000m降低到3000m以缩短游戏时间
    [SerializeField] private float currentVelocity = 0f;            // 当前速度 (m/s, 向下为正)
    [SerializeField] private float currentAcceleration = 0f;        // 当前加速度 (m/s²)
    [SerializeField] private float balloonVolume = 1.5f;           // 气球体积 (m³) - 提高起始体积，初期就能提供阻力
    [SerializeField] private float heliumRemaining = 80f;           // 剩余氦气量 (m³) - 充足氦气支持大体积
    
    [Header("物理参数")]
    [SerializeField] private float maxHeliumCapacity = 80f;         // 大氦气罐最大容量 (m³)
    [SerializeField] private float inflateRate = 4f;               // 充气速率 (m³/s) - 大幅提高，能在下降过程中明显增大体积
    [SerializeField] private float deflateRate = 1.2f;              // 放气速率 (m³/s) - 放气也保持可控
    [SerializeField] private float minBalloonVolume = 0.1f;         // 气球最小体积 (m³)
    [SerializeField] private float maxBalloonVolume = 70f;           // 气球最大体积 (m³) - 更大的球体积累更多浮力和阻力
    [SerializeField] private float baseCrossSectionalArea = 1.2f;   // 基础横截面积 (m²) - 增大基础阻力面积
    [SerializeField] private float simulationTime = 0f;             // 模拟时间 (s)
    
    [Header("兼容性 - 已废弃")]
    [SerializeField] private int heliumTanksRemaining = 1;          // 保留用于UI兼容（现在固定为1）

    [Header("状态标志")]
    [SerializeField] private bool isSimulating = false;              // 是否正在模拟
    [SerializeField] private bool hasLanded = false;                // 是否已着陆
    [SerializeField] private bool balloonExploded = false;          // 气球是否爆炸
    
    [Header("着陆检测")]
    [SerializeField] private float landingVelocity = 0f;            // 实际着陆速度（从碰撞检测获取）
    
    [Header("低空减速区配置")]
    [Tooltip("开始强力减速的高度（米），低于此高度且气球足够大时，会自动把速度压到安全值附近")]
    [SerializeField] private float brakeZoneHeight = 400f;
    [Tooltip("进入减速区所需的气球体积比例，balloonVolume / maxBalloonVolume 大于此值才触发减速")]
    [SerializeField] private float brakeVolumeRatio = 0.7f;
    [Tooltip("目标安全终端速度（米/秒），用于低空减速区")]
    [SerializeField] private float targetSafeSpeed = 4.5f;
    [Tooltip("减速强度（m/s² 的最大额外减速度）")]
    [SerializeField] private float maxBrakeDeceleration = 12f;
    #endregion

    #region 属性访问器
    public float CurrentHeight => currentHeight;
    public float CurrentVelocity => currentVelocity;
    public float CurrentAcceleration => currentAcceleration;
    public float BalloonVolume => balloonVolume;
    public float HeliumRemaining => heliumRemaining;
    public float MaxHeliumCapacity => maxHeliumCapacity;
    public bool IsSimulating => isSimulating;
    public bool HasLanded => hasLanded;
    public float SimulationTime => simulationTime;
    public bool BalloonExploded => balloonExploded;
    
    // 兼容性属性
    public int HeliumTanksRemaining => heliumTanksRemaining;
    #endregion

    #region 事件定义
    public event Action<float> OnHeightChanged;                    // 高度变化事件
    public event Action<float> OnVelocityChanged;                  // 速度变化事件
    public event Action<float> OnAccelerationChanged;              // 加速度变化事件
    public event Action<int> OnHeliumTanksChanged;                 // 氦气罐数量变化事件（兼容性）
    public event Action OnHeliumDepleted;                          // 氦气耗尽事件
    public event Action OnSafeVelocity;                            // 安全速度事件 (<6m/s)
    public event Action OnDangerousVelocity;                       // 危险速度事件 (>40m/s)
    public event Action<float> OnLanding;                          // 着陆事件 (参数为着陆速度)
    #endregion

    #region Unity生命周期
    private void Start()
    {
        // 确保物理模拟已开始
        if (!isSimulating)
        {
            StartSimulation();
        }
    }

    private void FixedUpdate()
    {
        if (isSimulating && !hasLanded)
        {
            UpdatePhysics(Time.fixedDeltaTime);
        }
    }
    #endregion

    #region 公共方法

    /// <summary>
    /// 开始物理模拟
    /// </summary>
    public void StartSimulation()
    {
        if (isSimulating)
        {
            Debug.LogWarning("物理模拟已经在运行中！");
            return;
        }

        isSimulating = true;
        hasLanded = false;
        balloonExploded = false;
        simulationTime = 0f;
        currentHeight = 3000f;
        currentVelocity = 0f;
        currentAcceleration = 0f;
        balloonVolume = 1.5f;
        heliumRemaining = maxHeliumCapacity;
        landingVelocity = 0f;

        Debug.Log("物理模拟开始！初始高度: 3000m");
    }

    /// <summary>
    /// 停止物理模拟
    /// </summary>
    public void StopSimulation()
    {
        isSimulating = false;
        Debug.Log("物理模拟已停止");
    }

    /// <summary>
    /// 充气 - 连续充气（每帧调用）
    /// </summary>
    /// <param name="deltaTime">时间间隔</param>
    /// <returns>是否成功充气</returns>
    public bool InflateBalloon(float deltaTime)
    {
        if (heliumRemaining <= 0f)
        {
            OnHeliumDepleted?.Invoke();
            return false;
        }

        // 计算本次充气量
        float volumeToAdd = inflateRate * deltaTime;
        volumeToAdd = Mathf.Min(volumeToAdd, heliumRemaining); // 不能超过剩余氦气
        float maxVolumeIncrease = maxBalloonVolume - balloonVolume;
        volumeToAdd = Mathf.Min(volumeToAdd, maxVolumeIncrease); // 不能超过最大体积
        
        if (volumeToAdd > 0f)
        {
            heliumRemaining -= volumeToAdd;
            balloonVolume += volumeToAdd;
            
            // 触发事件（降低频率，避免刷屏）
            if (Mathf.FloorToInt(simulationTime * 10f) != Mathf.FloorToInt((simulationTime - deltaTime) * 10f))
            {
                OnHeliumTanksChanged?.Invoke(1); // 兼容性：固定返回1
            }
            
            return true;
        }
        return false;
    }

    /// <summary>
    /// 放气 - 连续放气（每帧调用）
    /// </summary>
    /// <param name="deltaTime">时间间隔</param>
    /// <returns>是否成功放气</returns>
    public bool DeflateBalloon(float deltaTime)
    {
        if (balloonVolume <= minBalloonVolume)
        {
            return false;
        }
        
        float volumeToRemove = deflateRate * deltaTime;
        float maxVolumeDecrease = balloonVolume - minBalloonVolume;
        volumeToRemove = Mathf.Min(volumeToRemove, maxVolumeDecrease);
        
        if (volumeToRemove > 0f)
        {
            balloonVolume -= volumeToRemove;
            // 注意：放气时氦气不回收，直接释放到空气中
            return true;
        }
        return false;
    }

    /// <summary>
    /// 扔掉空罐子 - 减少质量（已废弃，保留用于兼容）
    /// </summary>
    public bool DetachEmptyTank()
    {
        Debug.Log("扔掉空罐子，减少质量");
        return true;
    }

    /// <summary>
    /// 打爆部分气球
    /// </summary>
    /// <param name="percentage">打爆的百分比 (0-1)</param>
    public void PopBalloon(float percentage)
    {
        percentage = Mathf.Clamp01(percentage);
        float volumeLost = balloonVolume * percentage;
        balloonVolume -= volumeLost;

        if (balloonVolume < minBalloonVolume)
        {
            balloonVolume = minBalloonVolume;
            balloonExploded = true; // 气球爆炸
        }

        Debug.Log($"打爆气球 {percentage * 100:F1}%！当前体积: {balloonVolume:F2}m³");
    }

    /// <summary>
    /// 检查是否着陆（从碰撞检测中调用，传入实际着陆速度）
    /// 只有真正碰撞地面时才判定，不再基于高度自动判定
    /// </summary>
    /// <param name="collisionVelocity">碰撞时的速度（从碰撞检测获取，必须 >= 0）</param>
    public void CheckLanding(float collisionVelocity = -1f)
    {
        // 必须提供碰撞速度（collisionVelocity >= 0）才判定为着陆
        // 这确保只有真正碰撞地面时才触发判定
        if (collisionVelocity < 0f)
        {
            Debug.LogWarning("CheckLanding: 未提供碰撞速度，不判定为着陆（必须通过碰撞检测触发）");
            return;
        }
        
        // 防止重复判定
        if (hasLanded)
        {
            Debug.LogWarning("CheckLanding: 已经判定过着陆，忽略重复调用");
            return;
        }
        
        // 严格检查高度：只有高度真正接近地面（< 5米）时才判定为着陆
        // 但如果是从备用检测调用的（collisionVelocity很小且高度<=1m），允许判定
        const float MAX_LANDING_HEIGHT = 5f;  // 最大允许着陆高度（米）
        bool isBackupCheck = (currentHeight <= GROUND_LEVEL + 1f && collisionVelocity < 5f);
        
        if (!isBackupCheck && currentHeight > GROUND_LEVEL + MAX_LANDING_HEIGHT)
        {
            Debug.LogWarning($"CheckLanding: 高度过高 ({currentHeight:F2}m > {MAX_LANDING_HEIGHT}m)，不是真正的着陆，忽略判定");
            return;  // 高度太高，不判定为着陆
        }
        
        if (isBackupCheck)
        {
            Debug.Log($"✅ 备用着陆检测通过: 高度={currentHeight:F2}m, 速度={collisionVelocity:F2}m/s");
        }
        
        hasLanded = true;
        landingVelocity = collisionVelocity;
        
        // 停止物理模拟（不再下降）
        isSimulating = false;
        
        Debug.Log($"=== 着陆检测（碰撞触发）===");
        Debug.Log($"着陆速度: {landingVelocity:F2} m/s");
        Debug.Log($"当前高度: {currentHeight:F2} m");
        Debug.Log($"气球体积: {balloonVolume:F2} m³");
        Debug.Log($"==================");
        
        OnLanding?.Invoke(landingVelocity);
    }

    /// <summary>
    /// 重置物理状态
    /// </summary>
    public void ResetPhysics()
    {
        currentHeight = 3000f;
        currentVelocity = 0f;
        currentAcceleration = 0f;
        balloonVolume = 1.5f;
        heliumRemaining = maxHeliumCapacity;
        simulationTime = 0f;
        hasLanded = false;
        balloonExploded = false;
        landingVelocity = 0f;
        isSimulating = false;
        
        // 重新开始模拟
        StartSimulation();
    }
    #endregion

    #region 物理计算

    /// <summary>
    /// 更新物理状态
    /// </summary>
    private void UpdatePhysics(float deltaTime)
    {
        simulationTime += deltaTime;

        // 计算当前空气密度（随高度指数衰减）
        float airDensity = CalculateAirDensity(currentHeight);

        // 计算浮力（增强效果，使充气对速度有明显影响）
        float buoyancyForce = CalculateBuoyancyForce(airDensity, balloonVolume);

        // 计算空气阻力（横截面积随体积变化）
        float currentCrossSectionalArea = CalculateCrossSectionalArea(balloonVolume);
        float dragForce = CalculateDragForce(airDensity, currentVelocity, currentCrossSectionalArea);

        // 计算总质量
        float totalMass = CalculateTotalMass();

        // 计算净力（向下为正）
        float netForce = (totalMass * GRAVITY) - buoyancyForce - dragForce;

        // 计算加速度
        currentAcceleration = netForce / totalMass;

        // 更新速度（基础物理）
        currentVelocity += currentAcceleration * deltaTime;

        // 低空减速区：当高度较低且气球足够大时，强制将速度往安全终端速度拉
        if (currentHeight < brakeZoneHeight)
        {
            float volumeRatio = balloonVolume / maxBalloonVolume;
            if (volumeRatio >= brakeVolumeRatio)
            {
                // 高度越低、气球越大，减速越强
                float heightFactor = Mathf.InverseLerp(brakeZoneHeight, 0f, currentHeight);               // 400m→0, 0m→1
                float volumeFactor = Mathf.InverseLerp(brakeVolumeRatio, 1f, Mathf.Clamp01(volumeRatio));  // ratio→0, 1→1
                float brakeFactor = Mathf.Clamp01(heightFactor * volumeFactor);

                if (currentVelocity > targetSafeSpeed && brakeFactor > 0f)
                {
                    // 额外减速度（向上），数值越大减速越狠
                    float extraDecel = maxBrakeDeceleration * brakeFactor * deltaTime;
                    currentVelocity -= extraDecel;
                }
            }
        }

        // 速度不能为负（向下为正）
        currentVelocity = Mathf.Max(0f, currentVelocity);

        // 更新高度
        float previousHeight = currentHeight;
        currentHeight -= currentVelocity * deltaTime;
        currentHeight = Mathf.Max(GROUND_LEVEL, currentHeight);

        // 触发事件
        if (Mathf.Abs(currentHeight - previousHeight) > 0.01f)
        {
            OnHeightChanged?.Invoke(currentHeight);
        }

        OnVelocityChanged?.Invoke(currentVelocity);
        OnAccelerationChanged?.Invoke(currentAcceleration);

        // 检查速度警告（改进：只有在浮力不足且速度过快时才警告）
        CheckVelocityWarnings();

        // 备用着陆检测：当高度<=0且速度很小时，自动判定为着陆
        // 这确保即使碰撞检测没有触发，也能判定着陆
        if (!hasLanded && currentHeight <= GROUND_LEVEL + 1f)
        {
            // 如果高度已经到0或接近0，且速度较小，判定为着陆
            if (currentVelocity < 5f)  // 放宽速度限制到5m/s
            {
                Debug.Log($"🔵 备用着陆检测触发: 高度={currentHeight:F2}m, 速度={currentVelocity:F2}m/s");
                // 直接调用CheckLanding，传入当前速度（确保>=0）
                float landingVel = Mathf.Max(0.1f, currentVelocity);
                CheckLanding(landingVel);
            }
            else
            {
                Debug.Log($"⚠️ 高度已到0但速度过快 ({currentVelocity:F2}m/s)，等待速度降低");
            }
        }
    }

    /// <summary>
    /// 计算空气密度（随高度指数衰减）
    /// </summary>
    private float CalculateAirDensity(float height)
    {
        return AIR_DENSITY_SEA_LEVEL * Mathf.Exp(-height / SCALE_HEIGHT);
    }

    /// <summary>
    /// 计算浮力（增强效果）
    /// </summary>
    private float CalculateBuoyancyForce(float airDensity, float volume)
    {
        // 浮力 = (空气密度 - 氦气密度) * 体积 * 重力加速度
        float densityDifference = airDensity - HELIUM_DENSITY;
        float buoyancy = densityDifference * volume * GRAVITY;
        
        // 增强浮力效果，使充气对速度有明显影响
        // 在低空时，浮力效果更明显（因为空气密度大）
        float heightFactor = Mathf.Clamp01(1f - currentHeight / 3000f); // 高度越低，因子越大（调整为3000m最大高度）
        buoyancy *= (1f + heightFactor * 1.4f); // 在低空时大幅增强浮力
        
        // 体积越大，浮力额外增强（模拟大气球更易产生升力）
        if (volume > 10f)
        {
            float volumeBonus = Mathf.Clamp01((volume - 10f) / 60f); // 10-70m³映射0-1
            buoyancy *= (1f + volumeBonus * 0.8f); // 最大额外增强80%
        }

        // 高空补偿：在2500m以上空气稀薄，额外给一定补偿（调整为3000m最大高度）
        if (currentHeight > 2500f)
        {
            float highAltitudeFactor = Mathf.Clamp01((currentHeight - 2500f) / 500f); // 2500-3000m
            buoyancy *= (1f + highAltitudeFactor * 0.5f); // 最高额外增强50%
        }

        // 下落过快时的浮力补偿，帮助玩家在高速阶段更快刹车
        if (currentVelocity > 20f)
        {
            float speedFactor = Mathf.Clamp01((currentVelocity - 20f) / 20f); // 20-40m/s
            buoyancy *= (1f + speedFactor * 0.25f); // 最高额外25%
        }

        return buoyancy;
    }

    /// <summary>
    /// 计算横截面积（随气球体积变化）
    /// </summary>
    private float CalculateCrossSectionalArea(float volume)
    {
        // 假设气球是球体，横截面积 = π * r²
        // 体积 V = (4/3)πr³，所以 r = ∛(3V/4π)
        // 横截面积 A = πr² = π * (∛(3V/4π))²
        
        if (volume <= 0f) return baseCrossSectionalArea;
        
        float radius = Mathf.Pow(3f * volume / (4f * Mathf.PI), 1f / 3f);
        float area = Mathf.PI * radius * radius;
        
        // 使用基础面积作为最小值
        return Mathf.Max(area, baseCrossSectionalArea);
    }

    /// <summary>
    /// 计算空气阻力
    /// </summary>
    private float CalculateDragForce(float airDensity, float velocity, float crossSectionalArea)
    {
        if (velocity <= 0f) return 0f;
        // 阻力 = 0.5 * 空气密度 * 阻力系数 * 横截面积 * 速度²
        return 0.5f * airDensity * DRAG_COEFFICIENT * crossSectionalArea * velocity * velocity;
    }

    /// <summary>
    /// 计算总质量
    /// </summary>
    private float CalculateTotalMass()
    {
        // 玩家质量 + 氦气质量（氦气也有质量）
        float heliumMass = HELIUM_DENSITY * balloonVolume;
        return PLAYER_MASS + heliumMass;
    }

    /// <summary>
    /// 检查速度警告（改进：只有在浮力不足且速度过快时才警告）
    /// </summary>
    private void CheckVelocityWarnings()
    {
        // 计算当前浮力是否足够
        float airDensity = CalculateAirDensity(currentHeight);
        float buoyancyForce = CalculateBuoyancyForce(airDensity, balloonVolume);
        float totalMass = CalculateTotalMass();
        float netGravityForce = totalMass * GRAVITY;
        
        // 只有在浮力不足且速度过快时才触发危险警告
        if (currentVelocity < 8f)
        {
            OnSafeVelocity?.Invoke();
        }
        else if (currentVelocity > 35f && buoyancyForce < netGravityForce * 0.6f) // 略微提高浮力判定，避免频繁报警
        {
            OnDangerousVelocity?.Invoke();
        }
    }
    #endregion

    #region 调试方法
    private void OnGUI()
    {
        if (!isSimulating) return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 16;
        style.normal.textColor = Color.white;

        float yPos = 10f;
        GUI.Label(new Rect(10, yPos, 500, 20), $"高度: {currentHeight:F2} m", style);
        yPos += 25;
        GUI.Label(new Rect(10, yPos, 500, 20), $"速度: {currentVelocity:F2} m/s", style);
        yPos += 25;
        GUI.Label(new Rect(10, yPos, 500, 20), $"加速度: {currentAcceleration:F2} m/s²", style);
        yPos += 25;
        GUI.Label(new Rect(10, yPos, 500, 20), $"气球体积: {balloonVolume:F2} m³", style);
        yPos += 25;
        GUI.Label(new Rect(10, yPos, 500, 20), $"剩余氦气: {heliumRemaining:F2} m³", style);
        yPos += 25;
        GUI.Label(new Rect(10, yPos, 500, 20), $"模拟时间: {simulationTime:F2} s", style);
        yPos += 25;
        
        // 显示浮力信息
        float airDensity = CalculateAirDensity(currentHeight);
        float buoyancyForce = CalculateBuoyancyForce(airDensity, balloonVolume);
        float totalMass = CalculateTotalMass();
        float netGravityForce = totalMass * GRAVITY;
        float netForce = netGravityForce - buoyancyForce;
        
        GUI.Label(new Rect(10, yPos, 500, 20), $"浮力: {buoyancyForce:F2} N", style);
        yPos += 25;
        GUI.Label(new Rect(10, yPos, 500, 20), $"重力: {netGravityForce:F2} N", style);
        yPos += 25;
        GUI.Label(new Rect(10, yPos, 500, 20), $"净力: {netForce:F2} N", style);
    }
    #endregion
}
