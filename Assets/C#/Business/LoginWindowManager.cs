using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Business.AccountSystem;

namespace Business
{
    /// <summary>
    /// 登录/注册窗口管理器
    ///
    /// 职责：
    ///   1. 游戏启动时根据登录状态决定「登录按钮」是否显示
    ///   2. 点击「登录按钮」时打开弹窗，默认进入登录模式
    ///   3. 窗口内可在「登录」和「注册」两种模式之间切换
    ///   4. 登录/注册成功后自动关闭窗口并隐藏入口按钮
    ///
    /// ── Inspector 配置清单 ────────────────────────────────────────────────
    ///   loginButtonRoot        : 游客时显示的「登录」按钮 GameObject
    ///   loginWindow            : 弹窗根 GameObject（默认不激活）
    ///   windowTitleText        : 弹窗标题文本（可选）
    ///   emailInput             : 邮箱输入框
    ///   passwordInput          : 密码输入框（Content Type = Password）
    ///   confirmPasswordInput   : 确认密码输入框（Content Type = Password）仅注册显示
    ///   confirmButton          : 提交按钮（文字自动变「登录」/「注册」）
    ///   confirmButtonText      : confirmButton 下的 TextMeshPro 文本组件（可选）
    ///   signUpButton           : 登录模式下「没有账号？Sign Up」按钮
    ///   backToLoginButton      : 注册模式下「已有账号？登录」按钮
    ///   closeButton            : 关闭按钮
    ///   statusText             : 状态/错误提示文本
    /// </summary>
    public class LoginWindowManager : MonoBehaviour
    {
        // ── Inspector 配置 ─────────────────────────────────────────────────────

        [Header("入口按钮（游客显示，邮箱登录后隐藏）")]
        [SerializeField] private GameObject loginButtonRoot;

        [Header("弹窗根对象（默认设为不激活）")]
        [SerializeField] private GameObject loginWindow;

        [Header("弹窗标题（可选，会自动切换文字）")]
        [SerializeField] private TMP_Text windowTitleText;

        [Header("公共输入框")]
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField passwordInput;

        [Header("仅注册模式显示")]
        [Tooltip("确认密码输入框，注册时才显示")]
        [SerializeField] private TMP_InputField confirmPasswordInput;

        [Header("提交按钮")]
        [SerializeField] private Button confirmButton;
        [Tooltip("confirmButton 下的 TextMeshProUGUI 组件，用于动态修改按钮文字（可选）")]
        [SerializeField] private TMP_Text confirmButtonText;

        [Header("模式切换按钮")]
        [Tooltip("登录模式下显示：「没有账号？Sign Up」")]
        [SerializeField] private Button signUpButton;
        [Tooltip("注册模式下显示：「已有账号？登录」")]
        [SerializeField] private Button backToLoginButton;

        [Header("公共按钮")]
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text statusText;

        [Header("可选配置")]
        [Tooltip("成功后停留几秒再关窗口")]
        [SerializeField] [Range(0f, 2f)] private float successDisplaySeconds = 0.8f;

        // ── 内部状态 ──────────────────────────────────────────────────────────

        private enum AuthMode { Login, Register }
        private AuthMode _currentMode = AuthMode.Login;
        private bool     _isLoading   = false;

        // ── 生命周期 ──────────────────────────────────────────────────────────

        private void Start()
        {
            loginWindow?.SetActive(false);
            RefreshLoginButtonVisibility();

            // 订阅事件
            if (AccountManager.Instance != null)
            {
                AccountManager.Instance.OnEmailLoginSuccess    += HandleLoginSuccess;
                AccountManager.Instance.OnEmailLoginFailed     += HandleLoginFailed;
                AccountManager.Instance.OnEmailRegisterSuccess += HandleRegisterSuccess;
                AccountManager.Instance.OnEmailRegisterFailed  += HandleRegisterFailed;
            }
            else
            {
                Debug.LogWarning("[LoginWindowManager] AccountManager.Instance is null. " +
                                 "Ensure BusinessSystemInitializer is initialized before this script.");
            }

            // 绑定按钮（Inspector 里也可以直接用 OnClick 拖入，效果相同）
            confirmButton?.onClick.AddListener(OnConfirmClicked);
            closeButton?.onClick.AddListener(CloseLoginWindow);
            signUpButton?.onClick.AddListener(SwitchToRegisterMode);
            backToLoginButton?.onClick.AddListener(SwitchToLoginMode);
        }

        private void OnDestroy()
        {
            if (AccountManager.Instance != null)
            {
                AccountManager.Instance.OnEmailLoginSuccess    -= HandleLoginSuccess;
                AccountManager.Instance.OnEmailLoginFailed     -= HandleLoginFailed;
                AccountManager.Instance.OnEmailRegisterSuccess -= HandleRegisterSuccess;
                AccountManager.Instance.OnEmailRegisterFailed  -= HandleRegisterFailed;
            }
        }

        // ── 公开接口（供 UI 按钮 OnClick 调用）───────────────────────────────

        /// <summary>
        /// 打开弹窗，默认进入登录模式
        /// </summary>
        public void OpenLoginWindow()
        {
            if (loginWindow == null) { Debug.LogWarning("[LoginWindowManager] loginWindow not assigned."); return; }

            ClearInputs();
            SetStatus(string.Empty);
            SetLoadingState(false);
            loginWindow.SetActive(true);
            SwitchToLoginMode();           // 每次打开默认从登录模式开始
            emailInput?.ActivateInputField();
        }

        /// <summary>
        /// 关闭弹窗（挂在关闭按钮 OnClick 上）
        /// </summary>
        public void CloseLoginWindow()
        {
            loginWindow?.SetActive(false);
            SetLoadingState(false);
        }

        /// <summary>
        /// 切换到注册模式（挂在 signUpButton OnClick 上）
        /// </summary>
        public void SwitchToRegisterMode()
        {
            _currentMode = AuthMode.Register;
            RefreshModeUI();
        }

        /// <summary>
        /// 切换到登录模式（挂在 backToLoginButton OnClick 上）
        /// </summary>
        public void SwitchToLoginMode()
        {
            _currentMode = AuthMode.Login;
            RefreshModeUI();
        }

        /// <summary>
        /// 点击「登录」/「注册」确认按钮（挂在 confirmButton OnClick 上）
        /// </summary>
        public void OnConfirmClicked()
        {
            if (_isLoading) return;

            if (_currentMode == AuthMode.Login)
                DoLogin();
            else
                DoRegister();
        }

        // ── 登录 / 注册逻辑 ───────────────────────────────────────────────────

        private void DoLogin()
        {
            string email    = emailInput?.text.Trim() ?? string.Empty;
            string password = passwordInput?.text     ?? string.Empty;

            if (string.IsNullOrEmpty(email))    { SetStatus("Please enter your email."); return; }
            if (string.IsNullOrEmpty(password)) { SetStatus("Please enter your password."); return; }

            SetLoadingState(true);
            SetStatus("Signing in... Please wait.");
            AccountManager.Instance?.LoginWithEmail(email, password);
        }

        private void DoRegister()
        {
            string email           = emailInput?.text.Trim()          ?? string.Empty;
            string password        = passwordInput?.text               ?? string.Empty;
            string confirmPassword = confirmPasswordInput?.text        ?? string.Empty;

            if (string.IsNullOrEmpty(email))           { SetStatus("Please enter your email."); return; }
            if (string.IsNullOrEmpty(password))        { SetStatus("Please enter your password."); return; }
            if (password.Length < 6)                   { SetStatus("Password must be at least 6 characters."); return; }
            if (password != confirmPassword)           { SetStatus("Passwords do not match."); return; }

            SetLoadingState(true);
            SetStatus("Creating account... Please wait.");
            AccountManager.Instance?.RegisterWithEmail(email, password);
        }

        // ── 事件回调 ──────────────────────────────────────────────────────────

        private void HandleLoginSuccess(string userId)
        {
            SetLoadingState(false);
            SetStatus("Login successful!");
            RefreshLoginButtonVisibility();
            Invoke(nameof(CloseLoginWindow), successDisplaySeconds);
        }

        private void HandleLoginFailed(string error)
        {
            SetLoadingState(false);
            SetStatus($"Login failed: {error}");
        }

        private void HandleRegisterSuccess(string userId)
        {
            SetLoadingState(false);
            SetStatus("Sign up successful! You are now logged in.");
            RefreshLoginButtonVisibility();
            Invoke(nameof(CloseLoginWindow), successDisplaySeconds);
        }

        private void HandleRegisterFailed(string error)
        {
            SetLoadingState(false);
            SetStatus($"Sign up failed: {error}");
        }

        // ── 私有辅助方法 ──────────────────────────────────────────────────────

        /// <summary>
        /// 根据当前模式刷新 UI 显示状态
        /// </summary>
        private void RefreshModeUI()
        {
            bool isLogin = (_currentMode == AuthMode.Login);

            // 窗口标题
            if (windowTitleText != null)
                windowTitleText.text = isLogin ? "Login" : "Sign Up";

            // 确认按钮文字
            if (confirmButtonText != null)
                confirmButtonText.text = isLogin ? "LOGIN" : "SIGN UP";

            // 确认密码框：只有注册模式需要
            if (confirmPasswordInput != null)
                confirmPasswordInput.gameObject.SetActive(!isLogin);

            // 模式切换按钮互斥显示
            signUpButton?.gameObject.SetActive(isLogin);
            backToLoginButton?.gameObject.SetActive(!isLogin);

            // 切换模式时清空确认密码和状态提示
            confirmPasswordInput?.SetTextWithoutNotify(string.Empty);
            SetStatus(string.Empty);
        }

        /// <summary>
        /// 根据邮箱登录状态显示或隐藏入口「登录」按钮
        /// </summary>
        private void RefreshLoginButtonVisibility()
        {
            if (loginButtonRoot == null) return;
            bool emailLoggedIn = AccountManager.Instance?.IsEmailLoggedIn ?? false;
            loginButtonRoot.SetActive(!emailLoggedIn);
        }

        private void ClearInputs()
        {
            emailInput?.SetTextWithoutNotify(string.Empty);
            passwordInput?.SetTextWithoutNotify(string.Empty);
            confirmPasswordInput?.SetTextWithoutNotify(string.Empty);
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
        }

        private void SetLoadingState(bool loading)
        {
            _isLoading = loading;
            if (confirmButton     != null) confirmButton.interactable     = !loading;
            if (closeButton       != null) closeButton.interactable       = !loading;
            if (signUpButton      != null) signUpButton.interactable      = !loading;
            if (backToLoginButton != null) backToLoginButton.interactable = !loading;
        }
    }
}
