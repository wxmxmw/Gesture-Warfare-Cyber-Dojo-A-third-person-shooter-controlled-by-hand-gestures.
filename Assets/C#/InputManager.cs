using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class InputManager : MonoBehaviour
{
    [Header("Crosshair Settings")]
    [SerializeField] private GameObject crosshairPrefab;
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private Image crosshairImage;
    [SerializeField] private Sprite crosshairSprite;
    [SerializeField] private Sprite muzzleFlashSprite;
    
    [Header("Fire Effect Settings")]
    [SerializeField] private float muzzleFlashDuration = 0.1f;
    [SerializeField] private float crosshairShakeIntensity = 5f;
    [SerializeField] private float crosshairShakeDuration = 0.1f;
    
    [Header("Crosshair Movement")]
    [SerializeField] private float crosshairSmoothTime = 0.1f;
    
    private Camera gameCamera;
    private Vector3 currentCrosshairPosition;
    private Vector3 targetCrosshairPosition;
    private Vector3 crosshairVelocity;
    private bool isFiring = false;
    
    // 开火效果相关
    private Image muzzleFlashImage;
    private Vector3 originalCrosshairPosition;
    private Coroutine fireEffectCoroutine;
    
    // 射击事件
    public System.Action<Vector3> OnShoot;
    
    private void Awake()
    {
        gameCamera = Camera.main ?? FindObjectOfType<Camera>();
        SetupCrosshair();
    }
    
    private void Start()
    {
        if (uiCanvas == null)
            uiCanvas = FindObjectOfType<Canvas>();
    }
    
    private void Update()
    {
        HandleInput();
        UpdateCrosshairPosition();
    }
    
    private void SetupCrosshair()
    {
        if (crosshairImage == null && uiCanvas != null)
        {
            // 创建准心UI
            GameObject crosshairGO = new GameObject("Crosshair");
            crosshairGO.transform.SetParent(uiCanvas.transform, false);
            
            crosshairImage = crosshairGO.AddComponent<Image>();
            crosshairImage.sprite = crosshairSprite;
            crosshairImage.color = Color.white;
            
            // 设置准心大小
            RectTransform rectTransform = crosshairImage.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(50, 50);
            
            // 创建枪火特效Image
            GameObject muzzleFlashGO = new GameObject("MuzzleFlash");
            muzzleFlashGO.transform.SetParent(crosshairGO.transform, false);
            
            muzzleFlashImage = muzzleFlashGO.AddComponent<Image>();
            muzzleFlashImage.sprite = muzzleFlashSprite;
            muzzleFlashImage.color = Color.white;
            muzzleFlashImage.gameObject.SetActive(false);
            
            RectTransform muzzleRect = muzzleFlashImage.GetComponent<RectTransform>();
            muzzleRect.sizeDelta = new Vector2(60, 60);
        }
    }
    
    private void HandleInput()
    {
        Vector3 inputPosition = Vector3.zero;
        bool hasInput = false;
        bool fireInput = false;
        
        // 处理鼠标输入
        if (Input.mousePresent)
        {
            inputPosition = Input.mousePosition;
            hasInput = true;
            fireInput = Input.GetMouseButtonDown(0);
        }
        
        // 处理触屏输入
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            inputPosition = touch.position;
            hasInput = true;
            fireInput = touch.phase == TouchPhase.Began;
        }
        
        if (hasInput)
        {
            // 将屏幕坐标转换为UI坐标
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                uiCanvas.GetComponent<RectTransform>(),
                inputPosition,
                uiCanvas.worldCamera,
                out Vector2 localPoint
            );
            
            targetCrosshairPosition = localPoint;
            
            // 显示准心
            if (crosshairImage != null && !crosshairImage.gameObject.activeInHierarchy)
                crosshairImage.gameObject.SetActive(true);
        }
        
        // 处理开火
        if (fireInput && !isFiring)
        {
            StartFireEffect();
            
            // 触发射击事件，传递屏幕坐标
            if (OnShoot != null)
            {
                OnShoot.Invoke(inputPosition);
            }
        }
    }
    
    private void UpdateCrosshairPosition()
    {
        if (crosshairImage != null && crosshairImage.gameObject.activeInHierarchy)
        {
            // 平滑移动准心
            currentCrosshairPosition = Vector3.SmoothDamp(
                currentCrosshairPosition,
                targetCrosshairPosition,
                ref crosshairVelocity,
                crosshairSmoothTime
            );
            
            crosshairImage.rectTransform.localPosition = currentCrosshairPosition;
        }
    }
    
    private void StartFireEffect()
    {
        if (fireEffectCoroutine != null)
            StopCoroutine(fireEffectCoroutine);
            
        fireEffectCoroutine = StartCoroutine(FireEffectCoroutine());
    }
    
    private IEnumerator FireEffectCoroutine()
    {
        isFiring = true;
        originalCrosshairPosition = crosshairImage.rectTransform.localPosition;
        
        // 显示枪火特效
        if (muzzleFlashImage != null)
        {
            muzzleFlashImage.gameObject.SetActive(true);
        }
        
        // 准心抖动效果
        float elapsedTime = 0f;
        while (elapsedTime < crosshairShakeDuration)
        {
            // 随机抖动
            Vector3 shakeOffset = new Vector3(
                Random.Range(-crosshairShakeIntensity, crosshairShakeIntensity),
                Random.Range(-crosshairShakeIntensity, crosshairShakeIntensity),
                0f
            );
            
            crosshairImage.rectTransform.localPosition = currentCrosshairPosition + shakeOffset;
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // 隐藏枪火特效
        if (muzzleFlashImage != null)
        {
            muzzleFlashImage.gameObject.SetActive(false);
        }
        
        // 恢复准心位置
        crosshairImage.rectTransform.localPosition = currentCrosshairPosition;
        
        isFiring = false;
    }
    
    // 公共方法供其他脚本调用
    public Vector3 GetCrosshairWorldPosition()
    {
        if (gameCamera != null && crosshairImage != null)
        {
            // 获取准心在屏幕上的位置
            Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(
                uiCanvas.worldCamera,
                crosshairImage.rectTransform.position
            );
            
            // 转换为世界坐标
            return gameCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
        }
        
        return Vector3.zero;
    }
    
    public Vector3 GetCrosshairScreenPosition()
    {
        if (crosshairImage != null)
        {
            return RectTransformUtility.WorldToScreenPoint(
                uiCanvas.worldCamera,
                crosshairImage.rectTransform.position
            );
        }
        return Vector3.zero;
    }
    
    public bool IsFiring()
    {
        return isFiring;
    }
    
    public void SetCrosshairSprite(Sprite newSprite)
    {
        if (crosshairImage != null)
            crosshairImage.sprite = newSprite;
    }
    
    public void SetMuzzleFlashSprite(Sprite newSprite)
    {
        if (muzzleFlashImage != null)
            muzzleFlashImage.sprite = newSprite;
    }
}
