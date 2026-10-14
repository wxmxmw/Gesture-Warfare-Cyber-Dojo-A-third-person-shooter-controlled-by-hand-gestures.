using UnityEngine;

/// <summary>
/// 摄像头诊断脚本 - 挂到任意GameObject上运行，在Console查看结果
/// 诊断完成后可以删除此脚本
/// </summary>
public class WebCamDiagnostic : MonoBehaviour
{
    void Start()
    {
        Debug.Log("====== 摄像头诊断开始 ======");

        // 1. 检查系统检测到的摄像头数量
        WebCamDevice[] devices = WebCamTexture.devices;
        Debug.Log($"[诊断] 检测到的摄像头数量: {devices.Length}");

        if (devices.Length == 0)
        {
            Debug.LogError("[诊断] 未检测到任何摄像头！请检查：\n" +
                           "1. 摄像头是否物理连接\n" +
                           "2. Windows设置→隐私→相机，是否允许桌面应用访问\n" +
                           "3. 设备管理器中摄像头驱动是否正常");
            return;
        }

        // 2. 列出所有摄像头信息
        for (int i = 0; i < devices.Length; i++)
        {
            Debug.Log($"[诊断] 摄像头[{i}]: 名称={devices[i].name}, " +
                      $"前置={devices[i].isFrontFacing}");
        }

        // 3. 尝试启动第一个摄像头
        Debug.Log($"[诊断] 尝试启动摄像头: {devices[0].name}");
        WebCamTexture testTexture = new WebCamTexture(devices[0].name, 640, 480, 30);
        testTexture.Play();
        StartCoroutine(CheckWebCam(testTexture));
    }

    System.Collections.IEnumerator CheckWebCam(WebCamTexture tex)
    {
        int waitFrames = 0;
        while (waitFrames < 120) // 最多等待120帧
        {
            yield return new WaitForEndOfFrame();
            waitFrames++;
            if (tex.width > 16)
            {
                Debug.Log($"[诊断] 摄像头启动成功！分辨率: {tex.width}x{tex.height}, 帧率: {tex.requestedFPS}");
                Debug.Log("[诊断] 摄像头工作正常，问题可能在MediaPipe配置或Bootstrap设置");
                tex.Stop();
                Debug.Log("====== 诊断完成：摄像头正常 ======");
                yield break;
            }
        }

        Debug.LogError($"[诊断] 摄像头启动超时（{waitFrames}帧后宽度仍为{tex.width}）\n" +
                       "可能原因：\n" +
                       "1. 摄像头被其他软件占用（关闭微信/钉钉/腾讯会议/Zoom等）\n" +
                       "2. 摄像头驱动异常，尝试重新安装驱动\n" +
                       "3. 虚拟摄像头软件冲突");
        tex.Stop();
        Debug.Log("====== 诊断完成：摄像头异常 ======");
    }
}
