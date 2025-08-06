using System;
using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Utils;

public class GameManager : MonoBehaviour
{
    [Header("Game Components")]
    public AbstractMap map;
    public PlayerController playerController;
    public ConfigurableTileCache tileCache; // 新的瓦片网格缓存系统
    public CameraController cameraController;
    
    
    [Header("Debug Settings")]
    public bool enableDebugUI = true;
    
    // 地图跳跃监测
    private Vector2d lastMapCenter;
    private float lastMapCenterCheckTime;
    private bool mapJumpDetected = false;
    private float lastMapJumpTime;
    
    void Start()
    {
        InitializeGame();
    }
    
    void InitializeGame()
    {
        // 查找并设置组件引用
        if (map == null)
            map = FindObjectOfType<AbstractMap>();
        
        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();
            
        if (tileCache == null)
            tileCache = FindObjectOfType<ConfigurableTileCache>();
            
        if (cameraController == null)
            cameraController = FindObjectOfType<CameraController>();
        
        
        // 初始化地图跳跃监测
        if (map != null)
        {
            lastMapCenter = map.CenterLatitudeLongitude;
            lastMapCenterCheckTime = Time.time;
        }
        
        Debug.Log("Game initialized successfully!");
    }
    
    void Update()
    {
        // 监测地图跳跃
        if (map != null && Time.time - lastMapCenterCheckTime > 0.5f) // 每0.5秒检查一次
        {
            Vector2d currentMapCenter = map.CenterLatitudeLongitude;
            double centerDistance = Math.Sqrt(
                Math.Pow(currentMapCenter.x - lastMapCenter.x, 2) + 
                Math.Pow(currentMapCenter.y - lastMapCenter.y, 2)
            ) * 111000;
            
            if (centerDistance > 10) // 如果地图中心移动超过10米
            {
                Debug.Log($"⚠️ Map JUMP detected! Center moved {centerDistance:F1}m - From ({lastMapCenter.x:F6}, {lastMapCenter.y:F6}) to ({currentMapCenter.x:F6}, {currentMapCenter.y:F6})");
                mapJumpDetected = true;
                lastMapJumpTime = Time.time;
            }
            
            // 清除跳跃状态（5秒后）
            if (mapJumpDetected && Time.time - lastMapJumpTime > 5f)
            {
                mapJumpDetected = false;
            }
            
            lastMapCenter = currentMapCenter;
            lastMapCenterCheckTime = Time.time;
        }
    }
    
    void OnGUI()
    {
        if (!enableDebugUI) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("PMW Debug Info");
        
        if (playerController != null)
        {
            var geoLocation = playerController.GetCurrentGeoLocation();
            var worldPosition = playerController.GetCurrentWorldPosition();
            GUILayout.Label($"Geo Location: {geoLocation.x:F6}, {geoLocation.y:F6}");
            GUILayout.Label($"World Position: {worldPosition.x:F1}, {worldPosition.y:F1}, {worldPosition.z:F1}");
            
            // 显示地图信息
            if (map != null)
            {
                GUILayout.Label($"Map Zoom: {map.Zoom}");
                
                // 显示地图中心信息
                var mapCenter = map.CenterLatitudeLongitude;
                GUILayout.Label($"Map Center: ({mapCenter.x:F6}, {mapCenter.y:F6})");
                
                // 显示玩家与地图中心的距离
                var playerGeoPos = playerController.GetCurrentGeoLocation();
                double deltaLat = playerGeoPos.x - mapCenter.x;
                double deltaLng = playerGeoPos.y - mapCenter.y;
                double distance = Math.Sqrt(deltaLat * deltaLat + deltaLng * deltaLng) * 111000;
                GUILayout.Label($"Distance from Center: {distance:F1}m");
                
                // 显示重置阈值
                GUILayout.Label($"Reset Threshold: {playerController.mapResetDistance}m");
                
                // 显示地图跳跃状态
                if (mapJumpDetected)
                {
                    GUILayout.Label("⚠️ MAP JUMP DETECTED!", "box");
                }
                else
                {
                    GUILayout.Label("✅ Map Stable", "box");
                }
            }
        }
        
        // 显示瓦片缓存系统信息
        if (tileCache != null)
        {
            var stats = tileCache.GetCacheStats();
            GUILayout.Label($"Tile Cache: {stats.ToString()}");
            GUILayout.Label($"Player Tile: {tileCache.GetCurrentPlayerTile()}");
            GUILayout.Label($"Load Range: {stats.currentLoadRange} ({stats.currentLoadRange}x{stats.currentLoadRange})");
            GUILayout.Label($"Tile Size: {tileCache.tileSize}u");
        }
        else
        {
            GUILayout.Label("Tile Cache: Not active");
        }
        
        // 显示摄像机信息
        if (cameraController != null)
        {
            GUILayout.Label($"Camera Mode: Fixed Angle Follow");
            GUILayout.Label($"Camera Rotating: {cameraController.IsRotating()}");
            
            // 显示当前摄像机角度
            Vector3 cameraRotation = cameraController.transform.eulerAngles;
            GUILayout.Label($"Camera Angle: Y:{cameraRotation.y:F0}° X:{cameraRotation.x:F0}°");
        }
        
        GUILayout.Label("Controls:");
        GUILayout.Label("WASD / Arrow Keys - Move");
        GUILayout.Label("Right Mouse - Rotate Camera");
        GUILayout.Label("ESC - Exit Camera Rotation");
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}