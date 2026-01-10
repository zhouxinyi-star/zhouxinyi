using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// UI 诊断工具：检查 Canvas、EventSystem、Graphic Raycaster 配置
/// 使用方法：将此脚本挂到任意 GameObject，运行游戏时会自动诊断并修复常见问题
/// </summary>
public class UIDiagnostic : MonoBehaviour
{
    [Header("自动修复")]
    [SerializeField] private bool autoFix = true;
    
    private void Start()
    {
        Debug.Log("=== UI 诊断开始 ===");
        CheckAndFixEventSystem();
        CheckAndFixCanvas();
        Debug.Log("=== UI 诊断完成 ===");
    }
    
    private void CheckAndFixEventSystem()
    {
        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        
        if (eventSystem == null)
        {
            Debug.LogError("❌ 未找到 EventSystem！正在创建...");
            if (autoFix)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystem = eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
                Debug.Log("✅ 已创建 EventSystem 和 StandaloneInputModule");
            }
        }
        else
        {
            Debug.Log($"✅ 找到 EventSystem: {eventSystem.gameObject.name}");
            
            // 检查 StandaloneInputModule
            StandaloneInputModule inputModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (inputModule == null)
            {
                Debug.LogError($"❌ EventSystem ({eventSystem.gameObject.name}) 缺少 StandaloneInputModule！正在添加...");
                if (autoFix)
                {
                    inputModule = eventSystem.gameObject.AddComponent<StandaloneInputModule>();
                    Debug.Log("✅ 已添加 StandaloneInputModule");
                }
            }
            else
            {
                Debug.Log($"✅ StandaloneInputModule 存在，Enabled: {inputModule.enabled}");
                if (!inputModule.enabled && autoFix)
                {
                    inputModule.enabled = true;
                    Debug.Log("✅ 已启用 StandaloneInputModule");
                }
            }
            
            // 检查 EventSystem 是否启用
            if (!eventSystem.enabled)
            {
                Debug.LogError($"❌ EventSystem ({eventSystem.gameObject.name}) 被禁用了！");
                if (autoFix)
                {
                    eventSystem.enabled = true;
                    Debug.Log("✅ 已启用 EventSystem");
                }
            }
        }
    }
    
    private void CheckAndFixCanvas()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        
        if (canvases.Length == 0)
        {
            Debug.LogWarning("⚠️ 场景中没有 Canvas！");
            return;
        }
        
        Debug.Log($"找到 {canvases.Length} 个 Canvas");
        
        foreach (Canvas canvas in canvases)
        {
            Debug.Log($"\n检查 Canvas: {canvas.gameObject.name}");
            
            // 检查 Render Mode
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                Debug.LogWarning($"⚠️ Canvas ({canvas.gameObject.name}) Render Mode 不是 Screen Space - Overlay，当前是: {canvas.renderMode}");
                if (autoFix)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    Debug.Log($"✅ 已设置 Render Mode 为 Screen Space - Overlay");
                }
            }
            else
            {
                Debug.Log("✅ Render Mode 正确: Screen Space - Overlay");
            }
            
            // 检查 Graphic Raycaster
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                Debug.LogError($"❌ Canvas ({canvas.gameObject.name}) 缺少 Graphic Raycaster！正在添加...");
                if (autoFix)
                {
                    raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
                    Debug.Log("✅ 已添加 Graphic Raycaster");
                }
            }
            else
            {
                Debug.Log($"✅ Graphic Raycaster 存在，Enabled: {raycaster.enabled}");
                if (!raycaster.enabled && autoFix)
                {
                    raycaster.enabled = true;
                    Debug.Log("✅ 已启用 Graphic Raycaster");
                }
            }
            
            // 检查 Canvas Group 是否阻挡交互
            CanvasGroup canvasGroup = canvas.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                if (!canvasGroup.interactable)
                {
                    Debug.LogWarning($"⚠️ Canvas ({canvas.gameObject.name}) 的 CanvasGroup.interactable = false，这会阻止按钮交互！");
                    if (autoFix)
                    {
                        canvasGroup.interactable = true;
                        Debug.Log("✅ 已启用 CanvasGroup.interactable");
                    }
                }
                if (!canvasGroup.blocksRaycasts)
                {
                    Debug.LogWarning($"⚠️ Canvas ({canvas.gameObject.name}) 的 CanvasGroup.blocksRaycasts = false，这会影响 UI 交互！");
                }
            }
        }
    }
    
    // 运行时测试：按 T 键重新诊断
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("\n=== 手动触发诊断 ===");
            CheckAndFixEventSystem();
            CheckAndFixCanvas();
        }
    }
}