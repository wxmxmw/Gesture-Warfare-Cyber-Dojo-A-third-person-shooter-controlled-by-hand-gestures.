using UnityEngine;
using TMPro;
using Business.AccountSystem;
using Business.IAPSystem;

namespace Business
{
    /// <summary>
    /// 商业化系统总初始化器（单例）
    /// 负责按正确顺序初始化两个系统，并桥接各系统与游戏逻辑
    ///
    /// 初始化顺序：
    ///   1. AccountSystem → 加载玩家数据
    ///   2. IAPSystem     → 需要用户信息
    ///
    /// 使用方法：
    ///   在场景中创建空 GameObject，挂载此组件
    /// </summary>
    public class BusinessSystemInitializer : MonoBehaviour
    {
        public static BusinessSystemInitializer Instance { get; private set; }

        // ── Inspector 配置 ─────────────────────────────────────────────────────
        [Header("游戏逻辑引用（选填，会自动查找）")]
        [Tooltip("游戏结束UI控制器，用于死亡额外复活币时调用 AddCoins")]
        [SerializeField] private GameOverUIController gameOverUIController;

        [Tooltip("玩家对象，用于监听死亡事件")]
        [SerializeField] private Player player;

        [Header("UI 显示（选填）")]
        [Tooltip("用于显示当前玩家 ID 的 TextMeshProUGUI，加载/生成账号或 Google 登录后会同步更新")]
        [SerializeField] private TMP_Text playerIdDisplayText;

        [Tooltip("Toast 消息管理器，用于在账号加载/登录成功/失败时弹出提示；留空则不显示 Toast")]
        [SerializeField] private ToastManager toastManager;

        // ── 子系统引用（自动挂载到同一 GameObject）────────────────────────────
        private AccountManager _accountManager;
        private IAPManager     _iapManager;

        public bool IsInitialized { get; private set; }

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
                return;
            }

            InitializeAllSystems();
        }

        private void Start()
        {
            // Start 中延迟查找场景组件，确保所有 MonoBehaviour 都已初始化
            FindGameReferences();
            SubscribeToGameEvents();

            // 所有组件就绪后，根据本次启动是新账号还是已有账号显示 Toast
            ShowInitialAccountToast();
        }

        // ── 初始化 ────────────────────────────────────────────────────────────

        private void InitializeAllSystems()
        {
            // ── Step 1：AccountSystem ─────────────────────────────────────────
            _accountManager = GetOrAddComponent<AccountManager>();
            _accountManager.OnPlayerDataLoaded += UpdatePlayerIdDisplay;
            _accountManager.Initialize();
            Debug.Log("[BusinessSystemInitializer] Step 1: AccountSystem initialized.");

            // ── Step 2：IAPSystem ─────────────────────────────────────────────
            _iapManager = GetOrAddComponent<IAPManager>();
            _iapManager.Initialize();
            Debug.Log("[BusinessSystemInitializer] Step 2: IAPSystem initialized.");

            IsInitialized = true;
            Debug.Log("[BusinessSystemInitializer] All business systems ready.");
        }

        private void FindGameReferences()
        {
            if (gameOverUIController == null)
                gameOverUIController = FindObjectOfType<GameOverUIController>();

            if (player == null)
                player = FindObjectOfType<Player>();
        }

        private void SubscribeToGameEvents()
        {
            // 玩家死亡 → 如果购买了 remove_ads，自动给 +1 复活币
            if (player != null)
            {
                player.OnPlayerDeath += OnPlayerDeathBonusCoin;
            }

            // Google 登录成功后刷新玩家 ID 显示 + Toast
            if (_accountManager != null)
            {
                _accountManager.OnGoogleLoginSuccess  += OnGoogleLoginSuccessRefreshId;
                _accountManager.OnGoogleLoginFailed   += OnGoogleLoginFailedShowToast;
                _accountManager.OnEmailRegisterSuccess += OnEmailRegisterSuccessHandler;
                _accountManager.OnEmailRegisterFailed  += OnEmailRegisterFailedHandler;
                _accountManager.OnEmailLoginSuccess    += OnEmailLoginSuccessHandler;
                _accountManager.OnEmailLoginFailed     += OnEmailLoginFailedHandler;
            }

            Debug.Log("[BusinessSystemInitializer] Game event subscriptions set up.");
        }

        /// <summary>
        /// 玩家数据加载/变更时，将玩家 ID 同步到 UI 的 TextMeshProUGUI
        /// </summary>
        private void UpdatePlayerIdDisplay(PlayerData data)
        {
            if (playerIdDisplayText == null) return;
            if (data != null)
                playerIdDisplayText.text = data.userId;
            else
                playerIdDisplayText.text = string.Empty;
        }

        private void OnGoogleLoginSuccessRefreshId(string googleId)
        {
            UpdatePlayerIdDisplay(_accountManager?.PlayerData);
            toastManager?.ShowToast("Google login success!");
        }

        private void OnGoogleLoginFailedShowToast(string error)
        {
            Debug.LogError($"[BusinessSystemInitializer] Google login failed: {error}");
            toastManager?.ShowToast($"Detect Google Play failed: {error}");
        }

        private void OnEmailRegisterSuccessHandler(string userId)
        {
            UpdatePlayerIdDisplay(_accountManager?.PlayerData);
            toastManager?.ShowToast("注册成功，欢迎加入！");
            Debug.Log($"[BusinessSystemInitializer] Email register success. userId={userId}");
        }

        private void OnEmailRegisterFailedHandler(string error)
        {
            toastManager?.ShowToast($"注册失败：{error}");
            Debug.LogWarning($"[BusinessSystemInitializer] Email register failed: {error}");
        }

        private void OnEmailLoginSuccessHandler(string userId)
        {
            UpdatePlayerIdDisplay(_accountManager?.PlayerData);
            toastManager?.ShowToast("登录成功，欢迎回来！");
            Debug.Log($"[BusinessSystemInitializer] Email login success. userId={userId}");
        }

        private void OnEmailLoginFailedHandler(string error)
        {
            toastManager?.ShowToast($"登录失败：{error}");
            Debug.LogWarning($"[BusinessSystemInitializer] Email login failed: {error}");
        }

        /// <summary>
        /// 游戏启动后，根据是否首次创建账号显示对应 Toast
        /// </summary>
        private void ShowInitialAccountToast()
        {
            if (_accountManager == null) return;

            if (_accountManager.WasNewAccount)
                toastManager?.ShowToast("New account created");
            else
                toastManager?.ShowToast($"Account detected, welcome back");
        }

        // ── 游戏事件响应 ──────────────────────────────────────────────────────

        private void OnPlayerDeathBonusCoin(Player deadPlayer)
        {
            // remove_ads 商品的持续效益：死亡时额外给予一枚复活币
            if (_accountManager?.PlayerData?.removeAds == true)
            {
                if (gameOverUIController != null)
                {
                    gameOverUIController.AddCoins(1);
                    Debug.Log("[BusinessSystemInitializer] removeAds bonus → +1 revival coin on death.");
                }
            }
        }

        // ── 对外接口（供 UI 按钮调用）────────────────────────────────────────

        /// <summary>
        /// 购买移除广告商品
        /// </summary>
        public void PurchaseRemoveAds()
        {
            _iapManager?.PurchaseRemoveAds();
        }

        /// <summary>
        /// 使用 Google 账号登录
        /// </summary>
        public void SignInWithGoogle()
        {
            _accountManager?.SignInWithGoogle();
        }

        /// <summary>
        /// 邮箱注册新账号
        /// 示例：在 UI 注册按钮中调用 BusinessSystemInitializer.Instance.RegisterWithEmail(email, pwd)
        /// </summary>
        public void RegisterWithEmail(string email, string password)
        {
            _accountManager?.RegisterWithEmail(email, password);
        }

        /// <summary>
        /// 邮箱 + 密码登录
        /// 示例：在 UI 登录按钮中调用 BusinessSystemInitializer.Instance.LoginWithEmail(email, pwd)
        /// </summary>
        public void LoginWithEmail(string email, string password)
        {
            _accountManager?.LoginWithEmail(email, password);
        }

        /// <summary>
        /// 检查是否已购买移除广告
        /// </summary>
        public bool HasRemovedAds()
        {
            return _accountManager?.PlayerData?.removeAds ?? false;
        }

        /// <summary>
        /// 获取当前玩家 ID
        /// </summary>
        public string GetPlayerId()
        {
            return _accountManager?.PlayerData?.userId ?? string.Empty;
        }

        /// <summary>
        /// 是否已通过邮箱完成登录（游客返回 false）
        /// </summary>
        public bool IsEmailLoggedIn => _accountManager?.IsEmailLoggedIn ?? false;

        /// <summary>
        /// 获取当前已登录的邮箱（游客或未登录时返回空字符串）
        /// </summary>
        public string GetCurrentEmail() => _accountManager?.CurrentEmail ?? string.Empty;

        // ── 工具方法 ──────────────────────────────────────────────────────────

        private T GetOrAddComponent<T>() where T : MonoBehaviour
        {
            T component = GetComponentInChildren<T>();
            if (component == null)
                component = new GameObject(typeof(T).Name).AddComponent<T>();
            return component;
        }

        private void OnDestroy()
        {
            if (_accountManager != null)
            {
                _accountManager.OnPlayerDataLoaded     -= UpdatePlayerIdDisplay;
                _accountManager.OnGoogleLoginSuccess   -= OnGoogleLoginSuccessRefreshId;
                _accountManager.OnGoogleLoginFailed    -= OnGoogleLoginFailedShowToast;
                _accountManager.OnEmailRegisterSuccess -= OnEmailRegisterSuccessHandler;
                _accountManager.OnEmailRegisterFailed  -= OnEmailRegisterFailedHandler;
                _accountManager.OnEmailLoginSuccess    -= OnEmailLoginSuccessHandler;
                _accountManager.OnEmailLoginFailed     -= OnEmailLoginFailedHandler;
            }
            if (player != null)      player.OnPlayerDeath       -= OnPlayerDeathBonusCoin;
        }
    }
}
