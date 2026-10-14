using System;
using UnityEngine;

#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

namespace Business.AccountSystem
{
    /// <summary>
    /// Google 登录适配器
    /// 封装 Google Play Games SDK，是唯一允许直接调用该 SDK 的地方
    /// 如需替换登录方式，只需修改此类
    /// </summary>
    public class LoginAdapter
    {
        /// <summary>Google 登录成功，返回 Google 用户 ID</summary>
        public event Action<string> OnLoginSuccess;

        /// <summary>Google 登录失败，返回错误信息</summary>
        public event Action<string> OnLoginFailed;

        public bool IsInitialized { get; private set; }

        /// <summary>
        /// 初始化 Google Play Games SDK
        /// </summary>
        public void Initialize()
        {
#if UNITY_ANDROID
            try
            {
                PlayGamesPlatform.Activate();
                IsInitialized = true;
                Debug.Log("[LoginAdapter] Google Play Games SDK activated.");
            }
            catch (Exception e)
            {
                IsInitialized = false;
                Debug.LogError($"[LoginAdapter] Initialization failed: {e.Message}");
            }
#else
            IsInitialized = false;
            Debug.Log("[LoginAdapter] Google login only supported on Android.");
#endif
        }

        /// <summary>
        /// 发起 Google 账号登录流程
        /// </summary>
        public void SignIn()
        {
            if (!IsInitialized)
            {
                string msg = "Google Play Games SDK not initialized.";
                Debug.LogWarning($"[LoginAdapter] {msg}");
                OnLoginFailed?.Invoke(msg);
                return;
            }

#if UNITY_ANDROID
            try
            {
                PlayGamesPlatform.Instance.Authenticate(status =>
                {
                    if (status == SignInStatus.Success)
                    {
                        string googleId = PlayGamesPlatform.Instance.GetUserId();
                        Debug.Log($"[LoginAdapter] Sign-in success. Google ID: {googleId}");
                        OnLoginSuccess?.Invoke(googleId);
                    }
                    else
                    {
                        string error = $"Sign-in failed: {status}";
                        Debug.LogError($"[LoginAdapter] {error}");
                        OnLoginFailed?.Invoke(error);
                    }
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[LoginAdapter] Sign-in exception: {e.Message}");
                OnLoginFailed?.Invoke(e.Message);
            }
#else
            OnLoginFailed?.Invoke("Google login not supported on this platform.");
#endif
        }

        /// <summary>
        /// 登出 Google 账号
        /// 注意：Google Play Games SDK v2.x 已移除 SignOut API。
        /// 新版本中 Google Play 账号由 Android 系统统一管理，不再提供应用内登出接口。
        /// 此方法保留仅做状态清理，不做实际 SDK 调用。
        /// </summary>
        public void SignOut()
        {
            Debug.Log("[LoginAdapter] SignOut called. Google Play Games v2.x does not support in-app sign-out.");
        }
    }
}
