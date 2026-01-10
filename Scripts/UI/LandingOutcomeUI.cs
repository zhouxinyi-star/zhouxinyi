using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 着陆结局UI显示系统 - 根据结局类型显示不同的UI效果
/// </summary>
public class LandingOutcomeUI : MonoBehaviour
{
    #region UI元素引用
    [Header("结局面板")]
    [SerializeField] private GameObject outcomePanel;  // 结局面板根对象
    
    [Header("标题和文本")]
    [SerializeField] private TextMeshProUGUI outcomeTitleText;      // 结局标题
    [SerializeField] private TextMeshProUGUI outcomeDescriptionText;  // 结局描述
    [SerializeField] private TextMeshProUGUI landingSpeedText;      // 着陆速度文本
    [SerializeField] private TextMeshProUGUI balloonStatusText;      // 气球状态文本
    
    [Header("背景和效果")]
    [SerializeField] private Image backgroundImage;      // 背景图片（用于颜色效果）
    [SerializeField] private GameObject explosionEffect; // 爆炸特效（可选）
    [SerializeField] private GameObject successEffect;   // 成功特效（可选）
    
    [Header("按钮")]
    [SerializeField] private Button restartButton;      // 重新开始按钮
    [SerializeField] private Button quitButton;         // 退出按钮
    
    [Header("颜色配置")]
    [SerializeField] private Color safeColor = new Color(0.2f, 0.8f, 0.2f);      // 安全降落颜色（绿色）
    [SerializeField] private Color injuredColor = new Color(0.9f, 0.7f, 0.1f);   // 受伤降落颜色（黄色）
    [SerializeField] private Color crashedColor = new Color(0.8f, 0.1f, 0.1f);   // 坠毁颜色（红色）
    #endregion

    #region 私有变量
    private LandingOutcomeSystem landingOutcomeSystem;
    private bool isShowing = false;
    #endregion

    #region Unity生命周期
    private void Awake()
    {
        // 初始化时隐藏面板
        if (outcomePanel != null)
        {
            outcomePanel.SetActive(false);
        }
    }

    private void Start()
    {
        // 查找LandingOutcomeSystem
        landingOutcomeSystem = FindObjectOfType<LandingOutcomeSystem>();
        
        if (landingOutcomeSystem != null)
        {
            // 订阅结局事件
            landingOutcomeSystem.OnLandingOutcome += ShowOutcome;
        }
        else
        {
            Debug.LogWarning("LandingOutcomeUI: 未找到 LandingOutcomeSystem，结局UI可能无法正常工作");
        }

        // 设置按钮事件
        SetupButtons();
    }

    private void OnDestroy()
    {
        // 取消订阅
        if (landingOutcomeSystem != null)
        {
            landingOutcomeSystem.OnLandingOutcome -= ShowOutcome;
        }
    }
    #endregion

    #region 事件处理
    /// <summary>
    /// 显示结局UI
    /// </summary>
    /// <param name="outcome">结局类型</param>
    /// <param name="velocity">着陆速度</param>
    /// <param name="balloonExploded">气球是否爆炸</param>
    private void ShowOutcome(LandingOutcomeSystem.LandingOutcome outcome, float velocity, bool balloonExploded)
    {
        if (isShowing) return;  // 防止重复显示
        
        isShowing = true;
        
        // 显示面板
        if (outcomePanel != null)
        {
            outcomePanel.SetActive(true);
        }

        // 更新UI内容
        UpdateOutcomeUI(outcome, velocity, balloonExploded);

        // 播放特效
        PlayOutcomeEffects(outcome, balloonExploded);

        // 暂停游戏
        Time.timeScale = 0f;
        
        // 解锁鼠标
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 播放动画（如果有）
        StartCoroutine(AnimateOutcomePanel());
    }

    /// <summary>
    /// 更新结局UI内容
    /// </summary>
    private void UpdateOutcomeUI(LandingOutcomeSystem.LandingOutcome outcome, float velocity, bool balloonExploded)
    {
        Color themeColor;
        string titleText;
        string descriptionText;

        // 根据结局类型设置内容和颜色
        switch (outcome)
        {
            case LandingOutcomeSystem.LandingOutcome.SafeLanding:
                themeColor = safeColor;
                titleText = "安全降落！";
                descriptionText = "恭喜！你成功安全着陆了！\n你的降落速度控制得很好。";
                break;

            case LandingOutcomeSystem.LandingOutcome.InjuredLanding:
                themeColor = injuredColor;
                titleText = "受伤降落";
                descriptionText = "你成功着陆了，但速度过快导致受伤。\n下次尝试更早开始减速。";
                break;

            case LandingOutcomeSystem.LandingOutcome.Crashed:
                themeColor = crashedColor;
                titleText = "坠毁";
                descriptionText = balloonExploded 
                    ? "气球爆炸导致坠毁！\n注意控制气球体积，避免过度充气。" 
                    : "速度过快导致坠毁！\n尝试更早开始充气减速。";
                break;

            default:
                themeColor = Color.white;
                titleText = "未知结局";
                descriptionText = "";
                break;
        }

        // 更新标题
        if (outcomeTitleText != null)
        {
            outcomeTitleText.text = titleText;
            outcomeTitleText.color = themeColor;
        }

        // 更新描述
        if (outcomeDescriptionText != null)
        {
            outcomeDescriptionText.text = descriptionText;
        }

        // 更新速度文本
        if (landingSpeedText != null)
        {
            landingSpeedText.text = $"着陆速度: {velocity:F2} m/s";
            landingSpeedText.color = themeColor;
        }

        // 更新气球状态文本
        if (balloonStatusText != null)
        {
            balloonStatusText.text = balloonExploded ? "气球状态: 已爆炸 ✗" : "气球状态: 完好 ✓";
            balloonStatusText.color = balloonExploded ? crashedColor : safeColor;
        }

        // 更新背景颜色
        if (backgroundImage != null)
        {
            Color bgColor = themeColor;
            bgColor.a = 0.3f;  // 半透明
            backgroundImage.color = bgColor;
        }
    }

    /// <summary>
    /// 播放结局特效
    /// </summary>
    private void PlayOutcomeEffects(LandingOutcomeSystem.LandingOutcome outcome, bool balloonExploded)
    {
        // 爆炸特效
        if (explosionEffect != null)
        {
            bool shouldShowExplosion = (outcome == LandingOutcomeSystem.LandingOutcome.Crashed) || balloonExploded;
            explosionEffect.SetActive(shouldShowExplosion);
            
            if (shouldShowExplosion)
            {
                // 可以在这里添加爆炸音效
                Debug.Log("播放爆炸特效");
            }
        }

        // 成功特效
        if (successEffect != null)
        {
            bool shouldShowSuccess = outcome == LandingOutcomeSystem.LandingOutcome.SafeLanding;
            successEffect.SetActive(shouldShowSuccess);
            
            if (shouldShowSuccess)
            {
                // 可以在这里添加成功音效
                Debug.Log("播放成功特效");
            }
        }
    }

    /// <summary>
    /// 动画显示面板
    /// </summary>
    private IEnumerator AnimateOutcomePanel()
    {
        // 简单的淡入效果（如果有CanvasGroup）
        CanvasGroup canvasGroup = outcomePanel?.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;  // 使用unscaledDeltaTime因为timeScale=0
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }
    }
    #endregion

    #region 按钮设置
    /// <summary>
    /// 设置按钮事件
    /// </summary>
    private void SetupButtons()
    {
        // 重新开始按钮
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(() =>
            {
                RestartGame();
            });
        }

        // 退出按钮
        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(() =>
            {
                QuitGame();
            });
        }
    }

    /// <summary>
    /// 重新开始游戏
    /// </summary>
    private void RestartGame()
    {
        // 恢复时间缩放
        Time.timeScale = 1f;

        // 重置结局系统
        if (landingOutcomeSystem != null)
        {
            landingOutcomeSystem.Reset();
        }

        // 重置物理管理器
        if (PhysicsManager.Instance != null)
        {
            PhysicsManager.Instance.ResetPhysics();
        }

        // 隐藏面板
        if (outcomePanel != null)
        {
            outcomePanel.SetActive(false);
        }

        isShowing = false;

        // 重新加载场景或重置游戏状态
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    private void QuitGame()
    {
        Time.timeScale = 1f;

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 手动显示结局（用于测试）
    /// </summary>
    public void TestShowOutcome(LandingOutcomeSystem.LandingOutcome outcome, float velocity, bool exploded)
    {
        ShowOutcome(outcome, velocity, exploded);
    }
    #endregion
}