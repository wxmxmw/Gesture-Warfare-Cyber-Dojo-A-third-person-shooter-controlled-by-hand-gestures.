using System.Collections;
using UnityEngine;
using TMPro;

namespace Business
{
    /// <summary>
    /// 屏幕提示消息管理器（Toast 通知）
    /// 使用方法：
    ///   1. 在 Canvas 下创建一个 Panel（作为 toastPanel），其子物体放一个 TMP_Text（作为 toastText）
    ///   2. 将此组件挂到该 Panel 的父物体或 Canvas 上
    ///   3. 把 Panel 和 TMP_Text 拖入 Inspector 对应字段
    ///   4. 调用 ToastManager.Instance.ShowToast("消息内容")
    /// </summary>
    public class ToastManager : MonoBehaviour
    {
        public static ToastManager Instance { get; private set; }

        [Header("UI 引用（必须）")]
        [Tooltip("Toast 背景面板，不显示时设为 inactive")]
        [SerializeField] private GameObject toastPanel;

        [Tooltip("Toast 显示文字的 TMP_Text 组件")]
        [SerializeField] private TMP_Text toastText;

        [Header("显示设置")]
        [Tooltip("不传 duration 时默认显示的秒数")]
        [SerializeField] private float defaultDuration = 2.5f;

        private Coroutine _hideCoroutine;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            // 初始隐藏
            if (toastPanel != null)
                toastPanel.SetActive(false);
        }

        /// <summary>
        /// 显示一条 Toast 消息
        /// </summary>
        /// <param name="message">要显示的文字内容</param>
        /// <param name="duration">显示持续秒数，传 -1 使用 defaultDuration</param>
        public void ShowToast(string message, float duration = -1f)
        {
            if (toastPanel == null || toastText == null)
            {
                Debug.LogWarning("[ToastManager] toastPanel 或 toastText 未在 Inspector 中赋值。");
                return;
            }

            // 有正在显示的 Toast 时先停掉旧的计时，立即刷新内容
            if (_hideCoroutine != null)
                StopCoroutine(_hideCoroutine);

            toastText.text = message;
            toastPanel.SetActive(true);

            float showDuration = duration < 0f ? defaultDuration : duration;
            _hideCoroutine = StartCoroutine(HideAfterDelay(showDuration));
        }

        private IEnumerator HideAfterDelay(float delay)
        {
            // WaitForSecondsRealtime 不受 Time.timeScale = 0 影响，在暂停界面也能正常消失
            yield return new WaitForSecondsRealtime(delay);
            toastPanel.SetActive(false);
            _hideCoroutine = null;
        }
    }
}
