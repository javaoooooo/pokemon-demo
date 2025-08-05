using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Utils;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float mapMoveSpeed = 0.001f; // 地图移动速度（经纬度单位）
    
    [Header("Map References")]
    public AbstractMap map;
    
    private Vector2d currentLocation;
    private bool isInitialized = false;
    
    void Start()
    {
        if (map == null)
        {
            map = FindObjectOfType<AbstractMap>();
        }
        
        if (map != null)
        {
            currentLocation = map.CenterLatitudeLongitude;
            isInitialized = true;
        }
    }
    
    void Update()
    {
        if (!isInitialized) return;
        
        HandleMovementInput();
    }
    
    void HandleMovementInput()
    {
        Vector2 input = Vector2.zero;
        
        // 获取键盘输入
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            input.y += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            input.y -= 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            input.x -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            input.x += 1f;
        
        // 如果有输入，更新位置
        if (input != Vector2.zero)
        {
            MovePlayer(input);
        }
    }
    
    void MovePlayer(Vector2 direction)
    {
        // 计算新的经纬度位置
        Vector2d deltaLocation = new Vector2d(
            direction.y * mapMoveSpeed * Time.deltaTime, // 纬度变化
            direction.x * mapMoveSpeed * Time.deltaTime  // 经度变化
        );
        
        currentLocation += deltaLocation;
        
        // 更新地图中心位置
        map.UpdateMap(currentLocation, map.Zoom);
        
        // 触发地图块更新检查
        MapTileManager mapTileManager = FindObjectOfType<MapTileManager>();
        if (mapTileManager != null)
        {
            mapTileManager.CheckForTileUpdate(currentLocation);
        }
    }
    
    public Vector2d GetCurrentLocation()
    {
        return currentLocation;
    }
    
    public void SetLocation(Vector2d newLocation)
    {
        currentLocation = newLocation;
        if (map != null)
        {
            map.UpdateMap(currentLocation, map.Zoom);
        }
    }
}