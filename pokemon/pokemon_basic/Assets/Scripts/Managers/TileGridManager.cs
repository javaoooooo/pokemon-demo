using System.Collections.Generic;
using UnityEngine;

// 移动方向枚举
public enum MoveDirection
{
    None,
    North,
    South,
    East,
    West,
    NorthEast,
    NorthWest,
    SouthEast,
    SouthWest
}

public class TileGridManager
{
    private ConfigurableTileCache cacheController;
    private LoadRange currentLoadRange;
    private HashSet<Vector2Int> currentGrid = new HashSet<Vector2Int>();
    
    public TileGridManager(ConfigurableTileCache controller)
    {
        cacheController = controller;
        currentLoadRange = controller.GetLoadRange();
    }
    
    /// <summary>
    /// 获取指定中心点的网格瓦片
    /// </summary>
    public HashSet<Vector2Int> GetGridTiles(Vector2Int centerTile)
    {
        HashSet<Vector2Int> grid = new HashSet<Vector2Int>();
        int range = ((int)currentLoadRange - 1) / 2; // 计算半径
        
        for (int x = -range; x <= range; x++)
        {
            for (int y = -range; y <= range; y++)
            {
                grid.Add(centerTile + new Vector2Int(x, y));
            }
        }
        
        return grid;
    }
    
    /// <summary>
    /// 计算需要加载的瓦片（增量更新）
    /// </summary>
    public List<Vector2Int> GetTilesToLoad(Vector2Int oldCenter, Vector2Int newCenter)
    {
        var newGrid = GetGridTiles(newCenter);
        var tilesToLoad = new List<Vector2Int>();
        
        // 找出新网格中不在旧网格中的瓦片
        foreach (Vector2Int tile in newGrid)
        {
            if (!currentGrid.Contains(tile))
            {
                tilesToLoad.Add(tile);
            }
        }
        
        // 更新当前网格
        currentGrid = newGrid;
        
        return tilesToLoad;
    }
    
    /// <summary>
    /// 计算需要卸载的瓦片（移至历史缓存）
    /// </summary>
    public List<Vector2Int> GetTilesToUnload(Vector2Int oldCenter, Vector2Int newCenter)
    {
        var oldGrid = GetGridTiles(oldCenter);
        var newGrid = GetGridTiles(newCenter);
        var tilesToUnload = new List<Vector2Int>();
        
        // 找出旧网格中不在新网格中的瓦片
        foreach (Vector2Int tile in oldGrid)
        {
            if (!newGrid.Contains(tile))
            {
                tilesToUnload.Add(tile);
            }
        }
        
        return tilesToUnload;
    }
    
    /// <summary>
    /// 计算移动方向
    /// </summary>
    public MoveDirection GetMoveDirection(Vector2Int oldTile, Vector2Int newTile)
    {
        Vector2Int delta = newTile - oldTile;
        
        if (delta.x == 0 && delta.y == 0)
            return MoveDirection.None;
        else if (delta.x == 0 && delta.y > 0)
            return MoveDirection.North;
        else if (delta.x == 0 && delta.y < 0)
            return MoveDirection.South;
        else if (delta.x > 0 && delta.y == 0)
            return MoveDirection.East;
        else if (delta.x < 0 && delta.y == 0)
            return MoveDirection.West;
        else if (delta.x > 0 && delta.y > 0)
            return MoveDirection.NorthEast;
        else if (delta.x < 0 && delta.y > 0)
            return MoveDirection.NorthWest;
        else if (delta.x > 0 && delta.y < 0)
            return MoveDirection.SouthEast;
        else if (delta.x < 0 && delta.y < 0)
            return MoveDirection.SouthWest;
        
        return MoveDirection.None;
    }
    
    /// <summary>
    /// 获取指定方向的边缘瓦片（用于优化加载）
    /// </summary>
    public List<Vector2Int> GetEdgeTiles(Vector2Int centerTile, MoveDirection direction)
    {
        var edgeTiles = new List<Vector2Int>();
        int range = ((int)currentLoadRange - 1) / 2;
        
        switch (direction)
        {
            case MoveDirection.North:
                // 获取北边缘的瓦片
                for (int x = -range; x <= range; x++)
                {
                    edgeTiles.Add(centerTile + new Vector2Int(x, range));
                }
                break;
                
            case MoveDirection.South:
                // 获取南边缘的瓦片
                for (int x = -range; x <= range; x++)
                {
                    edgeTiles.Add(centerTile + new Vector2Int(x, -range));
                }
                break;
                
            case MoveDirection.East:
                // 获取东边缘的瓦片
                for (int y = -range; y <= range; y++)
                {
                    edgeTiles.Add(centerTile + new Vector2Int(range, y));
                }
                break;
                
            case MoveDirection.West:
                // 获取西边缘的瓦片
                for (int y = -range; y <= range; y++)
                {
                    edgeTiles.Add(centerTile + new Vector2Int(-range, y));
                }
                break;
                
            // 对角线移动需要加载两个边缘
            case MoveDirection.NorthEast:
                edgeTiles.AddRange(GetEdgeTiles(centerTile, MoveDirection.North));
                edgeTiles.AddRange(GetEdgeTiles(centerTile, MoveDirection.East));
                break;
                
            case MoveDirection.NorthWest:
                edgeTiles.AddRange(GetEdgeTiles(centerTile, MoveDirection.North));
                edgeTiles.AddRange(GetEdgeTiles(centerTile, MoveDirection.West));
                break;
                
            case MoveDirection.SouthEast:
                edgeTiles.AddRange(GetEdgeTiles(centerTile, MoveDirection.South));
                edgeTiles.AddRange(GetEdgeTiles(centerTile, MoveDirection.East));
                break;
                
            case MoveDirection.SouthWest:
                edgeTiles.AddRange(GetEdgeTiles(centerTile, MoveDirection.South));
                edgeTiles.AddRange(GetEdgeTiles(centerTile, MoveDirection.West));
                break;
        }
        
        return edgeTiles;
    }
    
    /// <summary>
    /// 更新加载范围
    /// </summary>
    public void UpdateLoadRange(LoadRange newRange)
    {
        currentLoadRange = newRange;
    }
    
    /// <summary>
    /// 获取当前网格的瓦片数量
    /// </summary>
    public int GetCurrentGridSize()
    {
        return currentGrid.Count;
    }
    
    /// <summary>
    /// 获取当前网格
    /// </summary>
    public HashSet<Vector2Int> GetCurrentGrid()
    {
        return new HashSet<Vector2Int>(currentGrid);
    }
    
    /// <summary>
    /// 检查瓦片是否在当前网格内
    /// </summary>
    public bool IsInCurrentGrid(Vector2Int tile)
    {
        return currentGrid.Contains(tile);
    }
    
    /// <summary>
    /// 计算两个瓦片之间的曼哈顿距离
    /// </summary>
    public int GetManhattanDistance(Vector2Int tile1, Vector2Int tile2)
    {
        return Mathf.Abs(tile1.x - tile2.x) + Mathf.Abs(tile1.y - tile2.y);
    }
    
    /// <summary>
    /// 获取指定范围内的所有瓦片（用于调试）
    /// </summary>
    public List<Vector2Int> GetTilesInRange(Vector2Int centerTile, int range)
    {
        var tiles = new List<Vector2Int>();
        
        for (int x = -range; x <= range; x++)
        {
            for (int y = -range; y <= range; y++)
            {
                tiles.Add(centerTile + new Vector2Int(x, y));
            }
        }
        
        return tiles;
    }
    
    /// <summary>
    /// 清理资源
    /// </summary>
    public void Cleanup()
    {
        currentGrid.Clear();
        cacheController = null;
    }
    
    /// <summary>
    /// 获取加载范围对应的瓦片总数
    /// </summary>
    public int GetTotalTilesForRange(LoadRange range)
    {
        return (int)range * (int)range;
    }
    
    /// <summary>
    /// 调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        return $"Current Grid: {currentGrid.Count} tiles, Range: {currentLoadRange} ({GetTotalTilesForRange(currentLoadRange)} total)";
    }
}