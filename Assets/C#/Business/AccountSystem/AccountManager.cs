using System;
using UnityEngine;

namespace Business.AccountSystem
{
    /// <summary>
    /// 账号系统入口（单例）
    /// 负责玩家身份管理、数据读取与持久化
    /// 游戏逻辑只需通过此 Manager 访问账号与玩家数据
    /// </summary>
    public class AccountManager : MonoBehaviour
    {
        public static AccountManager Instance { get; private set; }

        private SaveService      _saveService;
        private LoginAdapter     _loginAdapter;
        private EmailAuthAdapter _emailAuthAdapter;
        private PlayerData       _playerData;

        /// <summary>临时记录本次登录/注册所用邮箱，成功后写入 PlayerData.email</summary>
        private string _pendingEmail;

        // ── 对外事件 ──────────────────────────────────────────────────────────
        /// <summary>玩家数据加载完成</summary>
        public event Action<PlayerData> OnPlayerDataLoaded;

        /// <summary>玩家数据已保存</summary>
        public event Action<PlayerData> OnPlayerDataSaved;

        /// <summary>Google 登录成功，参数为 Google 用户 ID</summary>
        public event Action<string> OnGoogleLoginSuccess;

        /// <summary>Google 登录失败，参数为错误信息</summary>
        public event Action<string> OnGoogleLoginFailed;

        /// <summary>邮箱注册成功，参数为服务器分配的 userId</summary>
        public event Action<string> OnEmailRegisterSuccess;

        /// <summary>邮箱注册失败，参数为错误原因</summary>
        public event Action<string> OnEmailRegisterFailed;

        /// <summary>邮箱登录成功，参数为服务器分配的 userId</summary>
        public event Action<string> OnEmailLoginSuccess;

        /// <summary>邮箱登录失败，参数为错误原因</summary>
        public event Action<string> OnEmailLoginFailed;

        // ── 属性 ──────────────────────────────────────────────────────────────
        /// <summary>当前加载的玩家数据（初始化后不为 null）</summary>
        public PlayerData PlayerData => _playerData;

        /// <summary>
        /// 是否已通过邮箱完成登录
        /// true = 邮箱账号有效；false = 游客或 Google 单独登录
        /// </summary>
        public bool IsEmailLoggedIn { get; private set; }

        /// <summary>是否已通过 Google 账号登录</summary>
        public bool IsGoogleLoggedIn { get; private set; }

        /// <summary>本次启动是否是全新创建的游客账号（false 表示加载了已有存档）</summary>
        public bool WasNewAccount { get; private set; }

        /// <summary>当前已登录的邮箱（仅邮箱登录后有值）</summary>
        public string CurrentEmail => _playerData?.email ?? string.Empty;

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
        /// 初始化账号系统，由 BusinessSystemInitializer 调用
        /// </summary>
        public void Initialize()
        {
            _saveService = new SaveService();

            _loginAdapter = new LoginAdapter();
            _loginAdapter.OnLoginSuccess += HandleGoogleLoginSuccess;
            _loginAdapter.OnLoginFailed  += HandleGoogleLoginFailed;
            _loginAdapter.Initialize();

            _emailAuthAdapter = new EmailAuthAdapter(this);
            _emailAuthAdapter.OnRegisterSuccess += HandleEmailRegisterSuccess;
            _emailAuthAdapter.OnRegisterFailed  += HandleEmailRegisterFailed;
            _emailAuthAdapter.OnLoginSuccess    += HandleEmailLoginSuccess;
            _emailAuthAdapter.OnLoginFailed     += HandleEmailLoginFailed;

            LoadOrCreatePlayerData();

            Debug.Log("[AccountManager] Initialized.");
        }

        // ── 登录 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 发起 Google 账号登录
        /// 登录成功后会将游客 userId 升级为 Google 用户 ID，数据保持不变
        /// </summary>
        public void SignInWithGoogle()
        {
            _loginAdapter?.SignIn();
        }

        /// <summary>
        /// 登出 Google 账号（数据保留，切回游客状态）
        /// </summary>
        public void SignOutFromGoogle()
        {
            _loginAdapter?.SignOut();
            IsGoogleLoggedIn = false;
            Debug.Log("[AccountManager] Signed out from Google.");
        }

        /// <summary>
        /// 用邮箱注册新账号
        /// 密码通过网络传输到服务器，服务器用 bcrypt 加密后存入 MySQL，本地永不存储密码
        /// </summary>
        public void RegisterWithEmail(string email, string password)
        {
            _pendingEmail = email.Trim().ToLower();
            Debug.Log($"[AccountManager] RegisterWithEmail called. email={_pendingEmail}");
            _emailAuthAdapter?.Register(_pendingEmail, password);
        }

        /// <summary>
        /// 用邮箱 + 密码登录
        /// 成功后本地 PlayerData.userId / email / loginType 更新并持久化
        /// </summary>
        public void LoginWithEmail(string email, string password)
        {
            _pendingEmail = email.Trim().ToLower();
            Debug.Log($"[AccountManager] LoginWithEmail called. email={_pendingEmail}");
            _emailAuthAdapter?.Login(_pendingEmail, password);
        }

        // ── 玩家数据操作 ──────────────────────────────────────────────────────

        /// <summary>
        /// 设置 removeAds 标志并保存
        /// </summary>
        public void SetRemoveAds(bool value)
        {
            if (_playerData == null) return;
            _playerData.removeAds = value;
            SavePlayerData();
        }

        /// <summary>
        /// 增加玩家货币并保存
        /// </summary>
        public void AddCoins(int amount)
        {
            if (_playerData == null || amount <= 0) return;
            _playerData.coins += amount;
            SavePlayerData();
        }

        /// <summary>
        /// 消耗玩家货币
        /// </summary>
        /// <returns>货币充足时返回 true 并扣除，不足时返回 false</returns>
        public bool SpendCoins(int amount)
        {
            if (_playerData == null || _playerData.coins < amount) return false;
            _playerData.coins -= amount;
            SavePlayerData();
            return true;
        }

        /// <summary>
        /// 强制立即保存玩家数据
        /// </summary>
        public void SavePlayerData()
        {
            if (_playerData == null) return;
            _saveService.Save(_playerData);
            OnPlayerDataSaved?.Invoke(_playerData);
        }

        // ── 私有方法 ──────────────────────────────────────────────────────────

        private void LoadOrCreatePlayerData()
        {
            _playerData = _saveService.Load();

            if (_playerData == null)
            {
                // 没有存档 → 创建游客账号（只存本地，不上传服务器）
                WasNewAccount   = true;
                IsEmailLoggedIn = false;
                _playerData     = PlayerData.CreateDefault();
                _saveService.Save(_playerData);
                Debug.Log($"[AccountManager] New guest account created. ID: {_playerData.userId}");
            }
            else
            {
                WasNewAccount             = false;
                _playerData.lastLoginTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                _saveService.Save(_playerData);

                // 上次是邮箱登录 → 恢复邮箱登录状态，无需重新输入密码
                if (_playerData.loginType == "email" && !string.IsNullOrEmpty(_playerData.email))
                {
                    IsEmailLoggedIn = true;
                    Debug.Log($"[AccountManager] Email account restored. email={_playerData.email}");
                }
                else
                {
                    Debug.Log($"[AccountManager] Guest/other account loaded. ID: {_playerData.userId}");
                }
            }

            OnPlayerDataLoaded?.Invoke(_playerData);
        }

        private void HandleGoogleLoginSuccess(string googleId)
        {
            _playerData.userId        = googleId;
            _playerData.lastLoginTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            _playerData.loginType     = "google";
            IsGoogleLoggedIn = true;

            SavePlayerData();

            Debug.Log($"[AccountManager] Account upgraded to Google. ID: {googleId}");
            OnGoogleLoginSuccess?.Invoke(googleId);
        }

        private void HandleGoogleLoginFailed(string error)
        {
            Debug.LogWarning($"[AccountManager] Google login failed: {error}");
            OnGoogleLoginFailed?.Invoke(error);
        }

        private void HandleEmailRegisterSuccess(string serverUserId)
        {
            _playerData.userId        = serverUserId;
            _playerData.email         = _pendingEmail ?? string.Empty;
            _playerData.lastLoginTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            _playerData.loginType     = "email";
            IsEmailLoggedIn           = true;
            _pendingEmail             = null;
            SavePlayerData();

            Debug.Log($"[AccountManager] Email register success. userId={serverUserId}");
            OnEmailRegisterSuccess?.Invoke(serverUserId);
        }

        private void HandleEmailRegisterFailed(string error)
        {
            Debug.LogWarning($"[AccountManager] Email register failed: {error}");
            OnEmailRegisterFailed?.Invoke(error);
        }

        private void HandleEmailLoginSuccess(string serverUserId)
        {
            _playerData.userId        = serverUserId;
            _playerData.email         = _pendingEmail ?? string.Empty;
            _playerData.lastLoginTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            _playerData.loginType     = "email";
            IsEmailLoggedIn           = true;
            _pendingEmail             = null;
            SavePlayerData();

            Debug.Log($"[AccountManager] Email login success. userId={serverUserId}");
            OnEmailLoginSuccess?.Invoke(serverUserId);
        }

        private void HandleEmailLoginFailed(string error)
        {
            Debug.LogWarning($"[AccountManager] Email login failed: {error}");
            OnEmailLoginFailed?.Invoke(error);
        }

        private void OnApplicationQuit()
        {
            SavePlayerData();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SavePlayerData();
        }
    }
}
