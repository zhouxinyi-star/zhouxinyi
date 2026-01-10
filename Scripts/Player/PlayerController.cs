using UnityEngine;
using System.Collections;

/// <summary>
/// 第一人称玩家控制器 - 处理玩家输入、视角控制和游戏交互
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    #region 组件引用
    [Header("组件引用")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private Transform balloonTransform;  // 气球模型Transform
    [SerializeField] private GameObject heliumTankPrefab;  // 氦气罐预制体
    [SerializeField] private Transform handTransform;      // 手部位置（用于扔罐子）
    #endregion

    #region 视角控制参数
    [Header("视角控制")]
    [SerializeField] private float mouseSensitivity = 2.0f;
    [SerializeField] private float verticalRotationLimit = 80f;  // 上下视角限制（度）
    private float verticalRotation = 0f;  // 当前垂直旋转角度
    #endregion

    #region 移动参数
    [Header("移动参数")]
    [SerializeField] private float moveSpeed = 8f;  // 提高移动速度，让空中移动更灵活
    [SerializeField] private float airControl = 0.8f;  // 大幅提高空中控制系数，让玩家在空中也能灵活移动
    private Vector3 moveDirection = Vector3.zero;
    #endregion

    #region 充气控制
    [Header("充气控制")]
    private bool isInflating = false;      // 是否正在充气
    private bool isDeflating = false;       // 是否正在放气
    #endregion

    #region 状态标志
    private bool isPaused = false;
    private bool isFirstPerson = true;
    private int heliumTanksInInventory = 0;  // 背包中的氦气罐数量
    private bool isSafeLanding = false;      // 是否是安全着陆
    #endregion

    #region Unity生命周期
    private void Awake()
    {
        Debug.Log("PlayerController Awake 被调用");
        
        // 自动获取组件
        if (characterController == null)
            characterController = GetComponent<CharacterController>();
        
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();
        
        if (cameraTransform == null && playerCamera != null)
            cameraTransform = playerCamera.transform;
        
        if (playerAnimator == null)
            playerAnimator = GetComponentInChildren<Animator>();
        
        // 锁定鼠标光标（延迟执行，确保在游戏开始后）
        StartCoroutine(LockCursorDelayed());
    }
    
    /// <summary>
    /// 延迟锁定鼠标，避免在编辑器模式下立即锁定
    /// </summary>
    private System.Collections.IEnumerator LockCursorDelayed()
    {
        yield return new WaitForSeconds(0.1f);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Debug.Log("鼠标已锁定");
    }

    private void Start()
    {
        // 初始化气球大小
        if (balloonTransform != null)
        {
            UpdateBalloonVisual();
        }

        // 根据物理高度初始化玩家位置，让玩家起始就在 currentHeight 高度
        if (PhysicsManager.Instance != null)
        {
            Vector3 pos = transform.position;
            pos.y = PhysicsManager.Instance.CurrentHeight;   // 1 个 Unity 单位 ≈ 1 米
            transform.position = pos;
        }
    }

    private void Update()
    {
        // 调试：每60帧打印一次（避免刷屏）
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"PlayerController运行中 - isPaused: {isPaused}, Time.timeScale: {Time.timeScale}");
        }

        // 如果游戏暂停，不处理输入（暂停状态由外部控制，例如 PauseMenu）
        if (isPaused)
        {
            return;
        }

        // 处理视角控制
        HandleMouseLook();

        // 处理移动输入
        HandleMovement();

        // 处理交互输入
        HandleInteractionInput();

        // 更新充气和放气（每帧执行）
        UpdateInflation();
        UpdateDeflation();

        // 更新气球视觉
        UpdateBalloonVisual();
    }

    /// <summary>
    /// 在物理更新后，同步玩家的实际高度到 PhysicsManager 的 currentHeight
    /// 但如果已经安全着陆，不再同步高度，允许玩家在地面自由移动
    /// </summary>
    private void LateUpdate()
    {
        if (PhysicsManager.Instance != null)
        {
            // 如果已经着陆，检查是否是安全着陆
            if (PhysicsManager.Instance.HasLanded)
            {
                // 使用缓存的着陆结果，避免每帧查找
                if (isSafeLanding)
                {
                    // 只确保玩家不会掉到地面以下
                    if (transform.position.y < 0f)
                    {
                        Vector3 safePos = transform.position;
                        safePos.y = 0f;
                        transform.position = safePos;
                    }
                    return;  // 不再同步高度
                }
            }
            
            // 未着陆或非安全着陆：正常同步高度
            Vector3 pos = transform.position;
            float targetHeight = PhysicsManager.Instance.CurrentHeight;
            
            // 确保高度不会低于地面
            if (targetHeight < 0f)
            {
                targetHeight = 0f;
            }
            
            pos.y = targetHeight;
            transform.position = pos;
            
            // 如果高度已经到0或接近0，且还没有判定着陆，尝试触发着陆检测
            if (!PhysicsManager.Instance.HasLanded && targetHeight <= 1f)
            {
                float currentVel = PhysicsManager.Instance.CurrentVelocity;
                
                // 检查CharacterController是否真的在地面上，或者速度很小
                bool shouldTrigger = false;
                if (characterController != null && characterController.isGrounded)
                {
                    shouldTrigger = true;
                    Debug.Log($"PlayerController: 检测到高度接近0且CharacterController已接地，触发着陆判定");
                }
                else if (currentVel < 5f)  // 即使没有接地，如果速度很小也判定
                {
                    shouldTrigger = true;
                    Debug.Log($"PlayerController: 检测到高度接近0且速度很小 ({currentVel:F2}m/s)，触发着陆判定");
                }
                
                if (shouldTrigger)
                {
                    float landingVelocity = Mathf.Max(0.1f, currentVel);
                    PhysicsManager.Instance.CheckLanding(landingVelocity);
                    CheckLandingOutcome(landingVelocity);
                }
            }
        }
    }
    #endregion

    #region 输入处理

    /// <summary>
    /// 处理鼠标视角控制
    /// </summary>
    private void HandleMouseLook()
    {
        if (cameraTransform == null) return;

        // 获取鼠标输入
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // 水平旋转（Y轴）
        transform.Rotate(0, mouseX, 0);

        // 垂直旋转（X轴），限制在-80到80度
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -verticalRotationLimit, verticalRotationLimit);
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
    }

    /// <summary>
    /// 处理移动输入
    /// </summary>
    private void HandleMovement()
    {
        // 获取输入
        float horizontal = Input.GetAxis("Horizontal");  // A/D
        float vertical = Input.GetAxis("Vertical");        // W/S

        // 如果没有输入，直接返回
        if (Mathf.Abs(horizontal) < 0.01f && Mathf.Abs(vertical) < 0.01f)
        {
            return;
        }

        // 计算移动方向（相对于玩家朝向）
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        // 在空中时使用空中控制系数
        float controlFactor = (characterController != null && characterController.isGrounded) ? 1f : airControl;
        moveDirection = (forward * vertical + right * horizontal) * moveSpeed * controlFactor;

        // 直接移动 Transform（适用于空中移动）
        // 注意：Y 轴移动会在 LateUpdate 中被 PhysicsManager 的高度覆盖
        Vector3 horizontalMovement = new Vector3(moveDirection.x, 0, moveDirection.z) * Time.deltaTime;
        transform.position += horizontalMovement;
        
        // 如果使用 CharacterController，也更新它（用于碰撞检测）
        if (characterController != null)
        {
            characterController.Move(horizontalMovement);
        }
    }

    /// <summary>
    /// 处理交互输入
    /// </summary>
    private void HandleInteractionInput()
    {
        // 空格键：充气（按住充气，松开停止）
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartInflation();
        }
        else if (Input.GetKeyUp(KeyCode.Space))
        {
            StopInflation();
        }

        // Shift键：放气（按住放气，松开停止）
        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
        {
            StartDeflation();
        }
        else if (Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift))
        {
            StopDeflation();
        }

        // F键：扔出空氦气罐（已废弃，保留用于兼容）
        if (Input.GetKeyDown(KeyCode.F))
        {
            DetachEmptyTank();
        }

        // 鼠标右键：紧急放气（打爆10%的气球）
        if (Input.GetMouseButtonDown(1))  // 右键
        {
            PopBalloon(0.1f);
        }

        // V键：切换视角
        if (Input.GetKeyDown(KeyCode.V))
        {
            ToggleViewMode();
        }
    }

    #endregion

    #region 充气控制

    /// <summary>
    /// 开始充气
    /// </summary>
    private void StartInflation()
    {
        // 如果正在放气，先停止放气
        if (isDeflating)
        {
            StopDeflation();
        }

        isInflating = true;

        // 播放充气动画
        if (playerAnimator != null)
        {
            // 检查参数是否存在，避免警告
            if (HasAnimatorParameter("IsInflating"))
                playerAnimator.SetBool("IsInflating", true);
            if (HasAnimatorParameter("IsDeflating"))
                playerAnimator.SetBool("IsDeflating", false);
        }

        Debug.Log("开始充气...");
    }

    /// <summary>
    /// 停止充气
    /// </summary>
    private void StopInflation()
    {
        isInflating = false;

        // 停止充气动画
        if (playerAnimator != null)
        {
            if (HasAnimatorParameter("IsInflating"))
                playerAnimator.SetBool("IsInflating", false);
        }

        Debug.Log("停止充气");
    }

    /// <summary>
    /// 更新充气逻辑（每帧调用，连续充气）
    /// </summary>
    private void UpdateInflation()
    {
        if (!isInflating) return;

        if (PhysicsManager.Instance != null)
        {
            // 连续充气，每帧调用
            bool success = PhysicsManager.Instance.InflateBalloon(Time.deltaTime);
            
            if (success)
            {
                // 每0.1秒触发一次充气事件，避免频繁触发
                if (Time.frameCount % 6 == 0)  // 假设60fps，每6帧约0.1秒
                {
                    if (EventManager.Instance != null)
                    {
                        EventManager.Instance.TriggerEvent(EventManager.EVENT_BALLOON_INFLATED);
                    }
                }
            }
            else
            {
                // 氦气耗尽，自动停止充气
                StopInflation();
            }
        }
    }

    /// <summary>
    /// 开始放气
    /// </summary>
    private void StartDeflation()
    {
        // 如果正在充气，先停止充气
        if (isInflating)
        {
            StopInflation();
        }

        isDeflating = true;

        // 播放放气动画
        if (playerAnimator != null)
        {
            if (HasAnimatorParameter("IsDeflating"))
                playerAnimator.SetBool("IsDeflating", true);
            if (HasAnimatorParameter("IsInflating"))
                playerAnimator.SetBool("IsInflating", false);
        }

        Debug.Log("开始放气...");
    }

    /// <summary>
    /// 停止放气
    /// </summary>
    private void StopDeflation()
    {
        isDeflating = false;

        // 停止放气动画
        if (playerAnimator != null)
        {
            if (HasAnimatorParameter("IsDeflating"))
                playerAnimator.SetBool("IsDeflating", false);
        }

        Debug.Log("停止放气");
    }

    /// <summary>
    /// 更新放气逻辑（每帧调用，连续放气）
    /// </summary>
    private void UpdateDeflation()
    {
        if (!isDeflating) return;

        if (PhysicsManager.Instance != null)
        {
            // 连续放气，每帧调用
            bool success = PhysicsManager.Instance.DeflateBalloon(Time.deltaTime);
            
            if (!success)
            {
                // 气球已达到最小体积，自动停止放气
                StopDeflation();
            }
        }
    }
    #endregion

    #region 交互操作

    /// <summary>
    /// 扔掉空氦气罐
    /// </summary>
    private void DetachEmptyTank()
    {
        if (PhysicsManager.Instance != null)
        {
            PhysicsManager.Instance.DetachEmptyTank();

            // 播放扔罐子动画
            if (playerAnimator != null && HasAnimatorParameter("ThrowTank"))
            {
                playerAnimator.SetTrigger("ThrowTank");
            }

            // 实例化氦气罐模型（可选）
            if (heliumTankPrefab != null && handTransform != null)
            {
                GameObject tank = Instantiate(heliumTankPrefab, handTransform.position, handTransform.rotation);
                Rigidbody rb = tank.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddForce(cameraTransform.forward * 5f, ForceMode.Impulse);
                }
            }

            Debug.Log("扔掉空氦气罐");
        }
    }

    /// <summary>
    /// 打爆部分气球
    /// </summary>
    /// <param name="percentage">打爆的百分比</param>
    private void PopBalloon(float percentage)
    {
        if (PhysicsManager.Instance != null)
        {
            PhysicsManager.Instance.PopBalloon(percentage);

            // 播放打爆动画
            if (playerAnimator != null && HasAnimatorParameter("PopBalloon"))
            {
                playerAnimator.SetTrigger("PopBalloon");
            }

            // 触发警告事件
            if (EventManager.Instance != null)
            {
                EventManager.Instance.TriggerEvent(EventManager.EVENT_WARNING_TRIGGERED, "气球被打爆");
            }

            Debug.Log($"打爆气球 {percentage * 100}%");
        }
    }
    #endregion

    #region 游戏控制

    /// <summary>
    /// 切换视角模式
    /// </summary>
    private void ToggleViewMode()
    {
        isFirstPerson = !isFirstPerson;

        if (playerCamera != null)
        {
            // 这里可以实现第一人称/第三人称切换
            // 目前简单实现：调整相机位置
            if (isFirstPerson)
            {
                // 第一人称：相机在头部位置
                cameraTransform.localPosition = new Vector3(0, 1.6f, 0);
            }
            else
            {
                // 第三人称：相机在背后
                cameraTransform.localPosition = new Vector3(0, 1.6f, -3f);
            }
        }

        Debug.Log($"切换到{(isFirstPerson ? "第一人称" : "第三人称")}视角");
    }
    #endregion

    #region 视觉更新

    /// <summary>
    /// 更新气球视觉大小
    /// </summary>
    private void UpdateBalloonVisual()
    {
        if (balloonTransform == null || PhysicsManager.Instance == null) return;

        // 根据气球体积更新大小
        float volume = PhysicsManager.Instance.BalloonVolume;
        
        // 假设球体体积 V = (4/3)πr³，所以 r = ∛(3V/4π)
        // 缩放比例基于体积的立方根
        float baseVolume = 0.5f;  // 初始体积
        float scale = Mathf.Pow(volume / baseVolume, 1f / 3f);
        
        balloonTransform.localScale = Vector3.one * scale;
    }
    #endregion

    #region 碰撞检测

    private void OnTriggerEnter(Collider other)
    {
        // 检测障碍物碰撞
        if (other.CompareTag("Obstacle"))
        {
            HandleObstacleCollision(other.gameObject);
        }

        // 检测氦气罐拾取
        if (other.CompareTag("HeliumTank"))
        {
            PickupHeliumTank(other.gameObject);
        }

        // 检测触发器区域
        if (other.CompareTag("TriggerZone"))
        {
            HandleTriggerZone(other);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 检测着陆（用于 Rigidbody）
        if (collision.gameObject.CompareTag("Ground"))
        {
            HandleLanding(collision);
        }
    }

    /// <summary>
    /// CharacterController 碰撞检测（重要：CharacterController 使用这个方法而不是 OnCollisionEnter）
    /// </summary>
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // 检测着陆
        if (hit.gameObject.CompareTag("Ground"))
        {
            Debug.Log($"CharacterController 碰撞地面: {hit.gameObject.name}");
            HandleCharacterControllerLanding(hit);
        }
    }

    /// <summary>
    /// 处理 CharacterController 的着陆
    /// </summary>
    private void HandleCharacterControllerLanding(ControllerColliderHit hit)
    {
        if (PhysicsManager.Instance != null && !PhysicsManager.Instance.HasLanded)
        {
            // 检查高度：只有真正接近地面时才判定着陆
            float currentHeight = PhysicsManager.Instance.CurrentHeight;
            const float MAX_LANDING_HEIGHT = 5f;  // 最大允许着陆高度（米）
            
            if (currentHeight > MAX_LANDING_HEIGHT)
            {
                Debug.Log($"CharacterController 碰撞检测: 高度过高 ({currentHeight:F2}m > {MAX_LANDING_HEIGHT}m)，不是真正的着陆，忽略");
                return;  // 高度太高，不判定为着陆
            }
            
            // 使用 CharacterController 的速度
            float velocity = 0f;
            
            if (characterController != null)
            {
                // 获取 CharacterController 的垂直速度（向下为正）
                velocity = Mathf.Abs(characterController.velocity.y);
            }
            
            // 如果没有速度，使用物理管理器的当前速度
            if (velocity < 0.1f)
            {
                velocity = PhysicsManager.Instance.CurrentVelocity;
            }
            
            Debug.Log($"CharacterController 着陆检测: 速度 = {velocity:F2} m/s, 高度 = {currentHeight:F2} m, 位置 = {transform.position}");
            
            // 传递速度给物理管理器（触发着陆判定）
            PhysicsManager.Instance.CheckLanding(velocity);
            
            // 根据着陆结果决定是否允许继续移动
            CheckLandingOutcome(velocity);
            
            Debug.Log($"PlayerController: CharacterController 着陆检测完成，最终速度: {velocity:F2} m/s");
        }
    }

    /// <summary>
    /// 拾取氦气罐
    /// </summary>
    private void PickupHeliumTank(GameObject tank)
    {
        heliumTanksInInventory++;
        Destroy(tank);
        Debug.Log($"拾取氦气罐！当前拥有: {heliumTanksInInventory} 个");
    }

    /// <summary>
    /// 处理触发器区域
    /// </summary>
    private void HandleTriggerZone(Collider trigger)
    {
        // 这里可以触发对话、成就等
        Debug.Log($"进入触发器区域: {trigger.name}");
        
        if (EventManager.Instance != null)
        {
            EventManager.Instance.TriggerEvent(EventManager.EVENT_DIALOGUE_REQUESTED, trigger.name);
        }
    }

    /// <summary>
    /// 处理障碍物碰撞
    /// </summary>
    private void HandleObstacleCollision(GameObject obstacle)
    {
        if (isPaused) return; // 如果已暂停（已着陆），忽略碰撞
        
        Debug.Log("⚠️ 撞到障碍物！");
        
        // 惩罚效果：增加下降速度（模拟碰撞导致失控）
        if (PhysicsManager.Instance != null)
        {
            // 注意：我们不能直接修改 PhysicsManager 的 currentVelocity（它是私有的）
            // 但可以通过打爆部分气球来模拟失控，减少浮力从而增加下降速度
            PhysicsManager.Instance.PopBalloon(0.1f); // 打爆10%的气球，减少浮力
            
            Debug.Log($"障碍物碰撞惩罚：气球体积减少10%，下降速度增加");
        }
        
        // 触发警告事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.TriggerEvent(EventManager.EVENT_WARNING_TRIGGERED, "撞到障碍物！");
        }
        
        // 销毁障碍物（避免重复碰撞）
        Destroy(obstacle);
        
        // 可以添加屏幕震动、音效等效果
        StartCoroutine(ScreenShake(0.2f));
    }

    /// <summary>
    /// 屏幕震动效果（简单实现）
    /// </summary>
    private System.Collections.IEnumerator ScreenShake(float duration)
    {
        if (cameraTransform == null) yield break;
        
        Vector3 originalPos = cameraTransform.localPosition;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float x = Random.Range(-0.1f, 0.1f);
            float y = Random.Range(-0.1f, 0.1f);
            cameraTransform.localPosition = originalPos + new Vector3(x, y, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        cameraTransform.localPosition = originalPos;
    }

    /// <summary>
    /// 处理着陆
    /// </summary>
    private void HandleLanding(Collision collision)
    {
        if (PhysicsManager.Instance != null && !PhysicsManager.Instance.HasLanded)
        {
            // 检查高度：只有真正接近地面时才判定着陆
            float currentHeight = PhysicsManager.Instance.CurrentHeight;
            const float MAX_LANDING_HEIGHT = 5f;  // 最大允许着陆高度（米）
            
            if (currentHeight > MAX_LANDING_HEIGHT)
            {
                Debug.Log($"OnCollisionEnter 碰撞检测: 高度过高 ({currentHeight:F2}m > {MAX_LANDING_HEIGHT}m)，不是真正的着陆，忽略");
                return;  // 高度太高，不判定为着陆
            }
            
            // 从碰撞中获取实际着陆速度
            // 使用相对速度的垂直分量
            float collisionVelocity = 0f;
            
            if (collision.relativeVelocity.magnitude > 0.1f)
            {
                // 获取碰撞相对速度的垂直分量（向下为正）
                Vector3 relativeVelocity = collision.relativeVelocity;
                collisionVelocity = Mathf.Abs(relativeVelocity.y);
            }
            else
            {
                // 如果没有相对速度，使用物理管理器的当前速度
                collisionVelocity = PhysicsManager.Instance.CurrentVelocity;
            }
            
            Debug.Log($"PlayerController: 检测到地面碰撞，高度 = {currentHeight:F2} m, 相对速度: {collision.relativeVelocity.magnitude:F2} m/s, 垂直分量: {collisionVelocity:F2} m/s");
            
            // 传递实际碰撞速度给物理管理器（触发着陆判定）
            PhysicsManager.Instance.CheckLanding(collisionVelocity);
            
            // 根据着陆结果决定是否允许继续移动
            CheckLandingOutcome(collisionVelocity);
            
            Debug.Log($"PlayerController: 着陆检测完成，最终速度: {collisionVelocity:F2} m/s");
        }
    }
    
    /// <summary>
    /// 检查着陆结果，决定是否允许继续移动或结束游戏
    /// </summary>
    private void CheckLandingOutcome(float landingVelocity)
    {
        // 查找 LandingOutcomeSystem
        LandingOutcomeSystem outcomeSystem = FindObjectOfType<LandingOutcomeSystem>();
        
        if (outcomeSystem != null)
        {
            // 等待一帧，让 LandingOutcomeSystem 处理完着陆事件
            StartCoroutine(ProcessLandingOutcomeDelayed(outcomeSystem, landingVelocity));
        }
        else
        {
            // 如果没有 LandingOutcomeSystem，使用简单判断
            // 速度 < 5 m/s 为安全着陆，允许继续移动
            if (landingVelocity < 5f)
            {
                Debug.Log($"✅ 安全着陆 ({landingVelocity:F2} m/s)，允许继续移动");
                // 不设置 isPaused，允许继续移动
            }
            else
            {
                Debug.Log($"❌ 着陆失败 ({landingVelocity:F2} m/s)，结束游戏");
                isPaused = true;
                // 可以在这里触发游戏结束UI
            }
        }
    }
    
    /// <summary>
    /// 延迟处理着陆结果（等待 LandingOutcomeSystem 处理完）
    /// </summary>
    private System.Collections.IEnumerator ProcessLandingOutcomeDelayed(LandingOutcomeSystem outcomeSystem, float velocity)
    {
        // 等待一帧，确保 LandingOutcomeSystem 已经处理完着陆事件
        yield return null;
        
        LandingOutcomeSystem.LandingOutcome outcome = outcomeSystem.GetCurrentOutcome();
        
        if (outcome == LandingOutcomeSystem.LandingOutcome.SafeLanding)
        {
            // 安全着陆：允许继续移动
            Debug.Log($"✅ 安全着陆 ({velocity:F2} m/s)，允许继续移动");
            isSafeLanding = true;  // 标记为安全着陆
            isPaused = false;      // 确保可以继续移动
            
            // 停止物理模拟的下降，但允许玩家在地面移动
            // PhysicsManager 已经停止了模拟，但我们可以继续更新玩家位置
        }
        else
        {
            // 受伤或坠毁：结束游戏，不允许移动
            Debug.Log($"❌ 着陆失败 ({velocity:F2} m/s)，结局: {outcome}，结束游戏");
            isSafeLanding = false;  // 标记为非安全着陆
            isPaused = true;
            
            // 触发游戏结束事件
            if (EventManager.Instance != null)
            {
                EventManager.Instance.TriggerEvent(EventManager.EVENT_GAME_OVER, velocity);
            }
        }
    }
    #endregion

    #region 公共方法

    /// <summary>
    /// 获取当前是否在充气
    /// </summary>
    public bool IsInflating => isInflating;

    /// <summary>
    /// 获取当前是否在放气
    /// </summary>
    public bool IsDeflating => isDeflating;

    /// <summary>
    /// 获取是否暂停
    /// </summary>
    public bool IsPaused => isPaused;

    /// <summary>
    /// 外部设置暂停状态（由 PauseMenu 控制 TimeScale/鼠标等）
    /// </summary>
    public void SetPaused(bool paused)
    {
        isPaused = paused;
    }

    /// <summary>
    /// 获取背包中的氦气罐数量
    /// </summary>
    public int HeliumTanksInInventory => heliumTanksInInventory;

    /// <summary>
    /// 设置鼠标灵敏度
    /// </summary>
    public void SetMouseSensitivity(float sensitivity)
    {
        mouseSensitivity = Mathf.Clamp(sensitivity, 0.1f, 10f);
    }
    #endregion

    #region 辅助方法
    /// <summary>
    /// 检查Animator参数是否存在
    /// </summary>
    /// <param name="parameterName">参数名称</param>
    /// <returns>参数是否存在</returns>
    private bool HasAnimatorParameter(string parameterName)
    {
        if (playerAnimator == null || !playerAnimator.isInitialized) return false;
        
        // 遍历所有参数，检查是否存在
        foreach (AnimatorControllerParameter param in playerAnimator.parameters)
        {
            if (param.name == parameterName)
            {
                return true;
            }
        }
        
        return false;
    }
    #endregion
}

