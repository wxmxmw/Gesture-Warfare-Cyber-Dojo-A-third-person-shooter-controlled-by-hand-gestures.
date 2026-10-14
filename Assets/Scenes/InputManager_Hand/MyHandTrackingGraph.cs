using Mediapipe.Unity;
using Mediapipe;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyHandTrackingGraph : GraphRunner
{
  //输入
  const string _InputStreamName = "input_video";

  //输出 （此处输出 手节点的坐标 和 左右手判断)
  OutputStream<NormalizedLandmarkListVectorPacket, List<NormalizedLandmarkList>> _handLandmarksStream;
  const string _HandLandmarksStreamName = "hand_landmarks";

  OutputStream<LandmarkListVectorPacket, List<LandmarkList>> _handWorldLandmarksStream;
  const string _HandWorldLandmarksStreamName = "hand_world_landmarks";
  
  OutputStream<ClassificationListVectorPacket, List<ClassificationList>> _handednessStream;
  const string _HandednessStreamName = "handedness";

  public event EventHandler<OutputEventArgs<List<NormalizedLandmarkList>>> OnHandLandmarksOutput
  {
    add => _handLandmarksStream.AddListener(value);
    remove => _handLandmarksStream.RemoveListener(value);
  }

  public event EventHandler<OutputEventArgs<List<ClassificationList>>> OnHandednessOutput
  {
    add => _handednessStream.AddListener(value);
    remove => _handednessStream.RemoveListener(value);
  }

  public event EventHandler<OutputEventArgs<List<LandmarkList>>> OnHandWorldLandmarksOutput
  {
    add => _handWorldLandmarksStream.AddListener(value);
    remove => _handWorldLandmarksStream.RemoveListener(value);
  }

  public bool TryGetNext(out List<NormalizedLandmarkList> handLandmarks, out List<ClassificationList> handedness, bool allowBlock = true)
  {
    var currentTimestampMicrosec = GetCurrentTimestampMicrosec();
    var r1 = TryGetNext(_handLandmarksStream, out handLandmarks, allowBlock, currentTimestampMicrosec);
    var r2 = TryGetNext(_handednessStream, out handedness, allowBlock, currentTimestampMicrosec);
    return r1 || r2;
  }

  public bool TryGetNext(out List<NormalizedLandmarkList> handLandmarks, out List<LandmarkList> handWorldLandmarks, out List<ClassificationList> handedness, bool allowBlock = true)
  {
    var currentTimestampMicrosec = GetCurrentTimestampMicrosec();
    var r1 = TryGetNext(_handLandmarksStream, out handLandmarks, allowBlock, currentTimestampMicrosec);
    var r2 = TryGetNext(_handWorldLandmarksStream, out handWorldLandmarks, allowBlock, currentTimestampMicrosec);
    var r3 = TryGetNext(_handednessStream, out handedness, allowBlock, currentTimestampMicrosec);
    return r1 || r2 || r3;
  }

  public override void StartRun(ImageSource imageSource)
  {
    //启动 
    if (runningMode.IsSynchronous())
    {
      _handLandmarksStream.StartPolling().AssertOk();
      _handWorldLandmarksStream.StartPolling().AssertOk();
      _handednessStream.StartPolling().AssertOk();
    }
    var sidePacket = new SidePacket();
    SetImageTransformationOptions(sidePacket, imageSource, true); // GraphRunner提供的接口
    sidePacket.Emplace("model_complexity", new IntPacket(0));
    sidePacket.Emplace("num_hands", new IntPacket(2));
    //calculatorGraph.StartRun(sidePacket).AssertOk();
    StartRun(sidePacket);

  }

  public override void Stop()
  {
    //释放
    _handLandmarksStream?.Close();
    _handLandmarksStream = null;
    _handWorldLandmarksStream?.Close();
    _handWorldLandmarksStream = null;
    _handednessStream?.Close();
    _handednessStream = null;
    base.Stop();
  }

  public void AddTextureFrameToInputStream(TextureFrame textureFrame)
  {
    AddTextureFrameToInputStream(_InputStreamName, textureFrame);
  }

  protected override Status ConfigureCalculatorGraph(CalculatorGraphConfig config)
  {
    if (runningMode == RunningMode.NonBlockingSync)
    {
      _handLandmarksStream = new OutputStream<NormalizedLandmarkListVectorPacket, List<NormalizedLandmarkList>>(
          calculatorGraph, _HandLandmarksStreamName, config.AddPacketPresenceCalculator(_HandLandmarksStreamName), timeoutMicrosec);
      _handWorldLandmarksStream = new OutputStream<LandmarkListVectorPacket, List<LandmarkList>>(
          calculatorGraph, _HandWorldLandmarksStreamName, config.AddPacketPresenceCalculator(_HandWorldLandmarksStreamName), timeoutMicrosec);
      _handednessStream = new OutputStream<ClassificationListVectorPacket, List<ClassificationList>>(
          calculatorGraph, _HandednessStreamName, config.AddPacketPresenceCalculator(_HandednessStreamName), timeoutMicrosec);
    }
    else
    {
      _handLandmarksStream = new OutputStream<NormalizedLandmarkListVectorPacket, List<NormalizedLandmarkList>>(
          calculatorGraph, _HandLandmarksStreamName, true, timeoutMicrosec);
      _handWorldLandmarksStream = new OutputStream<LandmarkListVectorPacket, List<LandmarkList>>(
          calculatorGraph, _HandWorldLandmarksStreamName, true, timeoutMicrosec);
      _handednessStream = new OutputStream<ClassificationListVectorPacket, List<ClassificationList>>(
          calculatorGraph, _HandednessStreamName, true, timeoutMicrosec);
    }

    return base.ConfigureCalculatorGraph(config);
  }

  protected override IList<WaitForResult> RequestDependentAssets()
  {
    return new List<WaitForResult>()
    {
        WaitForAsset("hand_landmark_lite.bytes"),
        WaitForAsset("palm_detection_lite.bytes"),
        WaitForAsset("handedness.txt"),
    };
  }
}

