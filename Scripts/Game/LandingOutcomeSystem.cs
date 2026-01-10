using UnityEngine;

/// <summary>
/// 着陆结局判断系统 - 根据着陆速度判断游戏结局
/// 支持三种结局：安全降落、受伤降落、坠毁
/// </summary>
public class LandingOutcomeSystem : MonoBehaviour
{
    #region 结局类型枚举
    /// <summary>
    /// 着陆结局类型
    /// </summary>
    public enum LandingOutcome
    {
        SafeLanding,      // 安全降落 (<5 m/s)
        InjuredLanding,  // 受伤降落 (5-15 m/s)
        Crashed          // 坠毁 (>15 m/s)
    }
    #endregion

    #region 配置参数
    [Header("速度阈值配置")]
    [Tooltip("安全降落的最大速度 (m/s)")]
    [SerializeField] private float safeLandingSpeed = 5f;
    
    [Tooltip("受伤降落的最大速度 (m/s)，超过此速度则为坠毁")]
    [SerializeField] private float injuredLandingSpeed = 15f;
    
    [Header("气球爆炸检测")]
    [Tooltip("是否启用气球爆炸检测")]
    [SerializeField] private bool checkBalloonExploded = true;
    
    [Tooltip("气球爆炸时的最小体积（如果体积小于此值，视为爆炸）")]
    [SerializeField] private float explodedBalloonVolume = 0.2f;
    #endregion

    #region 状态变量
    private bool hasProcessedLanding = false;  // 是否已处理着陆
    private LandingOutcome currentOutcome;     // 当前结局
    private float landingVelocity;              // 着陆速度
    private bool balloonExploded;               // 气球是否爆炸
    #endregion

    #region 事件定义
    /// <summary>
    /// 着陆结局事件 - 参数：结局类型、着陆速度、气球是否爆炸
    /// </summary>
    public System.Action<LandingOutcome, float, bool> OnLandingOutcome;
    #endregion

    #region Unity生命周期
    private void Awake()
    {
        // 确保初始状态
        hasProcessedLanding = false;
    }

    private void OnEnable()
    {
        // 延迟订阅，确保 PhysicsManager 已初始化
        StartCoroutine(SubscribeToPhysicsManager());
    }
    
    private System.Collections.IEnumerator SubscribeToPhysicsManager()
    {
        // 等待 PhysicsManager 初始化
        while (PhysicsManager.Instance == null)
        {
            yield return null;
        }
        
        // 订阅物理管理器事件
        PhysicsManager.Instance.OnLanding += HandleLanding;
        Debug.Log("LandingOutcomeSystem: 已订阅 PhysicsManager.OnLanding 事件");
    }

    private void OnDisable()
    {
        // 取消订阅
        if (PhysicsManager.Instance != null)
        {
            PhysicsManager.Instance.OnLanding -= HandleLanding;
        }
    }
    #endregion

    #region 着陆处理
    /// <summary>
    /// 处理着陆事件
    /// </summary>
    /// <param name="velocity">着陆速度 (m/s)</param>
    private void HandleLanding(float velocity)
    {
        Debug.Log($"LandingOutcomeSystem: 收到着陆事件，速度: {velocity:F2} m/s");
        
        // 防止重复处理
        if (hasProcessedLanding)
        {
            Debug.LogWarning("LandingOutcomeSystem: 着陆事件已被处理，忽略重复调用");
            return;
        }

        hasProcessedLanding = true;
        landingVelocity = velocity;

        // 检查气球是否爆炸
        balloonExploded = CheckBalloonExploded();

        // 判断结局类型
        currentOutcome = DetermineOutcome(velocity, balloonExploded);

        // 输出日志
        LogOutcome(currentOutcome, landingVelocity, balloonExploded);

        // 触发事件
        if (OnLandingOutcome != null)
        {
            Debug.Log($"LandingOutcomeSystem: 触发 OnLandingOutcome 事件，结局: {currentOutcome}");
            OnLandingOutcome.Invoke(currentOutcome, landingVelocity, balloonExploded);
        }
        else
        {
            Debug.LogWarning("LandingOutcomeSystem: OnLandingOutcome 事件没有订阅者！请检查 LandingOutcomeUI 是否正确订阅。");
        }

        // 触发EventManager事件
        if (EventManager.Instance != null)
        {
            string outcomeEventName = GetOutcomeEventName(currentOutcome);
            EventManager.Instance.TriggerEvent(outcomeEventName, landingVelocity, balloonExploded);
        }
    }

    /// <summary>
    /// 判断结局类型
    /// </summary>
    /// <param name="velocity">着陆速度</param>
    /// <param name="exploded">气球是否爆炸</param>
    /// <returns>结局类型</returns>
    private LandingOutcome DetermineOutcome(float velocity, bool exploded)
    {
        // 如果气球爆炸，直接判定为坠毁
        if (exploded)
        {
            return LandingOutcome.Crashed;
        }

        // 根据速度判断
        if (velocity < safeLandingSpeed)
        {
            return LandingOutcome.SafeLanding;
        }
        else if (velocity < injuredLandingSpeed)
        {
            return LandingOutcome.InjuredLanding;
        }
        else
        {
            return LandingOutcome.Crashed;
        }
    }

    /// <summary>
    /// 检查气球是否爆炸
    /// </summary>
    /// <returns>是否爆炸</returns>
    private bool CheckBalloonExploded()
    {
        if (!checkBalloonExploded) return false;

        if (PhysicsManager.Instance != null)
        {
            float currentVolume = PhysicsManager.Instance.BalloonVolume;
            return currentVolume < explodedBalloonVolume;
        }

        return false;
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 手动触发着陆判断（用于测试或特殊情况）
    /// </summary>
    /// <param name="velocity">着陆速度</param>
    public void ProcessLanding(float velocity)
    {
        hasProcessedLanding = false;  // 重置状态
        HandleLanding(velocity);
    }

    /// <summary>
    /// 重置系统状态（用于重新开始游戏）
    /// </summary>
    public void Reset()
    {
        hasProcessedLanding = false;
        currentOutcome = LandingOutcome.SafeLanding;
        landingVelocity = 0f;
        balloonExploded = false;
    }

    /// <summary>
    /// 获取当前结局
    /// </summary>
    public LandingOutcome GetCurrentOutcome()
    {
        return currentOutcome;
    }

    /// <summary>
    /// 获取着陆速度
    /// </summary>
    public float GetLandingVelocity()
    {
        return landingVelocity;
    }

    /// <summary>
    /// 获取气球是否爆炸
    /// </summary>
    public bool IsBalloonExploded()
    {
        return balloonExploded;
    }
    #endregion

    #region 辅助方法
    /// <summary>
    /// 获取结局对应的事件名称
    /// </summary>
    private string GetOutcomeEventName(LandingOutcome outcome)
    {
        switch (outcome)
        {
            case LandingOutcome.SafeLanding:
                return "landing_safe";
            case LandingOutcome.InjuredLanding:
                return "landing_injured";
            case LandingOutcome.Crashed:
                return "landing_crashed";
            default:
                return "landing_unknown";
        }
    }

    /// <summary>
    /// 输出结局日志
    /// </summary>
    private void LogOutcome(LandingOutcome outcome, float velocity, bool exploded)
    {
        string outcomeText = GetOutcomeText(outcome);
        string explodedText = exploded ? "（气球已爆炸）" : "";
        
        Debug.Log($"=== 着陆结局判断 ===");
        Debug.Log($"结局类型: {outcomeText}");
        Debug.Log($"着陆速度: {velocity:F2} m/s");
        Debug.Log($"气球状态: {(exploded ? "爆炸" : "完好")}");
        Debug.Log($"==================");
    }

    /// <summary>
    /// 获取结局文本描述
    /// </summary>
    private string GetOutcomeText(LandingOutcome outcome)
    {
        switch (outcome)
        {
            case LandingOutcome.SafeLanding:
                return "安全降落 ✓";
            case LandingOutcome.InjuredLanding:
                return "受伤降落 ⚠";
            case LandingOutcome.Crashed:
                return "坠毁 ✗";
            default:
                return "未知";
        }
    }
    #endregion
}

