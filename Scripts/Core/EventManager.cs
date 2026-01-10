using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局事件管理器 - 实现发布-订阅模式的事件系统
/// </summary>
public class EventManager : MonoBehaviour
{
    #region Singleton

    private static EventManager _instance;

    public static EventManager Instance
    {
        get
        {
            // 简化：仅返回实例，不再负责创建。初始化由Awake()保证。
            return _instance;
        }
    }

    private void Awake()
    {
        // 1. 执行单例检查，销毁重复项
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"EventManager: 发现重复实例，销毁 {gameObject.name}。");
            Destroy(gameObject);
            return;
        }

        // 2. 将此对象设为单例实例
        _instance = this;
        
        // 3. 关键一步：确保没有父对象，并设置为永久存在
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        
        Debug.Log($"EventManager: 单例初始化完成。位于独立GameObject: {gameObject.name}");
        
        // 4. (可选) 你原有的其他初始化代码可以保留在这里
        // InitializeEventDictionaries();
    }

    private void OnDestroy()
    {
        // 只有当销毁的是当前有效实例时，才清理静态引用
        if (_instance == this)
        {
            _instance = null;
            ClearAllListeners();
            Debug.LogWarning("EventManager: 实例被销毁，已清理静态引用。");
        }
    }

    #endregion

    #region 事件类型定义
    // 预定义事件名称常量
    public const string EVENT_PHYSICS_UPDATE = "physics_update";
    public const string EVENT_BALLOON_INFLATED = "balloon_inflated";
    public const string EVENT_HELIUM_TANK_EMPTY = "helium_tank_empty";
    public const string EVENT_WARNING_TRIGGERED = "warning_triggered";
    public const string EVENT_DIALOGUE_REQUESTED = "dialogue_requested";
    public const string EVENT_ACHIEVEMENT_UNLOCKED = "achievement_unlocked";
    public const string EVENT_GAME_OVER = "game_over";
    #endregion

    #region 事件存储
    // 存储不带参数的事件监听器
    private Dictionary<string, List<Action>> _eventListeners = new Dictionary<string, List<Action>>();
    
    // 存储带参数的事件监听器
    private Dictionary<string, List<Action<object[]>>> _eventListenersWithArgs = new Dictionary<string, List<Action<object[]>>>();
    
    // 用于线程安全的锁对象（Unity主要在主线程，但为了安全起见）
    private readonly object _lockObject = new object();
    
    // 用于避免重复注册的集合
    private HashSet<string> _registeredListeners = new HashSet<string>();
    #endregion

    #region 公共方法 - 不带参数的事件

    /// <summary>
    /// 添加事件监听器（不带参数）
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <param name="callback">回调函数</param>
    public void AddListener(string eventName, Action callback)
    {
        if (string.IsNullOrEmpty(eventName))
        {
            Debug.LogError("EventManager: 事件名称不能为空！");
            return;
        }

        if (callback == null)
        {
            Debug.LogError($"EventManager: 事件 {eventName} 的回调函数不能为空！");
            return;
        }

        lock (_lockObject)
        {
            // 创建唯一标识符，避免重复注册
            string listenerId = $"{eventName}_{callback.GetHashCode()}";
            
            if (_registeredListeners.Contains(listenerId))
            {
                Debug.LogWarning($"EventManager: 监听器 {listenerId} 已存在，跳过重复注册");
                return;
            }

            // 如果事件不存在，创建新的列表
            if (!_eventListeners.ContainsKey(eventName))
            {
                _eventListeners[eventName] = new List<Action>();
            }

            // 添加监听器
            _eventListeners[eventName].Add(callback);
            _registeredListeners.Add(listenerId);
            
            Debug.Log($"EventManager: 已添加监听器到事件 {eventName}");
        }
    }

    /// <summary>
    /// 移除事件监听器（不带参数）
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <param name="callback">回调函数</param>
    public void RemoveListener(string eventName, Action callback)
    {
        if (string.IsNullOrEmpty(eventName) || callback == null)
        {
            return;
        }

        lock (_lockObject)
        {
            string listenerId = $"{eventName}_{callback.GetHashCode()}";
            
            if (_eventListeners.ContainsKey(eventName))
            {
                _eventListeners[eventName].Remove(callback);
                _registeredListeners.Remove(listenerId);
                
                // 如果列表为空，移除事件
                if (_eventListeners[eventName].Count == 0)
                {
                    _eventListeners.Remove(eventName);
                }
                
                Debug.Log($"EventManager: 已移除事件 {eventName} 的监听器");
            }
        }
    }

    /// <summary>
    /// 触发事件（不带参数）
    /// </summary>
    /// <param name="eventName">事件名称</param>
    public void TriggerEvent(string eventName)
    {
        if (string.IsNullOrEmpty(eventName))
        {
            Debug.LogError("EventManager: 事件名称不能为空！");
            return;
        }

        List<Action> listeners = null;

        lock (_lockObject)
        {
            if (_eventListeners.ContainsKey(eventName))
            {
                // 创建副本以避免在迭代时修改集合
                listeners = new List<Action>(_eventListeners[eventName]);
            }
        }

        // 在锁外执行回调，避免死锁
        if (listeners != null && listeners.Count > 0)
        {
            foreach (var listener in listeners)
            {
                try
                {
                    listener?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"EventManager: 触发事件 {eventName} 时发生错误: {e.Message}");
                }
            }
        }
    }
    #endregion

    #region 公共方法 - 带参数的事件

    /// <summary>
    /// 添加事件监听器（带参数）
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <param name="callback">回调函数</param>
    public void AddListener(string eventName, Action<object[]> callback)
    {
        if (string.IsNullOrEmpty(eventName))
        {
            Debug.LogError("EventManager: 事件名称不能为空！");
            return;
        }

        if (callback == null)
        {
            Debug.LogError($"EventManager: 事件 {eventName} 的回调函数不能为空！");
            return;
        }

        lock (_lockObject)
        {
            string listenerId = $"{eventName}_args_{callback.GetHashCode()}";
            
            if (_registeredListeners.Contains(listenerId))
            {
                Debug.LogWarning($"EventManager: 监听器 {listenerId} 已存在，跳过重复注册");
                return;
            }

            if (!_eventListenersWithArgs.ContainsKey(eventName))
            {
                _eventListenersWithArgs[eventName] = new List<Action<object[]>>();
            }

            _eventListenersWithArgs[eventName].Add(callback);
            _registeredListeners.Add(listenerId);
            
            Debug.Log($"EventManager: 已添加带参数监听器到事件 {eventName}");
        }
    }

    /// <summary>
    /// 移除事件监听器（带参数）
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <param name="callback">回调函数</param>
    public void RemoveListener(string eventName, Action<object[]> callback)
    {
        if (string.IsNullOrEmpty(eventName) || callback == null)
        {
            return;
        }

        lock (_lockObject)
        {
            string listenerId = $"{eventName}_args_{callback.GetHashCode()}";
            
            if (_eventListenersWithArgs.ContainsKey(eventName))
            {
                _eventListenersWithArgs[eventName].Remove(callback);
                _registeredListeners.Remove(listenerId);
                
                if (_eventListenersWithArgs[eventName].Count == 0)
                {
                    _eventListenersWithArgs.Remove(eventName);
                }
                
                Debug.Log($"EventManager: 已移除事件 {eventName} 的带参数监听器");
            }
        }
    }

    /// <summary>
    /// 触发事件（带参数）
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <param name="args">事件参数</param>
    public void TriggerEvent(string eventName, params object[] args)
    {
        if (string.IsNullOrEmpty(eventName))
        {
            Debug.LogError("EventManager: 事件名称不能为空！");
            return;
        }

        List<Action<object[]>> listeners = null;

        lock (_lockObject)
        {
            if (_eventListenersWithArgs.ContainsKey(eventName))
            {
                listeners = new List<Action<object[]>>(_eventListenersWithArgs[eventName]);
            }
        }

        // 在锁外执行回调
        if (listeners != null && listeners.Count > 0)
        {
            foreach (var listener in listeners)
            {
                try
                {
                    listener?.Invoke(args);
                }
                catch (Exception e)
                {
                    Debug.LogError($"EventManager: 触发事件 {eventName} 时发生错误: {e.Message}");
                }
            }
        }

        // 同时触发不带参数的事件（如果有的话）
        TriggerEvent(eventName);
    }
    #endregion

    #region 辅助方法

    /// <summary>
    /// 清除所有监听器
    /// </summary>
    public void ClearAllListeners()
    {
        lock (_lockObject)
        {
            _eventListeners.Clear();
            _eventListenersWithArgs.Clear();
            _registeredListeners.Clear();
            Debug.Log("EventManager: 已清除所有监听器");
        }
    }

    /// <summary>
    /// 清除指定事件的所有监听器
    /// </summary>
    /// <param name="eventName">事件名称</param>
    public void ClearEventListeners(string eventName)
    {
        if (string.IsNullOrEmpty(eventName))
        {
            return;
        }

        lock (_lockObject)
        {
            if (_eventListeners.ContainsKey(eventName))
            {
                foreach (var listener in _eventListeners[eventName])
                {
                    string listenerId = $"{eventName}_{listener.GetHashCode()}";
                    _registeredListeners.Remove(listenerId);
                }
                _eventListeners.Remove(eventName);
            }

            if (_eventListenersWithArgs.ContainsKey(eventName))
            {
                foreach (var listener in _eventListenersWithArgs[eventName])
                {
                    string listenerId = $"{eventName}_args_{listener.GetHashCode()}";
                    _registeredListeners.Remove(listenerId);
                }
                _eventListenersWithArgs.Remove(eventName);
            }

            Debug.Log($"EventManager: 已清除事件 {eventName} 的所有监听器");
        }
    }

    /// <summary>
    /// 获取事件监听器数量（调试用）
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <returns>监听器数量</returns>
    public int GetListenerCount(string eventName)
    {
        lock (_lockObject)
        {
            int count = 0;
            if (_eventListeners.ContainsKey(eventName))
            {
                count += _eventListeners[eventName].Count;
            }
            if (_eventListenersWithArgs.ContainsKey(eventName))
            {
                count += _eventListenersWithArgs[eventName].Count;
            }
            return count;
        }
    }

    /// <summary>
    /// 检查事件是否有监听器
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <returns>是否有监听器</returns>
    public bool HasListeners(string eventName)
    {
        return GetListenerCount(eventName) > 0;
    }
    #endregion

    #region Unity生命周期
    
    private void Start()
    {
        // 只在运行时执行
        if (!Application.isPlaying)
        {
            return;
        }
        
        // 双重保险：确保 DontDestroyOnLoad 被设置，并且没有父对象
        if (_instance == this)
        {
            // 再次检查并移除父对象（防止在 Awake 和 Start 之间被重新设置父对象）
            if (transform.parent != null)
            {
                Debug.LogWarning($"EventManager: Start() 中检测到父对象 {transform.parent.name}，将其移除");
                transform.SetParent(null);
            }
            
            DontDestroyOnLoad(gameObject);
            Debug.Log("EventManager: Start() 中确认 DontDestroyOnLoad 已设置，确认无父对象");
        }
    }
    
    private void OnEnable()
    {
        // 只在运行时执行
        if (!Application.isPlaying)
        {
            // 编辑器中不做任何操作，避免 Inspector 访问时出错
            return;
        }
        
        // 添加空值检查，防止访问已销毁的对象
        if (this == null || gameObject == null || transform == null)
        {
            return;
        }
        
        // 每次启用时检查并确保设置正确
        try
        {
            if (_instance == this)
            {
                if (transform != null && transform.parent != null)
                {
                    Debug.LogWarning($"EventManager: OnEnable() 中检测到父对象 {transform.parent.name}，将其移除");
                    transform.SetParent(null);
                }
                if (gameObject != null)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"EventManager: OnEnable() 中发生错误: {e.Message}");
        }
    }
    
    private void OnDisable()
    {
        // 只在运行时执行
        if (!Application.isPlaying)
        {
            return;
        }
        
        // 如果 EventManager 被禁用（不应该发生），记录警告
        // 但只在真正被意外禁用时警告，场景切换时的临时禁用不算
        try
        {
            if (_instance == this && !gameObject.scene.isLoaded)
            {
                // 场景未加载时禁用是正常的（场景切换），不警告
                return;
            }
            
            if (_instance == this)
            {
                Debug.LogWarning("EventManager: 警告！EventManager 被禁用！这可能导致事件系统无法正常工作。");
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"EventManager: OnDisable() 中发生错误: {e.Message}");
        }
    }
    
    /// <summary>
    /// 编辑器中的 Reset 方法，用于在 Inspector 中点击 Reset 时调用
    /// </summary>
    private void Reset()
    {
        // 在编辑器中重置时，确保不会执行运行时逻辑
        // 这个方法只在编辑器中调用，不会在运行时调用
    }
    
    private void OnApplicationQuit()
    {
        // 应用程序退出时清理
        if (_instance == this)
        {
            ClearAllListeners();
            Debug.Log("EventManager: 应用程序退出，清理完成");
        }
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        // 当应用失去/获得焦点时，确保实例仍然有效
        if (hasFocus && _instance == null && this != null)
        {
            Debug.LogWarning("EventManager: 检测到实例丢失，尝试恢复...");
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
    #endregion
}

