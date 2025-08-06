using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target; // 玩家对象
    public bool autoFindTarget = true;
    
    [Header("Camera Position")]
    public float followHeight = 10f; // 摄像机高度（45°俯视角需要）
    public float followSpeed = 2f; // 跟随速度
    
    // 固定45度俯视角设置
    private readonly Vector3 fixedCameraAngle = new Vector3(45f, 0f, 0f);
    private readonly Vector3 cameraOffset = new Vector3(0, 1f, -1f); // 45°俯视的位置偏移比例
    
    [Header("Mouse Control")]
    public bool enableMouseControl = true;
    public float mouseXSensitivity = 2f; // 水平旋转敏感度（仅用于调整水平角度）
    
    // 私有变量
    private float currentYRotation = 0f; // 水平旋转角度
    private bool isRotating = false;
    
    // 摄像机组件
    private Camera playerCamera;
    
    void Start()
    {
        InitializeCamera();
    }
    
    void InitializeCamera()
    {
        // 获取摄像机组件
        playerCamera = GetComponent<Camera>();
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        
        // 自动查找目标
        if (autoFindTarget && target == null)
        {
            PlayerController playerController = FindObjectOfType<PlayerController>();
            if (playerController != null)
            {
                target = playerController.transform;
                Debug.Log($"CameraController: Found player target at {target.position}");
            }
            else
            {
                Debug.LogWarning("CameraController: PlayerController not found!");
            }
        }
        
        if (target != null)
        {
            // 设置初始45°俯视角度
            transform.eulerAngles = fixedCameraAngle;
            currentYRotation = 0f;
            
            // 设置初始位置
            Vector3 initialOffset = cameraOffset * followHeight;
            transform.position = target.position + initialOffset;
            
            Debug.Log($"Camera Controller initialized with 45° top-down view, target: {target.name}");
        }
        else
        {
            Debug.LogWarning("CameraController: No target found! Camera will not follow.");
        }
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        HandleMouseInput();
        UpdateCameraPosition();
        UpdateCameraRotation();
    }
    
    void HandleMouseInput()
    {
        if (!enableMouseControl) return;
        
        // 检查鼠标右键状态
        if (Input.GetMouseButtonDown(1))
        {
            isRotating = true;
            Cursor.lockState = CursorLockMode.Locked;
        }
        
        if (Input.GetMouseButtonUp(1))
        {
            isRotating = false;
            Cursor.lockState = CursorLockMode.None;
        }
        
        // 只有在按住右键时才处理鼠标移动（仅水平旋转）
        if (isRotating)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseXSensitivity;
            
            // 只允许水平旋转，俯视角固定为45°
            currentYRotation += mouseX;
            
            // 将角度标准化到0-360度范围
            while (currentYRotation > 360f) currentYRotation -= 360f;
            while (currentYRotation < 0f) currentYRotation += 360f;
        }
        
        // ESC键取消旋转模式
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isRotating = false;
            Cursor.lockState = CursorLockMode.None;
        }
    }
    
    void UpdateCameraPosition()
    {
        if (target == null)
        {
            Debug.LogWarning("CameraController: Target is null in UpdateCameraPosition!");
            return;
        }
        
        // 计算45°俯视角的位置偏移（基于水平旋转）
        Quaternion horizontalRotation = Quaternion.Euler(0, currentYRotation, 0);
        Vector3 rotatedOffset = horizontalRotation * (cameraOffset * followHeight);
        
        // 计算目标位置
        Vector3 targetPosition = target.position + rotatedOffset;
        
        // 平滑移动到目标位置
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
        
        // 偶尔输出调试信息
        if (Time.frameCount % 120 == 0) // 每2秒左右输出一次
        {
            Debug.Log($"Camera 45° follow - Target: {target.position}, Camera: {transform.position}, Y Rotation: {currentYRotation:F1}°");
        }
    }
    
    void UpdateCameraRotation()
    {
        // 保持固定45°俯视角 + 当前水平旋转
        Vector3 targetRotation = new Vector3(45f, currentYRotation, 0f);
        
        // 直接设置角度（无需平滑，因为只有水平角度会改变）
        transform.eulerAngles = targetRotation;
    }
    
    // 公共方法：设置目标
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            Debug.Log($"Camera target set to: {target.name}");
        }
    }
    
    // 公共方法：重置摄像机角度
    public void ResetCamera()
    {
        currentYRotation = 0f;
        transform.eulerAngles = fixedCameraAngle;
        Debug.Log("Camera reset to default 45° top-down view");
    }
    
    // 公共方法：设置摄像机高度
    public void SetFollowHeight(float newHeight)
    {
        followHeight = Mathf.Max(1f, newHeight);
    }
    
    // 公共方法：获取当前是否在旋转模式
    public bool IsRotating()
    {
        return isRotating;
    }
    
    // 公共方法：获取当前摄像机水平旋转角度
    public float GetCurrentRotation()
    {
        return currentYRotation;
    }
    
    // 公共方法：设置摄像机水平旋转角度
    public void SetCameraRotation(float yRotation)
    {
        currentYRotation = yRotation;
        while (currentYRotation > 360f) currentYRotation -= 360f;
        while (currentYRotation < 0f) currentYRotation += 360f;
    }
    
    // 调试信息
    void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            // 绘制目标到摄像机的连线
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(target.position, transform.position);
            
            // 绘制目标位置
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(target.position, 0.5f);
            
            // 绘制摄像机视角范围
            Gizmos.color = Color.green;
            if (playerCamera != null)
            {
                Gizmos.DrawFrustum(transform.position, playerCamera.fieldOfView, playerCamera.farClipPlane, playerCamera.nearClipPlane, playerCamera.aspect);
            }
        }
    }
}