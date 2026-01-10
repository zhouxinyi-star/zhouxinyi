using UnityEngine;

/// <summary>
/// 障碍物移动脚本 - 让障碍物向下移动
/// </summary>
public class ObstacleMovement : MonoBehaviour
{
    private float speed = 5f;
    private bool isMoving = true;
    private bool relativeToPlayer = false;
    private Transform playerTransform = null;
    private Vector3 initialOffset = Vector3.zero;  // 初始时相对于玩家的偏移

    private void Awake()
    {
        // 确保组件已正确初始化
        if (!this.enabled || !this.gameObject.activeInHierarchy)
        {
            enabled = false;
        }
    }

    private void Update()
    {
        // 安全检查
        if (!this.enabled || !this.gameObject.activeInHierarchy) return;
        if (!isMoving) return;
        
        if (relativeToPlayer && playerTransform != null)
        {
            // 相对于玩家移动：障碍物以比玩家更快的速度向下移动，确保障碍物能追上玩家
            // 障碍物的总速度 = 自身速度（向下）+ 玩家下降速度
            // 这样障碍物会以比玩家更快的速度下降，能追上玩家
            float playerVelocity = 0f;
            if (PhysicsManager.Instance != null)
            {
                playerVelocity = PhysicsManager.Instance.CurrentVelocity;
            }
            
            // 障碍物相对于玩家的移动速度 = 障碍物速度（向下）
            // 但障碍物在世界坐标中的实际速度 = 障碍物速度 + 玩家下降速度
            float totalSpeed = speed + playerVelocity;
            
            // 向下移动（世界坐标）
            transform.Translate(Vector3.down * totalSpeed * Time.deltaTime, Space.World);
        }
        else
        {
            // 绝对世界坐标移动：直接向下移动
            transform.Translate(Vector3.down * speed * Time.deltaTime, Space.World);
        }
        
        // 如果掉到地面以下，销毁
        if (transform.position.y < -100f)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 设置移动速度
    /// </summary>
    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    /// <summary>
    /// 停止移动
    /// </summary>
    public void StopMoving()
    {
        isMoving = false;
    }
    
    /// <summary>
    /// 设置是否相对于玩家移动
    /// </summary>
    public void SetRelativeToPlayer(bool relative, Transform player)
    {
        relativeToPlayer = relative;
        playerTransform = player;
        
        // 如果设置为相对于玩家，记录初始偏移
        if (relative && player != null)
        {
            initialOffset = transform.position - player.position;
        }
    }
}

