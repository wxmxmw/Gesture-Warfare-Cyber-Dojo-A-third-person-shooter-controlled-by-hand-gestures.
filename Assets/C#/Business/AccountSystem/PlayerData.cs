using System;

namespace Business.AccountSystem
{
    /// <summary>
    /// 玩家数据模型
    /// 包含账号信息和持久化游戏数据，通过 SaveService 存储在 persistentDataPath
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        /// <summary>玩家唯一标识符（游客为UUID，Google登录后更新为Google ID）</summary>
        public string userId;

        /// <summary>玩家持有的货币数量</summary>
        public int coins;

        /// <summary>是否已购买移除广告商品</summary>
        public bool removeAds;

        /// <summary>账号创建时间（ISO 8601格式）</summary>
        public string createTime;

        /// <summary>最后登录时间（ISO 8601格式）</summary>
        public string lastLoginTime;

        /// <summary>
        /// 登录邮箱（邮箱注册登录后记录；密码永远不存本地）
        /// </summary>
        public string email;

        /// <summary>登录方式：guest / email / google</summary>
        public string loginType;

        /// <summary>
        /// 创建默认游客账号数据
        /// </summary>
        public static PlayerData CreateDefault()
        {
            string now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            return new PlayerData
            {
                userId        = Guid.NewGuid().ToString(),
                coins         = 0,
                removeAds     = false,
                createTime    = now,
                lastLoginTime = now,
                email         = string.Empty,
                loginType     = "guest"
            };
        }
    }
}
