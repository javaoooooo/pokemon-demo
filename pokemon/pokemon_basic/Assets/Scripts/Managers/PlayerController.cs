using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using Mapbox.Unity.Utilities;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float mapResetDistance = 1000f; // 当玩家移动超过此距离时重新定位地图
    public float mapUpdateDistance = 50f; // 当玩家移动超过此距离时更新地图瓦片
    
    [Header("Map Update Settings")]
    public float mapUpdateCooldown = 2f; // 地图更新冷却时间
    
    [Header("Map References")]
    public AbstractMap map;
    
    
    private Vector2d initialMapCenter;
    private Vector3 mapCenterWorldPosition;
    private Vector3 lastMapUpdatePosition; // 上次地图更新时的位置
    private float lastMapUpdateTime; // 上次地图更新时间
    private bool isInitialized = false;
    
    void Start()
    {
        InitializePlayer();
    }
    
    void InitializePlayer()
    {
        // 查找地图引用
        if (map == null)
        {
            map = FindObjectOfType<AbstractMap>();
        }
        
        
        if (map != null)
        {
            // 记录初始地图中心和世界位置
            initialMapCenter = map.CenterLatitudeLongitude;
            mapCenterWorldPosition = map.GeoToWorldPosition(initialMapCenter);
            
            // 将玩家放置在地图中心
            transform.position = mapCenterWorldPosition;
            lastMapUpdatePosition = transform.position;
            
            isInitialized = true;
            Debug.Log($"Player initialized at world position: {transform.position}");
            Debug.Log($"Map center: {initialMapCenter}");
        }
    }
    
    void Update()
    {
        if (!isInitialized) return;
        
        HandleMovementInput();
    }
    
    void HandleMovementInput()
    {
        Vector3 movement = Vector3.zero;
        
        // 获取键盘输入 - 使用3D世界坐标
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            movement.z += 1f; // 向前（北）
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            movement.z -= 1f; // 向后（南）
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            movement.x -= 1f; // 向左（西）
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            movement.x += 1f; // 向右（东）
        
        // 如果有输入，移动玩家
        if (movement != Vector3.zero)
        {
            MovePlayer(movement.normalized);
        }
    }
    
    void MovePlayer(Vector3 direction)
    {
        // 在世界坐标系中移动玩家
        Vector3 deltaMovement = direction * moveSpeed * Time.deltaTime;
        transform.position += deltaMovement;
        
        // 检查是否需要重新定位地图（玩家移动太远时）
        float distanceFromMapCenter = Vector3.Distance(transform.position, mapCenterWorldPosition);
        if (distanceFromMapCenter > mapResetDistance)
        {
            RecenterMap();
        }
        
        // 注意：地图瓦片更新现在由ConfigurableTileCache组件自动处理
        // 基于瓦片网格的智能缓存系统会在玩家跨越瓦片边界时自动更新
    }
    
    // 已弃用：瓦片更新现在由ConfigurableTileCache系统自动处理
    // void UpdateMapTiles()
    // {
    //     // 这个方法已被ConfigurableTileCache.cs中的智能瓦片管理替代
    //     // 不再需要手动触发地图瓦片更新，避免地图跳跃问题
    // }
    
    
    void RecenterMap()
    {
        // 只有在玩家移动到极远距离时才重新定位地图
        // 这是必要的操作，但应该很少发生
        
        Vector2d playerGeoPosition = map.WorldToGeoPosition(transform.position);
        
        Debug.Log($"Force recentering map - Player moved too far from center");
        Debug.Log($"Distance from center: {Vector3.Distance(transform.position, mapCenterWorldPosition):F1}m (threshold: {mapResetDistance}m)");
        
        // 重新定位地图中心到玩家位置（这会导致一次地图跳跃，但是必要的）
        map.UpdateMap(playerGeoPosition, map.Zoom);
        
        // 更新地图中心记录
        initialMapCenter = playerGeoPosition;
        mapCenterWorldPosition = transform.position;
        lastMapUpdatePosition = transform.position;
        lastMapUpdateTime = Time.time;
        
        Debug.Log($"Map recentered to player position: ({playerGeoPosition.x:F6}, {playerGeoPosition.y:F6})");
        
        // 通知ConfigurableTileCache地图已重新定位，清理所有缓存
        ConfigurableTileCache tileCache = FindObjectOfType<ConfigurableTileCache>();
        if (tileCache != null)
        {
            tileCache.ClearHistoryCache();
            Debug.Log("Reset ConfigurableTileCache states after forced map recenter");
        }
    }
    
    
    // 获取玩家当前的地理位置
    public Vector2d GetCurrentGeoLocation()
    {
        if (map != null)
        {
            return map.WorldToGeoPosition(transform.position);
        }
        return initialMapCenter;
    }
    
    // 获取玩家当前的世界坐标位置
    public Vector3 GetCurrentWorldPosition()
    {
        return transform.position;
    }
    
    // 设置玩家位置（地理坐标）
    public void SetGeoLocation(Vector2d geoLocation)
    {
        if (map != null)
        {
            Vector3 worldPosition = map.GeoToWorldPosition(geoLocation);
            transform.position = worldPosition;
            
            // 如果位置变化很大，重新定位地图
            float distance = Vector3.Distance(worldPosition, mapCenterWorldPosition);
            if (distance > mapResetDistance * 0.5f)
            {
                RecenterMap();
            }
        }
    }
    
    // 设置玩家位置（世界坐标）
    public void SetWorldPosition(Vector3 worldPosition)
    {
        transform.position = worldPosition;
        
        // 检查是否需要重新定位地图
        float distance = Vector3.Distance(worldPosition, mapCenterWorldPosition);
        if (distance > mapResetDistance * 0.5f)
        {
            RecenterMap();
        }
    }
}