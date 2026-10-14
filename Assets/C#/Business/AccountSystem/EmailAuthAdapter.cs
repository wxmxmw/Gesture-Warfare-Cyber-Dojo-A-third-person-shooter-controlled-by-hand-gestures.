using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace Business.AccountSystem
{
    /// <summary>
    /// 邮箱注册/登录请求体（对应服务器 API 的 JSON 字段）
    /// </summary>
    [Serializable]
    internal class EmailAuthRequest
    {
        public string email;
        public string password;
    }

    /// <summary>
    /// 服务器返回体（注册和登录通用）
    /// </summary>
    [Serializable]
    internal class EmailAuthResponse
    {
        public bool   success;
        public string message;
        public string userId;
    }

    /// <summary>
    /// 邮箱注册/登录适配器
    /// 通过 HTTP/HTTPS POST 调用服务器 API，服务器负责 bcrypt 哈希密码并存入 MySQL
    /// 客户端永远不存储原始密码
    ///
    /// 注意：若使用 HTTP（非 HTTPS），需要在 Unity Player Settings → Other Settings
    ///       → Allow downloads over HTTP 中选择 "Always allowed"
    ///       Android 还需在 AndroidManifest.xml 的 application 标签添加
    ///       android:usesCleartextTraffic="true"
    /// </summary>
    public class EmailAuthAdapter
    {
        /// <summary>
        /// 服务器 API 根地址，修改此处切换环境
        /// 当前使用 HTTP，如服务器配置了 HTTPS 证书，改为 https:// 即可
        /// </summary>
        private const string BASE_URL = "http://149.88.70.16:5000";

        private const int TIMEOUT_SECONDS = 10;

        /// <summary>注册成功，参数为服务器分配的 userId</summary>
        public event Action<string> OnRegisterSuccess;

        /// <summary>注册失败，参数为错误原因</summary>
        public event Action<string> OnRegisterFailed;

        /// <summary>登录成功，参数为服务器分配的 userId</summary>
        public event Action<string> OnLoginSuccess;

        /// <summary>登录失败，参数为错误原因</summary>
        public event Action<string> OnLoginFailed;

        private readonly MonoBehaviour _coroutineRunner;

        private static readonly Regex EmailRegex =
            new Regex(@"^[\w\.\-]+@[\w\.\-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled);

        public EmailAuthAdapter(MonoBehaviour runner)
        {
            _coroutineRunner = runner;
        }

        // ── 公开接口 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 注册新账号（邮箱 + 密码）
        /// 先在本地校验格式，再发送给服务器；密码只在网络传输中存在，不落本地
        /// </summary>
        public void Register(string email, string password)
        {
            string error = ValidateInput(email, password);
            if (error != null)
            {
                OnRegisterFailed?.Invoke(error);
                return;
            }
            _coroutineRunner.StartCoroutine(SendRequest("/api/register", email, password, isRegister: true));
        }

        /// <summary>
        /// 用邮箱 + 密码登录
        /// 先在本地校验格式，再发送给服务器
        /// </summary>
        public void Login(string email, string password)
        {
            string error = ValidateInput(email, password);
            if (error != null)
            {
                OnLoginFailed?.Invoke(error);
                return;
            }
            _coroutineRunner.StartCoroutine(SendRequest("/api/login", email, password, isRegister: false));
        }

        // ── 私有方法 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 本地格式校验（邮箱格式、密码最短长度）
        /// 与服务器 validators 一致，避免无效请求
        /// </summary>
        private static string ValidateInput(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))  return "请输入邮箱";
            if (!EmailRegex.IsMatch(email))         return "邮箱格式不正确";
            if (string.IsNullOrWhiteSpace(password)) return "请输入密码";
            if (password.Length < 6)                return "密码长度至少6位";
            return null;
        }

        private IEnumerator SendRequest(string endpoint, string email, string password, bool isRegister)
        {
            var requestData = new EmailAuthRequest { email = email, password = password };
            string  json    = JsonUtility.ToJson(requestData);
            byte[]  bodyRaw = Encoding.UTF8.GetBytes(json);

            UnityWebRequest request = new UnityWebRequest(BASE_URL + endpoint, "POST");
            try
            {
                request.uploadHandler   = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = TIMEOUT_SECONDS;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<EmailAuthResponse>(request.downloadHandler.text);
                    if (response != null && response.success)
                    {
                        Debug.Log($"[EmailAuthAdapter] {endpoint} success. userId={response.userId}");
                        if (isRegister) OnRegisterSuccess?.Invoke(response.userId);
                        else            OnLoginSuccess?.Invoke(response.userId);
                    }
                    else
                    {
                        string msg = response?.message ?? "未知错误";
                        Debug.LogWarning($"[EmailAuthAdapter] {endpoint} server error: {msg}");
                        if (isRegister) OnRegisterFailed?.Invoke(msg);
                        else            OnLoginFailed?.Invoke(msg);
                    }
                }
                else
                {
                    string netError = "网络连接失败，请检查网络";
                    Debug.LogWarning($"[EmailAuthAdapter] {endpoint} network error: {request.error}");
                    if (isRegister) OnRegisterFailed?.Invoke(netError);
                    else            OnLoginFailed?.Invoke(netError);
                }
            }
            finally
            {
                request.Dispose();
            }
        }
    }
}
