using System;
using UnityEngine;

// Unity IAP 需要通过 Package Manager 安装：
// Window → Package Manager → Unity Registry → In App Purchasing
// 安装后会自动定义 UNITY_PURCHASING 符号
#if UNITY_PURCHASING
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif

namespace Business.IAPSystem
{
    /// <summary>
    /// Unity IAP 适配器
    /// 是项目中唯一允许直接调用 Unity IAP API 的地方
    /// 未安装 Unity IAP 时自动使用 Stub 模式（模拟购买行为），不影响编译
    /// </summary>
#if UNITY_PURCHASING
    public class IAPAdapter : IDetailedStoreListener
#else
    public class IAPAdapter
#endif
    {
        // ── 对外事件（命名区分于 IStoreListener 接口方法）────────────────────
        /// <summary>IAP 初始化成功</summary>
        public event Action AdapterInitialized;

        /// <summary>IAP 初始化失败，返回错误信息</summary>
        public event Action<string> AdapterInitializeFailed;

        /// <summary>购买成功，返回商品 ID</summary>
        public event Action<string> PurchaseSucceeded;

        /// <summary>购买失败，返回商品 ID 和错误信息</summary>
        public event Action<string, string> PurchaseFailed;

        /// <summary>恢复购买成功，返回商品 ID</summary>
        public event Action<string> PurchaseRestored;

        public bool IsInitialized { get; private set; }

#if UNITY_PURCHASING
        private IStoreController   _storeController;
        private IExtensionProvider _extensions;
#endif

        // ── 初始化 ────────────────────────────────────────────────────────────

        /// <summary>
        /// 初始化 Unity IAP，注册 ProductCatalog 中的所有商品
        /// </summary>
        public void Initialize()
        {
#if UNITY_PURCHASING
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            foreach (var product in ProductCatalog.AllProducts)
            {
                var unityType = ConvertProductType(product.Type);
                builder.AddProduct(product.Id, unityType);
            }

            UnityPurchasing.Initialize(this, builder);
            Debug.Log("[IAPAdapter] Unity IAP initializing...");
#else
            Debug.Log("[IAPAdapter] Unity IAP not installed. Running in Stub mode.");
            IsInitialized = true;
            AdapterInitialized?.Invoke();
#endif
        }

        // ── 购买 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 发起购买，由 IAPManager 调用
        /// </summary>
        public void Purchase(string productId)
        {
#if UNITY_PURCHASING
            if (!IsInitialized || _storeController == null)
            {
                Debug.LogError("[IAPAdapter] IAP not initialized yet.");
                PurchaseFailed?.Invoke(productId, "IAP not initialized");
                return;
            }

            var product = _storeController.products.WithID(productId);
            if (product == null || !product.availableToPurchase)
            {
                Debug.LogError($"[IAPAdapter] Product '{productId}' is not available.");
                PurchaseFailed?.Invoke(productId, "Product not available");
                return;
            }

            _storeController.InitiatePurchase(product);
            Debug.Log($"[IAPAdapter] Purchase initiated: {productId}");
#else
            // Stub 模式：模拟购买立即成功
            Debug.Log($"[IAPAdapter] Stub purchase success: {productId}");
            PurchaseSucceeded?.Invoke(productId);
#endif
        }

        /// <summary>
        /// 恢复已购商品（iOS 必须提供此功能）
        /// </summary>
        public void RestorePurchases()
        {
#if UNITY_PURCHASING && UNITY_IOS
            if (!IsInitialized || _extensions == null)
            {
                Debug.LogWarning("[IAPAdapter] Cannot restore: IAP not initialized.");
                return;
            }
            var apple = _extensions.GetExtension<IAppleExtensions>();
            apple?.RestoreTransactions((result, error) =>
            {
                if (result)
                    Debug.Log("[IAPAdapter] Purchases restored successfully.");
                else
                    Debug.LogError($"[IAPAdapter] Restore failed: {error}");
            });
#else
            Debug.Log("[IAPAdapter] RestorePurchases: not applicable on this platform.");
#endif
        }

        /// <summary>
        /// 检查非消耗型商品是否已购买（有收据）
        /// </summary>
        public bool IsProductOwned(string productId)
        {
#if UNITY_PURCHASING
            if (!IsInitialized || _storeController == null) return false;
            var product = _storeController.products.WithID(productId);
            return product != null && product.hasReceipt;
#else
            return false;
#endif
        }

        // ── Unity IAP IStoreListener / IDetailedStoreListener 接口实现 ────────
#if UNITY_PURCHASING
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _storeController = controller;
            _extensions      = extensions;
            IsInitialized    = true;
            Debug.Log("[IAPAdapter] Unity IAP initialized successfully.");
            AdapterInitialized?.Invoke();
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            OnInitializeFailed(error, error.ToString());
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            IsInitialized = false;
            string msg = $"{error}: {message}";
            Debug.LogError($"[IAPAdapter] Init failed: {msg}");
            AdapterInitializeFailed?.Invoke(msg);
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            string productId = args.purchasedProduct.definition.id;
            Debug.Log($"[IAPAdapter] Purchase processed: {productId}");
            PurchaseSucceeded?.Invoke(productId);
            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            string productId = product.definition.id;
            string error     = failureDescription.reason.ToString();
            Debug.LogError($"[IAPAdapter] Purchase failed for '{productId}': {error}");
            PurchaseFailed?.Invoke(productId, error);
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            string productId = product.definition.id;
            string error     = failureReason.ToString();
            Debug.LogError($"[IAPAdapter] Purchase failed for '{productId}': {error}");
            PurchaseFailed?.Invoke(productId, error);
        }

        private UnityEngine.Purchasing.ProductType ConvertProductType(IAPProductType type)
        {
            switch (type)
            {
                case IAPProductType.NonConsumable: return UnityEngine.Purchasing.ProductType.NonConsumable;
                case IAPProductType.Subscription:  return UnityEngine.Purchasing.ProductType.Subscription;
                default:                           return UnityEngine.Purchasing.ProductType.Consumable;
            }
        }
#else
        private void ConvertProductType(IAPProductType type) { }
#endif
    }
}
