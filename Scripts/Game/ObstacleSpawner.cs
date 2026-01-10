using UnityEngine;
using System.Collections;

/// <summary>
/// 障碍物生成器 - 在下降过程中生成障碍物供玩家躲避
/// </summary>
public class ObstacleSpawner : MonoBehaviour
{
    #region 配置参数
    [Header("障碍物配置")]
    [Tooltip("障碍物预制体（可以是球体、立方体等）")]
    [SerializeField] private GameObject obstaclePrefab;
    
    [Tooltip("生成间隔（秒）")]
    [SerializeField] private float spawnInterval = 3f;  // 改为3秒，更容易看到
    
    [Tooltip("生成范围：X轴左右各多少米")]
    [SerializeField] private float spawnRangeX = 20f;  // 减少范围，更集中
    
    [Tooltip("生成范围：Z轴前后各多少米")]
    [SerializeField] private float spawnRangeZ = 20f;  // 减少范围，更集中
    
    [Tooltip("障碍物在玩家前方多少米开始生成")]
    [SerializeField] private float spawnDistanceAhead = 30f;  // 减少到30米，更靠近玩家
    
    [Tooltip("障碍物在玩家上方多少米生成（负数表示下方）")]
    [SerializeField] private float spawnHeightAbove = 20f;  // 改为20米，在玩家上方生成，让障碍物向下移动时能碰到玩家
    
    [Tooltip("障碍物高度随机范围（米）")]
    [SerializeField] private float spawnHeightRandomRange = 40f;  // 在玩家上下各20米范围内随机生成
    
    [Tooltip("障碍物移动速度（向下，m/s，相对于玩家）")]
    [SerializeField] private float obstacleSpeed = 5f;
    
    [Tooltip("障碍物是否相对于玩家移动（true=相对于玩家，false=绝对世界坐标）")]
    [SerializeField] private bool moveRelativeToPlayer = true;
    
    [Header("生成控制")]
    [Tooltip("是否启用障碍物生成")]
    [SerializeField] private bool enableSpawning = true;
    
    [Tooltip("最低生成高度（低于此高度不再生成）")]
    [SerializeField] private float minSpawnHeight = 500f;
    
    [Tooltip("最高生成高度（高于此高度不再生成，默认等于初始高度）")]
    [SerializeField] private float maxSpawnHeight = 3000f;
    #endregion

    #region 私有变量
    private Transform playerTransform;
    private PhysicsManager physicsManager;
    private float lastSpawnTime = 0f;
    #endregion

    #region Unity生命周期
    private void Awake()
    {
        // 在 Awake 中初始化，确保在 Start 之前准备好
        physicsManager = PhysicsManager.Instance;
        Debug.Log($"🔧 ObstacleSpawner Awake: physicsManager = {(physicsManager != null ? "OK" : "NULL")}");
    }
    
    private void Start()
    {
        Debug.Log("🔧 ObstacleSpawner Start 被调用");
        
        // 查找玩家
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            Debug.Log($"✅ ObstacleSpawner: 找到玩家 '{player.name}'");
        }
        else
        {
            Debug.LogWarning("⚠️ ObstacleSpawner: 找不到带 'Player' 标签的 GameObject，尝试使用主摄像机...");
            playerTransform = Camera.main?.transform;
            if (playerTransform != null)
            {
                Debug.Log($"✅ ObstacleSpawner: 使用主摄像机 '{Camera.main.name}' 作为玩家参考");
            }
            else
            {
                Debug.LogError("❌ ObstacleSpawner: 找不到玩家！请确保玩家有 'Player' 标签或场景中有主摄像机。");
                enabled = false; // 禁用脚本，避免后续错误
                return;
            }
        }

        // 如果没有预制体，创建一个简单的默认障碍物
        if (obstaclePrefab == null)
        {
            Debug.Log("🔧 ObstacleSpawner: 创建默认障碍物预制体...");
            CreateDefaultObstaclePrefab();
        }
        else
        {
            Debug.Log($"✅ ObstacleSpawner: 使用预制体 '{obstaclePrefab.name}'");
        }
        
        // 初始化生成时间
        lastSpawnTime = Time.time;
        
        Debug.Log($"✅ ObstacleSpawner 初始化完成: enableSpawning={enableSpawning}, minSpawnHeight={minSpawnHeight}m, spawnInterval={spawnInterval}s");
    }

    private void Update()
    {
        // 安全检查
        if (!this.enabled)
        {
            // 只在第一次检测到时打印
            if (Time.time % 10f < Time.deltaTime)
            {
                Debug.LogWarning("⚠️ ObstacleSpawner: 脚本已禁用！");
            }
            return;
        }
        
        if (!this.gameObject.activeInHierarchy)
        {
            // 只在第一次检测到时打印
            if (Time.time % 10f < Time.deltaTime)
            {
                Debug.LogWarning("⚠️ ObstacleSpawner: GameObject 未激活！");
            }
            return;
        }
        
        if (!enableSpawning)
        {
            // 只在第一次检测到时打印
            if (Time.time % 10f < Time.deltaTime)
            {
                Debug.LogWarning("⚠️ ObstacleSpawner: enableSpawning = false，生成已禁用！");
            }
            return;
        }
        
        if (obstaclePrefab == null)
        {
            Debug.LogWarning("⚠️ ObstacleSpawner: obstaclePrefab 为 null！");
            return;
        }
        if (playerTransform == null)
        {
            Debug.LogWarning("⚠️ ObstacleSpawner: playerTransform 为 null！");
            return;
        }
        if (physicsManager == null)
        {
            Debug.LogWarning("⚠️ ObstacleSpawner: physicsManager 为 null！");
            return;
        }
        
        // 检查是否应该生成障碍物
        float currentHeight = physicsManager.CurrentHeight;
        if (currentHeight < minSpawnHeight)
        {
            // 每5秒打印一次调试信息（避免刷屏）
            if (Time.time % 5f < Time.deltaTime)
            {
                Debug.Log($"⚠️ ObstacleSpawner: 高度太低 ({currentHeight:F1}m < {minSpawnHeight}m)，不再生成障碍物");
            }
            return; // 太低不再生成
        }
        
        if (currentHeight > maxSpawnHeight)
        {
            // 超过最大生成高度，不再生成
            return;
        }

        // 按间隔生成
        float timeSinceLastSpawn = Time.time - lastSpawnTime;
        if (timeSinceLastSpawn >= spawnInterval)
        {
            Debug.Log($"🔵 ObstacleSpawner: 准备生成障碍物... 当前高度: {currentHeight:F1}m, 距离上次生成: {timeSinceLastSpawn:F1}s");
            SpawnObstacle();
            lastSpawnTime = Time.time;
        }
    }
    #endregion

    #region 障碍物生成
    /// <summary>
    /// 生成障碍物
    /// </summary>
    private void SpawnObstacle()
    {
        // 随机位置（在玩家周围）
        float randomX = Random.Range(-spawnRangeX, spawnRangeX);
        float randomZ = Random.Range(-spawnRangeZ, spawnRangeZ);
        
        // 使用物理管理器的高度，而不是玩家的 Transform 位置
        float currentHeight = physicsManager != null ? physicsManager.CurrentHeight : playerTransform.position.y;
        
        // 随机高度：在玩家上下一定范围内生成，让障碍物从各个方向接近玩家
        float randomHeight = Random.Range(-spawnHeightRandomRange, spawnHeightRandomRange);
        
        // 生成位置：在玩家周围（前方、侧方、上下），让障碍物能有效阻挡玩家
        Vector3 spawnPosition = new Vector3(
            playerTransform.position.x + randomX,
            currentHeight + spawnHeightAbove + randomHeight,  // 在玩家上方一定范围内随机生成
            playerTransform.position.z + randomZ + spawnDistanceAhead
        );
        
        // 计算与玩家的距离
        float distanceToPlayer = Vector3.Distance(spawnPosition, playerTransform.position);
        
        Debug.Log($"✅ 生成障碍物: 位置 = ({spawnPosition.x:F1}, {spawnPosition.y:F1}, {spawnPosition.z:F1}), 玩家高度 = {currentHeight:F1}m, 高度差 = {spawnHeightAbove:F1}m, 距离玩家 = {distanceToPlayer:F1}m, 预制体 = {(obstaclePrefab != null ? obstaclePrefab.name : "NULL")}");
        
        // 实例化障碍物
        GameObject obstacle = Instantiate(obstaclePrefab, spawnPosition, Quaternion.identity);
        if (obstacle == null)
        {
            Debug.LogError("❌ ObstacleSpawner: 无法实例化障碍物预制体！");
            return;
        }
        
        obstacle.name = "Obstacle_" + Time.time.ToString("F2");  // 便于调试
        
        // 确保障碍物是激活的
        obstacle.SetActive(true);
        
        // 检查渲染器
        Renderer renderer = obstacle.GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogWarning($"⚠️ 障碍物 {obstacle.name} 没有 Renderer 组件！");
        }
        else
        {
            Debug.Log($"✅ 障碍物渲染器: enabled={renderer.enabled}, visible={renderer.isVisible}, bounds={renderer.bounds}");
        }
        
        Debug.Log($"✅ 障碍物已创建: {obstacle.name}, 位置 = {obstacle.transform.position}, 缩放 = {obstacle.transform.localScale}, 激活状态 = {obstacle.activeSelf}");
        
        // 添加移动脚本（如果还没有）
        ObstacleMovement movement = obstacle.GetComponent<ObstacleMovement>();
        if (movement == null)
        {
            movement = obstacle.AddComponent<ObstacleMovement>();
        }
        
        // 延迟设置速度，确保组件已完全初始化
        if (movement != null)
        {
            // 设置障碍物的基础速度（相对于玩家的额外速度）
            // ObstacleMovement 会自动加上玩家的下降速度，确保障碍物能追上玩家
            movement.SetSpeed(obstacleSpeed);
            movement.SetRelativeToPlayer(moveRelativeToPlayer, playerTransform);
        }
        
        // 添加碰撞检测标签
        if (!obstacle.CompareTag("Obstacle"))
        {
            obstacle.tag = "Obstacle";
        }
        
        // 添加碰撞器（如果没有）
        Collider existingCollider = obstacle.GetComponent<Collider>();
        if (existingCollider == null)
        {
            SphereCollider collider = obstacle.AddComponent<SphereCollider>();
            collider.isTrigger = true; // 使用触发器，避免物理碰撞影响下降
            collider.radius = 2.5f; // 匹配缩放（5米直径 = 2.5米半径）
        }
        else if (existingCollider is SphereCollider)
        {
            // 如果已有 SphereCollider，确认为触发器
            ((SphereCollider)existingCollider).isTrigger = true;
        }
        
        // 确保障碍物在 Layer 0（Default），避免被遮挡
        obstacle.layer = 0;
        
        // 自动销毁（避免内存泄漏）
        Destroy(obstacle, 60f); // 60秒后销毁
        
        // 添加调试：显示障碍物与摄像机的距离
        if (Camera.main != null)
        {
            float distanceToCamera = Vector3.Distance(obstacle.transform.position, Camera.main.transform.position);
            Debug.Log($"📹 障碍物 {obstacle.name} 距离摄像机: {distanceToCamera:F1}m");
        }
    }

    /// <summary>
    /// 创建默认障碍物预制体（如果没有指定）
    /// </summary>
    private void CreateDefaultObstaclePrefab()
    {
        // 创建一个简单的球体作为障碍物
        GameObject defaultObstacle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        if (defaultObstacle == null)
        {
            Debug.LogError("ObstacleSpawner: 无法创建默认障碍物！");
            return;
        }
        
        defaultObstacle.name = "DefaultObstacle";
        defaultObstacle.transform.localScale = Vector3.one * 5f; // 增大到5米直径，更容易看到
        
        // 设置材质颜色（醒目的红色）
        Renderer renderer = defaultObstacle.GetComponent<Renderer>();
        if (renderer != null)
        {
            // 使用 URP Lit shader 而不是 Standard
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null)
            {
                litShader = Shader.Find("Standard");
            }
            
            Material mat = new Material(litShader);
            mat.color = Color.red; // 纯红色，更醒目
            renderer.material = mat;
            
            // 确保障碍物可见
            renderer.enabled = true;
            
            Debug.Log($"✅ 默认障碍物材质已设置: 颜色 = {mat.color}, Shader = {mat.shader.name}");
        }
        else
        {
            Debug.LogError("❌ 默认障碍物没有 Renderer 组件！");
        }
        
        // 移除默认碰撞器，添加触发器（使用 Destroy 而不是 DestroyImmediate）
        Collider oldCollider = defaultObstacle.GetComponent<Collider>();
        if (oldCollider != null)
        {
            // 在运行时使用 Destroy，在编辑器中才用 DestroyImmediate
            if (Application.isPlaying)
            {
                Destroy(oldCollider);
            }
            else
            {
                DestroyImmediate(oldCollider);
            }
        }
        
        SphereCollider trigger = defaultObstacle.AddComponent<SphereCollider>();
        if (trigger != null)
        {
            trigger.isTrigger = true;
            trigger.radius = 1.5f; // 半径与缩放匹配
        }
        
        obstaclePrefab = defaultObstacle;
        
        Debug.LogWarning("ObstacleSpawner: 未指定障碍物预制体，已创建默认球体障碍物");
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 设置生成开关
    /// </summary>
    public void SetSpawningEnabled(bool enabled)
    {
        enableSpawning = enabled;
    }
    #endregion
}

