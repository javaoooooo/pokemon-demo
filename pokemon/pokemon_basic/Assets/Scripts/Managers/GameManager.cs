using UnityEngine;
using Mapbox.Unity.Map;

public class GameManager : MonoBehaviour
{
    [Header("Game Components")]
    public AbstractMap map;
    public PlayerController playerController;
    public MapTileManager mapTileManager;
    
    [Header("Debug Settings")]
    public bool enableDebugUI = true;
    
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
            
        if (mapTileManager == null)
            mapTileManager = FindObjectOfType<MapTileManager>();
        
        Debug.Log("Game initialized successfully!");
    }
    
    void OnGUI()
    {
        if (!enableDebugUI) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("PMW Debug Info", EditorStyles.boldLabel);
        
        if (playerController != null)
        {
            var location = playerController.GetCurrentLocation();
            GUILayout.Label($"Player Location: {location.x:F6}, {location.y:F6}");
        }
        
        if (mapTileManager != null)
        {
            var loadedTiles = mapTileManager.GetLoadedTiles();
            GUILayout.Label($"Loaded Tiles: {loadedTiles.Count}");
        }
        
        GUILayout.Label("Controls:");
        GUILayout.Label("WASD / Arrow Keys - Move");
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}