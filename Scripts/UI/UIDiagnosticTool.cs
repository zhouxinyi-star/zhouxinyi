using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// UI诊断工具 - 检查按钮是否被遮挡，UI层级是否正确
/// </summary>
public class UIDiagnosticTool : MonoBehaviour
{
    [Header("诊断目标")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Button[] buttonsToCheck;
    
    [Header("自动诊断")]
    [SerializeField] private bool autoDiagnoseOnStart = true;
    [SerializeField] private bool fixIssuesAutomatically = true;

    private void Start()
    {
        if (autoDiagnoseOnStart)
        {
            DiagnoseUI();
        }
    }

    /// <summary>
    /// 诊断UI问题
    /// </summary>
    [ContextMenu("诊断UI")]
    public void DiagnoseUI()
    {
        Debug.Log("=== UI诊断开始 ===");
        
        if (pauseMenuPanel == null)
        {
            pauseMenuPanel = GameObject.Find("PauseMenu");
        }
        
        if (pauseMenuPanel == null)
        {
            Debug.LogError("❌ 未找到 PauseMenu 面板！");
            return;
        }
        
        // 检查Canvas设置
        CheckCanvasSettings();
        
        // 检查EventSystem
        CheckEventSystem();
        
        // 检查按钮
        CheckButtons();
        
        // 检查UI层级和遮挡
        CheckUIOverlap();
        
        // 检查Raycast设置
        CheckRaycastSettings();
        
        Debug.Log("=== UI诊断完成 ===");
    }

    /// <summary>
    /// 检查Canvas设置
    /// </summary>
    private void CheckCanvasSettings()
    {
        Debug.Log("--- 检查Canvas设置 ---");
        
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        Debug.Log($"找到 {canvases.Length} 个Canvas");
        
        foreach (Canvas canvas in canvases)
        {
            Debug.Log($"Canvas: {canvas.name}");
            Debug.Log($"  - Render Mode: {canvas.renderMode}");
            Debug.Log($"  - Sort Order: {canvas.sortingOrder}");
            Debug.Log($"  - Pixel Perfect: {canvas.pixelPerfect}");
            
            // 检查GraphicRaycaster
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                Debug.LogWarning($"  ⚠️ Canvas {canvas.name} 缺少 GraphicRaycaster！");
                if (fixIssuesAutomatically)
                {
                    raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
                    Debug.Log($"  ✅ 已添加 GraphicRaycaster");
                }
            }
            else if (!raycaster.enabled)
            {
                Debug.LogWarning($"  ⚠️ Canvas {canvas.name} 的 GraphicRaycaster 被禁用！");
                if (fixIssuesAutomatically)
                {
                    raycaster.enabled = true;
                    Debug.Log($"  ✅ 已启用 GraphicRaycaster");
                }
            }
            else
            {
                Debug.Log($"  ✅ GraphicRaycaster 正常");
            }
        }
    }

    /// <summary>
    /// 检查EventSystem
    /// </summary>
    private void CheckEventSystem()
    {
        Debug.Log("--- 检查EventSystem ---");
        
        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogError("❌ 未找到 EventSystem！");
            if (fixIssuesAutomatically)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
                Debug.Log("✅ 已创建 EventSystem");
            }
        }
        else
        {
            Debug.Log($"✅ EventSystem 存在: {eventSystem.name}");
            Debug.Log($"  - 启用状态: {eventSystem.enabled}");
            
            if (!eventSystem.enabled && fixIssuesAutomatically)
            {
                eventSystem.enabled = true;
                Debug.Log("✅ 已启用 EventSystem");
            }
        }
    }

    /// <summary>
    /// 检查按钮
    /// </summary>
    private void CheckButtons()
    {
        Debug.Log("--- 检查按钮 ---");
        
        if (pauseMenuPanel == null) return;
        
        Button[] allButtons = pauseMenuPanel.GetComponentsInChildren<Button>(true);
        Debug.Log($"找到 {allButtons.Length} 个按钮");
        
        foreach (Button btn in allButtons)
        {
            Debug.Log($"按钮: {btn.name}");
            Debug.Log($"  - 激活状态: {btn.gameObject.activeInHierarchy}");
            Debug.Log($"  - 可交互: {btn.interactable}");
            Debug.Log($"  - 位置: {btn.transform.position}");
            
            RectTransform rect = btn.GetComponent<RectTransform>();
            if (rect != null)
            {
                Debug.Log($"  - Rect: {rect.rect}");
                Debug.Log($"  - 世界坐标: {rect.position}");
            }
            
            // 检查Image组件
            Image img = btn.GetComponent<Image>();
            if (img != null)
            {
                Debug.Log($"  - Image Raycast Target: {img.raycastTarget}");
                if (!img.raycastTarget && fixIssuesAutomatically)
                {
                    img.raycastTarget = true;
                    Debug.Log($"  ✅ 已启用 Image Raycast Target");
                }
            }
            else
            {
                Debug.LogWarning($"  ⚠️ 按钮 {btn.name} 没有 Image 组件！");
            }
            
            // 检查按钮是否可交互
            if (!btn.interactable && fixIssuesAutomatically)
            {
                btn.interactable = true;
                Debug.Log($"  ✅ 已启用按钮交互");
            }
            
            // 检查CanvasGroup
            CanvasGroup canvasGroup = btn.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                Debug.Log($"  - CanvasGroup Interactable: {canvasGroup.interactable}");
                Debug.Log($"  - CanvasGroup Blocks Raycasts: {canvasGroup.blocksRaycasts}");
                if ((!canvasGroup.interactable || !canvasGroup.blocksRaycasts) && fixIssuesAutomatically)
                {
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                    Debug.Log($"  ✅ 已修复 CanvasGroup 设置");
                }
            }
        }
    }

    /// <summary>
    /// 检查UI重叠和遮挡
    /// </summary>
    private void CheckUIOverlap()
    {
        Debug.Log("--- 检查UI重叠和遮挡 ---");
        
        if (pauseMenuPanel == null) return;
        
        // 获取所有可射线检测的UI元素
        Graphic[] allGraphics = pauseMenuPanel.GetComponentsInChildren<Graphic>(true);
        List<Graphic> raycastableGraphics = allGraphics.Where(g => g.raycastTarget).ToList();
        
        Debug.Log($"找到 {raycastableGraphics.Count} 个可射线检测的UI元素");
        
        // 检查按钮是否被其他元素遮挡
        Button[] buttons = pauseMenuPanel.GetComponentsInChildren<Button>(true);
        
        foreach (Button btn in buttons)
        {
            RectTransform btnRect = btn.GetComponent<RectTransform>();
            if (btnRect == null) continue;
            
            Vector3[] btnCorners = new Vector3[4];
            btnRect.GetWorldCorners(btnCorners);
            
            // 检查是否有其他UI元素覆盖在按钮上
            List<Graphic> overlappingElements = new List<Graphic>();
            
            foreach (Graphic graphic in raycastableGraphics)
            {
                if (graphic.gameObject == btn.gameObject) continue;
                if (graphic.transform.IsChildOf(btn.transform)) continue; // 跳过按钮的子元素
                
                RectTransform graphicRect = graphic.GetComponent<RectTransform>();
                if (graphicRect == null) continue;
                
                Vector3[] graphicCorners = new Vector3[4];
                graphicRect.GetWorldCorners(graphicCorners);
                
                // 检查是否重叠
                if (IsOverlapping(btnCorners, graphicCorners))
                {
                    // 检查层级顺序（Hierarchy中的顺序）
                    int btnSiblingIndex = btn.transform.GetSiblingIndex();
                    int graphicSiblingIndex = graphic.transform.GetSiblingIndex();
                    
                    // 如果graphic在按钮之后（sibling index更大），且在同一父级下，可能会遮挡
                    if (graphic.transform.parent == btn.transform.parent && graphicSiblingIndex > btnSiblingIndex)
                    {
                        overlappingElements.Add(graphic);
                    }
                    // 如果graphic在更高的Canvas层级
                    else if (graphic.transform.parent != btn.transform.parent)
                    {
                        Canvas btnCanvas = btn.GetComponentInParent<Canvas>();
                        Canvas graphicCanvas = graphic.GetComponentInParent<Canvas>();
                        
                        if (graphicCanvas != null && btnCanvas != null && graphicCanvas != btnCanvas)
                        {
                            if (graphicCanvas.sortingOrder > btnCanvas.sortingOrder)
                            {
                                overlappingElements.Add(graphic);
                            }
                        }
                    }
                }
            }
            
            if (overlappingElements.Count > 0)
            {
                Debug.LogWarning($"⚠️ 按钮 {btn.name} 可能被以下元素遮挡:");
                foreach (Graphic g in overlappingElements)
                {
                    Debug.LogWarning($"  - {g.name} (类型: {g.GetType().Name})");
                    
                    // 如果是Image且是背景，可以禁用其raycastTarget
                    Image img = g as Image;
                    if (img != null && fixIssuesAutomatically)
                    {
                        // 检查是否是背景面板
                        if (img.name.Contains("Background") || img.name.Contains("Panel") || img.name == "PauseMenu")
                        {
                            img.raycastTarget = false;
                            Debug.Log($"  ✅ 已禁用背景 {img.name} 的 Raycast Target");
                        }
                    }
                }
            }
            else
            {
                Debug.Log($"✅ 按钮 {btn.name} 未被遮挡");
            }
        }
    }

    /// <summary>
    /// 检查两个矩形是否重叠
    /// </summary>
    private bool IsOverlapping(Vector3[] corners1, Vector3[] corners2)
    {
        // 简单的重叠检测：检查是否有任何角点在另一个矩形内
        foreach (Vector3 corner in corners1)
        {
            if (IsPointInRect(corner, corners2))
                return true;
        }
        
        foreach (Vector3 corner in corners2)
        {
            if (IsPointInRect(corner, corners1))
                return true;
        }
        
        return false;
    }

    /// <summary>
    /// 检查点是否在矩形内
    /// </summary>
    private bool IsPointInRect(Vector3 point, Vector3[] rectCorners)
    {
        if (rectCorners.Length < 4) return false;
        
        float minX = Mathf.Min(rectCorners[0].x, rectCorners[1].x, rectCorners[2].x, rectCorners[3].x);
        float maxX = Mathf.Max(rectCorners[0].x, rectCorners[1].x, rectCorners[2].x, rectCorners[3].x);
        float minY = Mathf.Min(rectCorners[0].y, rectCorners[1].y, rectCorners[2].y, rectCorners[3].y);
        float maxY = Mathf.Max(rectCorners[0].y, rectCorners[1].y, rectCorners[2].y, rectCorners[3].y);
        
        return point.x >= minX && point.x <= maxX && point.y >= minY && point.y <= maxY;
    }

    /// <summary>
    /// 检查Raycast设置
    /// </summary>
    private void CheckRaycastSettings()
    {
        Debug.Log("--- 检查Raycast设置 ---");
        
        if (pauseMenuPanel == null) return;
        
        // 检查CanvasGroup
        CanvasGroup canvasGroup = pauseMenuPanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            Debug.Log($"PauseMenu CanvasGroup:");
            Debug.Log($"  - Interactable: {canvasGroup.interactable}");
            Debug.Log($"  - Blocks Raycasts: {canvasGroup.blocksRaycasts}");
            Debug.Log($"  - Alpha: {canvasGroup.alpha}");
            
            if ((!canvasGroup.interactable || !canvasGroup.blocksRaycasts) && fixIssuesAutomatically)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                Debug.Log("✅ 已修复 CanvasGroup 设置");
            }
        }
        else
        {
            Debug.Log("PauseMenu 没有 CanvasGroup");
        }
        
        // 检查背景Image的raycastTarget
        Image backgroundImage = pauseMenuPanel.GetComponent<Image>();
        if (backgroundImage != null)
        {
            Debug.Log($"背景Image Raycast Target: {backgroundImage.raycastTarget}");
            if (backgroundImage.raycastTarget && fixIssuesAutomatically)
            {
                // 背景通常不应该阻挡按钮的点击
                backgroundImage.raycastTarget = false;
                Debug.Log("✅ 已禁用背景Image的Raycast Target（避免阻挡按钮点击）");
            }
        }
    }

    /// <summary>
    /// 修复所有发现的问题
    /// </summary>
    [ContextMenu("修复所有问题")]
    public void FixAllIssues()
    {
        bool originalFixSetting = fixIssuesAutomatically;
        fixIssuesAutomatically = true;
        DiagnoseUI();
        fixIssuesAutomatically = originalFixSetting;
    }
}

