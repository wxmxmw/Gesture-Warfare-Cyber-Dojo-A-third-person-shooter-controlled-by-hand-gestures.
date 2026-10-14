using UnityEngine;
using System.Collections;

// 场景移动模式枚举
public enum SceneMoveMode
{
    NormalMove,         // 普通位移模式（手势控制位置）
    RotationControl,    // 旋转控制模式（手势控制旋转）
    OrbitMode,          // 绕点旋转模式（绕某点圆周运动）
    DistortionMode      // 畸变模式（在普通移动基础上增加畸变效果）
}

// 场景切换效果枚举
public enum SceneTransitionType
{
    Instant,            // 立即切换（无过渡）
    Smooth,             // 平滑移动切换
    Fade,               // 渐变切换（黑屏过渡）
    FadeWhite,          // 白色渐变切换
    SlideLeft,          // 向左滑动切换
    SlideRight,         // 向右滑动切换
    Zoom,               // 缩放切换（先缩小再放大）
    Rotate              // 旋转切换
}

/// <summary>
/// 背景相机控制器
/// 根据手部位置控制背景相机的位移，实现伪3D效果
/// 支持多种移动模式和场景切换效果
/// </summary>
public class BackgroundCameraController : MonoBehaviour
{
    [Header("相机设置")]
    [SerializeField] private Camera backgroundCamera;
    [SerializeField] private Vector3 basePosition = new Vector3(0, 0, 1000); // 基础位置
    
    [Header("移动范围限制")]
    [SerializeField] private float maxXOffset = 20f; // X轴最大偏移量
    [SerializeField] private float minXOffset = -20f; // X轴最小偏移量
    [SerializeField] private float maxYOffset = 0f; // Y轴最大偏移量
    [SerializeField] private float minYOffset = -2f; // Y轴最小偏移量
    
    [Header("移动参数")]
    [SerializeField] private float moveSensitivity = 0.5f; // 移动敏感度
    [SerializeField] private float smoothTime = 0.3f; // 平滑移动时间
    [SerializeField] private bool enableVerticalMovement = true; // 是否启用垂直移动
    
    [Header("场景切换设置")]
    [SerializeField] private SceneConfig[] sceneConfigs; // 场景配置数组
    [SerializeField] private int currentSceneIndex = 0; // 当前场景索引
    
    [Header("调试信息")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 私有变量
    private Vector3 lastValidPosition; // 保存最后一个有效位置
    private Vector3 targetPosition; // 目标位置
    private Vector3 velocity; // 平滑移动速度
    private bool hasHandData = false; // 是否有手部数据
    private bool isAutoMoving = false; // 是否正在自动移动
    private Coroutine autoMoveCoroutine; // 自动移动协程
    
    // 旋转控制模式变量
    private Quaternion targetRotation; // 目标旋转
    private Quaternion lastValidRotation; // 最后有效旋转
    private Vector3 rotationVelocity; // 旋转速度
    
    // 绕点旋转模式变量
    private float currentOrbitAngle = 0f; // 当前绕点旋转角度
    private float targetOrbitAngle = 0f; // 目标绕点旋转角度
    
    // 畸变效果变量
    private Material distortionMaterial; // 畸变材质
    private float distortionTime = 0f; // 畸变时间计数器
    
    // 当前移动模式
    private SceneMoveMode currentMoveMode = SceneMoveMode.NormalMove;
    
    // 属性
    public Vector3 CurrentPosition => backgroundCamera.transform.position;
    public bool HasHandData => hasHandData;
    public int CurrentSceneIndex => currentSceneIndex;
    public bool IsAutoMoving => isAutoMoving;
    
    [System.Serializable]
    public class SceneConfig
    {
        [Header("场景信息")]
        public string sceneName = "Default Scene";
        public Vector3 sceneBasePosition = new Vector3(0, 0, 1000);
        
        [Header("场景移动模式")]
        public SceneMoveMode moveMode = SceneMoveMode.NormalMove;
        
        [Header("移动范围（NormalMove模式）")]
        public float minXOffset = -20f;
        public float maxXOffset = 20f;
        public float minYOffset = -2f;
        public float maxYOffset = 0f;
        
        [Header("移动参数")]
        public float moveSensitivity = 0.5f;
        public float smoothTime = 0.3f;
        public bool enableVerticalMovement = true;
        
        [Header("旋转控制模式设置（RotationControl模式）")]
        public float rotationSensitivity = 0.5f; // 旋转敏感度
        public float minRotationX = -30f; // X轴最小旋转角度
        public float maxRotationX = 30f;  // X轴最大旋转角度
        public float minRotationY = -30f; // Y轴最小旋转角度
        public float maxRotationY = 30f;  // Y轴最大旋转角度
        public bool enableRotationZ = false; // 是否启用Z轴旋转
        public float rotationZSpeed = 10f; // Z轴自动旋转速度
        
        [Header("绕点旋转模式设置（OrbitMode模式）")]
        public Vector3 orbitCenter = Vector3.zero; // 旋转中心点
        public float orbitRadius = 100f; // 旋转半径
        public float orbitSpeed = 30f; // 旋转速度（度/秒）
        public bool orbitAutoRotate = false; // 是否自动旋转
        public bool orbitHandControl = true; // 是否允许手势控制
        public bool alwaysLookAtCenter = true; // 是否始终朝向中心点
        
        [Header("畸变模式设置（DistortionMode模式）")]
        public float distortionStrength = 0.1f; // 畸变强度
        public float distortionFrequency = 1f; // 畸变频率
        public bool useRadialDistortion = false; // 使用径向畸变
        
        [Header("自动移动设置")]
        public bool hasAutoMovement = false;
        public Vector3 autoMoveDirection = Vector3.right;
        public float autoMoveSpeed = 1f;
        public float autoMoveDuration = 5f;
        public AnimationCurve autoMoveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("切换设置")]
        public SceneTransitionType transitionType = SceneTransitionType.Smooth;
        public float transitionDuration = 2f;
        public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public Color fadeColor = Color.black; // 渐变颜色（用于Fade类型）
    }
    
    private void Awake()
    {
        InitializeCamera();
    }
    
    private void Start()
    {
        // 初始化位置
        if (sceneConfigs != null && sceneConfigs.Length > 0)
        {
            ApplySceneConfig(sceneConfigs[currentSceneIndex]);
        }
        else
        {
            backgroundCamera.transform.position = basePosition;
            lastValidPosition = basePosition;
            targetPosition = basePosition;
        }
    }
    
    private void Update()
    {
        UpdateCameraPosition();
    }
    
    /// <summary>
    /// 初始化相机
    /// </summary>
    private void InitializeCamera()
    {
        if (backgroundCamera == null)
        {
            backgroundCamera = GetComponent<Camera>();
        }
        
        if (backgroundCamera == null)
        {
            Debug.LogError("BackgroundCameraController: 未找到Camera组件！");
            return;
        }
        
        // 设置相机基础位置
        backgroundCamera.transform.position = basePosition;
    }
    
    /// <summary>
    /// 更新相机位置
    /// </summary>
    private void UpdateCameraPosition()
    {
        // 根据当前移动模式调用不同的更新方法
        switch (currentMoveMode)
        {
            case SceneMoveMode.NormalMove:
                UpdateNormalMove();
                break;
            case SceneMoveMode.RotationControl:
                UpdateRotationControl();
                break;
            case SceneMoveMode.OrbitMode:
                UpdateOrbitMode();
                break;
            case SceneMoveMode.DistortionMode:
                UpdateDistortionMode();
                break;
        }
    }
    
    /// <summary>
    /// 更新普通移动模式
    /// </summary>
    private void UpdateNormalMove()
    {
        // 检查是否有手部数据可用
        if (InputManager_Hand.Instance != null && 
            (InputManager_Hand.Instance.HasHandData(1) || InputManager_Hand.Instance.HasHandData(2)))
        {
            Vector3 handPosition = InputManager_Hand.Instance.GetPlayerPosition();
            
            if (handPosition.x != 0)
            {
                // 计算目标位置
                Vector3 newTargetPosition = CalculateTargetPosition(handPosition);
                
                // 限制在指定范围内
                newTargetPosition = ClampPosition(newTargetPosition);
                
                targetPosition = newTargetPosition;
                lastValidPosition = newTargetPosition;
                hasHandData = true;
            }
            else
            {
                // 手腕出屏幕时，保持在最后有效位置
                targetPosition = lastValidPosition;
                hasHandData = false;
            }
        }
        else
        {
            // 当手部数据不可用时，保持在最后有效位置
            targetPosition = lastValidPosition;
            hasHandData = false;
        }
        
        // 平滑移动到目标位置
        backgroundCamera.transform.position = Vector3.SmoothDamp(
            backgroundCamera.transform.position, 
            targetPosition, 
            ref velocity, 
            smoothTime
        );
    }
    
    /// <summary>
    /// 更新旋转控制模式
    /// </summary>
    private void UpdateRotationControl()
    {
        SceneConfig config = GetCurrentSceneConfig();
        if (config == null) return;
        
        // 检查是否有手部数据
        if (InputManager_Hand.Instance != null && 
            (InputManager_Hand.Instance.HasHandData(1) || InputManager_Hand.Instance.HasHandData(2)))
        {
            Vector3 handPosition = InputManager_Hand.Instance.GetPlayerPosition();
            
            if (handPosition.x != 0)
            {
                // 计算目标旋转
                Quaternion newTargetRotation = CalculateTargetRotation(handPosition, config);
                targetRotation = newTargetRotation;
                lastValidRotation = newTargetRotation;
                hasHandData = true;
            }
            else
            {
                targetRotation = lastValidRotation;
                hasHandData = false;
            }
        }
        else
        {
            targetRotation = lastValidRotation;
            hasHandData = false;
        }
        
        // Z轴自动旋转
        if (config.enableRotationZ)
        {
            Vector3 currentEuler = targetRotation.eulerAngles;
            currentEuler.z += config.rotationZSpeed * Time.deltaTime;
            targetRotation = Quaternion.Euler(currentEuler);
        }
        
        // 平滑旋转到目标角度
        backgroundCamera.transform.rotation = Quaternion.Slerp(
            backgroundCamera.transform.rotation,
            targetRotation,
            Time.deltaTime / smoothTime
        );
        
        // 位置保持在基础位置
        backgroundCamera.transform.position = basePosition;
    }
    
    /// <summary>
    /// 更新绕点旋转模式
    /// </summary>
    private void UpdateOrbitMode()
    {
        SceneConfig config = GetCurrentSceneConfig();
        if (config == null) return;
        
        // 自动旋转
        if (config.orbitAutoRotate)
        {
            currentOrbitAngle += config.orbitSpeed * Time.deltaTime;
        }
        
        // 手势控制旋转角度
        if (config.orbitHandControl && InputManager_Hand.Instance != null && 
            (InputManager_Hand.Instance.HasHandData(1) || InputManager_Hand.Instance.HasHandData(2)))
        {
            Vector3 handPosition = InputManager_Hand.Instance.GetPlayerPosition();
            
            if (handPosition.x != 0)
            {
                // 根据手势位置计算目标角度
                float screenCenterX = Screen.width * 0.5f;
                float xOffset = (handPosition.x - screenCenterX) / screenCenterX; // -1 到 1
                targetOrbitAngle = xOffset * 180f; // -180 到 180 度
                hasHandData = true;
            }
            else
            {
                hasHandData = false;
            }
        }
        
        // 平滑插值到目标角度
        if (config.orbitHandControl && hasHandData)
        {
            currentOrbitAngle = Mathf.LerpAngle(currentOrbitAngle, targetOrbitAngle, Time.deltaTime / smoothTime);
        }
        
        // 计算绕点位置
        float angleRad = currentOrbitAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Cos(angleRad) * config.orbitRadius,
            0f,
            Mathf.Sin(angleRad) * config.orbitRadius
        );
        
        Vector3 orbitPosition = config.orbitCenter + offset;
        backgroundCamera.transform.position = orbitPosition;
        
        // 始终朝向中心点
        if (config.alwaysLookAtCenter)
        {
            backgroundCamera.transform.LookAt(config.orbitCenter);
        }
    }
    
    /// <summary>
    /// 更新畸变模式
    /// </summary>
    private void UpdateDistortionMode()
    {
        // 首先执行普通移动逻辑
        UpdateNormalMove();
        
        SceneConfig config = GetCurrentSceneConfig();
        if (config == null) return;
        
        // 更新畸变时间
        distortionTime += Time.deltaTime * config.distortionFrequency;
        
        // 应用畸变效果到相机位置
        Vector3 distortionOffset = Vector3.zero;
        
        if (config.useRadialDistortion)
        {
            // 径向畸变
            float radius = Vector3.Distance(backgroundCamera.transform.position, basePosition);
            float distortionAmount = Mathf.Sin(distortionTime) * config.distortionStrength * radius;
            Vector3 direction = (backgroundCamera.transform.position - basePosition).normalized;
            distortionOffset = direction * distortionAmount;
        }
        else
        {
            // 波形畸变
            distortionOffset = new Vector3(
                Mathf.Sin(distortionTime) * config.distortionStrength,
                Mathf.Cos(distortionTime * 1.3f) * config.distortionStrength,
                0f
            );
        }
        
        backgroundCamera.transform.position += distortionOffset;
    }
    
    /// <summary>
    /// 根据手部位置计算目标位置
    /// </summary>
    /// <param name="handPosition">手部位置</param>
    /// <returns>目标位置</returns>
    private Vector3 CalculateTargetPosition(Vector3 handPosition)
    {
        // 计算相对于屏幕中心的偏移量
        float screenCenterX = Screen.width * 0.5f;
        float screenCenterY = Screen.height * 0.5f;
        
        // 将手部位置转换为相对于屏幕中心的偏移
        float xOffset = (handPosition.x - screenCenterX) * moveSensitivity;
        float yOffset = 0f;
        
        if (enableVerticalMovement)
        {
            yOffset = (handPosition.y - screenCenterY) * moveSensitivity * 0.3f; // 垂直移动敏感度较低
        }
        
        // 计算目标位置
        Vector3 targetPos = basePosition + new Vector3(-xOffset, yOffset, 0f);
        
        return targetPos;
    }
    
    /// <summary>
    /// 根据手部位置计算目标旋转
    /// </summary>
    /// <param name="handPosition">手部位置</param>
    /// <param name="config">场景配置</param>
    /// <returns>目标旋转</returns>
    private Quaternion CalculateTargetRotation(Vector3 handPosition, SceneConfig config)
    {
        // 计算相对于屏幕中心的偏移量
        float screenCenterX = Screen.width * 0.5f;
        float screenCenterY = Screen.height * 0.5f;
        
        // 将手部位置转换为归一化偏移 (-1 到 1)
        float xOffset = (handPosition.x - screenCenterX) / screenCenterX;
        float yOffset = (handPosition.y - screenCenterY) / screenCenterY;
        
        // 根据偏移计算旋转角度
        float rotationY = xOffset * config.rotationSensitivity * (config.maxRotationY - config.minRotationY);
        float rotationX = -yOffset * config.rotationSensitivity * (config.maxRotationX - config.minRotationX);
        
        // 限制旋转范围
        rotationY = Mathf.Clamp(rotationY, config.minRotationY, config.maxRotationY);
        rotationX = Mathf.Clamp(rotationX, config.minRotationX, config.maxRotationX);
        
        return Quaternion.Euler(rotationX, rotationY, 0f);
    }
    
    /// <summary>
    /// 获取当前场景配置
    /// </summary>
    /// <returns>当前场景配置</returns>
    private SceneConfig GetCurrentSceneConfig()
    {
        if (sceneConfigs != null && currentSceneIndex >= 0 && currentSceneIndex < sceneConfigs.Length)
        {
            return sceneConfigs[currentSceneIndex];
        }
        return null;
    }
    
    /// <summary>
    /// 限制位置在指定范围内
    /// </summary>
    /// <param name="position">要限制的位置</param>
    /// <returns>限制后的位置</returns>
    private Vector3 ClampPosition(Vector3 position)
    {
        float clampedX = Mathf.Clamp(position.x, basePosition.x + minXOffset, basePosition.x + maxXOffset);
        float clampedY = Mathf.Clamp(position.y, basePosition.y + minYOffset, basePosition.y + maxYOffset);
        
        return new Vector3(clampedX, clampedY, position.z);
    }
    
    /// <summary>
    /// 切换到指定场景
    /// </summary>
    /// <param name="sceneIndex">场景索引</param>
    /// <param name="withTransition">是否使用过渡动画</param>
    public void SwitchToScene(int sceneIndex, bool withTransition = true)
    {
        if (sceneConfigs == null || sceneIndex < 0 || sceneIndex >= sceneConfigs.Length)
        {
            Debug.LogWarning($"BackgroundCameraController: 无效的场景索引 {sceneIndex}");
            return;
        }
        
        if (withTransition)
        {
            StartCoroutine(TransitionToScene(sceneIndex));
        }
        else
        {
            ApplySceneConfig(sceneConfigs[sceneIndex]);
            currentSceneIndex = sceneIndex;
        }
    }
    
    /// <summary>
    /// 切换到指定场景（通过场景名称）
    /// </summary>
    /// <param name="sceneName">场景名称</param>
    /// <param name="withTransition">是否使用过渡动画</param>
    public void SwitchToScene(string sceneName, bool withTransition = true)
    {
        int sceneIndex = FindSceneIndexByName(sceneName);
        if (sceneIndex >= 0)
        {
            SwitchToScene(sceneIndex, withTransition);
        }
        else
        {
            Debug.LogWarning($"BackgroundCameraController: 未找到场景 '{sceneName}'");
        }
    }
    
    /// <summary>
    /// 过渡到指定场景的协程
    /// </summary>
    /// <param name="targetSceneIndex">目标场景索引</param>
    private IEnumerator TransitionToScene(int targetSceneIndex)
    {
        SceneConfig targetConfig = sceneConfigs[targetSceneIndex];
        
        // 停止自动移动
        StopAutoMovement();
        
        // 根据不同的过渡类型执行不同的过渡效果
        switch (targetConfig.transitionType)
        {
            case SceneTransitionType.Instant:
                yield return TransitionInstant(targetConfig);
                break;
            case SceneTransitionType.Smooth:
                yield return TransitionSmooth(targetConfig);
                break;
            case SceneTransitionType.Fade:
                yield return TransitionFade(targetConfig, false);
                break;
            case SceneTransitionType.FadeWhite:
                yield return TransitionFade(targetConfig, true);
                break;
            case SceneTransitionType.SlideLeft:
                yield return TransitionSlide(targetConfig, Vector3.left);
                break;
            case SceneTransitionType.SlideRight:
                yield return TransitionSlide(targetConfig, Vector3.right);
                break;
            case SceneTransitionType.Zoom:
                yield return TransitionZoom(targetConfig);
                break;
            case SceneTransitionType.Rotate:
                yield return TransitionRotate(targetConfig);
                break;
        }
        
        // 应用新场景配置
        ApplySceneConfig(targetConfig);
        currentSceneIndex = targetSceneIndex;
        
        // 如果新场景有自动移动，启动它
        if (targetConfig.hasAutoMovement)
        {
            StartAutoMovement(targetConfig);
        }
    }
    
    /// <summary>
    /// 立即切换场景（无过渡）
    /// </summary>
    private IEnumerator TransitionInstant(SceneConfig targetConfig)
    {
        backgroundCamera.transform.position = targetConfig.sceneBasePosition;
        backgroundCamera.transform.rotation = Quaternion.identity;
        yield return null;
    }
    
    /// <summary>
    /// 平滑移动切换场景
    /// </summary>
    private IEnumerator TransitionSmooth(SceneConfig targetConfig)
    {
        Vector3 startPosition = backgroundCamera.transform.position;
        Vector3 targetPosition = targetConfig.sceneBasePosition;
        Quaternion startRotation = backgroundCamera.transform.rotation;
        Quaternion targetRotation = Quaternion.identity;
        
        float elapsedTime = 0f;
        float duration = targetConfig.transitionDuration;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            float curveValue = targetConfig.transitionCurve.Evaluate(progress);
            
            backgroundCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, curveValue);
            backgroundCamera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, curveValue);
            
            yield return null;
        }
        
        backgroundCamera.transform.position = targetPosition;
        backgroundCamera.transform.rotation = targetRotation;
    }
    
    /// <summary>
    /// 渐变切换场景（黑屏或白屏过渡）
    /// </summary>
    private IEnumerator TransitionFade(SceneConfig targetConfig, bool useWhite)
    {
        // 创建渐变遮罩（使用UI Image或者Camera的ClearFlags）
        GameObject fadeObj = new GameObject("FadeTransition");
        Canvas canvas = fadeObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        
        UnityEngine.UI.Image fadeImage = fadeObj.AddComponent<UnityEngine.UI.Image>();
        fadeImage.color = useWhite ? Color.white : targetConfig.fadeColor;
        fadeImage.rectTransform.anchorMin = Vector2.zero;
        fadeImage.rectTransform.anchorMax = Vector2.one;
        fadeImage.rectTransform.sizeDelta = Vector2.zero;
        
        float halfDuration = targetConfig.transitionDuration * 0.5f;
        
        // 淡入（变黑/变白）
        float elapsedTime = 0f;
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / halfDuration;
            Color color = fadeImage.color;
            color.a = progress;
            fadeImage.color = color;
            yield return null;
        }
        
        // 在完全遮罩时切换场景
        backgroundCamera.transform.position = targetConfig.sceneBasePosition;
        backgroundCamera.transform.rotation = Quaternion.identity;
        
        // 淡出（变透明）
        elapsedTime = 0f;
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / halfDuration;
            Color color = fadeImage.color;
            color.a = 1f - progress;
            fadeImage.color = color;
            yield return null;
        }
        
        // 销毁渐变对象
        Destroy(fadeObj);
    }
    
    /// <summary>
    /// 滑动切换场景
    /// </summary>
    private IEnumerator TransitionSlide(SceneConfig targetConfig, Vector3 slideDirection)
    {
        Vector3 startPosition = backgroundCamera.transform.position;
        Vector3 targetPosition = targetConfig.sceneBasePosition;
        
        // 计算滑动距离
        float slideDistance = Vector3.Distance(startPosition, targetPosition) * 2f;
        Vector3 slideOffset = slideDirection * slideDistance;
        
        Vector3 exitPosition = startPosition + slideOffset;
        Vector3 enterPosition = targetPosition - slideOffset;
        
        float halfDuration = targetConfig.transitionDuration * 0.5f;
        
        // 滑出当前场景
        float elapsedTime = 0f;
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / halfDuration;
            float curveValue = targetConfig.transitionCurve.Evaluate(progress);
            
            backgroundCamera.transform.position = Vector3.Lerp(startPosition, exitPosition, curveValue);
            yield return null;
        }
        
        // 跳转到新场景的入口位置
        backgroundCamera.transform.position = enterPosition;
        
        // 滑入新场景
        elapsedTime = 0f;
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / halfDuration;
            float curveValue = targetConfig.transitionCurve.Evaluate(progress);
            
            backgroundCamera.transform.position = Vector3.Lerp(enterPosition, targetPosition, curveValue);
            yield return null;
        }
        
        backgroundCamera.transform.position = targetPosition;
    }
    
    /// <summary>
    /// 缩放切换场景
    /// </summary>
    private IEnumerator TransitionZoom(SceneConfig targetConfig)
    {
        Vector3 startPosition = backgroundCamera.transform.position;
        Vector3 targetPosition = targetConfig.sceneBasePosition;
        float startFOV = backgroundCamera.fieldOfView;
        float minFOV = startFOV * 0.3f; // 缩小到30%
        
        float halfDuration = targetConfig.transitionDuration * 0.5f;
        
        // 缩小
        float elapsedTime = 0f;
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / halfDuration;
            float curveValue = targetConfig.transitionCurve.Evaluate(progress);
            
            backgroundCamera.fieldOfView = Mathf.Lerp(startFOV, minFOV, curveValue);
            yield return null;
        }
        
        // 切换场景
        backgroundCamera.transform.position = targetPosition;
        backgroundCamera.transform.rotation = Quaternion.identity;
        
        // 放大
        elapsedTime = 0f;
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / halfDuration;
            float curveValue = targetConfig.transitionCurve.Evaluate(progress);
            
            backgroundCamera.fieldOfView = Mathf.Lerp(minFOV, startFOV, curveValue);
            yield return null;
        }
        
        backgroundCamera.fieldOfView = startFOV;
    }
    
    /// <summary>
    /// 旋转切换场景
    /// </summary>
    private IEnumerator TransitionRotate(SceneConfig targetConfig)
    {
        Vector3 startPosition = backgroundCamera.transform.position;
        Vector3 targetPosition = targetConfig.sceneBasePosition;
        Quaternion startRotation = backgroundCamera.transform.rotation;
        
        float elapsedTime = 0f;
        float duration = targetConfig.transitionDuration;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            float curveValue = targetConfig.transitionCurve.Evaluate(progress);
            
            // 位置插值
            backgroundCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, curveValue);
            
            // 旋转一圈（360度）
            float rotationAngle = curveValue * 360f;
            backgroundCamera.transform.rotation = startRotation * Quaternion.Euler(0f, 0f, rotationAngle);
            
            yield return null;
        }
        
        backgroundCamera.transform.position = targetPosition;
        backgroundCamera.transform.rotation = Quaternion.identity;
    }
    
    /// <summary>
    /// 应用场景配置
    /// </summary>
    /// <param name="config">场景配置</param>
    private void ApplySceneConfig(SceneConfig config)
    {
        basePosition = config.sceneBasePosition;
        minXOffset = config.minXOffset;
        maxXOffset = config.maxXOffset;
        minYOffset = config.minYOffset;
        maxYOffset = config.maxYOffset;
        moveSensitivity = config.moveSensitivity;
        smoothTime = config.smoothTime;
        enableVerticalMovement = config.enableVerticalMovement;
        
        // 设置当前移动模式
        currentMoveMode = config.moveMode;
        
        // 设置相机位置
        backgroundCamera.transform.position = basePosition;
        lastValidPosition = basePosition;
        targetPosition = basePosition;
        velocity = Vector3.zero;
        
        // 重置旋转
        backgroundCamera.transform.rotation = Quaternion.identity;
        targetRotation = Quaternion.identity;
        lastValidRotation = Quaternion.identity;
        
        // 根据移动模式进行特殊初始化
        switch (config.moveMode)
        {
            case SceneMoveMode.OrbitMode:
                // 初始化绕点旋转模式
                currentOrbitAngle = 0f;
                targetOrbitAngle = 0f;
                break;
            case SceneMoveMode.DistortionMode:
                // 初始化畸变模式
                distortionTime = 0f;
                break;
        }
    }
    
    /// <summary>
    /// 启动自动移动
    /// </summary>
    /// <param name="config">场景配置</param>
    private void StartAutoMovement(SceneConfig config)
    {
        if (autoMoveCoroutine != null)
        {
            StopCoroutine(autoMoveCoroutine);
        }
        
        autoMoveCoroutine = StartCoroutine(AutoMoveCoroutine(config));
        Debug.Log("start auto background moving");
    }
    
    /// <summary>
    /// 停止自动移动
    /// </summary>
    public void StopAutoMovement()
    {
        if (autoMoveCoroutine != null)
        {
            StopCoroutine(autoMoveCoroutine);
            autoMoveCoroutine = null;
        }
        isAutoMoving = false;
    }
    
    /// <summary>
    /// 自动移动协程
    /// </summary>
    /// <param name="config">场景配置</param>
    private IEnumerator AutoMoveCoroutine(SceneConfig config)
    {
        /*
        isAutoMoving = true;
        Vector3 startPosition = backgroundCamera.transform.position;
        Vector3 endPosition = startPosition + config.autoMoveDirection * config.autoMoveSpeed * config.autoMoveDuration;
        
        float elapsedTime = 0f;
        float duration = config.autoMoveDuration;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            float curveValue = config.autoMoveCurve.Evaluate(progress);
            
            Vector3 currentPosition = Vector3.Lerp(startPosition, endPosition, curveValue);
            currentPosition = ClampPosition(currentPosition);
            
            backgroundCamera.transform.position = currentPosition;
            lastValidPosition = currentPosition;
            targetPosition = currentPosition;
            
            yield return null;
        }
        
        isAutoMoving = false;
        autoMoveCoroutine = null;
        */

        isAutoMoving = true;
        // 只在Y方向上移动
        if(config.autoMoveDirection.y != 0 && config.autoMoveDirection.x == 0 && config.autoMoveDirection.z == 0)
        {
            float startY = backgroundCamera.transform.position.y;
            float endY = startY + config.autoMoveDirection.y * config.autoMoveSpeed * config.autoMoveDuration;

            float elapsedTime = 0f;
            float duration = config.autoMoveDuration;

            while (elapsedTime < duration) 
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / duration;
                float curveValue = config.autoMoveCurve.Evaluate(progress);

                float currentY = startY + (endY - startY) * curveValue;

                Vector3 AddVector3 = new Vector3(0, currentY - backgroundCamera.transform.position.y, 0);

                backgroundCamera.transform.position += AddVector3;

                yield return null;
            }

        }
        isAutoMoving = false;
        autoMoveCoroutine = null;
    }
    
    /// <summary>
    /// 根据场景名称查找场景索引
    /// </summary>
    /// <param name="sceneName">场景名称</param>
    /// <returns>场景索引，未找到返回-1</returns>
    private int FindSceneIndexByName(string sceneName)
    {
        if (sceneConfigs == null) return -1;
        
        for (int i = 0; i < sceneConfigs.Length; i++)
        {
            if (sceneConfigs[i].sceneName == sceneName)
            {
                return i;
            }
        }
        return -1;
    }
    
    /// <summary>
    /// 设置移动敏感度
    /// </summary>
    /// <param name="sensitivity">敏感度值</param>
    public void SetMoveSensitivity(float sensitivity)
    {
        moveSensitivity = Mathf.Max(0f, sensitivity);
    }
    
    /// <summary>
    /// 设置平滑移动时间
    /// </summary>
    /// <param name="smoothTime">平滑时间</param>
    public void SetSmoothTime(float smoothTime)
    {
        this.smoothTime = Mathf.Max(0f, smoothTime);
    }
    
    /// <summary>
    /// 设置移动范围
    /// </summary>
    /// <param name="minX">X轴最小偏移</param>
    /// <param name="maxX">X轴最大偏移</param>
    /// <param name="minY">Y轴最小偏移</param>
    /// <param name="maxY">Y轴最大偏移</param>
    public void SetMoveRange(float minX, float maxX, float minY, float maxY)
    {
        minXOffset = minX;
        maxXOffset = maxX;
        minYOffset = minY;
        maxYOffset = maxY;
    }
    
    /// <summary>
    /// 重置相机位置到基础位置
    /// </summary>
    public void ResetToBasePosition()
    {
        backgroundCamera.transform.position = basePosition;
        lastValidPosition = basePosition;
        targetPosition = basePosition;
        velocity = Vector3.zero;
    }
    
    /// <summary>
    /// 启用/禁用垂直移动
    /// </summary>
    /// <param name="enable">是否启用</param>
    public void SetVerticalMovement(bool enable)
    {
        enableVerticalMovement = enable;
    }
    
    // 编辑器方法
    [ContextMenu("重置到基础位置")]
    public void ResetPosition()
    {
        ResetToBasePosition();
    }
    
    [ContextMenu("测试移动到左边界")]
    public void TestMoveToLeftBoundary()
    {
        targetPosition = new Vector3(basePosition.x + minXOffset, basePosition.y, basePosition.z);
    }
    
    [ContextMenu("测试移动到右边界")]
    public void TestMoveToRightBoundary()
    {
        targetPosition = new Vector3(basePosition.x + maxXOffset, basePosition.y, basePosition.z);
    }
    
    [ContextMenu("切换到下一个场景")]
    public void SwitchToNextScene()
    {
        if (sceneConfigs != null && sceneConfigs.Length > 0)
        {
            int nextIndex = (currentSceneIndex + 1) % sceneConfigs.Length;
            SwitchToScene(nextIndex);
        }
    }
    
    [ContextMenu("停止自动移动")]
    public void StopAutoMovementEditor()
    {
        StopAutoMovement();
    }
    
    // 调试绘制
    private void OnDrawGizmosSelected()
    {
        if (backgroundCamera == null) return;
        
        // 绘制移动范围
        Gizmos.color = Color.yellow;
        Vector3 center = basePosition;
        Vector3 size = new Vector3(maxXOffset - minXOffset, maxYOffset - minYOffset, 0.1f);
        Gizmos.DrawWireCube(center + new Vector3((maxXOffset + minXOffset) * 0.5f, (maxYOffset + minYOffset) * 0.5f, 0), size);
        
        // 绘制当前位置
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(backgroundCamera.transform.position, 2f);
        
        // 绘制目标位置
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(targetPosition, 1.5f);
        
        // 绘制场景配置位置
        if (sceneConfigs != null)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < sceneConfigs.Length; i++)
            {
                if (i == currentSceneIndex)
                {
                    Gizmos.color = Color.cyan;
                }
                else
                {
                    Gizmos.color = Color.blue;
                }
                Gizmos.DrawWireSphere(sceneConfigs[i].sceneBasePosition, 3f);
            }
        }
    }
}
