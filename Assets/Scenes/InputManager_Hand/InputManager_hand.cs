using KalmanFilter;
using Mediapipe;
using System.Collections.Generic;
using UnityEngine;
using System.Text;



/// <summary>
/// 手部关键点枚举
/// </summary>
public enum HandNodeIndex : int//21 keypoints
{
    wrist,//0手腕
    tcmc,//1大拇指手掌连接点
    tmcp,//2大拇指底
    tip,//3大拇指中
    ttip,//4大拇指尖
    ifmcp,//5食指手掌连接处
    ifpip,//6食指底
    ifdip,//7食指中
    iftip,//8食指尖
    mfmcp,//9中指手掌连接处
    mfpip,//10中指底
    mfdip,//11中指中
    mftip,//12中指尖
    rfmcp,//13无名指手掌连接处
    rfpip,//14无名指底
    rfdip,//15无名指中
    rftip,//16无名指尖
    pmcp,//17小指手掌连接处
    ppip,//18小指底
    pdip,//19小指中
    ptip,//20小指尖
    none
}

/// <summary>
/// 手指类型枚举
/// </summary>
public enum FingerType
{
    Thumb, // 0大拇指->节点1-4
    Index, // 1食指->节点5-8
    Middle, // 2中指->节点9-12
    Ring, // 3无名指->节点13-16
    Pinky // 4小拇指->节点17-20
}

/// <summary>
/// 手势状态枚举
/// </summary>
public enum HandGestureState
{
    None = 0,      // 无手势
    Aiming = 1,    // 狙击瞄准手势（狙击枪模式特有手势，仅有食指与大拇指伸直）
    Shooting = 2,  // 射击手势（狙击枪单发模式：拇指按到食指；默认手枪模式：拇指伸直弯曲都行）
    PulseGrenade = 3, // 脉冲手雷手势（握拳，拇指在外）
    HealthRecover = 4, // 生命值恢复手势（OK手势）
    Reload = 5,    // 装弹手势（点赞手势，拇指向上）
    Pause = 6      // 暂停手势（五指张开）
}

public class InputManager_Hand : MonoBehaviour
{
    // 单手关键点数量常量
    public const int HAND_KEYPOINTS_COUNT = 21;

    // 单例实例
    public static InputManager_Hand Instance { get; private set; }

    // 手势状态记录变量 - 分别记录左右手状态
    private HandGestureState leftHandGestureState = HandGestureState.None;
    private HandGestureState rightHandGestureState = HandGestureState.None;

    // 跳跃状态记录变量
    private bool isJumping = false;

    // 手势状态变化事件
    public System.Action<HandGestureState, int> OnCurrentHandGestureChanged; // 参数：手势状态，手部索引(1=左手，2=右手)

    // 目标手部索引变化事件（左右手切换）
    public System.Action<int> OnTargetHandIndexChanged; // 参数：新的目标手部索引(0=无手，1=左手，2=右手)

    // 跳跃事件（仅在开始跳跃时触发）
    public System.Action OnJumpEvent; // 当从非跳跃状态变为跳跃状态时触发
    /// <summary>
    /// 获取左手手势状态
    /// </summary>
    public HandGestureState LeftHandGestureState
    {
        get { return leftHandGestureState; }
        set
        {
            // 允许射击手势重复触发事件，以实现连发功能
            if (leftHandGestureState != value || value == HandGestureState.Shooting)
            {
                leftHandGestureState = value;
                OnCurrentHandGestureChanged?.Invoke(value, 1); // 1表示左手
            }
        }
    }
    /// <summary>
    /// 获取右手手势状态
    /// </summary>
    public HandGestureState RightHandGestureState
    {
        get { return rightHandGestureState; }
        set
        {
            // 允许射击手势重复触发事件，以实现连发功能
            if (rightHandGestureState != value || value == HandGestureState.Shooting)
            {
                rightHandGestureState = value;
                OnCurrentHandGestureChanged?.Invoke(value, 2); // 2表示右手
            }
        }
    }

    /// <summary>
    /// 获取当前手势状态（保持向后兼容性）
    /// 优先返回左手状态，如果左手无手势则返回右手状态
    /// </summary>
    public HandGestureState CurrentHandGestureState
    {
        get { return leftHandGestureState != HandGestureState.None ? leftHandGestureState : rightHandGestureState; }
        set
        {
            // 为了向后兼容，设置时同时更新左手状态
            leftHandGestureState = value;
        }
    }
    public Transform objRoot; // 存放小球的父节点
    public Transform objRoot2; // 存放小球的父节点
    public GameObject boneObj;
    public GameObject palmCenter;//模拟玩家的位置（紫色球）
    public float m_screenWidth;
    public float m_screenHeight;

    // 摄像头引用，用于动态调整位置
    private Camera mainCamera;

    //深度检测相关参数
    static float m_baseDist = 0.3f;
    static float m_depthRatio = -50f;
    //static float SCALE_BASE = 2.0f;//手部关键点可视化材料大小倍数

    // 第三人称视角下玩家Y坐标锁定比例常量
    public const float PLAYER_Y_POSITION_RATIO = 0.1f; // 屏幕高度的1/10 
    // 跳跃检测Y坐标阈值比例常量
    public const float JUMP_Y_THRESHOLD_RATIO = 1f / 3f; // 屏幕高度的1/3 
    // 公开屏幕宽高属性，供其他脚本访问
    // 注意：现在返回的是实际屏幕像素尺寸，而不是世界坐标尺寸
    public float ScreenWidth { get { return m_screenWidth; } }
    public float ScreenHeight { get { return m_screenHeight; } }

    private List<GameObject> m_boneObjList = new List<GameObject>(); // 第一只手的关键点小球
    private List<GameObject> m_boneObjList2 = new List<GameObject>(); // 第二只手的关键点小球
                                                                      // 卡尔曼滤波器数组：左关键点，每个关键点有X、Y两个滤波器
    private KalmanFilter.KalmanFilter[][] kalmanFilters1;
    // 卡尔曼滤波器数组：右手关键点，每个关键点有X、Y两个滤波器
    private KalmanFilter.KalmanFilter[][] kalmanFilters2;
    private CyberHandVisualizer cyberpunkVisualizer;
    [Header("Debug")]
    public bool showLandmarkDebug = true;
    public bool showWorldLandmarkDebug = true;
    private string landmarkDebugText = "";
    private string worldLandmarkDebugText = "";
    public bool showFingerBentDebug = true;
    private string fingerBentDebugText = "";

    // 双手的手部关键点位置世界坐标向量组
    private Vector3[] handNodePositions1 = new Vector3[HAND_KEYPOINTS_COUNT]; // 左手关键点的世界坐标
    private Vector3[] handNodePositions2 = new Vector3[HAND_KEYPOINTS_COUNT]; // 右手关键点的世界坐标

    // 目标手部索引，每帧更新
    private int targetHandIndex = 0;

    /// <summary>
    /// 获取当前活跃的手部索引（只读属性）
    /// 0: 无手部数据, 1: 左手, 2: 右手
    /// </summary>
    public int TargetHandIndex
    {
        get { return targetHandIndex; }
    }
    List<NormalizedLandmarkList> m_currList;
    List<LandmarkList> m_currWorldList;
    List<ClassificationList> m_currHandedness; // 存储左右手判断数据
    bool m_newLandMark = false;
    bool m_newWorldLandMark = false;
    void Awake()
    {
        // 单例模式实现
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); //跨场景保持
        }
        else
        {
            Destroy(gameObject); // 如果已存在实例，销毁当前对象
        }
    }
    void Start()
    {
        // 使用实际屏幕分辨率
        m_screenWidth = Screen.width;
        m_screenHeight = Screen.height;

        // 获取主摄像头引用
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("Main Camera not found! Camera position will not be adjusted.");
        }

        Vector3 currentPos = mainCamera.transform.position;
        // X,Y坐标，保持Z不变
        mainCamera.transform.position = new Vector3(m_screenWidth / 2f, m_screenHeight / 2f, currentPos.z);
        // 初始化卡尔曼滤波器数组
        InitializeKalmanFilters();

        while (m_boneObjList.Count < HAND_KEYPOINTS_COUNT) //第一只手
        {
            m_boneObjList.Add(Instantiate(boneObj, objRoot));

        }
        while (m_boneObjList2.Count < HAND_KEYPOINTS_COUNT)//第二只手
        {
            m_boneObjList2.Add(Instantiate(boneObj, objRoot2));
        }

        // 初始化赛博朋克视觉效果
        InitializeCyberpunkVisuals();
    }
    //区分了同步和异步的处理，
    //同步数据直接接入 DrawNow()，异步数据则接入到 DrawLater()，稍后从Update里处理，
    //因为它不允许在异步线程里面操作。
    public void DrawLater(List<NormalizedLandmarkList> list)
    {
        // 更新当前手部数据列表
        if (list == null)
        {
            Debug.Log("No hand detected");
            // 当没有检测到手时，确保 m_currList 为 null
            m_currList = null;
            m_newLandMark = true;
        }
        else
        {
            //Debug.Log($"Hand detected: {list.Count} hands");
            m_currList = list;
            m_newLandMark = true;
        }
    }

    public void UpdateWorldLandmarks(List<LandmarkList> list)
    {
        m_currWorldList = list;
        m_newWorldLandMark = true;
    }

    void Update()
    {   /*
        // 动态更新屏幕尺寸（应对窗口大小变化）
        m_screenWidth = Screen.width;
        m_screenHeight = Screen.height;*/
        // UpdateCameraPosition();

        // 每帧更新目标手部索引
        int newTargetHandIndex = GetActiveHandIndex();
        if (targetHandIndex != newTargetHandIndex)
        {
            targetHandIndex = newTargetHandIndex;
            OnTargetHandIndexChanged?.Invoke(targetHandIndex);
        }

        if (m_newLandMark)
        {
            UpdateHandNodePosition();
            DrawNow();
        }

        /*打印世界坐标
        if (m_newWorldLandMark)
        {
            UpdateWorldLandmarkDebugText();
            m_newWorldLandMark = false;
        }
        */
        // 跳跃检测逻辑
        CheckJumpGesture();
        //UpdateFingerBentDebugText();
    }

    public void UpdateHandNodePosition()
    {
        if (showLandmarkDebug) landmarkDebugText = "";
        // 如果没有手部数据，清空向量组
        if (m_currList == null)
        {
            // 清空左手的位置数据
            for (int i = 0; i < HAND_KEYPOINTS_COUNT; i++)
            {
                handNodePositions1[i] = Vector3.zero;
            }
            // 清空右手的位置数据
            for (int i = 0; i < HAND_KEYPOINTS_COUNT; i++)
            {
                handNodePositions2[i] = Vector3.zero;
            }
            return;
        }

        // 先清空左右手的位置数据
        for (int i = 0; i < HAND_KEYPOINTS_COUNT; i++)
        {
            handNodePositions1[i] = Vector3.zero; // 左手
            handNodePositions2[i] = Vector3.zero; // 右手
        }

        // 遍历所有检测到的手，根据左右手判断分配到对应的位置数组
        for (int handIdx = 0; handIdx < m_currList.Count && handIdx < 2; handIdx++)
        {
            var landmarks = m_currList[handIdx].Landmark;
            if (landmarks.Count < HAND_KEYPOINTS_COUNT) continue;

            // 获取当前手的左右手判断
            string handedness = GetHandedness(handIdx + 1);

            // 根据左右手判断选择对应的位置数组和卡尔曼滤波器
            Vector3[] targetPositions;
            KalmanFilter.KalmanFilter[][] targetFilters;

            if (handedness == "Left")
            {
                targetPositions = handNodePositions1; // 左手使用 handNodePositions1
                targetFilters = kalmanFilters1;
            }
            else if (handedness == "Right")
            {
                targetPositions = handNodePositions2; // 右手使用 handNodePositions2
                targetFilters = kalmanFilters2;
            }
            else
            {
                // 如果无法判断左右手，跳过这只手
                continue;
            }

            // 更新对应手的关键点位置
            for (int i = 0; i < landmarks.Count && i < HAND_KEYPOINTS_COUNT; i++)
            {
                var mark = landmarks[i];
                // 转换为世界坐标
                targetPositions[i] = new Vector3(
                    (1 - mark.X) * m_screenWidth,
                     (1 - mark.Y) * m_screenHeight,
                    mark.Z * m_screenWidth
                );

                // 使用对应的卡尔曼滤波器平滑坐标
                targetFilters[i][0].Update(targetPositions[i].x);
                targetFilters[i][1].Update(targetPositions[i].y);
                targetFilters[i][2].Update(targetPositions[i].z);

                targetPositions[i].x = targetFilters[i][0].GetState();
                targetPositions[i].y = targetFilters[i][1].GetState();
                targetPositions[i].z = targetFilters[i][2].GetState();
            }
            /*打印 Normalized Landmarks
            if (showLandmarkDebug)
            {
                var sb = new StringBuilder();
                sb.Append($"Hand {handIdx + 1} ({handedness}) Landmarks:\n");
                for (int i = 0; i < HAND_KEYPOINTS_COUNT; i++)
                {
                    var p = targetPositions[i];
                    sb.Append($"#{i}: {p.x:F1}, {p.y:F1}, {p.z:F1}\n");
                }
                landmarkDebugText += sb.ToString();
            }*/

        }
    }
    void OnGUI()
    {
        if (showLandmarkDebug)
        {
            var rect = new UnityEngine.Rect(Screen.width * 0.2f, 10, Screen.width * 0.7f, Screen.height * 0.9f);
            GUI.Label(rect, landmarkDebugText);
        }

        if (showWorldLandmarkDebug)
        {
            var rect = new UnityEngine.Rect(Screen.width * 0.6f, 10, Screen.width * 0.35f, Screen.height * 0.9f);
            GUI.Label(rect, worldLandmarkDebugText);
        }
        if (showFingerBentDebug)
        {
            var rect = new UnityEngine.Rect(Screen.width * 0.2f, 10, Screen.width * 0.3f, Screen.height * 0.5f);
            GUI.Label(rect, fingerBentDebugText);
        }
    }

    private void UpdateWorldLandmarkDebugText()
    {
        if (!showWorldLandmarkDebug)
        {
            worldLandmarkDebugText = "";
            return;
        }

        if (m_currWorldList == null || m_currWorldList.Count == 0)
        {
            worldLandmarkDebugText = "";
            return;
        }

        var sb = new StringBuilder();
        for (int handIdx = 0; handIdx < m_currWorldList.Count && handIdx < 2; handIdx++)
        {
            var landmarks = m_currWorldList[handIdx].Landmark;
            if (landmarks == null || landmarks.Count < HAND_KEYPOINTS_COUNT) continue;

            sb.Append($"World Hand {handIdx + 1} Landmarks:\n");
            for (int i = 0; i < HAND_KEYPOINTS_COUNT; i++)
            {
                var p = landmarks[i];
                sb.Append($"#{i}: {p.X:F3}, {p.Y:F3}, {p.Z:F3}\n");
            }
        }
        worldLandmarkDebugText = sb.ToString();
    }
    public void DrawNow()
    {
        m_newLandMark = false;

        // 如果没有手部数据，隐藏所有手
        if (m_currList == null)
        {
            HideBones(1, 0, HAND_KEYPOINTS_COUNT); // 隐藏左手
            HideBones(2, 0, HAND_KEYPOINTS_COUNT); // 隐藏右手
            palmCenter.SetActive(false);
            return;
        }
        // 检查是否有左手数据并绘制
        if (HasHandData(1))
        {
            DrawLeftHand();
            // 设置手腕位置(模拟玩家位置）
            palmCenter.SetActive(true);
            palmCenter.transform.position = GetPlayerPosition();
        }
        else
        {
            HideBones(1, 0, HAND_KEYPOINTS_COUNT); // 隐藏左手
        }
        // 检查是否有右手数据并绘制
        if (HasHandData(2))
        {
            DrawRightHand();
            palmCenter.SetActive(true);
            palmCenter.transform.position = GetPlayerPosition();
        }
        else
        {
            HideBones(2, 0, HAND_KEYPOINTS_COUNT); // 隐藏右手
        }
    }

    private void UpdateFingerBentDebugText()
    {
        if (!showFingerBentDebug)
        {
            fingerBentDebugText = "";
            return;
        }
        int handIdx = targetHandIndex;
        if (handIdx != 1 && handIdx != 2)
        {
            fingerBentDebugText = "";
            return;
        }
        if (!HasHandData(handIdx))
        {
            fingerBentDebugText = "";
            return;
        }
        bool thumb = IsFingerBent(handIdx, FingerType.Thumb);
        bool index = IsFingerBent(handIdx, FingerType.Index);
        bool middle = IsFingerBent(handIdx, FingerType.Middle);
        bool ring = IsFingerBent(handIdx, FingerType.Ring);
        bool pinky = IsFingerBent(handIdx, FingerType.Pinky);
        float thumbAngle = GetFingerBendAngle(handIdx, FingerType.Thumb);
        float indexAngle = GetFingerBendAngle(handIdx, FingerType.Index);
        float middleAngle = GetFingerBendAngle(handIdx, FingerType.Middle);
        float ringAngle = GetFingerBendAngle(handIdx, FingerType.Ring);
        float pinkyAngle = GetFingerBendAngle(handIdx, FingerType.Pinky);
        var sb = new StringBuilder();
        sb.Append("手指弯曲状态\n");
        sb.Append("拇指: ").Append(thumb ? "弯曲" : "伸直").Append(" (").Append(thumbAngle.ToString("F1")).Append("°)\n");
        sb.Append("食指: ").Append(index ? "弯曲" : "伸直").Append(" (").Append(indexAngle.ToString("F1")).Append("°)\n");
        sb.Append("中指: ").Append(middle ? "弯曲" : "伸直").Append(" (").Append(middleAngle.ToString("F1")).Append("°)\n");
        sb.Append("无名指: ").Append(ring ? "弯曲" : "伸直").Append(" (").Append(ringAngle.ToString("F1")).Append("°)\n");
        sb.Append("小指: ").Append(pinky ? "弯曲" : "伸直").Append(" (").Append(pinkyAngle.ToString("F1")).Append("°)");
        fingerBentDebugText = sb.ToString();
    }

    private float GetFingerBendAngle(int handIndex, FingerType fingerType)
    {
        if (!HasHandData(handIndex))
        {
            return 0f;
        }
        List<int> fingerJointIndices = GetFingerJointIndices(fingerType);
        if (fingerJointIndices == null || fingerJointIndices.Count < 3)
        {
            return 0f;
        }
        
        // 获取关键点坐标
        Vector3[] jointPositions = new Vector3[fingerJointIndices.Count];
        for (int i = 0; i < fingerJointIndices.Count; i++)
        {
            jointPositions[i] = GetHandNodePosition(handIndex, fingerJointIndices[i]);
        }
        // 计算两个关节的向量
        Vector3 vector1 = jointPositions[1] - jointPositions[0];
        Vector3 vector2 = jointPositions[2] - jointPositions[1];

        // 计算夹角
        return VectorAngle(vector1, vector2);
    }
    /// <summary>
    /// 绘制左手的关键点
    /// </summary>
    private void DrawLeftHand()
    {
        // 获取左手的数据和对象
        var boneObjList = m_boneObjList;
        var objRoot = this.objRoot;

        // 计算根位置和深度（使用左手的第一个关键点）
        var rootPos = handNodePositions1[0];

        // 找到对应的原始landmark数据来计算深度
        NormalizedLandmark mark1 = null, mark2 = null;
        for (int handIdx = 0; handIdx < m_currList.Count; handIdx++)
        {
            string handedness = GetHandedness(handIdx + 1);
            if (handedness == "Left")
            {
                var landmarks = m_currList[handIdx].Landmark;
                if (landmarks.Count >= 2)
                {
                    mark1 = landmarks[0];
                    mark2 = landmarks[1];
                    break;
                }
            }
        }

        if (mark1 != null && mark2 != null)
        {
            GetDepthByLandmark(mark1, mark2, out var depth, out var scale);

            // 设置根节点位置和 (缩放)
            objRoot.position = new Vector3(rootPos.x, rootPos.y, depth);
            // objRoot.localScale = new Vector3(scale * SCALE_BASE, scale * SCALE_BASE, scale * SCALE_BASE);

            // 绘制所有关键点
            for (var i = 0; i < HAND_KEYPOINTS_COUNT; i++)
            {
                if (handNodePositions1[i] != Vector3.zero)
                {
                    boneObjList[i].SetActive(true);
                    //boneObjList[i].transform.localScale = new Vector3(scale * SCALE_BASE, scale * SCALE_BASE, scale * SCALE_BASE);
                    boneObjList[i].transform.position = handNodePositions1[i];
                }
                else
                {
                    boneObjList[i].SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// 绘制右手的关键点
    /// </summary>
    private void DrawRightHand()
    {
        // 获取右手的数据和对象
        var boneObjList = m_boneObjList2;
        var objRoot = objRoot2;

        // 计算根位置和深度（使用右手的第一个关键点）
        var rootPos = handNodePositions2[0];

        // 找到对应的原始landmark数据来计算深度
        NormalizedLandmark mark1 = null, mark2 = null;
        for (int handIdx = 0; handIdx < m_currList.Count; handIdx++)
        {
            string handedness = GetHandedness(handIdx + 1);
            if (handedness == "Right")
            {
                var landmarks = m_currList[handIdx].Landmark;
                if (landmarks.Count >= 2)
                {
                    mark1 = landmarks[0];
                    mark2 = landmarks[1];
                    break;
                }
            }
        }

        if (mark1 != null && mark2 != null)
        {
            GetDepthByLandmark(mark1, mark2, out var depth, out var scale);

            // 设置根节点位置和缩放
            objRoot.position = new Vector3(rootPos.x, rootPos.y, depth);
            //objRoot.localScale = new Vector3(scale * SCALE_BASE, scale * SCALE_BASE, scale * SCALE_BASE);
            palmCenter.SetActive(true);

            // 绘制所有关键点
            for (var i = 0; i < HAND_KEYPOINTS_COUNT; i++)
            {
                if (handNodePositions2[i] != Vector3.zero)
                {
                    boneObjList[i].SetActive(true);
                    //boneObjList[i].transform.localScale = new Vector3(scale * SCALE_BASE, scale * SCALE_BASE, scale * SCALE_BASE);
                    boneObjList[i].transform.position = handNodePositions2[i];
                }
                else
                {
                    boneObjList[i].SetActive(false);
                }
            }
        }
    }


    void GetDepthByLandmark(NormalizedLandmark mark1, NormalizedLandmark mark2, out float depth, out float scale)
    {
        var pos1 = new Vector3(mark1.X, mark1.Y, mark1.Z);
        var pos2 = new Vector3(mark2.X, mark2.Y, mark2.Z);
        var length = Vector3.Distance(pos1, pos2);
        scale = m_baseDist * length * (m_screenWidth / 10);
        depth = m_baseDist * m_depthRatio / length;
    }

    public void HideBones(int handIndex, int BoneindexBegin, int BoneindexEnd)
    {
        if (handIndex == 1)
        {
            for (var i = BoneindexBegin; i < BoneindexEnd; i++)//左手消失，删除小球
            {
                m_boneObjList[i].SetActive(false);
            }
            palmCenter.SetActive(false);
        }
        else if (handIndex == 2)
        {
            for (var i = BoneindexBegin; i < BoneindexEnd; i++)//右手消失，删除小球
            {
                m_boneObjList2[i].SetActive(false);
            }

        }
    }


    /// <summary>
    /// 获取玩家位置
    /// </summary>
    /// <returns> 手腕位置世界坐标Vec3,其中Y坐标锁定为屏幕高/10，Z坐标恒为0</returns>
    public Vector3 GetPlayerPosition()
    {

        Vector3 originalWristPosition;
        if (TargetHandIndex == 1)
        {
            originalWristPosition = handNodePositions1[0];
        }
        else if (TargetHandIndex == 2)
        {
            originalWristPosition = handNodePositions2[0];
        }
        else
        {
            return Vector3.zero;
        }
        // 返回修改后的坐标：X坐标保持原值，Y坐标锁定为屏幕高度的指定比例，Z坐标恒为0
        return new Vector3(originalWristPosition.x, m_screenHeight * PLAYER_Y_POSITION_RATIO, 0f);
    }

    /// <summary>
    /// 检查指定手的数据是否可用
    /// </summary>
    /// <param name="handIndex">手的索引（1 表示左手，2 表示右手）</param>
    /// <returns>指定手的数据可用，返回 true</returns>
    public bool HasHandData(int handIndex)
    {
        // 检查手部索引是否有效
        if (handIndex != 1 && handIndex != 2)
        {
            return false;
        }

        // 检查 m_currList 是否为 null
        if (m_currList == null)
        {
            return false;
        }

        // 根据手部索引检查对应的手部数据
        if (handIndex == 1) // 左手
        {
            // 检查 handNodePositions1（左手）是否有有效数据
            for (int i = 0; i < HAND_KEYPOINTS_COUNT; i++)
            {
                if (handNodePositions1[i] != Vector3.zero)
                {
                    return true;
                }
            }
            return false;
        }
        else // handIndex == 2，右手
        {
            // 检查 handNodePositions2（右手）是否有有效数据
            for (int i = 0; i < HAND_KEYPOINTS_COUNT; i++)
            {
                if (handNodePositions2[i] != Vector3.zero)
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// 检查指定手指是否弯曲
    /// </summary>
    /// <param name="handIndex">手的索引（1 表示左手，2 表示右手）</param>
    /// <param name="fingerType">手指类型</param>
    /// <returns>返回 true 表示弯曲，false 表示伸直</returns>
    public bool IsFingerBent(int handIndex, FingerType fingerType)
    {
        // 使用 HasHandData 方法检查手部数据是否可用
        if (!HasHandData(handIndex))
        {
            return false;
        }
        // 获取手指的关键点索引
        List<int> fingerJointIndices = GetFingerJointIndices(fingerType);
        if (fingerJointIndices == null || fingerJointIndices.Count < 3)
        {
            Debug.LogError("Invalid finger type or not enough joint indices!");
            return false;
        }

        // 获取关键点坐标
        Vector3[] jointPositions = new Vector3[fingerJointIndices.Count];
        for (int i = 0; i < fingerJointIndices.Count; i++)
        {
            jointPositions[i] = GetHandNodePosition(handIndex, fingerJointIndices[i]);
        }
        // 计算两个关节的向量
        Vector3 vector1 = jointPositions[1] - jointPositions[0];
        Vector3 vector2 = jointPositions[2] - jointPositions[1];

        // 计算夹角
        float angle = VectorAngle(vector1, vector2);

        // 判断是否弯曲。调整弯曲阈值以提高检测准确性
        float bendThreshold;
        if (fingerType == FingerType.Thumb)
        {
            bendThreshold = 35f;
        }
        else if (fingerType == FingerType.Index)
        {
            bendThreshold = 65f;
        }
        else
            bendThreshold = 15f;
        return angle > bendThreshold; // 大于弯曲阈值判断为弯曲
    }


    /// <summary>
    /// 获取手指的关键点索引
    /// </summary>
    /// <param name="fingerType">手指类型</param>
    /// <returns>手指的关键点索引列表</returns>
    private List<int> GetFingerJointIndices(FingerType fingerType)
    {
        switch (fingerType)
        {
            case FingerType.Thumb:
                // 拇指关键点索引使用 tmcp(2)、tip(3)、ttip(4) 来计算拇指的弯曲度
                return new List<int> { (int)HandNodeIndex.tmcp, (int)HandNodeIndex.tip, (int)HandNodeIndex.ttip };
            case FingerType.Index:
                return new List<int> { (int)HandNodeIndex.ifmcp, (int)HandNodeIndex.ifpip, (int)HandNodeIndex.ifdip };
            case FingerType.Middle:
                return new List<int> { (int)HandNodeIndex.mfmcp, (int)HandNodeIndex.mfpip, (int)HandNodeIndex.mfdip };
            case FingerType.Ring:
                return new List<int> { (int)HandNodeIndex.rfmcp, (int)HandNodeIndex.rfpip, (int)HandNodeIndex.rfdip };
            case FingerType.Pinky:
                return new List<int> { (int)HandNodeIndex.pmcp, (int)HandNodeIndex.ppip, (int)HandNodeIndex.pdip };
            default:
                return null;
        }
    }

    /// <summary>
    /// 计算两个向量之间的夹角（单位：度）
    /// </summary>
    /// <param name="vector1">向量1</param>
    /// <param name="vector2">向量2</param>
    /// <returns>夹角（单位：度）</returns>
    private float VectorAngle(Vector3 vector1, Vector3 vector2)
    {
        float dotProduct = Vector3.Dot(vector1.normalized, vector2.normalized);
        dotProduct = Mathf.Clamp(dotProduct, -1f, 1f); // 防止浮点数误差导致超出范围
        float angleRadians = Mathf.Acos(dotProduct);
        return angleRadians * Mathf.Rad2Deg; // 转换为角度
    }


    /// <summary>
    /// 获取食指的指向方向（单位向量）
    /// </summary>
    /// <param name="handIndex">手的索引（1 表示左手，2 表示右手）</param>
    /// <returns>返回食指的指向方向Vec3（单位向量），如果数据不足则返回零向量</returns>
    public Vector3 GetIndexFingerDirection(int handIndex)
    {
        // 检查对应手的数据是否可用
        if (!HasHandData(handIndex)) return Vector3.zero;

        // 获取食指指尖和食指手掌连接处的关键点坐标
        Vector3 indexFingerTipPosition = GetHandNodePosition(handIndex, (int)HandNodeIndex.iftip);
        Vector3 ifmcpPosition = GetHandNodePosition(handIndex, (int)HandNodeIndex.ifmcp);

        // 计算方向向量并归一化
        Vector3 direction = (indexFingerTipPosition - ifmcpPosition).normalized;
        return direction;
    }

    /// <summary>
    /// 获取玩家方向（当前活跃手的食指指向方向）
    /// </summary>
    /// <returns>返回当前活跃手的食指指向方向Vec3（单位向量），如果数据不足则返回零向量</returns>
    public Vector3 GetPlayerDirection()
    {
        if (TargetHandIndex == 0) return Vector3.zero; // 没有活跃的手
        Vector3 Direction = GetIndexFingerDirection(TargetHandIndex);
        Direction.z = 0;
        // 返回当前活跃手的食指方向
        return Direction;
    }


    /// <summary>
    /// 获取指定手的指定关键点的世界坐标
    /// </summary>
    /// <param name="handIndex">手的索引（1 表示左手，2 表示右手）</param>
    /// <param name="handNodeIndex">关键点索引</param>
    /// <returns>返回手部的某个关键点的世界坐标，如果数据不可用则返回 Vector3.zero</returns>
    public Vector3 GetHandNodePosition(int handIndex, int nodeIndex)
    {
        // 检查手部索引是否有效
        if (handIndex != 1 && handIndex != 2)
        {
            Debug.LogError("手部索引只能取1或2");
            return Vector3.zero;
        }

        // 检查关键点索引是否有效
        if (nodeIndex < 0 || nodeIndex >= HAND_KEYPOINTS_COUNT)
        {
            Debug.LogError("关键点索引超出范围（0-20）");
            return Vector3.zero;
        }

        // 根据手部索引返回对应的关键点位置（1=左手，2=右手）
        if (handIndex == 1)
        {
            return handNodePositions1[nodeIndex]; // 左手数据
        }
        else // handIndex == 2
        {
            return handNodePositions2[nodeIndex]; // 右手数据
        }
    }

    /// <summary>
    /// 获取指定手的关键点位置数据（归一化坐标）
    /// </summary>
    /// <param name="handIndex">手的索引（1 表示左手，2 表示右手）</param>
    /// <returns>返回手部关键点数据，如果数据不可用则返回 null</returns>
    public NormalizedLandmarkList GetHandLandmarks(int handIndex)
    {
        // 检查数据是否可用 
        if (!HasHandData(handIndex)) return null;

        // 根据左右手判断找到对应的原始数据
        for (int i = 0; i < m_currList.Count; i++)
        {
            string handedness = GetHandedness(i + 1);
            if ((handIndex == 1 && handedness == "Left") ||
                (handIndex == 2 && handedness == "Right"))
            {
                return m_currList[i];
            }
        }

        return null;
    }


    /// <summary>
    /// 更新左右手判断数据
    /// </summary>
    /// <param name="handedness">左右手判断数据列表</param>
    public void UpdateHandedness(List<ClassificationList> handedness)
    {
        m_currHandedness = handedness;
    }

    /// <summary>
    /// 获取指定手的左右手判断
    /// </summary>
    /// <param name="handIndex">手的索引（1 表示第一只手，2 表示第二只手）</param>
    /// <returns>返回"Left"或"Right"，如果无法判断则返回"Unknown"</returns>
    public string GetHandedness(int handIndex)
    {
        // 检查手部索引是否有效
        if (handIndex != 1 && handIndex != 2)
        {
            Debug.LogError("手部索引只能取1或2");
            return "Unknown";
        }

        // 检查是否有左右手判断数据
        if (m_currHandedness == null || m_currHandedness.Count < handIndex)
        {
            return "Unknown";
        }

        // 获取对应手的分类数据
        var classification = m_currHandedness[handIndex - 1];
        if (classification == null || classification.Classification.Count == 0)
        {
            return "Unknown";
        }

        // 返回置信度最高的分类标签
        var bestClassification = classification.Classification[0];
        foreach (var cls in classification.Classification)
        {
            if (cls.Score > bestClassification.Score)
            {
                bestClassification = cls;
            }
        }

        return bestClassification.Label;
    }

    /// <summary>
    /// 初始化卡尔曼滤波器数组
    /// </summary>
    void InitializeKalmanFilters()
    {
        // 初始化左手的卡尔曼滤波器
        kalmanFilters1 = new KalmanFilter.KalmanFilter[HAND_KEYPOINTS_COUNT][];
        for (int i = 0; i < HAND_KEYPOINTS_COUNT; i++)
        {
            kalmanFilters1[i] = new KalmanFilter.KalmanFilter[3]; // X, Y, Z 三个坐标
            kalmanFilters1[i][0] = new KalmanFilter.KalmanFilter(); // X坐标滤波器
            kalmanFilters1[i][1] = new KalmanFilter.KalmanFilter(); // Y坐标滤波器
            kalmanFilters1[i][2] = new KalmanFilter.KalmanFilter(); // Z坐标滤波器
        }

        // 初始化右手的卡尔曼滤波器
        kalmanFilters2 = new KalmanFilter.KalmanFilter[HAND_KEYPOINTS_COUNT][];
        for (int i = 0; i < HAND_KEYPOINTS_COUNT; i++)
        {
            kalmanFilters2[i] = new KalmanFilter.KalmanFilter[3]; // X, Y, Z 三个坐标
            kalmanFilters2[i][0] = new KalmanFilter.KalmanFilter(); // X坐标滤波器
            kalmanFilters2[i][1] = new KalmanFilter.KalmanFilter(); // Y坐标滤波器
            kalmanFilters2[i][2] = new KalmanFilter.KalmanFilter(); // Z坐标滤波器
        }
    }

    /// <summary>
    /// 检测瞄准手势（手枪手势）
    /// 食指伸直，拇指和食指成90°
    /// </summary>
    /// <param name="handIndex">手的索引（1 表示左手，2 表示右手）</param>
    /// <returns>是否为瞄准手势</returns>
    public bool IsAimingGesture(int handIndex)
    {
        if (!HasHandData(handIndex)) return false;

        // 检查食指是否伸直
        bool indexStraight = !IsFingerBent(handIndex, FingerType.Index);
        if (!indexStraight) return false; // 食指必须伸直
        // 检查拇指是否伸直
        bool thumbStraight = !IsFingerBent(handIndex, FingerType.Thumb);

        // 检查中指、无名指、小指是否弯曲（握拳状态）
        bool middleBent = IsFingerBent(handIndex, FingerType.Middle);
        bool ringBent = IsFingerBent(handIndex, FingerType.Ring);
        bool pinkyBent = IsFingerBent(handIndex, FingerType.Pinky);

        return indexStraight && thumbStraight && middleBent && ringBent && pinkyBent;
    }


    /// <summary>
    /// 检测射击手势（食指伸直，其他手指弯曲）
    /// </summary>
    /// <param name="handIndex">手的索引（1 表示左手，2 表示右手）</param>
    /// <returns>是否为射击手势</returns>
    public bool IsShootingGesture(int handIndex)
    {
        if (!HasHandData(handIndex)) return false;

        // 检查食指是否伸直
        bool indexStraight = !IsFingerBent(handIndex, FingerType.Index);
        if (!indexStraight) return false; // 食指必须伸直

        // 检查其他手指的弯曲状态
        // 修改：不再检查拇指状态，合并瞄准（拇指伸直）和射击（拇指弯曲）两种情况
        // bool thumbBent = IsFingerBent(handIndex, FingerType.Thumb);
        bool middleBent = IsFingerBent(handIndex, FingerType.Middle);
        bool ringBent = IsFingerBent(handIndex, FingerType.Ring);
        bool pinkyBent = IsFingerBent(handIndex, FingerType.Pinky);

        return indexStraight && middleBent && ringBent && pinkyBent;
    }

    /// <summary>
    /// 检测狙击枪射击手势（食指伸直，拇指弯曲，其他手指弯曲）
    /// </summary>
    /// <param name="handIndex">手的索引（1 表示左手，2 表示右手）</param>
    /// <returns>是否为狙击枪射击手势</returns>
    public bool IsSniperShootingGesture(int handIndex)
    {
        if (!HasHandData(handIndex)) return false;

        // 检查食指是否伸直
        bool indexStraight = !IsFingerBent(handIndex, FingerType.Index);
        if (!indexStraight) return false; // 食指必须伸直

        // 检查拇指是否弯曲
        bool thumbBent = IsFingerBent(handIndex, FingerType.Thumb);

        // 检查其他手指的弯曲状态
        bool middleBent = IsFingerBent(handIndex, FingerType.Middle);
        bool ringBent = IsFingerBent(handIndex, FingerType.Ring);
        bool pinkyBent = IsFingerBent(handIndex, FingerType.Pinky);

        return indexStraight && thumbBent && middleBent && ringBent && pinkyBent;
    }

    /// <summary>
    /// 检测脉冲手雷手势（握拳）
    /// </summary>
    /// <param name="handIndex">手的索引（1 表示左手，2 表示右手）</param>
    /// <returns>是否为脉冲手雷手势</returns>
    public bool IsPulseGrenadeGesture(int handIndex)
    {
        if (!HasHandData(handIndex)) return false;

        bool indexBent = IsFingerBent(handIndex, FingerType.Index);
        bool middleBent = IsFingerBent(handIndex, FingerType.Middle);
        bool ringBent = IsFingerBent(handIndex, FingerType.Ring);
        bool pinkyBent = IsFingerBent(handIndex, FingerType.Pinky);

        if (!(indexBent && middleBent && ringBent && pinkyBent)) return false;

        Vector3 thumbTip = GetHandNodePosition(handIndex, (int)HandNodeIndex.ttip);
        Vector3 indexTip = GetHandNodePosition(handIndex, (int)HandNodeIndex.iftip);
        Vector3 middleTip = GetHandNodePosition(handIndex, (int)HandNodeIndex.mftip);
        Vector3 ringTip = GetHandNodePosition(handIndex, (int)HandNodeIndex.rftip);
        Vector3 pinkyTip = GetHandNodePosition(handIndex, (int)HandNodeIndex.ptip);

        float threshold = m_screenWidth * 0.08f;
        int closeCount = 0;
        if (Vector3.Distance(thumbTip, indexTip) < threshold) closeCount++;
        if (Vector3.Distance(thumbTip, middleTip) < threshold) closeCount++;
        if (Vector3.Distance(thumbTip, ringTip) < threshold) closeCount++;
        if (Vector3.Distance(thumbTip, pinkyTip) < threshold) closeCount++;

        return closeCount >= 3;
    }

    /// <summary>
    /// 检测生命值恢复手势（OK手势：拇指与食指形成圆圈，其余三指伸直）
    /// </summary>
    /// <param name="handIndex">手的索引（1 表示左手，2 表示右手）</param>
    /// <returns>是否为生命值恢复手势</returns>
    public bool IsHealthRecoverGesture(int handIndex)
    {
        if (!HasHandData(handIndex)) return false;

        // 获取拇指尖和食指尖的位置

        Vector3 thumbPos = GetHandNodePosition(handIndex, (int)HandNodeIndex.ttip);
        Vector3 indexPos = GetHandNodePosition(handIndex, (int)HandNodeIndex.iftip);

        // 检查拇指和食指是否接触（形成圆圈）
        float thumbIndexDist = Vector3.Distance(thumbPos, indexPos);
        float okThreshold = 40f; // OK手势的距离阈值

        // 检查其余三指是否伸直
        bool middleStraight = !IsFingerBent(handIndex, FingerType.Middle);
        bool ringStraight = !IsFingerBent(handIndex, FingerType.Ring);
        bool pinkyStraight = !IsFingerBent(handIndex, FingerType.Pinky);

        return thumbIndexDist < okThreshold && middleStraight && ringStraight && pinkyStraight;
    }

    /// <summary>
    /// 检测装弹手势（食指单独弯曲）
    /// </summary>
    /// <param name="handIndex">手的索引（1 表示左手，2 表示右手）</param>
    /// <returns>是否为装弹手势</returns>
    public bool IsReloadGesture(int handIndex)
    {
        if (!HasHandData(handIndex)) return false;

        // 检查食指是否弯曲
        bool indexBent = IsFingerBent(handIndex, FingerType.Index);

        // 检查拇指是否伸直
        bool thumbStraight = !IsFingerBent(handIndex, FingerType.Thumb);

        // 检查中指、无名指、小指是否弯曲（握拳状态）
        bool middleBent = IsFingerBent(handIndex, FingerType.Middle);
        bool ringBent = IsFingerBent(handIndex, FingerType.Ring);
        bool pinkyBent = IsFingerBent(handIndex, FingerType.Pinky);

        if (!(indexBent && thumbStraight && middleBent && ringBent && pinkyBent)) return false;

        Vector3 thumbTip = GetHandNodePosition(handIndex, (int)HandNodeIndex.ttip);
        Vector3 thumbMcp = GetHandNodePosition(handIndex, (int)HandNodeIndex.tmcp);
        Vector3 v = thumbTip - thumbMcp;
        if (v == Vector3.zero) return false;

        Vector3 dir = v.normalized;
        return dir.y > 0.6f;
    }

    /// <summary>
    /// 检测暂停手势（五指张开，手掌正对屏幕）
    /// </summary>
    /// <param name="handIndex">手的索引（1 表示左手，2 表示右手）</param>
    /// <returns>是否为暂停手势</returns>
    public bool IsPauseGesture(int handIndex)
    {
        if (!HasHandData(handIndex)) return false;

        // 检查所有手指是否都伸直
        bool thumbStraight = !IsFingerBent(handIndex, FingerType.Thumb);
        bool indexStraight = !IsFingerBent(handIndex, FingerType.Index);
        bool middleStraight = !IsFingerBent(handIndex, FingerType.Middle);
        bool ringStraight = !IsFingerBent(handIndex, FingerType.Ring);
        bool pinkyStraight = !IsFingerBent(handIndex, FingerType.Pinky);

        return thumbStraight && indexStraight && middleStraight && ringStraight && pinkyStraight;
    }

    /// <summary>
    /// 更新摄像头位置，使其X,Y坐标始终在手部中心 ,可选用
    /// </summary>
    private void UpdateCameraPosition()
    {
        if (mainCamera == null) return;

        // 计算手部中心X,Y坐标
        Vector2 centerXY = CalculateHandsCenterXY();

        // 获取当前摄像头位置
        Vector3 currentPos = mainCamera.transform.position;

        // 更新X,Y坐标，保持Z不变
        mainCamera.transform.position = new Vector3(centerXY.x, m_screenHeight / 2f, currentPos.z);
    }

    /// <summary>
    /// 计算所有检测到的手部的中心X,Y坐标
    /// </summary>
    /// <returns>手部中心X,Y坐标，如果没有检测到手则返回屏幕中心</returns>
    private Vector2 CalculateHandsCenterXY()
    {
        float totalX = 0f;
        float totalY = 0f;
        int handCount = 0;

        // 检查左手是否有数据
        if (HasHandData(1))
        {
            // 使用手腕位置作为手的中心点
            Vector3 leftHandCenter = GetHandNodePosition(1, (int)HandNodeIndex.wrist);
            if (leftHandCenter != Vector3.zero)
            {
                totalX += leftHandCenter.x;
                totalY += leftHandCenter.y;
                handCount++;
            }
        }

        // 检查右手是否有数据
        if (HasHandData(2))
        {
            // 使用手腕位置作为手的中心点
            Vector3 rightHandCenter = GetHandNodePosition(2, (int)HandNodeIndex.wrist);
            if (rightHandCenter != Vector3.zero)
            {
                totalX += rightHandCenter.x;
                totalY += rightHandCenter.y;
                handCount++;
            }
        }

        // 如果检测到手，返回平均中心位置；否则返回屏幕中心
        if (handCount > 0)
        {
            return new Vector2(totalX / handCount, totalY / handCount);
        }
        else
        {
            // 没有检测到手时，返回屏幕中心坐标
            return new Vector2(m_screenWidth / 2f, m_screenHeight / 2f);
        }
    }

    /// <summary>
    /// 初始化赛博朋克视觉效果
    /// </summary>
    private void InitializeCyberpunkVisuals()
    {
        // 添加CyberpunkHandVisualizer组件
        cyberpunkVisualizer = gameObject.GetComponent<CyberHandVisualizer>();
        if (cyberpunkVisualizer == null)
        {
            cyberpunkVisualizer = gameObject.AddComponent<CyberHandVisualizer>();
        }
        //Debug.Log("CyberpunkHandVisualizer: 赛博朋克视觉效果已启用");
    }
    /// <summary>
    /// 智能选择活跃的手：优先左手，如果没有左手则选择右手
    /// </summary>
    /// <returns>活跃手的索引（1=左手，2=右手，0=无手）</returns>
    private int GetActiveHandIndex()
    {
        // 如果有左手数据，优先选择左手
        if (HasHandData(1))
        {
            return 1; // 左手
        }
        // 如果没有左手但有右手，选择右手
        else if (HasHandData(2))
        {
            return 2; // 右手
        }
        // 没有任何手部数据
        else
        {
            return 0; // 无手
        }
    }

    /// <summary>
    /// 获取当前跳跃状态
    /// </summary>
    public bool IsJumping
    {
        get { return isJumping; }
    }

    /// <summary>
    /// 检测跳跃手势
    /// 当目标手的手腕Y坐标超过屏幕高度的指定比例时，判断为跳跃
    /// </summary>
    private void CheckJumpGesture()
    {
        // 如果没有活跃的手部数据，不进行跳跃检测
        if (targetHandIndex == 0)
        {
            if (isJumping)
            {
                isJumping = false;
            }
            return;
        }

        // 获取当前活跃手的手腕位置（索引0为手腕）
        Vector3 wristPosition;
        if (targetHandIndex == 1)
        {
            wristPosition = handNodePositions1[0]; // 左手手腕
        }
        else // targetHandIndex == 2
        {
            wristPosition = handNodePositions2[0]; // 右手手腕
        }

        // 如果手腕位置无效，不进行检测
        if (wristPosition == Vector3.zero)
        {
            if (isJumping)
            {
                isJumping = false;
            }
            return;
        }

        // 计算跳跃阈值（屏幕高度的指定比例）
        float jumpThreshold = m_screenHeight * JUMP_Y_THRESHOLD_RATIO;

        // 检测跳跃：手腕Y坐标超过阈值
        bool shouldBeJumping = wristPosition.y > jumpThreshold;

        // 只在从非跳跃状态变为跳跃状态时触发事件
        if (!isJumping && shouldBeJumping)
        {
            isJumping = true;
            OnJumpEvent?.Invoke();
        }
        else if (isJumping && !shouldBeJumping)
        {
            isJumping = false;
        }
    }

}


