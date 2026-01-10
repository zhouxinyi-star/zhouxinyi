using UnityEngine;

/// <summary>
/// Terrain Collider 匹配工具 - 自动匹配 Box Collider 到 Terrain 大小
/// 使用方法：将此脚本挂到有 Terrain 组件的 GameObject 上，运行游戏或点击 Inspector 中的按钮
/// </summary>
[RequireComponent(typeof(Terrain))]
public class TerrainColliderMatcher : MonoBehaviour
{
    [Header("自动匹配设置")]
    [Tooltip("是否在 Start 时自动匹配")]
    [SerializeField] private bool autoMatchOnStart = true;
    
    [Tooltip("Collider 高度（厚度）")]
    [SerializeField] private float colliderHeight = 0.1f;
    
    [Header("调试")]
    [Tooltip("显示匹配信息")]
    [SerializeField] private bool showDebugInfo = true;

    private Terrain terrain;
    private BoxCollider boxCollider;

    private void Start()
    {
        if (autoMatchOnStart)
        {
            MatchColliderToTerrain();
        }
    }

    /// <summary>
    /// 匹配 Collider 到 Terrain 大小
    /// </summary>
    [ContextMenu("匹配 Collider 到 Terrain")]
    public void MatchColliderToTerrain()
    {
        // 获取 Terrain 组件
        terrain = GetComponent<Terrain>();
        if (terrain == null)
        {
            Debug.LogError("TerrainColliderMatcher: 未找到 Terrain 组件！");
            return;
        }

        // 获取或创建 Box Collider
        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider>();
            Debug.Log("TerrainColliderMatcher: 已创建 Box Collider");
        }

        // 获取 Terrain 数据
        TerrainData terrainData = terrain.terrainData;
        if (terrainData == null)
        {
            Debug.LogError("TerrainColliderMatcher: Terrain Data 为空！");
            return;
        }

        // 获取 Terrain 的大小（世界坐标）
        Vector3 terrainSize = terrainData.size;
        
        // 设置 Box Collider 的大小
        boxCollider.size = new Vector3(terrainSize.x, colliderHeight, terrainSize.z);
        
        // 设置 Box Collider 的中心位置
        // Terrain 的中心在 (0, 0, 0)，但 Collider 的中心应该在 Terrain 高度的中间
        float terrainHeight = terrainSize.y;
        boxCollider.center = new Vector3(0, terrainHeight / 2f - colliderHeight / 2f, 0);

        // 确保 Collider 是触发器（如果需要）
        // boxCollider.isTrigger = false; // 碰撞检测需要非触发器

        if (showDebugInfo)
        {
            Debug.Log($"=== Terrain Collider 匹配完成 ===");
            Debug.Log($"Terrain 大小: {terrainSize}");
            Debug.Log($"Box Collider 大小: {boxCollider.size}");
            Debug.Log($"Box Collider 中心: {boxCollider.center}");
            Debug.Log($"Terrain 位置: {transform.position}");
            Debug.Log($"==================");
        }
    }

    /// <summary>
    /// 使用 Terrain Collider（推荐用于 Terrain）
    /// </summary>
    [ContextMenu("使用 Terrain Collider（推荐）")]
    public void UseTerrainCollider()
    {
        terrain = GetComponent<Terrain>();
        if (terrain == null)
        {
            Debug.LogError("TerrainColliderMatcher: 未找到 Terrain 组件！");
            return;
        }

        // 检查 Terrain Data 是否存在
        if (terrain.terrainData == null)
        {
            Debug.LogError("TerrainColliderMatcher: Terrain 组件缺少 Terrain Data！");
            Debug.LogError("请先为 Terrain 组件分配 Terrain Data：");
            Debug.LogError("1. 在 Project 窗口右键 → Create → Terrain（创建新的 Terrain Data）");
            Debug.LogError("2. 或者使用现有的 Terrain Data 资源");
            Debug.LogError("3. 将 Terrain Data 拖到 Terrain 组件的 'Terrain Data' 字段");
            Debug.LogError("4. 然后再次运行此方法");
            return;
        }

        // 移除 Box Collider（如果存在）
        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            DestroyImmediate(boxCollider);
            Debug.Log("TerrainColliderMatcher: 已移除 Box Collider");
        }

        // 获取或创建 Terrain Collider
        TerrainCollider terrainCollider = GetComponent<TerrainCollider>();
        if (terrainCollider == null)
        {
            terrainCollider = gameObject.AddComponent<TerrainCollider>();
            Debug.Log("TerrainColliderMatcher: 已创建 Terrain Collider");
        }

        // 设置 Terrain Data（关键步骤！）
        if (terrainCollider.terrainData == null)
        {
            terrainCollider.terrainData = terrain.terrainData;
            Debug.Log($"TerrainColliderMatcher: 已设置 Terrain Data: {terrain.terrainData.name}");
        }
        else if (terrainCollider.terrainData != terrain.terrainData)
        {
            // 如果 Terrain Collider 的 Terrain Data 与 Terrain 组件的不一致，更新它
            Debug.LogWarning($"TerrainColliderMatcher: Terrain Collider 的 Terrain Data 与 Terrain 组件不一致，正在更新...");
            terrainCollider.terrainData = terrain.terrainData;
            Debug.Log($"TerrainColliderMatcher: 已更新 Terrain Data: {terrain.terrainData.name}");
        }

        // 确保有 Ground 标签
        if (!gameObject.CompareTag("Ground"))
        {
            Debug.LogWarning("TerrainColliderMatcher: Terrain 没有 'Ground' 标签！请手动添加。");
        }

        if (showDebugInfo)
        {
            Debug.Log($"=== 已切换到 Terrain Collider ===");
            Debug.Log($"Terrain Collider 已配置");
            Debug.Log($"Terrain Data: {(terrainCollider.terrainData != null ? terrainCollider.terrainData.name : "未设置")}");
            Debug.Log($"==================");
        }
    }

    /// <summary>
    /// 检查 Collider 配置
    /// </summary>
    [ContextMenu("检查 Collider 配置")]
    public void CheckColliderSetup()
    {
        terrain = GetComponent<Terrain>();
        boxCollider = GetComponent<BoxCollider>();
        TerrainCollider terrainCollider = GetComponent<TerrainCollider>();

        Debug.Log($"=== Collider 配置检查 ===");
        Debug.Log($"Terrain: {(terrain != null ? "存在" : "不存在")}");
        Debug.Log($"Box Collider: {(boxCollider != null ? "存在" : "不存在")}");
        Debug.Log($"Terrain Collider: {(terrainCollider != null ? "存在" : "不存在")}");
        
        if (terrain != null)
        {
            TerrainData terrainData = terrain.terrainData;
            if (terrainData != null)
            {
                Debug.Log($"Terrain 大小: {terrainData.size}");
                Debug.Log($"Terrain 位置: {transform.position}");
            }
        }
        
        if (boxCollider != null)
        {
            Debug.Log($"Box Collider 大小: {boxCollider.size}");
            Debug.Log($"Box Collider 中心: {boxCollider.center}");
            Debug.Log($"Box Collider 是否触发器: {boxCollider.isTrigger}");
        }
        
        if (terrainCollider != null)
        {
            Debug.Log($"Terrain Collider Terrain Data: {(terrainCollider.terrainData != null ? "已设置" : "未设置（需要配置！）")}");
        }
        
        Debug.Log($"标签: {gameObject.tag}");
        Debug.Log($"==================");
    }
}

