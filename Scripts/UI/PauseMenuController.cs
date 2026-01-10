using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// 简易暂停菜单：按下 ESC 显示/隐藏，提供重新开始与退出游戏。
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [Header("面板与按钮")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Button resumeButton;      // 继续游戏按钮
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;

    [Header("按键设置")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
    [SerializeField] private bool resumeWithPauseKey = false; // 是否允许再次按键恢复游戏

    [Header("可选引用")]
    [SerializeField] private PlayerController playerController; // 同步玩家暂停状态
    private bool isPaused;
    private EventSystem eventSystem;
    private CanvasGroup canvasGroup;  // Canvas组，用于控制交互性

    private void Awake()
    {
        // 确保初始状态为未暂停
        isPaused = false;

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);

        // 自动寻找 PlayerController（可选）
        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
        }
        
        // 检查并确保 EventSystem 存在
        eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogError("PauseMenu: 未找到 EventSystem！UI 按钮将无法工作。");
        }
        else
        {
            Debug.Log($"PauseMenu: 找到 EventSystem: {eventSystem.gameObject.name}");
        }
        
        // 获取或添加 CanvasGroup（用于控制交互性）
        if (pauseMenuPanel != null)
        {
            canvasGroup = pauseMenuPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = pauseMenuPanel.AddComponent<CanvasGroup>();
            }
        }

        // 设置继续游戏按钮
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(ResumeGame);
            Debug.Log("✅ PauseMenu: Resume Button 已设置");
        }
        else
        {
            Debug.LogWarning("⚠️ PauseMenu: Resume Button 未指定。");
        }

        // 设置重新开始按钮
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartScene);
            Debug.Log("✅ PauseMenu: Restart Button 已设置");
        }
        else
        {
            Debug.LogWarning("⚠️ PauseMenu: Restart Button 未指定。");
        }

        // 设置退出游戏按钮
        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitGame);
            Debug.Log("✅ PauseMenu: Quit Button 已设置");
        }
        else
        {
            Debug.LogWarning("⚠️ PauseMenu: Quit Button 未指定。");
        }
    }

    private void Update()
    {
        // 使用 unscaledTime 来检测输入，即使 timeScale = 0 也能工作
        // 但 GetKeyDown 本身不受 timeScale 影响，所以这里保持原样
        if (Input.GetKeyDown(pauseKey))
        {
            Debug.Log("PauseMenu: 检测到暂停按键");
            if (isPaused)
            {
                if (resumeWithPauseKey)
                {
                    ResumeGame();
                }
            }
            else
            {
                PauseGame();
            }
        }
    }

    private void PauseGame()
    {
        isPaused = true;
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
            
            // 确保 CanvasGroup 允许交互
            if (canvasGroup != null)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.alpha = 1f;
            }
        }
        
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (playerController != null) playerController.SetPaused(true);
        
        // 使用协程延迟执行，确保UI完全初始化后再设置按钮状态
        StartCoroutine(DelayedButtonSetup());
        
        Debug.Log("PauseMenu: 已暂停并显示面板");
    }
    
    /// <summary>
    /// 延迟设置按钮状态（确保UI完全初始化）
    /// </summary>
    private System.Collections.IEnumerator DelayedButtonSetup()
    {
        // 等待一帧，确保UI完全渲染
        yield return null;
        
        // 确保 EventSystem 在暂停时仍然可用
        if (eventSystem == null)
        {
            eventSystem = FindObjectOfType<EventSystem>();
        }
        if (eventSystem != null)
        {
            if (!eventSystem.enabled)
            {
                eventSystem.enabled = true;
                Debug.LogWarning("⚠️ PauseMenu: EventSystem 被禁用，已重新启用");
            }
            
            // 强制更新 EventSystem（确保它能处理输入）
            eventSystem.UpdateModules();
        }
        else
        {
            Debug.LogError("❌ PauseMenu: 未找到 EventSystem！");
        }
        
        // 修复背景Image可能阻挡按钮的问题
        FixBackgroundRaycast();
        
        // 确保所有按钮都是可交互的
        EnsureButtonsInteractable();
        
        // 诊断信息
        Debug.Log($"✅ PauseMenu: EventSystem 状态 - 存在: {eventSystem != null}, 启用: {eventSystem != null && eventSystem.enabled}");
        Debug.Log($"✅ PauseMenu: Resume Button - 存在: {resumeButton != null}, 可交互: {resumeButton != null && resumeButton.interactable}");
        Debug.Log($"✅ PauseMenu: Restart Button - 存在: {restartButton != null}, 可交互: {restartButton != null && restartButton.interactable}");
        Debug.Log($"✅ PauseMenu: Quit Button - 存在: {quitButton != null}, 可交互: {quitButton != null && quitButton.interactable}");
    }
    
    /// <summary>
    /// 修复背景Image可能阻挡按钮点击的问题
    /// </summary>
    private void FixBackgroundRaycast()
    {
        if (pauseMenuPanel == null) return;
        
        // 检查PauseMenu面板本身的Image组件
        Image panelImage = pauseMenuPanel.GetComponent<Image>();
        if (panelImage != null && panelImage.raycastTarget)
        {
            // 背景面板不应该阻挡按钮点击，禁用其raycastTarget
            panelImage.raycastTarget = false;
            Debug.Log("✅ 已禁用 PauseMenu 背景的 Raycast Target（避免阻挡按钮点击）");
        }
        
        // 检查是否有其他背景元素阻挡按钮
        Image[] allImages = pauseMenuPanel.GetComponentsInChildren<Image>(true);
        foreach (Image img in allImages)
        {
            // 如果是背景类型的Image（不是按钮的Image），禁用raycastTarget
            if (img.raycastTarget && img.GetComponent<Button>() == null)
            {
                // 检查是否是背景（通过名称或是否是面板本身）
                if (img.name.Contains("Background") || img.name.Contains("Panel") || img.gameObject == pauseMenuPanel)
                {
                    img.raycastTarget = false;
                    Debug.Log($"✅ 已禁用背景 {img.name} 的 Raycast Target");
                }
            }
        }
    }
    
    /// <summary>
    /// 确保所有按钮都是可交互的
    /// </summary>
    private void EnsureButtonsInteractable()
    {
        // 确保继续游戏按钮可交互
        if (resumeButton != null)
        {
            resumeButton.interactable = true;
            // 确保按钮的 Image 组件允许射线检测
            Image img = resumeButton.GetComponent<Image>();
            if (img != null)
            {
                img.raycastTarget = true;
            }
            Debug.Log($"✅ Resume Button 已设置为可交互");
        }
        else
        {
            // 尝试自动查找继续游戏按钮
            if (pauseMenuPanel != null)
            {
                Button[] allButtons = pauseMenuPanel.GetComponentsInChildren<Button>(true);
                foreach (Button btn in allButtons)
                {
                    if (btn != null && (btn.name.Contains("Continue") || btn.name.Contains("继续") || btn.name.Contains("Resume")))
                    {
                        resumeButton = btn;
                        resumeButton.onClick.RemoveAllListeners();
                        resumeButton.onClick.AddListener(ResumeGame);
                        Debug.Log($"✅ 自动找到并设置 Resume Button: {btn.name}");
                        break;
                    }
                }
            }
        }
        
        // 确保重新开始按钮可交互
        if (restartButton != null)
        {
            restartButton.interactable = true;
            Image img = restartButton.GetComponent<Image>();
            if (img != null)
            {
                img.raycastTarget = true;
            }
            Debug.Log($"✅ Restart Button 已设置为可交互");
        }
        
        // 确保退出游戏按钮可交互
        if (quitButton != null)
        {
            quitButton.interactable = true;
            Image img = quitButton.GetComponent<Image>();
            if (img != null)
            {
                img.raycastTarget = true;
            }
            Debug.Log($"✅ Quit Button 已设置为可交互");
        }
        
        // 检查暂停面板下的所有按钮
        if (pauseMenuPanel != null)
        {
            Button[] allButtons = pauseMenuPanel.GetComponentsInChildren<Button>(true);
            Debug.Log($"🔍 找到 {allButtons.Length} 个按钮在 PauseMenu 面板下");
            
            foreach (Button btn in allButtons)
            {
                if (btn != null)
                {
                    btn.interactable = true;
                    
                    // 确保按钮的 Image 允许射线检测
                    Image buttonImage = btn.GetComponent<Image>();
                    if (buttonImage != null)
                    {
                        buttonImage.raycastTarget = true;
                    }
                    
                    // 确保按钮的父 Canvas 有 GraphicRaycaster
                    Canvas parentCanvas = btn.GetComponentInParent<Canvas>();
                    if (parentCanvas != null)
                    {
                        GraphicRaycaster raycaster = parentCanvas.GetComponent<GraphicRaycaster>();
                        if (raycaster == null)
                        {
                            raycaster = parentCanvas.gameObject.AddComponent<GraphicRaycaster>();
                            Debug.LogWarning($"⚠️ 为 Canvas {parentCanvas.name} 添加了 GraphicRaycaster");
                        }
                        if (raycaster != null && !raycaster.enabled)
                        {
                            raycaster.enabled = true;
                            Debug.LogWarning($"⚠️ 启用了 Canvas {parentCanvas.name} 的 GraphicRaycaster");
                        }
                    }
                    
                    Debug.Log($"✅ 按钮 {btn.name} 已设置为可交互");
                }
            }
        }
        
        // 确保 Canvas 的 GraphicRaycaster 可用
        if (pauseMenuPanel != null)
        {
            Canvas canvas = pauseMenuPanel.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster == null)
                {
                    raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
                    Debug.LogWarning($"⚠️ 为 Canvas {canvas.name} 添加了 GraphicRaycaster");
                }
                if (raycaster != null && !raycaster.enabled)
                {
                    raycaster.enabled = true;
                    Debug.LogWarning($"⚠️ 启用了 Canvas {canvas.name} 的 GraphicRaycaster");
                }
            }
        }
    }

    public void ResumeGame()
    {
        Debug.Log("🔵 PauseMenu: ResumeGame() 被调用！");
        isPaused = false;
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (playerController != null) playerController.SetPaused(false);
        Debug.Log("✅ PauseMenu: 已恢复并隐藏面板");
    }

    public void RestartScene()
    {
        Debug.Log("🔵 PauseMenu: RestartScene() 被调用！");
        Time.timeScale = 1f;
        isPaused = false;
        
        // 使用 GameStarter 来重启（如果存在）
        if (GameStarter.Instance != null)
        {
            GameStarter.RestartGame();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void QuitGame()
    {
        Debug.Log("🔵 PauseMenu: QuitGame() 被调用！");
        Time.timeScale = 1f;
        isPaused = false;
        
        // 使用 GameStarter 来退出（如果存在）
        if (GameStarter.Instance != null)
        {
            GameStarter.QuitGame();
        }
        else
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }
    
    /// <summary>
    /// 测试按钮点击（用于调试）
    /// </summary>
    public void TestButtonClick(string buttonName)
    {
        Debug.Log($"🔵 测试按钮点击: {buttonName}");
    }
}

