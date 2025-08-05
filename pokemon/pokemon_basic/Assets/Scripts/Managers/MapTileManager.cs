using UnityEngine;
using Mapbox.Utils;
using System.Collections.Generic;

public class MapTileManager : MonoBehaviour
{
    [Header("Tile Settings")]
    public float tileSize = 0.01f; // 每个地图块的经纬度大小
    public int gridSize = 3; // 3x3 = 9宫格
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    
    private Vector2d lastPlayerLocation;
    private Vector2d currentCenterTile;
    private HashSet<Vector2Int> loadedTiles = new HashSet<Vector2Int>();
    private bool isInitialized = false;
    
    // 9宫格偏移量 (-1,-1) 到 (1,1)
    private readonly Vector2Int[] nineGridOffsets = new Vector2Int[]
    {
        new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1),
        new Vector2Int(-1,  0), new Vector2Int(0,  0), new Vector2Int(1,  0),
        new Vector2Int(-1,  1), new Vector2Int(0,  1), new Vector2Int(1,  1)
    };
    
    void Start()
    {
        // 延迟初始化，等待其他组件设置完成
        Invoke(nameof(Initialize), 0.5f);
    }
    
    void Initialize()
    {
        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            lastPlayerLocation = playerController.GetCurrentLocation();
            currentCenterTile = GetTileCoordinate(lastPlayerLocation);
            LoadNineGridTiles(currentCenterTile);
            isInitialized = true;
            
            if (enableDebugLogs)
            {
                Debug.Log($"MapTileManager initialized at location: {lastPlayerLocation}");
                Debug.Log($"Center tile: {currentCenterTile}");
            }
        }
    }
    
    public void CheckForTileUpdate(Vector2d playerLocation)
    {
        if (!isInitialized) return;
        
        Vector2d newCenterTile = GetTileCoordinate(playerLocation);
        
        // 检查是否需要更新地图块
        if (newCenterTile != currentCenterTile)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"Player moved to new tile: {newCenterTile}");
            }
            
            currentCenterTile = newCenterTile;
            UpdateNineGridTiles(currentCenterTile);
        }
        
        lastPlayerLocation = playerLocation;
    }
    
    Vector2d GetTileCoordinate(Vector2d worldLocation)
    {
        // 将世界坐标转换为瓦片坐标
        double tileX = System.Math.Floor(worldLocation.x / tileSize);
        double tileY = System.Math.Floor(worldLocation.y / tileSize);
        return new Vector2d(tileX, tileY);
    }
    
    void LoadNineGridTiles(Vector2d centerTile)
    {
        loadedTiles.Clear();
        
        foreach (Vector2Int offset in nineGridOffsets)
        {
            Vector2Int tileCoord = new Vector2Int(
                (int)centerTile.x + offset.x,
                (int)centerTile.y + offset.y
            );
            
            LoadTile(tileCoord);
            loadedTiles.Add(tileCoord);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"Loaded 9-grid tiles around center: {centerTile}");
        }
    }
    
    void UpdateNineGridTiles(Vector2d newCenterTile)
    {
        HashSet<Vector2Int> newTiles = new HashSet<Vector2Int>();
        
        // 计算新的9宫格瓦片
        foreach (Vector2Int offset in nineGridOffsets)
        {
            Vector2Int tileCoord = new Vector2Int(
                (int)newCenterTile.x + offset.x,
                (int)newCenterTile.y + offset.y
            );
            newTiles.Add(tileCoord);
        }
        
        // 卸载不再需要的瓦片
        foreach (Vector2Int oldTile in loadedTiles)
        {
            if (!newTiles.Contains(oldTile))
            {
                UnloadTile(oldTile);
            }
        }
        
        // 加载新的瓦片
        foreach (Vector2Int newTile in newTiles)
        {
            if (!loadedTiles.Contains(newTile))
            {
                LoadTile(newTile);
            }
        }
        
        loadedTiles = newTiles;
        
        if (enableDebugLogs)
        {
            Debug.Log($"Updated tiles. Total loaded: {loadedTiles.Count}");
        }
    }
    
    void LoadTile(Vector2Int tileCoord)
    {
        // 这里可以添加具体的瓦片加载逻辑
        // 例如：加载宠物生成点、地形数据等
        
        if (enableDebugLogs)
        {
            Debug.Log($"Loading tile: ({tileCoord.x}, {tileCoord.y})");
        }
        
        // 示例：可以在这里触发宠物生成点的加载
        LoadTileData(tileCoord);
    }
    
    void UnloadTile(Vector2Int tileCoord)
    {
        // 这里可以添加具体的瓦片卸载逻辑
        // 例如：清理宠物生成点、释放资源等
        
        if (enableDebugLogs)
        {
            Debug.Log($"Unloading tile: ({tileCoord.x}, {tileCoord.y})");
        }
        
        UnloadTileData(tileCoord);
    }
    
    void LoadTileData(Vector2Int tileCoord)
    {
        // 这里是具体的瓦片数据加载逻辑
        // 可以加载：
        // 1. 宠物生成点
        // 2. 地形特征
        // 3. 其他游戏对象
        
        // 示例：创建一个标记显示瓦片边界（用于调试）
        if (enableDebugLogs)
        {
            CreateTileMarker(tileCoord);
        }
    }
    
    void UnloadTileData(Vector2Int tileCoord)
    {
        // 清理瓦片相关的游戏对象和数据
        
        // 示例：移除调试标记
        RemoveTileMarker(tileCoord);
    }
    
    void CreateTileMarker(Vector2Int tileCoord)
    {
        // 创建一个简单的立方体作为瓦片标记（仅用于调试）
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = $"TileMarker_{tileCoord.x}_{tileCoord.y}";
        marker.transform.position = new Vector3(tileCoord.x, 0, tileCoord.y);
        marker.transform.localScale = Vector3.one * 0.5f;
        
        // 添加标识组件，方便后续查找和删除
        TileMarker markerComponent = marker.AddComponent<TileMarker>();
        markerComponent.tileCoord = tileCoord;
    }
    
    void RemoveTileMarker(Vector2Int tileCoord)
    {
        TileMarker[] markers = FindObjectsOfType<TileMarker>();
        foreach (TileMarker marker in markers)
        {
            if (marker.tileCoord == tileCoord)
            {
                DestroyImmediate(marker.gameObject);
                break;
            }
        }
    }
    
    // 获取当前加载的瓦片信息
    public HashSet<Vector2Int> GetLoadedTiles()
    {
        return new HashSet<Vector2Int>(loadedTiles);
    }
    
    // 获取指定位置的瓦片坐标
    public Vector2Int GetTileAt(Vector2d worldLocation)
    {
        Vector2d tileCoord = GetTileCoordinate(worldLocation);
        return new Vector2Int((int)tileCoord.x, (int)tileCoord.y);
    }
}

// 辅助组件，用于标识瓦片标记
public class TileMarker : MonoBehaviour
{
    public Vector2Int tileCoord;
}