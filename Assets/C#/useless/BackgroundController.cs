using UnityEngine;

public class BackgroundController : MonoBehaviour
{
    [Header("Background Settings")]
    [SerializeField] private SpriteRenderer backgroundSprite;
    [SerializeField] private Sprite backgroundTexture;
    [SerializeField] private Color backgroundColor = Color.black;
    [SerializeField] private bool tileBackground = true;
    [SerializeField] private float backgroundDistance = 50f; // 背景距离相机的距离
    
    [Header("Border Settings")]
    [SerializeField] private bool createBorders = true;
    [SerializeField] private SpriteRenderer[] borderSprites = new SpriteRenderer[4]; // 上下左右
    [SerializeField] private Sprite borderTexture;
    [SerializeField] private Color borderColor = Color.gray;
    [SerializeField] private float borderThickness = 1f;
    
    private Camera targetCamera;
    
    public void SetupBackground(Camera camera)
    {
        targetCamera = camera;
        CreateBackgroundSprite();
        
        if (createBorders)
            CreateBorderSprites();
            
        UpdateBackgroundSize();
    }
    
    private void CreateBackgroundSprite()
    {
        if (backgroundSprite == null)
        {
            GameObject bgObject = new GameObject("Background");
            bgObject.transform.SetParent(transform);
            backgroundSprite = bgObject.AddComponent<SpriteRenderer>();
        }
        
        // 设置背景属性
        backgroundSprite.sprite = backgroundTexture;
        backgroundSprite.color = backgroundColor;
        backgroundSprite.sortingOrder = -100; // 确保在最底层
        
        // 设置位置 - 将背景放在相机视野的最后方
        Vector3 bgPosition = targetCamera.transform.position + targetCamera.transform.forward * backgroundDistance;
        backgroundSprite.transform.position = bgPosition;
    }
    
    private void CreateBorderSprites()
    {
        string[] borderNames = { "TopBorder", "BottomBorder", "LeftBorder", "RightBorder" };
        
        for (int i = 0; i < 4; i++)
        {
            if (borderSprites[i] == null)
            {
                GameObject borderObject = new GameObject(borderNames[i]);
                borderObject.transform.SetParent(transform);
                borderSprites[i] = borderObject.AddComponent<SpriteRenderer>();
            }
            
            borderSprites[i].sprite = borderTexture;
            borderSprites[i].color = borderColor;
            borderSprites[i].sortingOrder = -99; // 在背景之上，但在游戏对象之下
        }
    }
    
    private void UpdateBackgroundSize()
    {
        if (targetCamera == null || backgroundSprite == null) return;
        
        // 根据透视相机计算背景大小
        float backgroundSize = CalculateBackgroundSizeForPerspectiveCamera();
        
        // 扩展背景以确保完全覆盖
        float expandFactor = 1.5f; // 增加扩展因子确保完全覆盖
        Vector3 backgroundScale = new Vector3(
            backgroundSize * expandFactor,
            backgroundSize * expandFactor,
            1f
        );
        
        if (backgroundTexture != null)
        {
            // 根据贴图大小调整缩放
            float textureWidth = backgroundTexture.bounds.size.x;
            float textureHeight = backgroundTexture.bounds.size.y;
            
            backgroundScale.x = (backgroundSize * expandFactor) / textureWidth;
            backgroundScale.y = (backgroundSize * expandFactor) / textureHeight;
        }
        
        backgroundSprite.transform.localScale = backgroundScale;
        
        // 更新边框
        if (createBorders && borderSprites[0] != null)
        {
            UpdateBorderPositions(backgroundSize);
        }
    }
    
    private float CalculateBackgroundSizeForPerspectiveCamera()
    {
        // 根据透视相机的视野角度和背景距离计算背景大小
        float fieldOfView = targetCamera.fieldOfView;
        float distance = backgroundDistance;
        
        // 使用三角函数计算在指定距离处的视野大小
        float height = 2f * distance * Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
        float width = height * targetCamera.aspect;
        
        // 返回较大的值确保完全覆盖
        return Mathf.Max(width, height);
    }
    
    private void UpdateBorderPositions(float backgroundSize)
    {
        Vector3 cameraPos = targetCamera.transform.position;
        Vector3 backgroundPos = cameraPos + targetCamera.transform.forward * backgroundDistance;
        
        float halfSize = backgroundSize * 0.5f;
        
        // 上边框
        if (borderSprites[0] != null)
        {
            borderSprites[0].transform.position = new Vector3(backgroundPos.x, backgroundPos.y + halfSize + borderThickness * 0.5f, backgroundPos.z - 0.1f);
            borderSprites[0].transform.localScale = new Vector3(backgroundSize + borderThickness * 2, borderThickness, 1f);
        }
        
        // 下边框
        if (borderSprites[1] != null)
        {
            borderSprites[1].transform.position = new Vector3(backgroundPos.x, backgroundPos.y - halfSize - borderThickness * 0.5f, backgroundPos.z - 0.1f);
            borderSprites[1].transform.localScale = new Vector3(backgroundSize + borderThickness * 2, borderThickness, 1f);
        }
        
        // 左边框
        if (borderSprites[2] != null)
        {
            borderSprites[2].transform.position = new Vector3(backgroundPos.x - halfSize - borderThickness * 0.5f, backgroundPos.y, backgroundPos.z - 0.1f);
            borderSprites[2].transform.localScale = new Vector3(borderThickness, backgroundSize, 1f);
        }
        
        // 右边框
        if (borderSprites[3] != null)
        {
            borderSprites[3].transform.position = new Vector3(backgroundPos.x + halfSize + borderThickness * 0.5f, backgroundPos.y, backgroundPos.z - 0.1f);
            borderSprites[3].transform.localScale = new Vector3(borderThickness, backgroundSize, 1f);
        }
    }
    
    private void Update()
    {
        // 实时更新背景大小（如果分辨率发生变化）
        if (targetCamera != null)
        {
            UpdateBackgroundSize();
        }
    }
    
    // 公共方法
    public void SetBackgroundTexture(Sprite newTexture)
    {
        backgroundTexture = newTexture;
        if (backgroundSprite != null)
            backgroundSprite.sprite = newTexture;
        UpdateBackgroundSize();
    }
    
    public void SetBackgroundColor(Color newColor)
    {
        backgroundColor = newColor;
        if (backgroundSprite != null)
            backgroundSprite.color = newColor;
    }
    
    public void SetBorderColor(Color newColor)
    {
        borderColor = newColor;
        for (int i = 0; i < borderSprites.Length; i++)
        {
            if (borderSprites[i] != null)
                borderSprites[i].color = newColor;
            }
        }
    
    // 设置背景距离
    public void SetBackgroundDistance(float distance)
    {
        backgroundDistance = distance;
        if (targetCamera != null)
        {
            UpdateBackgroundSize();
        }
    }
    
    // 获取当前背景距离
    public float GetBackgroundDistance()
    {
        return backgroundDistance;
    }
}
