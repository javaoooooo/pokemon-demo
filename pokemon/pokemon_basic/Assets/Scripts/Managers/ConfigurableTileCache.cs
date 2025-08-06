using System;
using System.Collections.Generic;
using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Utils;
#if UNITY_EDITOR
using UnityEditor;
#endif

// 加载范围枚举
public enum LoadRange
{
    Minimal = 1,    // 1x1 - 仅当前瓦片
    Small = 3,      // 3x3 - 当前+周围8个
    Medium = 5,     // 5x5 - 标准范围
    Large = 7,      // 7x7 - 大范围
    XLarge = 9      // 9x9 - 最大范围
}

// 瓦片数据结构
[System.Serializable]
public class TileData
{
    public Vector2Int coordinate;
    public DateTime lastAccessed;
    public bool isActive; // 是否在当前加载网格中
    
    public TileData(Vector2Int coord)
    {
        coordinate = coord;
        lastAccessed = DateTime.Now;
        isActive = false;
    }
    
    public void UpdateAccess()
    {
        lastAccessed = DateTime.Now;
    }
}

// 缓存统计信息
[System.Serializable]
public class CacheStatistics
{
    public int activeTiles;
    public int historyCachedTiles;
    public int totalCachedTiles;
    public LoadRange currentLoadRange;
    
    public override string ToString()
    {
        return $"Active: {activeTiles}, History: {historyCachedTiles}, Total: {totalCachedTiles}, Range: {currentLoadRange}";
    }
}

public class ConfigurableTileCache : MonoBehaviour
{
    [Header("Cache Configuration")]
    [SerializeField] private LoadRange loadRange = LoadRange.Medium;
    [SerializeField] private int historyCacheSize = 10;
    
    [Header("References")]
    public Transform playerTransform;
    public AbstractMap map;
    public bool autoFindReferences = true;
    
    [Header("Performance")]
    public float tileSize = 100f; // 每个瓦片的世界坐标大小
    public float mapUpdateCooldown = 0.5f; // 地图更新冷却时间（仅用于调试监控）
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    public bool showDebugGizmos = true;
    public bool showTileInfo = true; // 显示瓦片详细信息
    
    // 私有变量
    private TileGridManager gridManager;
    private Dictionary<Vector2Int, TileData> tileCache = new Dictionary<Vector2Int, TileData>();
    private Queue<Vector2Int> historyCache = new Queue<Vector2Int>(); // LRU历史缓存
    private Vector2Int currentPlayerTile;
    private float lastMapUpdateTime;
    
    // 事件
    public event System.Action<Vector2Int> OnPlayerEnterNewTile;
    
    void Start()
    {
        InitializeCache();
    }
    
    void InitializeCache()
    {
        // 自动查找引用
        if (autoFindReferences)
        {
            if (playerTransform == null)
            {
                PlayerController player = FindObjectOfType<PlayerController>();
                if (player != null) playerTransform = player.transform;
            }
            
            if (map == null)
            {
                map = FindObjectOfType<AbstractMap>();
            }
        }
        
        // 创建网格管理器
        gridManager = new TileGridManager(this);
        
        if (playerTransform != null)
        {
            // 计算玩家当前瓦片位置
            currentPlayerTile = WorldToTileCoordinate(playerTransform.position);
            
            // 初始化当前网格
            LoadInitialGrid();
            
            if (enableDebugLogs)
            {
                Debug.Log($"ConfigurableTileCache initialized at tile {currentPlayerTile}, range: {loadRange}");
            }
        }
        else
        {
            Debug.LogWarning("ConfigurableTileCache: Player transform not found!");
        }
    }
    
    void Update()
    {
        if (playerTransform == null || map == null) return;
        
        CheckPlayerTilePosition();
    }
    
    void CheckPlayerTilePosition()
    {
        Vector2Int newPlayerTile = WorldToTileCoordinate(playerTransform.position);
        
        // 检查玩家是否移动到新瓦片
        if (newPlayerTile != currentPlayerTile)
        {
            Vector2Int oldTile = currentPlayerTile;
            currentPlayerTile = newPlayerTile;
            
            // 触发瓦片更新
            OnPlayerTileChanged(oldTile, newPlayerTile);
            
            // 触发事件
            OnPlayerEnterNewTile?.Invoke(newPlayerTile);
            
            if (enableDebugLogs)
            {
                Vector3 worldPos = playerTransform.position;
                Vector2d geoPos = map != null ? map.WorldToGeoPosition(worldPos) : Vector2d.zero;
                Debug.Log($"Player moved from tile {oldTile} to {newPlayerTile}");
                Debug.Log($"World position: ({worldPos.x:F2}, {worldPos.z:F2})");
                Debug.Log($"Geo position: ({geoPos.x:F6}, {geoPos.y:F6})");
            }
        }
    }
    
    void OnPlayerTileChanged(Vector2Int oldTile, Vector2Int newTile)
    {
        // 基于瓦片边界的真正增量更新
        MoveDirection direction = gridManager.GetMoveDirection(oldTile, newTile);
        
        if (enableDebugLogs)
        {
            Debug.Log($"Player crossed tile boundary: {oldTile} → {newTile}, Direction: {direction}");
        }
        
        // 根据移动方向进行增量瓦片更新
        PerformIncrementalTileUpdate(direction, newTile);
    }
    
    void PerformIncrementalTileUpdate(MoveDirection direction, Vector2Int newPlayerTile)
    {
        List<Vector2Int> tilesToLoad = new List<Vector2Int>();
        List<Vector2Int> tilesToUnload = new List<Vector2Int>();
        
        int range = ((int)loadRange - 1) / 2; // 网格半径
        
        switch (direction)
        {
            case MoveDirection.West: // 向左移动
                // 加载左侧边缘的5个瓦片
                for (int y = -range; y <= range; y++)
                {
                    tilesToLoad.Add(newPlayerTile + new Vector2Int(-range, y));
                }
                // 卸载右侧边缘的5个瓦片
                for (int y = -range; y <= range; y++)
                {
                    tilesToUnload.Add(newPlayerTile + new Vector2Int(range + 1, y));
                }
                break;
                
            case MoveDirection.East: // 向右移动
                // 加载右侧边缘的5个瓦片
                for (int y = -range; y <= range; y++)
                {
                    tilesToLoad.Add(newPlayerTile + new Vector2Int(range, y));
                }
                // 卸载左侧边缘的5个瓦片
                for (int y = -range; y <= range; y++)
                {
                    tilesToUnload.Add(newPlayerTile + new Vector2Int(-range - 1, y));
                }
                break;
                
            case MoveDirection.North: // 向上移动
                // 加载上方边缘的5个瓦片
                for (int x = -range; x <= range; x++)
                {
                    tilesToLoad.Add(newPlayerTile + new Vector2Int(x, range));
                }
                // 卸载下方边缘的5个瓦片
                for (int x = -range; x <= range; x++)
                {
                    tilesToUnload.Add(newPlayerTile + new Vector2Int(x, -range - 1));
                }
                break;
                
            case MoveDirection.South: // 向下移动
                // 加载下方边缘的5个瓦片
                for (int x = -range; x <= range; x++)
                {
                    tilesToLoad.Add(newPlayerTile + new Vector2Int(x, -range));
                }
                // 卸载上方边缘的5个瓦片
                for (int x = -range; x <= range; x++)
                {
                    tilesToUnload.Add(newPlayerTile + new Vector2Int(x, range + 1));
                }
                break;
                
            // 对角移动：加载L形区域
            case MoveDirection.NorthEast:
                LoadLShapedTiles(tilesToLoad, tilesToUnload, newPlayerTile, range, 1, 1);
                break;
            case MoveDirection.NorthWest:
                LoadLShapedTiles(tilesToLoad, tilesToUnload, newPlayerTile, range, -1, 1);
                break;
            case MoveDirection.SouthEast:
                LoadLShapedTiles(tilesToLoad, tilesToUnload, newPlayerTile, range, 1, -1);
                break;
            case MoveDirection.SouthWest:
                LoadLShapedTiles(tilesToLoad, tilesToUnload, newPlayerTile, range, -1, -1);
                break;
                
            default:
                // 如果方向不明确，使用原来的全量更新逻辑
                var allTilesToLoad = gridManager.GetTilesToLoad(newPlayerTile - Vector2Int.one, newPlayerTile);
                var allTilesToUnload = gridManager.GetTilesToUnload(newPlayerTile - Vector2Int.one, newPlayerTile);
                tilesToLoad.AddRange(allTilesToLoad);
                tilesToUnload.AddRange(allTilesToUnload);
                break;
        }
        
        // 执行瓦片加载和卸载
        foreach (Vector2Int tile in tilesToUnload)
        {
            UnloadTileToHistory(tile);
        }
        
        foreach (Vector2Int tile in tilesToLoad)
        {
            LoadTileIncremental(tile); // 使用新的增量加载方法
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"Incremental update - Direction: {direction}, Loaded: {tilesToLoad.Count}, Unloaded: {tilesToUnload.Count}");
        }
    }
    
    void LoadLShapedTiles(List<Vector2Int> tilesToLoad, List<Vector2Int> tilesToUnload, 
                         Vector2Int playerTile, int range, int xDir, int yDir)
    {
        // 对角移动时加载L形区域的瓦片
        // 例如向东北移动：加载东边缘 + 北边缘（去除重复的角落）
        
        // 加载水平边缘
        for (int y = -range; y <= range; y++)
        {
            tilesToLoad.Add(playerTile + new Vector2Int(xDir * range, y));
        }
        
        // 加载垂直边缘（避免重复角落）
        for (int x = -range; x < range; x++) // 注意这里是 < range 避免重复
        {
            tilesToLoad.Add(playerTile + new Vector2Int(x, yDir * range));
        }
        
        // 卸载对角的瓦片
        for (int y = -range; y <= range; y++)
        {
            tilesToUnload.Add(playerTile + new Vector2Int(-xDir * (range + 1), y));
        }
        for (int x = -range; x < range; x++)
        {
            tilesToUnload.Add(playerTile + new Vector2Int(x, -yDir * (range + 1)));
        }
    }
    
    void LoadInitialGrid()
    {
        var initialTiles = gridManager.GetGridTiles(currentPlayerTile);
        
        foreach (Vector2Int tile in initialTiles)
        {
            LoadTile(tile);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"Loaded initial grid: {initialTiles.Count} tiles");
        }
    }
    
    void LoadTile(Vector2Int tileCoord)
    {
        // 传统的瓦片加载方法（用于初始化）
        LoadTileIncremental(tileCoord);
        
        // 只在初始化时触发一次地图更新检查
        TriggerMapUpdate();
    }
    
    void LoadTileIncremental(Vector2Int tileCoord)
    {
        // 新的增量瓦片加载方法：只管理状态，不强制更新地图
        if (tileCache.ContainsKey(tileCoord))
        {
            tileCache[tileCoord].isActive = true;
            tileCache[tileCoord].UpdateAccess();
            
            if (enableDebugLogs)
            {
                Debug.Log($"Reactivated cached tile: {tileCoord}");
            }
        }
        else
        {
            // 创建新瓦片数据
            TileData newTile = new TileData(tileCoord);
            newTile.isActive = true;
            tileCache[tileCoord] = newTile;
            
            if (enableDebugLogs)
            {
                Debug.Log($"Created new tile: {tileCoord}");
            }
        }
        
        // 关键改变：不再调用TriggerMapUpdate()
        // 让Mapbox自然处理瓦片的流式加载
    }
    
    void UnloadTileToHistory(Vector2Int tileCoord)
    {
        if (tileCache.ContainsKey(tileCoord))
        {
            tileCache[tileCoord].isActive = false;
            
            // 添加到历史缓存队列
            if (!historyCache.Contains(tileCoord))
            {
                historyCache.Enqueue(tileCoord);
                
                // 限制历史缓存大小
                while (historyCache.Count > historyCacheSize)
                {
                    Vector2Int oldTile = historyCache.Dequeue();
                    if (tileCache.ContainsKey(oldTile) && !tileCache[oldTile].isActive)
                    {
                        tileCache.Remove(oldTile);
                    }
                }
            }
        }
    }
    
    void TriggerMapUpdate()
    {
        // 新的增量更新系统：完全依赖瓦片边界事件，不使用距离触发
        // 这个方法现在只用于调试监控，不再强制更新地图
        
        if (enableDebugLogs && Time.frameCount % 300 == 0 && map != null && playerTransform != null)
        {
            // 仅用于调试：监控地图状态，但不更新
            Vector2d playerGeoCoord = map.WorldToGeoPosition(playerTransform.position);
            Vector2d mapCenter = map.CenterLatitudeLongitude;
            
            double deltaLat = playerGeoCoord.x - mapCenter.x;
            double deltaLng = playerGeoCoord.y - mapCenter.y;
            double distance = Math.Sqrt(deltaLat * deltaLat + deltaLng * deltaLng) * 111000;
            
            Debug.Log($"Tile System Status - Player: ({playerGeoCoord.x:F6}, {playerGeoCoord.y:F6}), Map Center: ({mapCenter.x:F6}, {mapCenter.y:F6}), Distance: {distance:F1}m");
            Debug.Log($"Player Tile: {currentPlayerTile}, Active Tiles: {GetActiveTiles().Count}");
        }
    }
    
    Vector2Int WorldToTileCoordinate(Vector3 worldPosition)
    {
        // 高精度瓦片坐标转换，确保与瓦片边界完全对齐
        float tileX = worldPosition.x / tileSize;
        float tileZ = worldPosition.z / tileSize;
        
        // 使用数学上正确的floor函数处理负数坐标
        int tileCoordX = Mathf.FloorToInt(tileX);
        int tileCoordZ = Mathf.FloorToInt(tileZ);
        
        // 确保边界处理的一致性：当玩家正好在边界上时的处理
        if (Mathf.Approximately(tileX, tileCoordX + 1.0f))
        {
            tileCoordX += 1; // 处理浮点精度问题导致的边界误判
        }
        if (Mathf.Approximately(tileZ, tileCoordZ + 1.0f))
        {
            tileCoordZ += 1;
        }
        
        return new Vector2Int(tileCoordX, tileCoordZ);
    }
    
    Vector3 TileToWorldPosition(Vector2Int tileCoord)
    {
        return new Vector3(
            tileCoord.x * tileSize + tileSize * 0.5f,
            0,
            tileCoord.y * tileSize + tileSize * 0.5f
        );
    }
    
    // 公共接口方法
    public void SetLoadRange(LoadRange range)
    {
        if (loadRange != range)
        {
            loadRange = range;
            gridManager?.UpdateLoadRange(range);
            
            // 重新加载网格
            if (playerTransform != null)
            {
                LoadInitialGrid();
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"Load range changed to {range}");
            }
        }
    }
    
    public void SetHistoryCacheSize(int size)
    {
        historyCacheSize = Mathf.Max(0, size);
        
        // 清理超出的历史缓存
        while (historyCache.Count > historyCacheSize)
        {
            Vector2Int oldTile = historyCache.Dequeue();
            if (tileCache.ContainsKey(oldTile) && !tileCache[oldTile].isActive)
            {
                tileCache.Remove(oldTile);
            }
        }
    }
    
    public void ClearHistoryCache()
    {
        // 移除所有非活跃瓦片
        var tilesToRemove = new List<Vector2Int>();
        foreach (var kvp in tileCache)
        {
            if (!kvp.Value.isActive)
            {
                tilesToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var tile in tilesToRemove)
        {
            tileCache.Remove(tile);
        }
        
        historyCache.Clear();
        
        if (enableDebugLogs)
        {
            Debug.Log($"History cache cleared, removed {tilesToRemove.Count} tiles");
        }
    }
    
    public CacheStatistics GetCacheStats()
    {
        var stats = new CacheStatistics();
        stats.currentLoadRange = loadRange;
        stats.historyCachedTiles = historyCache.Count;
        stats.totalCachedTiles = tileCache.Count;
        
        // 计算活跃瓦片数
        foreach (var tile in tileCache.Values)
        {
            if (tile.isActive)
                stats.activeTiles++;
        }
        
        return stats;
    }
    
    public Vector2Int GetCurrentPlayerTile()
    {
        return currentPlayerTile;
    }
    
    public LoadRange GetLoadRange()
    {
        return loadRange;
    }
    
    public HashSet<Vector2Int> GetActiveTiles()
    {
        var activeTiles = new HashSet<Vector2Int>();
        foreach (var kvp in tileCache)
        {
            if (kvp.Value.isActive)
            {
                activeTiles.Add(kvp.Key);
            }
        }
        return activeTiles;
    }
    
    public void SetTileSize(float newTileSize)
    {
        if (newTileSize > 0 && newTileSize != tileSize)
        {
            tileSize = newTileSize;
            
            // 重新计算当前玩家瓦片位置
            if (playerTransform != null)
            {
                currentPlayerTile = WorldToTileCoordinate(playerTransform.position);
                ClearHistoryCache(); // 清理旧的缓存，因为瓦片大小已改变
                LoadInitialGrid(); // 重新加载网格
                
                if (enableDebugLogs)
                {
                    Debug.Log($"Tile size changed to {tileSize}, player now at tile {currentPlayerTile}");
                }
            }
        }
    }
    
    // SetMapUpdateThreshold方法已移除，因为新系统基于瓦片边界而非距离
    
    // 调试可视化
    void OnDrawGizmos()
    {
        if (!showDebugGizmos || playerTransform == null) return;
        
        Vector3 playerPos = playerTransform.position;
        
        // 绘制当前活跃瓦片网格
        Gizmos.color = Color.green;
        var activeTiles = GetActiveTiles();
        foreach (Vector2Int tile in activeTiles)
        {
            Vector3 center = TileToWorldPosition(tile);
            Gizmos.DrawWireCube(center, new Vector3(tileSize, 1f, tileSize));
            
            // 显示瓦片坐标（如果启用详细信息）
            if (showTileInfo)
            {
#if UNITY_EDITOR
                Handles.Label(center + Vector3.up, $"{tile.x},{tile.y}");
#endif
            }
        }
        
        // 绘制历史缓存瓦片
        Gizmos.color = Color.blue;
        foreach (Vector2Int tile in historyCache)
        {
            if (!activeTiles.Contains(tile))
            {
                Vector3 center = TileToWorldPosition(tile);
                center.y = 0.5f;
                Gizmos.DrawWireCube(center, new Vector3(tileSize * 0.8f, 1f, tileSize * 0.8f));
                
                if (showTileInfo)
                {
#if UNITY_EDITOR
                    Handles.Label(center + Vector3.up, $"H:{tile.x},{tile.y}");
#endif
                }
            }
        }
        
        // 绘制玩家当前瓦片（高亮显示）
        Gizmos.color = Color.red;
        Vector3 playerTileCenter = TileToWorldPosition(currentPlayerTile);
        playerTileCenter.y = 1f;
        Gizmos.DrawWireCube(playerTileCenter, new Vector3(tileSize * 0.9f, 2f, tileSize * 0.9f));
        
        // 绘制玩家实际位置
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(playerPos, 2f);
        
        // 绘制瓦片边界线（帮助识别增量更新边缘）
        Gizmos.color = Color.white;
        int gridRange = ((int)loadRange - 1) / 2;
        
        // 绘制以瓦片为单位的网格线
        Vector2Int playerTileCoord = WorldToTileCoordinate(playerPos);
        for (int i = -gridRange - 1; i <= gridRange + 1; i++)
        {
            // 垂直网格线
            float x = (playerTileCoord.x + i) * tileSize;
            Vector3 start1 = new Vector3(x, 0, (playerTileCoord.y - gridRange - 1) * tileSize);
            Vector3 end1 = new Vector3(x, 0, (playerTileCoord.y + gridRange + 1) * tileSize);
            Gizmos.DrawLine(start1, end1);
            
            // 水平网格线
            float z = (playerTileCoord.y + i) * tileSize;
            Vector3 start2 = new Vector3((playerTileCoord.x - gridRange - 1) * tileSize, 0, z);
            Vector3 end2 = new Vector3((playerTileCoord.x + gridRange + 1) * tileSize, 0, z);
            Gizmos.DrawLine(start2, end2);
        }
        
        // 高亮显示玩家当前瓦片的边界
        Gizmos.color = Color.cyan;
        Vector3 currentTileCenter = TileToWorldPosition(playerTileCoord);
        Gizmos.DrawWireCube(currentTileCenter, new Vector3(tileSize, 0.5f, tileSize));
        
        // 显示地图中心（如果有地图引用）
        if (map != null)
        {
            Gizmos.color = Color.magenta;
            Vector2d mapCenter = map.CenterLatitudeLongitude;
            Vector3 mapCenterWorld = map.GeoToWorldPosition(mapCenter);
            Gizmos.DrawWireSphere(mapCenterWorld, 5f);
            
            if (showTileInfo)
            {
#if UNITY_EDITOR
                Handles.Label(mapCenterWorld + Vector3.up * 3f, "Map Center");
#endif
            }
        }
    }
    
    void OnDestroy()
    {
        gridManager?.Cleanup();
    }
}