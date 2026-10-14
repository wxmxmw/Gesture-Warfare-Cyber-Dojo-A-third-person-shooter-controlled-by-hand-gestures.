namespace Business.IAPSystem
{
    /// <summary>
    /// 内购流程状态枚举
    /// </summary>
    public enum PurchaseState
    {
        /// <summary>空闲，等待操作</summary>
        Idle,

        /// <summary>购买流程进行中</summary>
        Purchasing,

        /// <summary>购买成功</summary>
        Success,

        /// <summary>购买失败</summary>
        Failed,

        /// <summary>恢复购买成功</summary>
        Restored
    }
}
