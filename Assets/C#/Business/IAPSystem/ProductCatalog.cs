namespace Business.IAPSystem
{
    /// <summary>
    /// 商品类型
    /// </summary>
    public enum IAPProductType
    {
        /// <summary>消耗型（可重复购买）</summary>
        Consumable,

        /// <summary>非消耗型（一次性购买）</summary>
        NonConsumable,

        /// <summary>订阅型</summary>
        Subscription
    }

    /// <summary>
    /// 商品定义
    /// </summary>
    public class ProductDefinition
    {
        public string Id          { get; }
        public IAPProductType Type { get; }
        public string DisplayName { get; }
        public string Description { get; }

        public ProductDefinition(string id, IAPProductType type, string displayName, string description)
        {
            Id          = id;
            Type        = type;
            DisplayName = displayName;
            Description = description;
        }
    }

    /// <summary>
    /// 商品目录
    /// 添加新商品时在此处注册即可，IAPAdapter 会自动注册到 Unity IAP
    /// </summary>
    public static class ProductCatalog
    {
        /// <summary>移除广告商品 ID</summary>
        public const string REMOVE_ADS = "remove_ads";

        /// <summary>所有商品定义</summary>
        public static readonly ProductDefinition[] AllProducts =
        {
            new ProductDefinition(
                REMOVE_ADS,
                IAPProductType.NonConsumable,
                "移除广告",
                "永久移除所有广告，且每次死亡时额外获得一枚复活币"
            )
        };
    }
}
