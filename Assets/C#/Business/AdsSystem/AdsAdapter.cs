using System;
using UnityEngine;

// 若已通过 Package Manager 安装 Google Mobile Ads Unity Plugin，
// 请在 Player Settings → Other Settings → Scripting Define Symbols 中添加：ADMOB_AVAILABLE
#if ADMOB_AVAILABLE
using GoogleMobileAds.Api;
#endif

namespace Business.AdsSystem
{
    /// <summary>
    /// AdMob SDK 适配器
    /// 是项目中唯一允许直接调用 AdMob SDK 的地方
    /// 未安装 AdMob SDK 时自动使用 Stub 模式（模拟广告行为），不影响编译
    /// </summary>
    public class AdsAdapter
    {
        // ── 对外事件 ──────────────────────────────────────────────────────────
        public event Action OnRewardedAdLoaded;
        public event Action<string> OnRewardedAdFailedToLoad;
        public event Action OnRewardedAdCompleted;
        public event Action OnRewardedAdShowFailed;

        public event Action OnInterstitialAdLoaded;
        public event Action<string> OnInterstitialAdFailedToLoad;
        public event Action OnInterstitialAdClosed;

        public event Action OnBannerAdLoaded;
        public event Action<string> OnBannerAdFailedToLoad;

        // ── 状态 ──────────────────────────────────────────────────────────────
        public bool IsRewardedReady     { get; private set; }
        public bool IsInterstitialReady { get; private set; }
        public bool IsBannerShowing     { get; private set; }

        private AdsConfig _config;

#if ADMOB_AVAILABLE
        private RewardedAd    _rewardedAd;
        private InterstitialAd _interstitialAd;
        private BannerView    _bannerView;
#endif

        // ── 初始化 ────────────────────────────────────────────────────────────

        /// <summary>
        /// 初始化 AdMob SDK 并开始预加载广告
        /// </summary>
        public void Initialize(AdsConfig config)
        {
            _config = config;

#if ADMOB_AVAILABLE
            MobileAds.Initialize(status =>
            {
                Debug.Log("[AdsAdapter] AdMob initialized.");
                LoadRewardedAd();
                LoadInterstitialAd();
            });
#else
            Debug.Log("[AdsAdapter] AdMob SDK not present. Running in Stub mode.");
            // Stub 模式：直接模拟广告就绪
            IsRewardedReady     = true;
            IsInterstitialReady = true;
            OnRewardedAdLoaded?.Invoke();
            OnInterstitialAdLoaded?.Invoke();
#endif
        }

        // ── 激励广告 ──────────────────────────────────────────────────────────

        public void LoadRewardedAd()
        {
#if ADMOB_AVAILABLE
            IsRewardedReady = false;
            var adRequest = new AdRequest();
            RewardedAd.Load(_config.rewardedAdUnitId, adRequest, (ad, error) =>
            {
                if (error != null || ad == null)
                {
                    string msg = error?.GetMessage() ?? "ad is null";
                    Debug.LogError($"[AdsAdapter] Rewarded load failed: {msg}");
                    IsRewardedReady = false;
                    OnRewardedAdFailedToLoad?.Invoke(msg);
                    return;
                }
                _rewardedAd = ad;
                IsRewardedReady = true;
                Debug.Log("[AdsAdapter] Rewarded ad ready.");
                OnRewardedAdLoaded?.Invoke();
            });
#else
            // Stub：立即就绪
            IsRewardedReady = true;
            OnRewardedAdLoaded?.Invoke();
#endif
        }

        public void ShowRewardedAd()
        {
#if ADMOB_AVAILABLE
            if (_rewardedAd == null || !_rewardedAd.CanShowAd())
            {
                Debug.LogWarning("[AdsAdapter] Rewarded ad not ready to show.");
                OnRewardedAdShowFailed?.Invoke();
                return;
            }
            _rewardedAd.Show(reward =>
            {
                Debug.Log($"[AdsAdapter] Reward earned: {reward.Amount} {reward.Type}");
                IsRewardedReady = false;
                OnRewardedAdCompleted?.Invoke();
                LoadRewardedAd(); // 预加载下一个
            });
#else
            // Stub：立即触发奖励
            Debug.Log("[AdsAdapter] Stub rewarded ad completed.");
            IsRewardedReady = false;
            OnRewardedAdCompleted?.Invoke();
            LoadRewardedAd();
#endif
        }

        // ── 插屏广告 ──────────────────────────────────────────────────────────

        public void LoadInterstitialAd()
        {
#if ADMOB_AVAILABLE
            IsInterstitialReady = false;
            var adRequest = new AdRequest();
            InterstitialAd.Load(_config.interstitialAdUnitId, adRequest, (ad, error) =>
            {
                if (error != null || ad == null)
                {
                    string msg = error?.GetMessage() ?? "ad is null";
                    Debug.LogError($"[AdsAdapter] Interstitial load failed: {msg}");
                    IsInterstitialReady = false;
                    OnInterstitialAdFailedToLoad?.Invoke(msg);
                    return;
                }
                _interstitialAd = ad;
                IsInterstitialReady = true;
                Debug.Log("[AdsAdapter] Interstitial ad ready.");
                OnInterstitialAdLoaded?.Invoke();
            });
#else
            IsInterstitialReady = true;
            OnInterstitialAdLoaded?.Invoke();
#endif
        }

        public void ShowInterstitialAd()
        {
#if ADMOB_AVAILABLE
            if (_interstitialAd == null || !_interstitialAd.CanShowAd())
            {
                Debug.LogWarning("[AdsAdapter] Interstitial ad not ready.");
                return;
            }
            _interstitialAd.Show();
            IsInterstitialReady = false;
            OnInterstitialAdClosed?.Invoke();
            LoadInterstitialAd();
#else
            Debug.Log("[AdsAdapter] Stub interstitial ad shown.");
            IsInterstitialReady = false;
            OnInterstitialAdClosed?.Invoke();
            LoadInterstitialAd();
#endif
        }

        // ── 横幅广告 ──────────────────────────────────────────────────────────

        public void ShowBannerAd()
        {
#if ADMOB_AVAILABLE
            if (_bannerView != null)
            {
                _bannerView.Show();
                IsBannerShowing = true;
                return;
            }

            _bannerView = new BannerView(_config.bannerAdUnitId, AdSize.Banner, AdPosition.Bottom);

            _bannerView.OnBannerAdLoaded += () =>
            {
                IsBannerShowing = true;
                OnBannerAdLoaded?.Invoke();
            };

            _bannerView.OnBannerAdLoadFailed += (error) =>
            {
                OnBannerAdFailedToLoad?.Invoke(error.GetMessage());
            };

            _bannerView.LoadAd(new AdRequest());
#else
            Debug.Log("[AdsAdapter] Stub banner shown.");
            IsBannerShowing = true;
            OnBannerAdLoaded?.Invoke();
#endif
        }

        public void HideBannerAd()
        {
#if ADMOB_AVAILABLE
            _bannerView?.Hide();
#endif
            IsBannerShowing = false;
        }

        public void DestroyBannerAd()
        {
#if ADMOB_AVAILABLE
            _bannerView?.Destroy();
            _bannerView = null;
#endif
            IsBannerShowing = false;
        }
    }
}
