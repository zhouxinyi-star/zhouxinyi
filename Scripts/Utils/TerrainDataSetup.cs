using UnityEngine;
using System.Linq;

/// <summary>
/// Terrain Data 设置工具 - 自动创建或分配 Terrain Data
/// 使用方法：将此脚本挂到有 Terrain 组件的 GameObject 上，运行游戏或点击 Inspector 中的按钮
/// </summary>
[RequireComponent(typeof(Terrain))]
public class TerrainDataSetup : MonoBehaviour
{
    [Header("Terrain Data 设置")]
    [Tooltip("Terrain Data 资源名称（如果已存在）")]
    [SerializeField] private string terrainDataName = "New Terrain Data";
    
    [Tooltip("Terrain 大小（世界单位）")]
    [SerializeField] private Vector3 terrainSize = new Vector3(1000f, 600f, 1000f);
    
    [Tooltip("高度图分辨率（必须是 2^n + 1，例如 513, 1025）")]
    [SerializeField] private int heightmapResolution = 513;
    
    [Header("自动设置")]
    [Tooltip("是否在 Start 时自动设置")]
    [SerializeField] private bool autoSetupOnStart = false;
    
    [Header("调试")]
    [Tooltip("显示设置信息")]
    [SerializeField] private bool showDebugInfo = true;

    private Terrain terrain;

    private void Start()
    {
        if (autoSetupOnStart)
        {
            SetupTerrainData();
        }
    }

    /// <summary>
    /// 设置 Terrain Data（创建新的或使用现有的）
    /// </summary>
    [ContextMenu("设置 Terrain Data")]
    public void SetupTerrainData()
    {
        terrain = GetComponent<Terrain>();
        if (terrain == null)
        {
            Debug.LogError("TerrainDataSetup: 未找到 Terrain 组件！");
            return;
        }

        // 检查是否已有 Terrain Data
        if (terrain.terrainData != null)
        {
            Debug.Log($"TerrainDataSetup: Terrain 已有 Terrain Data: {terrain.terrainData.name}");
            ConfigureTerrainCollider();
            return;
        }

        // 尝试查找现有的 Terrain Data
        TerrainData existingData = Resources.FindObjectsOfTypeAll<TerrainData>()
            .FirstOrDefault(td => td.name == terrainDataName);

        if (existingData != null)
        {
            Debug.Log($"TerrainDataSetup: 找到现有 Terrain Data: {existingData.name}");
            terrain.terrainData = existingData;
            ConfigureTerrainCollider();
            return;
        }

        // 创建新的 Terrain Data
        Debug.Log($"TerrainDataSetup: 创建新的 Terrain Data: {terrainDataName}");
        TerrainData newTerrainData = CreateTerrainData();
        
        if (newTerrainData != null)
        {
            terrain.terrainData = newTerrainData;
            ConfigureTerrainCollider();
            
            if (showDebugInfo)
            {
                Debug.Log($"=== Terrain Data 设置完成 ===");
                Debug.Log($"Terrain Data 名称: {newTerrainData.name}");
                Debug.Log($"Terrain 大小: {terrainSize}");
                Debug.Log($"高度图分辨率: {heightmapResolution}");
                Debug.Log($"==================");
            }
        }
    }

    /// <summary>
    /// 创建新的 Terrain Data
    /// </summary>
    private TerrainData CreateTerrainData()
    {
        // 确保高度图分辨率是有效的（2^n + 1）
        int validResolution = GetValidHeightmapResolution(heightmapResolution);
        
        TerrainData terrainData = new TerrainData();
        terrainData.name = terrainDataName;
        
        // 设置 Terrain Data 的大小
        terrainData.size = terrainSize;
        
        // 设置高度图分辨率
        terrainData.heightmapResolution = validResolution;
        
        // 注意：detailResolution 和 detailResolutionPerPatch 是只读属性，由 Unity 自动设置，不能手动赋值
        
        // 设置控制贴图分辨率
        terrainData.alphamapResolution = 512;
        
        // 设置基础贴图分辨率
        terrainData.baseMapResolution = 1024;
        
        // 创建默认的平坦高度图
        float[,] heights = new float[validResolution, validResolution];
        terrainData.SetHeights(0, 0, heights);
        
        // 保存到项目（可选）
        #if UNITY_EDITOR
        string path = $"Assets/{terrainDataName}.asset";
        UnityEditor.AssetDatabase.CreateAsset(terrainData, path);
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log($"TerrainDataSetup: 已保存 Terrain Data 到: {path}");
        #endif
        
        return terrainData;
    }

    /// <summary>
    /// 获取有效的高度图分辨率（2^n + 1）
    /// </summary>
    private int GetValidHeightmapResolution(int resolution)
    {
        // Unity 要求高度图分辨率必须是 2^n + 1 的形式
        // 常见值：33, 65, 129, 257, 513, 1025, 2049, 4097
        int[] validResolutions = { 33, 65, 129, 257, 513, 1025, 2049, 4097 };
        
        // 找到最接近的有效分辨率
        int closest = validResolutions[0];
        foreach (int valid in validResolutions)
        {
            if (valid <= resolution)
            {
                closest = valid;
            }
            else
            {
                break;
            }
        }
        
        if (closest != resolution)
        {
            Debug.LogWarning($"TerrainDataSetup: 高度图分辨率 {resolution} 无效，使用 {closest} 代替");
        }
        
        return closest;
    }

    /// <summary>
    /// 配置 Terrain Collider
    /// </summary>
    private void ConfigureTerrainCollider()
    {
        TerrainCollider terrainCollider = GetComponent<TerrainCollider>();
        if (terrainCollider == null)
        {
            terrainCollider = gameObject.AddComponent<TerrainCollider>();
            Debug.Log("TerrainDataSetup: 已创建 Terrain Collider");
        }

        // 设置 Terrain Data
        if (terrainCollider.terrainData == null && terrain.terrainData != null)
        {
            terrainCollider.terrainData = terrain.terrainData;
            Debug.Log("TerrainDataSetup: 已设置 Terrain Collider 的 Terrain Data");
        }

        // 确保有 Ground 标签
        if (!gameObject.CompareTag("Ground"))
        {
            Debug.LogWarning("TerrainDataSetup: Terrain 没有 'Ground' 标签！请手动添加。");
        }
    }

    /// <summary>
    /// 检查当前配置
    /// </summary>
    [ContextMenu("检查配置")]
    public void CheckConfiguration()
    {
        terrain = GetComponent<Terrain>();
        TerrainCollider terrainCollider = GetComponent<TerrainCollider>();

        Debug.Log($"=== Terrain 配置检查 ===");
        Debug.Log($"Terrain 组件: {(terrain != null ? "存在" : "不存在")}");
        Debug.Log($"Terrain Data: {(terrain != null && terrain.terrainData != null ? terrain.terrainData.name : "未设置")}");
        
        if (terrain != null && terrain.terrainData != null)
        {
            Debug.Log($"Terrain 大小: {terrain.terrainData.size}");
            Debug.Log($"高度图分辨率: {terrain.terrainData.heightmapResolution}");
        }
        
        Debug.Log($"Terrain Collider: {(terrainCollider != null ? "存在" : "不存在")}");
        Debug.Log($"Terrain Collider Terrain Data: {(terrainCollider != null && terrainCollider.terrainData != null ? terrainCollider.terrainData.name : "未设置")}");
        Debug.Log($"标签: {gameObject.tag}");
        Debug.Log($"==================");
    }
}

