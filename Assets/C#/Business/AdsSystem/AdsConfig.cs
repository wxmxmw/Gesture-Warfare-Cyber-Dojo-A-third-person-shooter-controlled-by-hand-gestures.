using UnityEngine;

namespace Business.AdsSystem
{
    /// <summary>
    /// 广告配置数据（ScriptableObject）
    /// 在 Project 窗口右键 → Create → Business → AdsConfig 创建配置资产
    /// 将创建好的资产拖入 BusinessSystemInitializer 的 Ads Config 字段
    /// </summary>
    [CreateAssetMenu(fileName = "AdsConfig", menuName = "Business/AdsConfig")]
    public class AdsConfig : ScriptableObject
    {
        [Header("AdMob App ID（需与 AndroidManifest 中一致）")]
        [Tooltip("在 AdMob 后台获取，格式：ca-app-pub-XXXXXXXXXXXXXXXX~XXXXXXXXXX")]
        public string androidAppId = "ca-app-pub-3940256099942544~3347511713"; // AdMob 测试 AppID

        [Header("广告单元 ID（Android）")]
        [Tooltip("激励广告单元 ID")]
        public string rewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917"; // 测试 ID

        [Tooltip("插屏广告单元 ID")]
        public string interstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712"; // 测试 ID

        [Tooltip("横幅广告单元 ID")]
        public string bannerAdUnitId = "ca-app-pub-3940256099942544/6300978111"; // 测试 ID

        [Header("加载设置")]
        [Tooltip("广告加载失败后的重试延迟（秒）")]
        public float retryDelay = 5f;
    }
}
