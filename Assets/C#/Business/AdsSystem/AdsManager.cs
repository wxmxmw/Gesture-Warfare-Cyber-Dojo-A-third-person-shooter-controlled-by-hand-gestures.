using System;
using UnityEngine;

namespace Business.AdsSystem
{
    /// <summary>
    /// 广告系统入口（单例）
    /// 管理广告加载状态，对外提供统一广告接口
    /// 游戏逻辑只调用此 Manager，不直接接触 AdMob SDK
    /// </summary>
    public class AdsManager : MonoBehaviour
    {
        public static AdsManager Instance { get; private set; }

        [SerializeField]
        [Tooltip("广告配置，需在 Project 中创建 AdsConfig 资产并拖入此字段")]
        private AdsConfig _config;

        private AdsAdapter _adapter;
        private bool _adsDisabled;

        // ── 广告状态 ──────────────────────────────────────────────────────────
        private AdLoadState _rewardedState     = AdLoadState.NotLoaded;
        private AdLoadState _interstitialState = AdLoadState.NotLoaded;

        // ── 对外事件 ──────────────────────────────────────────────────────────
        /// <summary>激励广告就绪（可以调用播放）</summary>
        public event Action OnRewardedAdReady;

        /// <summary>激励广告奖励已发放（玩家看完广告，游戏逻辑订阅此事件发奖励）</summary>
        public event Action OnRewardEarned;

        /// <summary>插屏广告就绪</summary>
        public event Action OnInterstitialAdReady;

        /// <summary>插屏广告关闭</summary>
        public event Action OnInterstitialAdClosed;

        // ── 属性 ──────────────────────────────────────────────────────────────
        public AdLoadState RewardedAdState     => _rewardedState;
        public AdLoadState InterstitialAdState => _interstitialState;
        public bool AreAdsDisabled             => _adsDisabled;

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
        /// 初始化广告系统，由 BusinessSystemInitializer 调用
        /// </summary>
        /// <param name="adsDisabled">是否已购买移除广告（来自 AccountManager.PlayerData.removeAds）</param>
        public void Initialize(bool adsDisabled = false, AdsConfig config = null)
        {
            _adsDisabled = adsDisabled;

            if (config != null) _config = config;

            if (_adsDisabled)
            {
                Debug.Log("[AdsManager] Ads disabled (removeAds purchased). Skipping ad loading.");
                return;
            }

            if (_config == null)
            {
                Debug.LogError("[AdsManager] AdsConfig is null! Please assign it in BusinessSystemInitializer.");
                return;
            }

            _adapter = new AdsAdapter();
            BindAdapterEvents();
            _adapter.Initialize(_config);

            _rewardedState     = AdLoadState.Loading;
            _interstitialState = AdLoadState.Loading;

            Debug.Log("[AdsManager] Initialized.");
        }

        // ── 激励广告 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 播放激励广告（需处于 Ready 状态）
        /// 广告看完后触发 OnRewardEarned 事件
        /// </summary>
        public void ShowRewardedAd()
        {
            if (_rewardedState != AdLoadState.Ready)
            {
                Debug.LogWarning($"[AdsManager] Rewarded ad not ready. Current state: {_rewardedState}");
                return;
            }

            _rewardedState = AdLoadState.Showing;
            _adapter.ShowRewardedAd();
        }

        /// <summary>
        /// 检查激励广告是否就绪
        /// </summary>
        public bool IsRewardedAdReady()
        {
            return _rewardedState == AdLoadState.Ready;
        }

        // ── 插屏广告 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 播放插屏广告（已购买去广告则跳过）
        /// </summary>
        public void ShowInterstitialAd()
        {
            if (_adsDisabled) return;

            if (_interstitialState != AdLoadState.Ready)
            {
                Debug.LogWarning($"[AdsManager] Interstitial ad not ready. Current state: {_interstitialState}");
                return;
            }

            _interstitialState = AdLoadState.Showing;
            _adapter.ShowInterstitialAd();
        }

        // ── 横幅广告 ──────────────────────────────────────────────────────────

        /// <summary>显示横幅广告（已购买去广告则跳过）</summary>
        public void ShowBannerAd()
        {
            if (_adsDisabled) return;
            _adapter?.ShowBannerAd();
        }

        /// <summary>隐藏横幅广告</summary>
        public void HideBannerAd()
        {
            _adapter?.HideBannerAd();
        }

        // ── 状态控制 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 更新广告禁用状态（内购成功后由 IAPManager 调用）
        /// </summary>
        public void SetAdsDisabled(bool disabled)
        {
            _adsDisabled = disabled;
            if (disabled)
            {
                HideBannerAd();
                _adapter?.DestroyBannerAd();
                Debug.Log("[AdsManager] Ads have been disabled.");
            }
        }

        // ── 私有方法 ──────────────────────────────────────────────────────────

        private void BindAdapterEvents()
        {
            _adapter.OnRewardedAdLoaded += () =>
            {
                _rewardedState = AdLoadState.Ready;
                OnRewardedAdReady?.Invoke();
                Debug.Log("[AdsManager] Rewarded ad ready.");
            };

            _adapter.OnRewardedAdFailedToLoad += (error) =>
            {
                _rewardedState = AdLoadState.NotLoaded;
                Debug.LogWarning($"[AdsManager] Rewarded ad load failed: {error}");
            };

            _adapter.OnRewardedAdCompleted += () =>
            {
                _rewardedState = AdLoadState.Loading; // 已触发重新加载
                OnRewardEarned?.Invoke();
                Debug.Log("[AdsManager] Reward earned event fired.");
            };

            _adapter.OnRewardedAdShowFailed += () =>
            {
                _rewardedState = AdLoadState.NotLoaded;
                Debug.LogWarning("[AdsManager] Rewarded ad show failed.");
            };

            _adapter.OnInterstitialAdLoaded += () =>
            {
                _interstitialState = AdLoadState.Ready;
                OnInterstitialAdReady?.Invoke();
            };

            _adapter.OnInterstitialAdFailedToLoad += (error) =>
            {
                _interstitialState = AdLoadState.NotLoaded;
                Debug.LogWarning($"[AdsManager] Interstitial load failed: {error}");
            };

            _adapter.OnInterstitialAdClosed += () =>
            {
                _interstitialState = AdLoadState.Loading;
                OnInterstitialAdClosed?.Invoke();
            };
        }
    }
}
