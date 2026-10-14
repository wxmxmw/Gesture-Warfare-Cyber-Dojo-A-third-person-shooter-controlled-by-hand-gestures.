# Gesture Warfare: Cyber Dojo

基于 Unity 的手势控制第三人称射击项目。

## 打开项目

1. 安装 Unity **2022.3.62f3**，并安装 Git LFS。
2. 克隆仓库后，运行 `git lfs pull`，确保大型插件和字体资源已下载。
3. 在 Unity Hub 中打开仓库根目录，等待 Unity 导入资源。
4. 打开 `Assets/Scenes/HandDemo.unity`。这是当前 Build Settings 中启用的场景。

项目依赖记录在 `Packages/manifest.json` 和 `Packages/packages-lock.json`。`Assets/`、`Packages/`、`ProjectSettings/` 及 Unity 的 `.meta` 文件均应保留。

## 发布前检查

项目包含第三方 Unity 资源、字体和音乐。公开仓库前请核对这些素材的再分发许可。邮箱登录代码当前连接一个写在源码中的 HTTP 服务地址；公开代码或正式发布前，也请确认服务地址、访问权限和传输安全设置。
