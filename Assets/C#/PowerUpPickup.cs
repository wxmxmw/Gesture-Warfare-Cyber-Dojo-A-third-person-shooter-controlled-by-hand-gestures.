using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// 道具拾取组件
public class PowerUpPickup : MonoBehaviour
{
    private PowerUpData powerUpConfig;
    private bool isPickedUp = false;
    
    // 动画效果
    [SerializeField] private float floatSpeed = 1f;
    [SerializeField] private float floatAmplitude = 0.5f;
    [SerializeField] private float rotateSpeed = 90f;
    
    private Vector3 startPosition;
    private float timeOffset;
    
    public void Initialize(PowerUpData config)
    {
        powerUpConfig = config;
        startPosition = transform.position;
        timeOffset = Random.Range(0f, 2f * Mathf.PI);
        
        // 设置自动销毁时间（避免道具永久存在）
        Destroy(gameObject, 30f);
    }
    
    private void Update()
    {
        if (isPickedUp) return;
        
        // 漂浮效果
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed + timeOffset) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        
        // 旋转效果
        transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isPickedUp) return;
        
        if (other.CompareTag("PlayerAttack"))
        {
            PickUp();
        }
    }
    
    private void PickUp()
    {
        if (powerUpConfig == null || PowerUpManager.Instance == null) return;
        
        isPickedUp = true;
        
        // 激活道具效果
        PowerUpManager.Instance.ActivatePowerUp(powerUpConfig);
        
        // 播放拾取效果（可以添加音效和粒子效果）
        // PlayPickupEffect();
        
        // 销毁道具对象
        Destroy(gameObject);
    }
}