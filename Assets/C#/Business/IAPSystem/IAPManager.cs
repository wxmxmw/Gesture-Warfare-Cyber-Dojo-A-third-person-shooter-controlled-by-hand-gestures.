using System;
using UnityEngine;
using Business.AccountSystem;

namespace Business.IAPSystem
{
    /// <summary>
    /// 内购系统入口（单例）
    /// 管理商品购买流程，游戏逻辑只调用此 Manager
    /// </summary>
    public class IAPManager : MonoBehaviour
    {
        public static IAPManager Instance { get; private set; }

        private IAPAdapter   _adapter;
        private PurchaseState _currentState = PurchaseState.Idle;

        // ── 对外事件 ──────────────────────────────────────────────────────────
        /// <summary>IAP 系统已就绪，可以发起购买</summary>
        public event Action OnIAPReady;

        /// <summary>购买成功，参数为商品 ID</summary>
        public event Action<string> OnPurchaseSuccess;

        /// <summary>购买失败，参数为商品 ID 和错误信息</summary>
        public event Action<string, string> OnPurchaseFailed;

        // ── 属性 ──────────────────────────────────────────────────────────────
        public bool IsReady            => _currentState != PurchaseState.Purchasing &&
                                          _adapter != null && _adapter.IsInitialized;
        public PurchaseState State     => _currentState;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // ── 初始化 ────────────────────────────────────────────────────────────

        /// <summary>
        /// 初始化内购系统，由 BusinessSystemInitializer 调用
        /// </summary>
        public void Initialize()
        {
            _adapter = new IAPAdapter();

            _adapter.AdapterInitialized += () =>
            {
                _currentState = PurchaseState.Idle;
                OnIAPReady?.Invoke();
                Debug.Log("[IAPManager] IAP ready.");

                // 检查已有购买记录（应对重新安装的情况）
                RestoreExistingPurchases();
            };

            _adapter.AdapterInitializeFailed += (error) =>
            {
                Debug.LogError($"[IAPManager] IAP init failed: {error}");
            };

            _adapter.PurchaseSucceeded  += HandlePurchaseSuccess;
            _adapter.PurchaseFailed     += HandlePurchaseFailed;
            _adapter.PurchaseRestored   += HandlePurchaseRestored;

            _adapter.Initialize();
            Debug.Log("[IAPManager] Initialized.");
        }

        // ── 购买接口 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 购买移除广告商品
        /// </summary>
        public void PurchaseRemoveAds()
        {
            Purchase(ProductCatalog.REMOVE_ADS);
        }

        /// <summary>
        /// 发起购买指定商品
        /// </summary>
        public void Purchase(string productId)
        {
            if (!_adapter.IsInitialized)
            {
                Debug.LogWarning("[IAPManager] IAP not initialized yet.");
                return;
            }

            if (_currentState == PurchaseState.Purchasing)
            {
                Debug.LogWarning("[IAPManager] Another purchase is already in progress.");
                return;
            }

            _currentState = PurchaseState.Purchasing;
            _adapter.Purchase(productId);
        }

        /// <summary>
        /// 恢复已购商品（iOS 需要在 UI 中提供此按钮）
        /// </summary>
        public void RestorePurchases()
        {
            _adapter?.RestorePurchases();
        }

        /// <summary>
        /// 检查非消耗型商品是否已购买
        /// </summary>
        public bool IsProductOwned(string productId)
        {
            return _adapter?.IsProductOwned(productId) ?? false;
        }

        // ── 私有方法 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 初始化完成后检查本地收据，同步已购状态到 AccountSystem
        /// </summary>
        private void RestoreExistingPurchases()
        {
            if (_adapter.IsProductOwned(ProductCatalog.REMOVE_ADS))
            {
                ApplyRemoveAdsBenefit();
                Debug.Log("[IAPManager] Existing remove_ads purchase detected and applied.");
            }
        }

        private void HandlePurchaseSuccess(string productId)
        {
            _currentState = PurchaseState.Success;
            Debug.Log($"[IAPManager] Purchase success: {productId}");

            if (productId == ProductCatalog.REMOVE_ADS)
            {
                ApplyRemoveAdsBenefit();
            }

            OnPurchaseSuccess?.Invoke(productId);

            // 购买完成后立即保存玩家数据
            AccountManager.Instance?.SavePlayerData();

            _currentState = PurchaseState.Idle;
        }

        private void HandlePurchaseFailed(string productId, string error)
        {
            _currentState = PurchaseState.Failed;
            Debug.LogError($"[IAPManager] Purchase failed '{productId}': {error}");
            OnPurchaseFailed?.Invoke(productId, error);
            _currentState = PurchaseState.Idle;
        }

        private void HandlePurchaseRestored(string productId)
        {
            Debug.Log($"[IAPManager] Purchase restored: {productId}");

            if (productId == ProductCatalog.REMOVE_ADS)
            {
                ApplyRemoveAdsBenefit();
            }
        }

        /// <summary>
        /// 应用移除广告商品的效果：
        /// 1. 更新 PlayerData.removeAds = true
        /// </summary>
        private void ApplyRemoveAdsBenefit()
        {
            if (AccountManager.Instance != null)
            {
                AccountManager.Instance.SetRemoveAds(true);
                Debug.Log("[IAPManager] PlayerData.removeAds set to true.");
            }
        }
    }
}
