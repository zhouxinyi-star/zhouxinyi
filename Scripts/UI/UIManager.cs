using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

/// <summary>
/// UI管理器 - 负责更新游戏界面显示，响应游戏事件
/// </summary>
public class UIManager : MonoBehaviour
{
    #region UI元素引用
    [Header("信息显示文本")]
    [SerializeField] private TextMeshProUGUI heightText;              // 高度显示
    [SerializeField] private TextMeshProUGUI velocityText;           // 速度显示
    [SerializeField] private TextMeshProUGUI accelerationText;        // 加速度显示
    [SerializeField] private TextMeshProUGUI volumeText;              // 气球体积显示
    [SerializeField] private TextMeshProUGUI heliumText;             // 氦气剩余量显示
    [SerializeField] private TextMeshProUGUI timeText;                // 模拟时间显示
    
    [Header("警告和提示")]
    [SerializeField] private GameObject warningPanel;     // 警告面板
    [SerializeField] private TextMeshProUGUI warningText;            // 警告文本
    [SerializeField] private Image warningBackground;     // 警告背景（用于闪烁效果）
    
    [Header("操作提示")]
    [SerializeField] private GameObject controlsPanel;    // 操作提示面板
    [SerializeField] private TextMeshProUGUI controlsText;           // 操作提示文本
    
    [Header("暂停菜单")]
    [SerializeField] private GameObject pauseMenu;         // 暂停菜单面板
    [SerializeField] private Button resumeButton;          // 继续游戏按钮
    [SerializeField] private Button restartButton;         // 重新开始按钮
    [SerializeField] private Button quitButton;           // 退出游戏按钮
    
    [Header("游戏结束界面")]
    [SerializeField] private GameObject gameOverPanel;    // 游戏结束面板
    [SerializeField] private TextMeshProUGUI gameOverTitle;           // 游戏结束标题
    [SerializeField] private TextMeshProUGUI landingSpeedText;        // 着陆速度显示
    [SerializeField] private Button restartGameButton;    // 重新开始按钮
    [SerializeField] private Button quitGameButton;        // 退出游戏按钮
    
    [Header("速度指示器")]
    [SerializeField] private Image speedIndicator;        // 速度指示器（颜色变化）
    [SerializeField] private Color safeColor = Color.green;      // 安全速度颜色（<6m/s）
    [SerializeField] private Color warningColor = Color.yellow;  // 警告速度颜色（6-40m/s）
    [SerializeField] private Color dangerColor = Color.red;      // 危险速度颜色（>40m/s）
    #endregion

    #region 私有变量
    private float warningDisplayTime = 3f;  // 警告显示时间
    private Coroutine warningCoroutine;     // 警告协程
    private bool isPaused = false;           // 是否暂停
    #endregion

    #region Unity生命周期
    private void OnEnable()
    {
        // 最早执行：确保UI状态正确
        InitializeUIState();
    }

    private void Awake()
    {
        // 初始化UI状态
        InitializeUIState();
    }

    private void Start()
    {
        // 三重保险：再次确保UI状态正确
        InitializeUIState();
        
        // 延迟一帧再次确保隐藏（处理UI初始化顺序问题）
        StartCoroutine(DelayedHidePanels());
        
        // 订阅事件
        SubscribeToEvents();
        
        // 设置按钮事件
        SetupButtons();
        
        // 初始化操作提示文本
        UpdateControlsText();
    }

    /// <summary>
    /// 延迟隐藏面板（确保UI完全初始化后）
    /// </summary>
    private IEnumerator DelayedHidePanels()
    {
        yield return null; // 等待一帧
        
        if (pauseMenu != null && pauseMenu.activeSelf)
        {
            pauseMenu.SetActive(false);
            Debug.Log("UIManager: 延迟隐藏 PauseMenu");
        }
        
        if (gameOverPanel != null && gameOverPanel.activeSelf)
        {
            gameOverPanel.SetActive(false);
            Debug.Log("UIManager: 延迟隐藏 GameOverPanel");
        }
    }

    /// <summary>
    /// 初始化UI状态，确保面板正确显示/隐藏
    /// </summary>
    private void InitializeUIState()
    {
        // 如果引用为空，尝试自动查找
        if (pauseMenu == null)
        {
            pauseMenu = GameObject.Find("PauseMenu");
            if (pauseMenu == null)
            {
                // 尝试在Canvas下查找
                Canvas canvas = FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    Transform pauseTransform = canvas.transform.Find("PauseMenu");
                    if (pauseTransform != null)
                        pauseMenu = pauseTransform.gameObject;
                }
            }
        }

        if (gameOverPanel == null)
        {
            gameOverPanel = GameObject.Find("GameOverPanel");
            if (gameOverPanel == null)
            {
                Canvas canvas = FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    Transform gameOverTransform = canvas.transform.Find("GameOverPanel");
                    if (gameOverTransform != null)
                        gameOverPanel = gameOverTransform.gameObject;
                }
            }
        }

        // 强制隐藏暂停菜单和游戏结束界面（无论编辑器中的状态如何）
        if (pauseMenu != null)
        {
            // 先激活面板，确保所有子对象可见
            pauseMenu.SetActive(true);
            // 然后立即隐藏整个面板（这会隐藏所有子对象）
            pauseMenu.SetActive(false);
            Debug.Log("UIManager: PauseMenu 已隐藏");
        }
        else
        {
            Debug.LogWarning("UIManager: 未找到 PauseMenu，请检查引用设置");
        }

        if (gameOverPanel != null)
        {
            // 先激活面板，确保所有子对象可见
            gameOverPanel.SetActive(true);
            // 然后立即隐藏整个面板（这会隐藏所有子对象）
            gameOverPanel.SetActive(false);
            Debug.Log("UIManager: GameOverPanel 已隐藏");
        }
        else
        {
            Debug.LogWarning("UIManager: 未找到 GameOverPanel，请检查引用设置");
        }

        // 警告面板初始隐藏
        if (warningPanel != null)
            warningPanel.SetActive(false);
        
        // 操作提示面板初始显示
        if (controlsPanel != null)
            controlsPanel.SetActive(true);
    }

    private void Update()
    {
        // 更新UI显示
        UpdateUI();
        
        // 处理ESC键暂停
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu();
        }
    }

    private void OnDestroy()
    {
        // 取消订阅事件
        UnsubscribeFromEvents();
    }
    #endregion

    #region 事件订阅
    /// <summary>
    /// 订阅游戏事件
    /// </summary>
    private void SubscribeToEvents()
    {
        if (EventManager.Instance != null)
        {
            // 订阅警告事件
            EventManager.Instance.AddListener(EventManager.EVENT_WARNING_TRIGGERED, OnWarningTriggered);
            
            // 订阅游戏结束事件
            EventManager.Instance.AddListener(EventManager.EVENT_GAME_OVER, OnGameOver);
            
            // 订阅暂停事件
            EventManager.Instance.AddListener("game_paused", OnGamePaused);
        }

        // 订阅物理管理器事件
        if (PhysicsManager.Instance != null)
        {
            PhysicsManager.Instance.OnSafeVelocity += OnSafeVelocity;
            PhysicsManager.Instance.OnDangerousVelocity += OnDangerousVelocity;
            PhysicsManager.Instance.OnHeliumDepleted += OnHeliumDepleted;
        }
    }

    /// <summary>
    /// 取消订阅游戏事件
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RemoveListener(EventManager.EVENT_WARNING_TRIGGERED, OnWarningTriggered);
            EventManager.Instance.RemoveListener(EventManager.EVENT_GAME_OVER, OnGameOver);
            EventManager.Instance.RemoveListener("game_paused", OnGamePaused);
        }

        if (PhysicsManager.Instance != null)
        {
            PhysicsManager.Instance.OnSafeVelocity -= OnSafeVelocity;
            PhysicsManager.Instance.OnDangerousVelocity -= OnDangerousVelocity;
            PhysicsManager.Instance.OnHeliumDepleted -= OnHeliumDepleted;
        }
    }
    #endregion

    #region UI更新
    /// <summary>
    /// 更新UI显示
    /// </summary>
    private void UpdateUI()
    {
        if (PhysicsManager.Instance == null) return;

        // 更新高度显示
        if (heightText != null)
        {
            float height = PhysicsManager.Instance.CurrentHeight;
            heightText.text = $"高度: {height:F0} m";
        }

        // 更新速度显示
        if (velocityText != null)
        {
            float velocity = PhysicsManager.Instance.CurrentVelocity;
            velocityText.text = $"速度: {velocity:F1} m/s";
            
            // 更新速度指示器颜色
            UpdateSpeedIndicator(velocity);
        }

        // 更新加速度显示
        if (accelerationText != null)
        {
            float acceleration = PhysicsManager.Instance.CurrentAcceleration;
            accelerationText.text = $"加速度: {acceleration:F2} m/s²";
        }

        // 更新气球体积显示
        if (volumeText != null)
        {
            float volume = PhysicsManager.Instance.BalloonVolume;
            volumeText.text = $"气球体积: {volume:F2} m³";
        }

        // 更新氦气剩余量显示
        if (heliumText != null)
        {
            float heliumRemaining = PhysicsManager.Instance.HeliumRemaining;
            float maxCapacity = PhysicsManager.Instance.MaxHeliumCapacity;
            float percentage = (heliumRemaining / maxCapacity) * 100f;
            
            heliumText.text = $"氦气: {heliumRemaining:F2} m³ ({percentage:F0}%)";
            
            // 根据剩余量改变颜色
            if (percentage <= 20f)
            {
                heliumText.color = Color.red;  // 危险：剩余20%以下
            }
            else if (percentage <= 50f)
            {
                heliumText.color = Color.yellow;  // 警告：剩余50%以下
            }
            else
            {
                heliumText.color = Color.white;  // 正常
            }
        }

        // 更新模拟时间显示
        if (timeText != null)
        {
            float time = PhysicsManager.Instance.SimulationTime;
            int minutes = Mathf.FloorToInt(time / 60);
            int seconds = Mathf.FloorToInt(time % 60);
            timeText.text = $"时间: {minutes:D2}:{seconds:D2}";
        }
    }

    /// <summary>
    /// 更新速度指示器颜色
    /// </summary>
    private void UpdateSpeedIndicator(float velocity)
    {
        if (speedIndicator == null) return;

        if (velocity < 6f)
        {
            // 安全速度（绿色）
            speedIndicator.color = safeColor;
        }
        else if (velocity < 40f)
        {
            // 警告速度（黄色）
            speedIndicator.color = warningColor;
        }
        else
        {
            // 危险速度（红色）
            speedIndicator.color = dangerColor;
        }
    }

    /// <summary>
    /// 更新操作提示文本
    /// </summary>
    private void UpdateControlsText()
    {
        if (controlsText == null) return;

        controlsText.text = 
            "操作提示:\n" +
            "空格键 - 充气（按住）\n" +
            "Shift键 - 放气（按住）\n" +
            "鼠标右键 - 紧急放气10%\n" +
            "ESC - 暂停/继续\n" +
            "V键 - 切换视角";
    }
    #endregion

    #region 事件响应
    /// <summary>
    /// 警告事件响应
    /// </summary>
    private void OnWarningTriggered(object[] args)
    {
        string message = "警告！";
        if (args != null && args.Length > 0 && args[0] is string)
        {
            message = (string)args[0];
        }
        ShowWarning(message);
    }

    /// <summary>
    /// 游戏结束事件响应
    /// </summary>
    private void OnGameOver(object[] args)
    {
        float landingSpeed = 0f;
        if (args != null && args.Length > 0 && args[0] is float)
        {
            landingSpeed = (float)args[0];
        }

        ShowGameOver(landingSpeed);
    }

    /// <summary>
    /// 游戏暂停事件响应
    /// </summary>
    private void OnGamePaused(object[] args)
    {
        if (args != null && args.Length > 0 && args[0] is bool)
        {
            isPaused = (bool)args[0];
        }
    }

    /// <summary>
    /// 安全速度事件响应
    /// </summary>
    private void OnSafeVelocity()
    {
        // 可以在这里添加安全速度的视觉反馈
        // 例如：显示"安全速度"提示
    }

    /// <summary>
    /// 危险速度事件响应
    /// </summary>
    private void OnDangerousVelocity()
    {
        ShowWarning("⚠️ 速度过快！危险！");
    }

    /// <summary>
    /// 氦气耗尽事件响应
    /// </summary>
    private void OnHeliumDepleted()
    {
        ShowWarning("⚠️ 氦气罐已用完！");
    }
    #endregion

    #region UI显示方法
    /// <summary>
    /// 显示警告信息
    /// </summary>
    public void ShowWarning(string message)
    {
        if (warningPanel == null || warningText == null) return;

        // 停止之前的警告协程
        if (warningCoroutine != null)
        {
            StopCoroutine(warningCoroutine);
        }

        // 显示警告
        warningPanel.SetActive(true);
        warningText.text = message;

        // 启动闪烁效果
        warningCoroutine = StartCoroutine(FlashWarning());
    }

    /// <summary>
    /// 隐藏警告
    /// </summary>
    private void HideWarning()
    {
        if (warningPanel != null)
        {
            warningPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 警告闪烁效果
    /// </summary>
    private IEnumerator FlashWarning()
    {
        if (warningBackground == null) yield break;

        float elapsedTime = 0f;

        while (elapsedTime < warningDisplayTime)
        {
            // 闪烁效果：在红色和半透明之间切换
            Color color = warningBackground.color;
            color.a = Mathf.PingPong(elapsedTime * 5f, 1f) * 0.5f + 0.5f;
            warningBackground.color = color;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 恢复透明度
        Color finalColor = warningBackground.color;
        finalColor.a = 0.5f;
        warningBackground.color = finalColor;

        // 隐藏警告
        HideWarning();
    }

    /// <summary>
    /// 显示游戏结束界面
    /// </summary>
    public void ShowGameOver(float landingSpeed)
    {
        if (gameOverPanel == null) return;

        gameOverPanel.SetActive(true);

        // 判断是否安全着陆
        bool isSafe = landingSpeed < 6f;
        
        if (gameOverTitle != null)
        {
            gameOverTitle.text = isSafe ? "安全着陆！" : "着陆失败！";
            gameOverTitle.color = isSafe ? Color.green : Color.red;
        }

        if (landingSpeedText != null)
        {
            landingSpeedText.text = $"着陆速度: {landingSpeed:F2} m/s";
            
            // 根据速度设置颜色
            if (landingSpeed < 6f)
            {
                landingSpeedText.color = Color.green;
            }
            else if (landingSpeed < 40f)
            {
                landingSpeedText.color = Color.yellow;
            }
            else
            {
                landingSpeedText.color = Color.red;
            }
        }
    }

    /// <summary>
    /// 切换暂停菜单
    /// </summary>
    public void TogglePauseMenu()
    {
        if (pauseMenu == null) return;

        isPaused = !isPaused;
        pauseMenu.SetActive(isPaused);

        // 暂停时显示鼠标，继续时隐藏
        if (isPaused)
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            // 确保按钮可交互
            EnsurePauseButtonsInteractable();
        }
        else
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        // 同步 PlayerController 的暂停状态
        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            playerController.SetPaused(isPaused);
        }
    }
    
    /// <summary>
    /// 确保暂停菜单按钮可交互
    /// </summary>
    private void EnsurePauseButtonsInteractable()
    {
        if (resumeButton != null)
        {
            resumeButton.interactable = true;
        }
        if (restartButton != null)
        {
            restartButton.interactable = true;
        }
        if (quitButton != null)
        {
            quitButton.interactable = true;
        }
        
        // 确保 EventSystem 可用
        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem != null && !eventSystem.enabled)
        {
            eventSystem.enabled = true;
        }
        
        // 确保 Canvas 的 GraphicRaycaster 可用
        Canvas canvas = pauseMenu.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null && !raycaster.enabled)
            {
                raycaster.enabled = true;
            }
        }
    }
    #endregion

    #region 按钮设置
    /// <summary>
    /// 设置按钮事件
    /// </summary>
    private void SetupButtons()
    {
        // 继续游戏按钮
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(() => {
                Debug.Log("UIManager: 继续游戏按钮被点击");
                TogglePauseMenu();
                // 通过EventManager触发恢复游戏事件
                if (EventManager.Instance != null)
                {
                    EventManager.Instance.TriggerEvent("game_paused", false);
                }
            });
        }

        // 重新开始按钮
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(() => {
                Debug.Log("UIManager: 重新开始按钮被点击");
                Time.timeScale = 1f;  // 恢复时间缩放
                GameStarter.RestartGame();
            });
        }

        // 退出游戏按钮
        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(() => {
                Debug.Log("UIManager: 退出游戏按钮被点击");
                Time.timeScale = 1f;  // 恢复时间缩放
                GameStarter.QuitGame();
            });
        }

        // 游戏结束界面的重新开始按钮
        if (restartGameButton != null)
        {
            restartGameButton.onClick.AddListener(() => {
                GameStarter.RestartGame();
            });
        }

        // 游戏结束界面的退出按钮
        if (quitGameButton != null)
        {
            quitGameButton.onClick.AddListener(() => {
                GameStarter.QuitGame();
            });
        }
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 隐藏操作提示
    /// </summary>
    public void HideControls()
    {
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 显示操作提示
    /// </summary>
    public void ShowControls()
    {
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(true);
        }
    }
    #endregion
}

