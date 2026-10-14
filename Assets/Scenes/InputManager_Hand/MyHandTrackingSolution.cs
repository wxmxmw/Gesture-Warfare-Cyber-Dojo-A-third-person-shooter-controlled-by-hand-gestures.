using Mediapipe;
using Mediapipe.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyHandTrackingSolution : ImageSourceSolution<MyHandTrackingGraph>
{
    [Header("摄像头检测设置")]
    public float cameraTimeout = 3.0f; // 摄像头超时时间（秒）
    private float lastFrameTime;
    private bool isCameraRunning = true; // 默认为true，避免刚启动就报警
    private HandGestureUI handGestureUI;
    
    [Header("摄像头自动重连设置")]
    public bool enableCameraAutoReconnect = true;
    public float cameraReconnectInterval = 1.0f;
    private float nextReconnectTime;
    private bool isReconnectInProgress;

    protected override void OnStartRun()
    {
        // 初始化
        lastFrameTime = Time.time;
        handGestureUI = FindObjectOfType<HandGestureUI>();

        //绑定graph的输出监听
        if (!runningMode.IsSynchronous())
        {
            graphRunner.OnHandLandmarksOutput += OnHandLandmarksOutput;
            graphRunner.OnHandWorldLandmarksOutput += OnHandWorldLandmarksOutput;
            graphRunner.OnHandednessOutput += OnHandednessOutput;
        }
    }
    void OnHandLandmarksOutput(object stream, OutputEventArgs<List<NormalizedLandmarkList>> eventArgs)
    {
        // 这里可以对得到的数据进行处理
        //首先我们拿到的是 List<NormalizedLandmarkList>,遍历下来数量有0到2个，这是识别到手的数量，每只手一个NormalizedLandmarkList

        //它下面有个 Landmark，也是集合类型
        //遍历出来的是NormalizedLandmark，数量是21，对应手的骨骼节点，下面有3个属性：X、Y、Z是坐标

        // 确保即使没有检测到手，也会更新视图
        List<NormalizedLandmarkList> handLandmarks = eventArgs.value ?? new List<NormalizedLandmarkList>();

        // 使用单例访问InputManager_Hand
        if (InputManager_Hand.Instance != null)
        {
            // 将手部数据传递给视图进行绘制
            InputManager_Hand.Instance.DrawLater(handLandmarks);
        }
    }

    void OnHandWorldLandmarksOutput(object stream, OutputEventArgs<List<LandmarkList>> eventArgs)
    {
        List<LandmarkList> handWorldLandmarks = eventArgs.value ?? new List<LandmarkList>();
        if (InputManager_Hand.Instance != null)
        {
            InputManager_Hand.Instance.UpdateWorldLandmarks(handWorldLandmarks);
        }
    }

    void OnHandednessOutput(object stream, OutputEventArgs<List<ClassificationList>> eventArgs)
    {
        // 处理左右手判断数据
        List<ClassificationList> handedness = eventArgs.value ?? new List<ClassificationList>();

        // 使用单例访问InputManager_Hand，传递左右手信息
        if (InputManager_Hand.Instance != null)
        {
            // 将左右手判断数据传递给InputManager_Hand处理
            InputManager_Hand.Instance.UpdateHandedness(handedness);
        }
    }


    protected override void SetupScreen(ImageSource imageSource)
    {
        // 不调用基类的SetupScreen方法，这样就不会初始化摄像头画面显示
        // base.SetupScreen(imageSource);

        // 如果需要完全禁用Screen组件，可以将其设置为非活动状态
        if (screen != null)
        {
            screen.gameObject.SetActive(false);
        }
    }

    protected override void RenderCurrentFrame(TextureFrame textureFrame)
    {
        // 不调用基类的RenderCurrentFrame方法，这样就不会渲染摄像头画面
        // base.RenderCurrentFrame(textureFrame);

        // 这里留空，不进行任何渲染操作
    }

    protected override void AddTextureFrameToInputStream(TextureFrame textureFrame)
    {
        // 更新最后帧时间
        lastFrameTime = Time.time;

        // 此处接入到graph的输入接口
        graphRunner.AddTextureFrameToInputStream(textureFrame);
    }

    private void Update()
    {
        // 尝试重新获取 HandGestureUI 引用，防止初始化时未找到
        if (handGestureUI == null)
        {
            handGestureUI = FindObjectOfType<HandGestureUI>();
        }

        // 检查摄像头是否超时
        if (Time.time - lastFrameTime > cameraTimeout)
        {
            if (isCameraRunning)
            {
                isCameraRunning = false;
                if (handGestureUI != null)
                {
                    handGestureUI.ShowCameraWarning(true);
                }
                Debug.LogWarning("摄像头信号中断或未启动！");
            }

            if (enableCameraAutoReconnect && !isReconnectInProgress && Time.time >= nextReconnectTime)
            {
                StartCoroutine(TryReconnectCamera());
            }
        }
        else
        {
            if (!isCameraRunning)
            {
                isCameraRunning = true;
                if (handGestureUI != null)
                {
                    handGestureUI.ShowCameraWarning(false);
                }
            }
        }
    }

    private IEnumerator TryReconnectCamera()
    {
        isReconnectInProgress = true;
        nextReconnectTime = Time.time + cameraReconnectInterval;

        Play();
        yield return null;

        isReconnectInProgress = false;
    }

    protected override IEnumerator WaitForNextValue()
    {
        //区分一下，同步有两个模式，阻塞同步 和 不阻塞同步，由参数 runningMode 决定（定义在ImageSourceSolution）
        List<NormalizedLandmarkList> handLandmarks = null;
        List<LandmarkList> handWorldLandmarks = null;
        List<ClassificationList> handedness = null;

        if (runningMode == RunningMode.Sync)
        {
            graphRunner.TryGetNext(out handLandmarks, out handWorldLandmarks, out handedness, true);
        }
        else if (runningMode == RunningMode.NonBlockingSync)
        {
            yield return new WaitUntil(() => graphRunner.TryGetNext(out handLandmarks, out handWorldLandmarks, out handedness, false));
        }

        // 在同步模式下，手动处理数据
        if (runningMode.IsSynchronous())
        {
            // 处理手部坐标数据
            if (InputManager_Hand.Instance != null)
            {
                InputManager_Hand.Instance.DrawLater(handLandmarks ?? new List<NormalizedLandmarkList>());
                InputManager_Hand.Instance.UpdateWorldLandmarks(handWorldLandmarks ?? new List<LandmarkList>());
                InputManager_Hand.Instance.UpdateHandedness(handedness ?? new List<ClassificationList>());
            }
        }
    }
}
