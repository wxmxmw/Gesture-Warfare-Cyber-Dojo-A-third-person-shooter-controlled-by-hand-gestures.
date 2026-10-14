using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HandGestureUI : MonoBehaviour
{
    [Header("UI设置")]
    public Canvas targetCanvas;

    // UI元素引用
    private GameObject leftHandPanel;
    private GameObject rightHandPanel;
    private Text leftHandTitle;
    private Text rightHandTitle;
    private Text[] leftHandGestureTexts = new Text[5];
    private Text[] rightHandGestureTexts = new Text[5];

    // 跳跃提示UI
    private Text jumpNotificationText;
    // 摄像头报警提示UI
    private Text cameraWarningText;
    // 武器类型提示UI
    private Text weaponTypeText;
    private Text gestureTutorialText;

    private HandGestureController handGestureController;

    // 手势名称数组（对应HandGestureState枚举，排除None）
    private string[] gestureNames = {
        "狙击瞄准",
        "射击",
        "脉冲手雷",
        "装弹",
        "暂停"
    };
    private HandGestureState[] gestureStates = new HandGestureState[] {
        HandGestureState.Aiming,
        HandGestureState.Shooting,
        HandGestureState.PulseGrenade,
        HandGestureState.Reload,
        HandGestureState.Pause
    };

    private InputManager_Hand inputManager;

    void Awake()
    {
        // 查找Canvas - 提前到Awake执行，确保外部调用时UI已准备好
        if (targetCanvas == null)
        {
            targetCanvas = FindObjectOfType<Canvas>();
        }

        if (targetCanvas == null)
        {
            Debug.LogError("No Canvas found! Please assign a Canvas to targetCanvas.");
            return;
        }

        // 创建UI元素
        CreateUI();
    }

    void Start()
    {
        // 获取HandGestureController实例
        handGestureController = FindObjectOfType<HandGestureController>();

        // 获取InputManager_Hand实例
        inputManager = InputManager_Hand.Instance;
        if (inputManager == null)
        {
            Debug.LogError("InputManager_Hand instance not found!");
            return;
        }

        // 订阅手势状态变化事件
        inputManager.OnCurrentHandGestureChanged += OnCurrentHandGestureChanged;

        // 订阅跳跃事件
        inputManager.OnJumpEvent += OnJumpEvent;
        // 初始化UI显示
        UpdateHandGestureUI(inputManager.LeftHandGestureState, leftHandGestureTexts, "左手");
        UpdateHandGestureUI(inputManager.RightHandGestureState, rightHandGestureTexts, "右手");
    }

    void OnDestroy()
    {
        // 取消订阅事件，防止内存泄漏
        if (inputManager != null)
        {
            inputManager.OnCurrentHandGestureChanged -= OnCurrentHandGestureChanged;
            inputManager.OnJumpEvent -= OnJumpEvent;
        }
    }

    /// <summary>
    /// 当前手势状态变化回调
    /// </summary>
    /// <param name="newGesture">新的手势状态</param>
    /// <param name="handIndex">手部索引(1=左手，2=右手)</param>
    private void OnCurrentHandGestureChanged(HandGestureState newGesture, int handIndex)
    {
        // 根据targetHandIndex决定更新哪只手的UI
        if (inputManager.TargetHandIndex == handIndex)
        {
            if (handIndex == 1) // 左手
            {
                UpdateHandGestureUI(newGesture, leftHandGestureTexts, "左手");
            }
            else if (handIndex == 2) // 右手
            {
                UpdateHandGestureUI(newGesture, rightHandGestureTexts, "右手");
            }
        }
    }

    void Update()
    {
        // 尝试获取HandGestureController引用（如果Start中未获取到）
        if (handGestureController == null)
        {
            handGestureController = FindObjectOfType<HandGestureController>();
        }

        // 更新武器类型显示。后续需要优化不要每一帧都检查！ 用事件订阅变更。
        if (handGestureController != null && weaponTypeText != null)
        {
            string weaponName = handGestureController.isSniper ? "狙击枪" : "手枪";
            weaponTypeText.text = $"当前武器: {weaponName}";
            // 狙击枪显示为红色，手枪显示为青色
            weaponTypeText.color = handGestureController.isSniper ? new Color(1f, 0.3f, 0.3f) : Color.cyan;
        }
    }

    /// <summary>
    /// 动态创建UI元素
    /// </summary>
    private void CreateUI()
    {
        // 创建左手面板 - 调整位置更靠下
        leftHandPanel = CreatePanel("LeftHandPanel", new Vector2(10, -100), new Vector2(250, 350), new Vector2(0, 0.3f), new Vector2(0, 0.8f));
        leftHandPanel.transform.SetParent(targetCanvas.transform, false);

        // 创建右手面板 - 调整位置更靠下
        rightHandPanel = CreatePanel("RightHandPanel", new Vector2(-10, -100), new Vector2(250, 350), new Vector2(1, 0.3f), new Vector2(1, 0.8f));
        rightHandPanel.transform.SetParent(targetCanvas.transform, false);

        // 创建左手标题 - 进一步增大字体
        leftHandTitle = CreateText("LeftHandTitle", "左手手势", leftHandPanel.transform, new Vector2(0, 150), 24, FontStyle.Bold);

        // 创建右手标题 - 进一步增大字体
        rightHandTitle = CreateText("RightHandTitle", "右手手势", rightHandPanel.transform, new Vector2(0, 150), 24, FontStyle.Bold);

        // 创建左手手势文本 - 进一步增大字体和间距
        for (int i = 0; i < gestureNames.Length; i++)
        {
            leftHandGestureTexts[i] = CreateText($"LeftGesture{i}", gestureNames[i], leftHandPanel.transform,
                new Vector2(0, 110 - i * 35), 22, FontStyle.Normal);
        }

        // 创建右手手势文本 - 进一步增大字体和间距
        for (int i = 0; i < gestureNames.Length; i++)
        {
            rightHandGestureTexts[i] = CreateText($"RightGesture{i}", gestureNames[i], rightHandPanel.transform,
                new Vector2(0, 110 - i * 35), 22, FontStyle.Normal);
        }

        // 创建跳跃提示文本 - 位于屏幕上方中央
        jumpNotificationText = CreateJumpNotificationText("JumpNotification", "跳跃发生！", targetCanvas.transform);
        jumpNotificationText.gameObject.SetActive(false); // 初始隐藏

        // 创建摄像头报警文本 - 位于屏幕中央，红色醒目
        cameraWarningText = CreateText("CameraWarning", "摄像头无信号！请检查设备", targetCanvas.transform, new Vector2(0, 0), 40, FontStyle.Bold);
        cameraWarningText.color = Color.red;
        cameraWarningText.gameObject.SetActive(false); // 初始隐藏

        // 创建武器类型显示文本 - 位于屏幕右下角
        weaponTypeText = CreateText("WeaponType", "当前武器: 手枪", targetCanvas.transform, Vector2.zero, 30, FontStyle.Bold);
        RectTransform rt = weaponTypeText.rectTransform;
        rt.anchorMin = new Vector2(1, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(1, 0);
        rt.anchoredPosition = new Vector2(-30, 30);
        rt.sizeDelta = new Vector2(300, 50);
        weaponTypeText.alignment = TextAnchor.MiddleRight;
        weaponTypeText.color = Color.cyan;

        gestureTutorialText = CreateText("GestureTutorial", "", targetCanvas.transform, new Vector2(0, -120), 32, FontStyle.Bold);
        RectTransform tr = gestureTutorialText.rectTransform;
        tr.anchorMin = new Vector2(0.5f, 1f);
        tr.anchorMax = new Vector2(0.5f, 1f);
        tr.pivot = new Vector2(0.5f, 1f);
        tr.sizeDelta = new Vector2(800, 80);
        gestureTutorialText.alignment = TextAnchor.MiddleCenter;
        gestureTutorialText.supportRichText = true;
        gestureTutorialText.gameObject.SetActive(false);
    }

    /// <summary>
    /// 显示或隐藏摄像头报警
    /// </summary>
    /// <param name="show">是否显示</param>
    public void ShowCameraWarning(bool show)
    {
        if (cameraWarningText != null && cameraWarningText.gameObject.activeSelf != show)
        {
            cameraWarningText.gameObject.SetActive(show);
        }
    }

    public void SetGestureTutorialMessage(string message)
    {
        if (gestureTutorialText == null) return;
        if (string.IsNullOrEmpty(message))
        {
            gestureTutorialText.gameObject.SetActive(false);
        }
        else
        {
            gestureTutorialText.gameObject.SetActive(true);
            gestureTutorialText.text = message;
        }
    }

    /// <summary>
    /// 创建面板
    /// </summary>
    private GameObject CreatePanel(string name, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject panel = new GameObject(name);
        panel.layer = 5; // UI layer

        RectTransform rectTransform = panel.AddComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;
        rectTransform.pivot = new Vector2(anchorMin.x, 0.5f);

        // 添加背景图像（可选）
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0.3f); // 半透明黑色背景

        return panel;
    }

    /// <summary>
    /// 创建文本组件
    /// </summary>
    private Text CreateText(string name, string text, Transform parent, Vector2 anchoredPosition, int fontSize, FontStyle fontStyle)
    {
        GameObject textObj = new GameObject(name);
        textObj.layer = 5; // UI layer
        textObj.transform.SetParent(parent, false);

        RectTransform rectTransform = textObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 0.5f);
        rectTransform.anchorMax = new Vector2(1, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        // 自动调整高度以适应字体大小，确保至少有1.2倍行高
        float height = Mathf.Max(35, fontSize * 1.5f);
        rectTransform.sizeDelta = new Vector2(0, height);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        Text textComponent = textObj.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = fontStyle;
        textComponent.color = Color.white;
        textComponent.alignment = TextAnchor.MiddleCenter;
        // 设置溢出模式，防止文本不可见
        textComponent.verticalOverflow = VerticalWrapMode.Overflow;

        return textComponent;
    }

    /// <summary>
    /// 创建跳跃提示文本组件
    /// </summary>
    private Text CreateJumpNotificationText(string name, string text, Transform parent)
    {
        GameObject textObj = new GameObject(name);
        textObj.layer = 5; // UI layer
        textObj.transform.SetParent(parent, false);

        RectTransform rectTransform = textObj.AddComponent<RectTransform>();
        // 设置锚点为屏幕上方中央
        rectTransform.anchorMin = new Vector2(0.5f, 1f);
        rectTransform.anchorMax = new Vector2(0.5f, 1f);
        rectTransform.anchoredPosition = new Vector2(0, -50); // 距离顶部50像素
        rectTransform.sizeDelta = new Vector2(400, 60); // 固定宽高
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        Text textComponent = textObj.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComponent.fontSize = 36;
        textComponent.fontStyle = FontStyle.Bold;
        textComponent.color = Color.yellow;
        textComponent.alignment = TextAnchor.MiddleCenter;

        return textComponent;
    }

    /// <summary>
    /// 更新指定手的手势UI显示
    /// </summary>
    /// <param name="currentGesture">当前手势状态</param>
    /// <param name="gestureTexts">手势文本数组</param>
    /// <param name="handName">手的名称（用于调试）</param>
    private void UpdateHandGestureUI(HandGestureState currentGesture, Text[] gestureTexts, string handName)
    {
        for (int i = 0; i < gestureTexts.Length && i < gestureNames.Length && i < gestureStates.Length; i++)
        {
            if (gestureTexts[i] != null)
            {
                HandGestureState gestureState = gestureStates[i];

                if (currentGesture == gestureState && currentGesture != HandGestureState.None)
                {
                    gestureTexts[i].text = gestureNames[i] + " ✓";
                    gestureTexts[i].color = Color.green;
                }
                else
                {
                    gestureTexts[i].text = gestureNames[i];
                    gestureTexts[i].color = Color.white;
                }
            }
        }
    }

    /// <summary>
    /// 跳跃事件回调
    /// </summary>
    private void OnJumpEvent()
    {
        // 显示跳跃提示
        StartCoroutine(ShowJumpNotification());
    }

    /// <summary>
    /// 显示跳跃提示协程
    /// </summary>
    private System.Collections.IEnumerator ShowJumpNotification()
    {
        if (jumpNotificationText != null)
        {
            // 显示文本
            jumpNotificationText.gameObject.SetActive(true);

            // 等待0.5秒
            yield return new WaitForSeconds(0.5f);

            // 隐藏文本
            jumpNotificationText.gameObject.SetActive(false);
        }
    }
}
