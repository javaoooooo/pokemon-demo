using UnityEngine;

public class PlayerObject : MonoBehaviour
{
    [Header("Visual Settings")]
    public Color playerColor = Color.blue;
    public float playerSize = 1f;
    
    private Renderer playerRenderer;
    
    void Start()
    {
        SetupPlayerVisual();
    }
    
    void SetupPlayerVisual()
    {
        // 获取或创建渲染器
        playerRenderer = GetComponent<Renderer>();
        if (playerRenderer == null)
        {
            // 如果没有渲染器，创建一个简单的立方体
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(transform);
            cube.transform.localPosition = Vector3.zero;
            cube.transform.localScale = Vector3.one * playerSize;
            playerRenderer = cube.GetComponent<Renderer>();
        }
        
        // 设置颜色
        if (playerRenderer != null)
        {
            Material playerMaterial = new Material(Shader.Find("Standard"));
            playerMaterial.color = playerColor;
            playerRenderer.material = playerMaterial;
        }
    }
    
    public void SetColor(Color newColor)
    {
        playerColor = newColor;
        if (playerRenderer != null)
        {
            playerRenderer.material.color = playerColor;
        }
    }
    
    public void SetSize(float newSize)
    {
        playerSize = newSize;
        transform.localScale = Vector3.one * playerSize;
    }
}