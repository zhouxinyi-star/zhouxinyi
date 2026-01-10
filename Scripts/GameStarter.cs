using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏启动器 - 负责游戏的初始化、重启和退出
/// 应该在场景开始时自动执行初始化逻辑
/// </summary>
public class GameStarter : MonoBehaviour
{
    #region 单例模式（可选，根据项目需求决定）
    
    private static GameStarter _instance;
    
    /// <summary>
    /// 获取 GameStarter 的单例实例（如果需要全局访问）
    /// </summary>
    public static GameStarter Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameStarter>();
            }
            return _instance;
        }
    }
    
    #endregion
    
    #region Unity 生命周期
    
    /// <summary>
    /// 在对象创建时调用，早于 Start()
    /// </summary>
    private void Awake()
    {
        // 如果使用单例模式，确保只有一个实例存在
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        
        // 如果希望在场景切换时保持 GameStarter 存在，可以取消下面的注释
        // DontDestroyOnLoad(gameObject);
    }
    
    /// <summary>
    /// 在对象启用后的第一帧调用，用于初始化游戏
    /// </summary>
    private void Start()
    {
        InitializeGame();
    }
    
    /// <summary>
    /// 当对象被禁用或销毁时调用
    /// </summary>
    private void OnDestroy()
    {
        // 清理单例引用
        if (_instance == this)
        {
            _instance = null;
        }
    }
    
    #endregion
    
    #region 游戏初始化
    
    /// <summary>
    /// 初始化游戏
    /// 设置游戏的初始状态，调用各个管理器的初始化方法
    /// </summary>
    private void InitializeGame()
    {
        // 打印游戏开始信息到控制台
        Debug.Log("游戏开始！");
        
        // 设置游戏初始状态
        SetInitialGameState();
        
        // 初始化各个管理器
        InitializeManagers();
        
        Debug.Log("GameStarter: 游戏初始化完成");
    }
    
    /// <summary>
    /// 设置游戏的初始状态
    /// 例如：时间缩放、帧率限制、游戏状态标志等
    /// </summary>
    private void SetInitialGameState()
    {
        // 设置时间缩放为正常速度（1.0 = 正常，2.0 = 2倍速，0.5 = 慢动作）
        Time.timeScale = 1.0f;
        
        // 设置目标帧率（0 = 无限制，60 = 限制为60帧）
        Application.targetFrameRate = 60;
        
        // 设置屏幕不休眠（移动平台常用）
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        
        // 可以在这里添加其他初始状态设置
        // 例如：游戏状态标志、玩家数据重置等
        
        Debug.Log("GameStarter: 游戏初始状态已设置");
    }
    
    /// <summary>
    /// 初始化各个管理器
    /// 调用项目中各个管理器的初始化方法
    /// </summary>
    private void InitializeManagers()
    {
        // 初始化物理管理器（如果存在）
        if (PhysicsManager.Instance != null)
        {
            // 确保物理模拟从 10000m 开始运行
            PhysicsManager.Instance.StartSimulation();
            Debug.Log("GameStarter: PhysicsManager 已初始化并开始物理模拟");
        }
        
        // 在这里添加其他管理器的初始化
        // 例如：
        // - AudioManager
        // - UIManager
        // - GameManager
        // - SceneManager（Unity内置）
        // 等等...
        
        Debug.Log("GameStarter: 所有管理器初始化完成");
    }
    
    #endregion
    
    #region 公共方法
    
    /// <summary>
    /// 重新开始游戏
    /// 静态方法，可以从任何地方调用，例如：GameStarter.RestartGame()
    /// </summary>
    public static void RestartGame()
    {
        // 方法1：重新加载当前场景（推荐用于单场景游戏）
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
        
        // 方法2：如果需要重置时间缩放（可选）
        // Time.timeScale = 1.0f;
        
        Debug.Log($"GameStarter: 重新加载场景 - {currentScene.name}");
    }
    
    /// <summary>
    /// 退出游戏
    /// 静态方法，可以从任何地方调用，例如：GameStarter.QuitGame()
    /// 注意：在 Unity Editor 中不会真正退出，只在构建后的应用程序中生效
    /// </summary>
    public static void QuitGame()
    {
        Debug.Log("GameStarter: 退出游戏");
        
        // 在 Unity Editor 中运行时
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        // 在构建后的应用程序中运行时
        Application.Quit();
        #endif
    }
    
    /// <summary>
    /// 重新开始游戏（实例方法版本）
    /// 如果希望通过实例调用，可以使用此方法
    /// </summary>
    public void Restart()
    {
        RestartGame();
    }
    
    /// <summary>
    /// 退出游戏（实例方法版本）
    /// 如果希望通过实例调用，可以使用此方法
    /// </summary>
    public void Quit()
    {
        QuitGame();
    }
    
    #endregion
    
    #region 辅助方法（可选）
    
    /// <summary>
    /// 暂停游戏
    /// 将时间缩放设置为 0，暂停所有时间相关的更新
    /// </summary>
    public void PauseGame()
    {
        Time.timeScale = 0f;
        Debug.Log("GameStarter: 游戏已暂停");
    }
    
    /// <summary>
    /// 恢复游戏
    /// 将时间缩放恢复为 1，恢复正常的时间更新
    /// </summary>
    public void ResumeGame()
    {
        Time.timeScale = 1f;
        Debug.Log("GameStarter: 游戏已恢复");
    }
    
    /// <summary>
    /// 切换游戏暂停状态
    /// </summary>
    public void TogglePause()
    {
        if (Time.timeScale > 0f)
        {
            PauseGame();
        }
        else
        {
            ResumeGame();
        }
    }
    
    #endregion
}