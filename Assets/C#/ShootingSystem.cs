using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ShootingSystem : MonoBehaviour
{
    [Header("Shooting Settings")]
    [SerializeField] private float bulletDamage = 25f;
    [SerializeField] private float shootingRange = 50f;
    [SerializeField] private LayerMask enemyLayerMask = -1;
    [SerializeField] private float fireRate = 0.5f; // 射击间隔
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject muzzleFlashEffect;
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private GameObject missEffect;
    [SerializeField] private float effectDuration = 0.2f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioSource audioSource;
    
    [Header("UI Elements")]
    [SerializeField] private Image crosshairImage;
    [SerializeField] private Canvas uiCanvas;
    
    private Camera gameCamera;
    private InputManager inputManager;
    private float lastShotTime;
    private bool canShoot = true;
    
    // 射线可视化
    [Header("Debug")]
    [SerializeField] private bool showShootingRay = true;
    [SerializeField] private float rayDisplayDuration = 0.1f;
    
    private void Awake()
    {
        gameCamera = Camera.main ?? FindObjectOfType<Camera>();
        inputManager = GetComponent<InputManager>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }
    
    private void Start()
    {
        if (uiCanvas == null)
            uiCanvas = FindObjectOfType<Canvas>();
            
        // 订阅输入管理器的射击事件
        if (inputManager != null)
        {
            inputManager.OnShoot += OnShootInput;
        }
    }
    
    private void OnDestroy()
    {
        if (inputManager != null)
        {
            inputManager.OnShoot -= OnShootInput;
        }
    }
    
    private void Update()
    {
        // 移除这里的HandleShootingInput，现在由InputManager直接调用
    }
    
    private void OnShootInput(Vector3 screenPosition)
    {
        if (CanShoot())
        {
            Shoot(screenPosition);
        }
    }
    
    private bool CanShoot()
    {
        return canShoot && Time.time >= lastShotTime + fireRate;
    }
    
    public void Shoot(Vector3 screenPosition)
    {
        if (!CanShoot()) return;
        
        lastShotTime = Time.time;
        
        // 透视相机下的正确射线计算
        Ray shootRay = CalculateShootRay(screenPosition);
        
        // 进行射线检测
        RaycastHit2D hit = Physics2D.Raycast(shootRay.origin, shootRay.direction, shootingRange, enemyLayerMask);
        
        // 播放射击音效
        PlayShootSound();
        
        // 显示枪口火焰效果
        ShowMuzzleFlash(screenPosition);
        Debug.Log(hit.collider.name);
        Debug.Log(hit.collider.transform.position);
        if (hit.collider != null)
        {
            // 射中目标
            HandleHit(hit);
        }
        else
        {
            // 未射中
            HandleMiss(shootRay);
        }
        
        // 可视化射线（调试用）
        if (showShootingRay)
        {
            Vector3 endPoint = hit.collider != null ? hit.point : shootRay.origin + shootRay.direction * shootingRange;
            StartCoroutine(ShowShootingRay(shootRay.origin, endPoint));
        }
    }
    
    private Ray CalculateShootRay(Vector3 screenPosition)
    {
        // 透视相机下的正确射线计算
        if (gameCamera.orthographic)
        {
            // 正交相机：直接使用ScreenPointToRay
            return gameCamera.ScreenPointToRay(screenPosition);
        }
        else
        {
            // 透视相机：需要特殊处理
            // 将屏幕坐标转换为世界坐标，然后计算射线方向
            Vector3 worldPoint = gameCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 10f));
            
            // 射线从相机位置射向世界点
            Vector3 direction = (worldPoint - gameCamera.transform.position).normalized;
            
            return new Ray(gameCamera.transform.position, direction);
        }
    }
    
    private void HandleHit(RaycastHit2D hit)
    {
        // 检查是否击中敌人
        EnemyBodyPart bodyPart = hit.collider.GetComponent<EnemyBodyPart>();
        if (bodyPart != null && !bodyPart.isBoss)
        {
            Enemy enemy = bodyPart.GetParentEnemy().GetComponent<Enemy>();
            //Debug.Log(enemy.name);
            BodyPart hitBodyPart = bodyPart.GetBodyPartData();
            //Debug.Log(bodyPart.GetParentEnemy());

            if (enemy != null && !enemy.IsDead)
            {
                // 对敌人造成伤害
                enemy.TakeDamage(bulletDamage, hitBodyPart);

                // 显示击中效果
                ShowHitEffect(hit.point);

                // 播放击中音效
                PlayHitSound();

                Debug.Log($"Hit {enemy.name} in {hitBodyPart.partName} for {bulletDamage} damage!");
            }
        }
        else if (bodyPart != null && bodyPart.isBoss) 
        {
            HandleBossHit(bodyPart, hit);
        }
        else
        {
            // 击中其他物体
            ShowHitEffect(hit.point);
        }
    }

private void HandleBossHit(EnemyBodyPart bodyPart, RaycastHit2D hit)
{
    //Debug.Log($"Hit Boss body part: {bodyPart.name}");
    
    // 获取身体部位数据
    BodyPart hitBodyPart = bodyPart.GetBodyPartData();
    if (hitBodyPart != null)
    {
        // 通过身体部位找到父级Boss
        var parentEntity = bodyPart.GetParentEnemy();
        if (parentEntity != null)
        {
            // 尝试获取BossController组件
            BossController boss = parentEntity.GetComponent<BossController>();
            if (boss != null && !boss.IsDead)
            {
                // 对Boss造成部位伤害
                boss.TakeDamage(bulletDamage, hitBodyPart);
                
                // 显示击中效果
                ShowHitEffect(hit.point);
                
                // 播放击中音效
                PlayHitSound();
                
                Debug.Log($"Hit Boss {boss.name} in {hitBodyPart.partName} for {bulletDamage} damage!");
            }
            else
            {
                Debug.LogWarning($"Boss component not found or Boss is dead on {parentEntity.name}");
            }
        }
        else
        {
            Debug.LogWarning($"Parent entity not found for body part {bodyPart.name}");
        }
    }
    else
    {
        Debug.LogWarning($"Body part data not found for {bodyPart.name}");
    }
}

    
    private void HandleMiss(Ray shootRay)
    {
        // 显示未击中效果（可选）
        Vector3 missPoint = shootRay.origin + shootRay.direction * shootingRange;
        ShowMissEffect(missPoint);
        
        Debug.Log("Shot missed!");
    }
    
    private void ShowMuzzleFlash(Vector3 screenPosition)
    {
        if (muzzleFlashEffect != null)
        {
            // 在射击位置显示枪口火焰
            Vector3 worldPosition = gameCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 5f));
            GameObject flash = Instantiate(muzzleFlashEffect, worldPosition, Quaternion.identity);
            Destroy(flash, effectDuration);
        }
    }
    
    private void ShowHitEffect(Vector3 hitPosition)
    {
        if (hitEffect != null)
        {
            GameObject hit = Instantiate(hitEffect, hitPosition, Quaternion.identity);
            Destroy(hit, effectDuration);
        }
    }
    
    private void ShowMissEffect(Vector3 missPosition)
    {
        if (missEffect != null)
        {
            GameObject miss = Instantiate(missEffect, missPosition, Quaternion.identity);
            Destroy(miss, effectDuration);
        }
    }
    
    private void PlayShootSound()
    {
        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
    }
    
    private void PlayHitSound()
    {
        if (audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound);
        }
    }
    
    private System.Collections.IEnumerator ShowShootingRay(Vector3 start, Vector3 end)
    {
        Debug.DrawLine(start, end, Color.red, rayDisplayDuration);
        yield return new WaitForSeconds(rayDisplayDuration);
    }
    
    // 公共方法
    public void SetBulletDamage(float damage)
    {
        bulletDamage = damage;
    }
    
    public void SetFireRate(float rate)
    {
        fireRate = rate;
    }
    
    public void EnableShooting(bool enable)
    {
        canShoot = enable;
    }
} 