using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏开始界面管理器 - 处理主菜单的显示和交互
/// </summary>
public class GameStartMenu : MonoBehaviour
{
    #region UI元素引用
    [Header("开始界面元素")]
    [SerializeField] private GameObject startMenuPanel;      // 开始菜单面板
    [SerializeField] private TextMeshProUGUI titleText;      // 游戏标题
    [SerializeField] private Button startButton;             // 开始游戏按钮
    [SerializeField] private Button quitButton;              // 退出游戏按钮
    
    [Header("可选元素")]
    [SerializeField] private TextMeshProUGUI instructionText; // 说明文本
    [SerializeField] private GameObject loadingPanel;         // 加载面板（可选）
    [SerializeField] private string nextSceneName = "";        // 要切换的场景名（留空则用Build索引+1）
    #endregion

    #region 私有变量
    private bool gameStarted = false;
    #endregion

    #region Unity生命周期
    private void Awake()
    {
        LogMissingReferences();
        // 初始化：显示开始菜单
        ShowStartMenu();
    }

    private void Start()
    {
        LogMissingReferences();
        // 设置按钮事件
        SetupButtons();
        
        // 暂停游戏，直到玩家点击开始
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        // 如果游戏未开始，按ESC可以退出
        if (!gameStarted && Input.GetKeyDown(KeyCode.Escape))
        {
            QuitGame();
        }
    }
    #endregion

    #region 按钮设置
    /// <summary>
    /// 设置按钮事件
    /// </summary>
    private void SetupButtons()
    {
        // 开始游戏按钮
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartGame);
        }
        else
        {
            Debug.LogWarning("GameStartMenu: Start Button 未指定，无法点击开始。");
        }

        // 退出游戏按钮
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }
        else
        {
            Debug.LogWarning("GameStartMenu: Quit Button 未指定，无法退出游戏。");
        }
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 显示开始菜单
    /// </summary>
    public void ShowStartMenu()
    {
        if (startMenuPanel != null)
        {
            startMenuPanel.SetActive(true);
        }
        
        gameStarted = false;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// 隐藏开始菜单
    /// </summary>
    public void HideStartMenu()
    {
        if (startMenuPanel != null)
        {
            startMenuPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 开始游戏
    /// </summary>
    public void StartGame()
    {
        if (gameStarted) return;

        gameStarted = true;
        
        // 隐藏开始菜单
        HideStartMenu();
        
        // 恢复游戏时间
        Time.timeScale = 1f;
        
        // 锁定鼠标（第一人称游戏）
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // 初始化游戏系统
        InitializeGame();

        // 切换到游戏场景
        LoadNextScene();
        
        Debug.Log("游戏开始！");
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("退出游戏");
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    /// <summary>
    /// 初始化游戏系统
    /// </summary>
    private void InitializeGame()
    {
        // 确保GameStarter初始化
        if (GameStarter.Instance != null)
        {
            // GameStarter会在Start中自动初始化
        }
        
        // 确保PhysicsManager开始模拟
        if (PhysicsManager.Instance != null)
        {
            PhysicsManager.Instance.StartSimulation();
        }
    }

    /// <summary>
    /// 切换到指定或下一个场景
    /// </summary>
    private void LoadNextScene()
    {
        if (!string.IsNullOrWhiteSpace(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName.Trim());
            return;
        }

        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;

        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextIndex);
        }
        else
        {
            Debug.LogWarning("未找到下一个场景，请在Build Settings添加或填写场景名。");
        }
    }

    /// <summary>
    /// 打印未配置的引用，帮助排查
    /// </summary>
    private void LogMissingReferences()
    {
        if (startMenuPanel == null) Debug.LogWarning("GameStartMenu: Start Menu Panel 未指定。");
        if (titleText == null) Debug.LogWarning("GameStartMenu: Title Text 未指定。");
        if (startButton == null) Debug.LogWarning("GameStartMenu: Start Button 未指定。");
        if (quitButton == null) Debug.LogWarning("GameStartMenu: Quit Button 未指定。");
        if (instructionText == null) Debug.LogWarning("GameStartMenu: Instruction Text 未指定（可选）。");
        if (loadingPanel == null) Debug.LogWarning("GameStartMenu: Loading Panel 未指定（可选）。");
    }
    #endregion
}

