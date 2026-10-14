namespace Business.AdsSystem
{
    /// <summary>
    /// 广告加载/播放状态枚举
    /// AdsManager 使用此状态控制广告播放时机（只有 Ready 状态才允许播放）
    /// </summary>
    public enum AdLoadState
    {
        /// <summary>未加载</summary>
        NotLoaded,

        /// <summary>加载中</summary>
        Loading,

        /// <summary>已就绪，可以播放</summary>
        Ready,

        /// <summary>正在播放中</summary>
        Showing
    }

    /// <summary>
    /// 广告类型枚举
    /// </summary>
    public enum AdType
    {
        Rewarded,
        Interstitial,
        Banner
    }
}
