// ignore_for_file: slash_for_doc_comments, unintended_html_in_doc_comment

import 'dart:async'; // 异步支持（计时器）
import 'package:flutter/material.dart'; // Flutter UI 核心库
import 'package:flutter_svg/flutter_svg.dart'; // 加载 SVG 图标
import 'package:hive/hive.dart'; // 本地持久化（倒计时结束时间）
import '../widgets/protocol_dialog.dart';
import '../services/api_service.dart';
import '../utils/validators.dart';
import '../utils/lang_manager.dart';
import '../utils/asset_manager.dart';

/***************************************************************************
  函数名称：RegisterPage
  功    能：显示用户注册页面，包含邮箱、用户名、密码、验证码输入及协议同意
  输入参数：key-可选的Widget键
  返 回 值：StatefulWidget组件实例
  说    明：提供注册流程的UI入口，集成验证码发送、协议展示与注册提交
***************************************************************************/
class RegisterPage extends StatefulWidget {
  const RegisterPage({super.key});

  @override
  State<RegisterPage> createState() => _RegisterPageState();
}

/***************************************************************************
  函数名称：_RegisterPageState
  功    能：管理注册页面的状态和业务逻辑，包括输入控制、倒计时、协议校验与注册提交
  输入参数：继承自State<RegisterPage>
  返 回 值：State实例
  说    明：负责处理UI事件、与ApiService交互及倒计时持久化逻辑
***************************************************************************/
class _RegisterPageState extends State<RegisterPage> {
  // ================== 可视状态/控制器区域 ==================
  bool _obscurePwd = true; // 密码输入框文字是否隐藏
  bool _obscureConfirm = true; // 确认密码输入框文字是否隐藏
  String selectedLanguage = LangManager.currentLang; // 当前语言（用于协议内容）
  bool agreeProtocol = false; // 用户是否勾选同意协议

  // 输入控制器（需在 dispose 中释放）
  final TextEditingController emailController = TextEditingController();
  final TextEditingController usernameController = TextEditingController();
  final TextEditingController passwordController = TextEditingController();
  final TextEditingController confirmController = TextEditingController();
  final TextEditingController codeController = TextEditingController();

  // 发送验证码按钮的工作状态
  bool isSendingCode = false; // 是否正在向后端请求发送验证码
  int _countdown = 0; // 剩余倒计时秒数；>0 表示倒计时进行中，按钮禁用
  Timer? _countdownTimer; // 周期计时器，用于每秒刷新倒计时

  // Hive Box：保存一次发送后的“验证码失效时间”(结束时间戳)，支持页面重建恢复倒计时
  Box? _registerStateBox; // 可能因 Hive 初始化失败为 null，代码已降级处理

  @override
  /***************************************************************************
  函数名称：initState
  功    能：初始化倒计时持久化并设置初始页面状态
  输入参数：无
  返 回 值：void
  说    明：在页面创建时尝试恢复上次验证码倒计时状态
***************************************************************************/
  void initState() {
    super.initState();
    _initCountdownPersistence();
  }

  /***************************************************************************
  函数名称：_initCountdownPersistence
  功    能：打开本地持久化存储并恢复验证码倒计时状态（如存在）
  输入参数：无
  返 回 值：Future<void>
  说    明：使用 Hive 存储倒计时结束时间，实现页面热重载/重建时倒计时延续
***************************************************************************/
  Future<void> _initCountdownPersistence() async {
    // 打开本地 box 并尝试读取之前存储的倒计时结束时间（毫秒时间戳）
    // 若用户中途热重载/重建页面，且倒计时尚未结束，可继续显示剩余时间
    try {
      _registerStateBox = await Hive.openBox('register_state');
      final endMs = _registerStateBox!.get('codeCountdownEnd');
      if (endMs is int) {
        final now = DateTime.now().millisecondsSinceEpoch;
        final remainingMs = endMs - now;
        if (remainingMs > 1000) {
          _countdown = (remainingMs / 1000).ceil();
          // 恢复计时，使用页面 setState
          _countdownTimer = Timer.periodic(const Duration(seconds: 1), (t) {
            if (!mounted) {
              t.cancel();
              return;
            }
            if (_countdown <= 1) {
              t.cancel();
              _countdown = 0;
              // 倒计时自然结束，清理持久化
              _registerStateBox?.delete('codeCountdownEnd');
            } else {
              _countdown--;
            }
            setState(() {});
          });
        } else {
          // 已过期，清除
          _registerStateBox!.delete('codeCountdownEnd');
        }
      }
    } catch (_) {
      // 忽略 Hive 打开失败，倒计时持久化降级为非持久
    }
  }

  /***************************************************************************
  函数名称：_isVerifySuccess
  功    能：判断验证码发送接口返回值是否表示成功
  输入参数：res-后端返回的Map结构结果
  返 回 值：bool-是否判定为成功
  说    明：兼容多种后端返回格式（布尔、code=0或message文本包含特定关键字）
***************************************************************************/
  bool _isVerifySuccess(Map res) {
    // 统一判断“发送验证码”接口是否成功
    try {
      final dynamic successFlag = res['success'];
      if (successFlag == true || successFlag == 'true') return true;
      if (res['code'] == 0) return true;
      final msg = res['message']?.toString().toLowerCase();
      if (msg != null) {
        // 兼容不同返回内容：'Send successful', 'success', 'send success', etc.
        if (msg.contains('Send successful') && msg.length <= 30) {
          return true;
        }
      }
    } catch (_) {}
    return false;
  }

  /***************************************************************************
  函数名称：_startCountdown
  功    能：启动验证码发送后的本地倒计时并将结束时间持久化
  输入参数：refreshButton-StateSetter用于局部刷新按钮状态
  返 回 值：void
  说    明：启动60秒周期计时，倒计时结束时清理Hive中的结束时间
***************************************************************************/
  void _startCountdown(StateSetter refreshButton) {
    // 启动 60 秒倒计时：
    // 1. 取消旧计时器（防重入）
    // 2. 写入结束时间到 Hive（如果可用）
    // 3. 每秒刷新本地剩余秒数；归零时移除 Hive 键
    _countdownTimer?.cancel();
    _countdown = 60; // 60 秒倒计时
    refreshButton(() {}); // 触发按钮重建显示 60s
    // 持久化结束时间
    final end = DateTime.now()
        .add(const Duration(seconds: 60))
        .millisecondsSinceEpoch;
    try {
      _registerStateBox?.put('codeCountdownEnd', end);
    } catch (_) {}
    _countdownTimer = Timer.periodic(const Duration(seconds: 1), (t) {
      if (!mounted) {
        t.cancel();
        return;
      }
      if (_countdown <= 1) {
        t.cancel();
        _countdown = 0;
        // 倒计时结束移除持久化
        try {
          _registerStateBox?.delete('codeCountdownEnd');
        } catch (_) {}
      } else {
        _countdown--;
      }
      refreshButton(() {});
    });
  }

  /***************************************************************************
  函数名称：_register
  功    能：验证输入并调用注册接口完成用户注册流程
  输入参数：无（使用页面表单控制器内的值）
  返 回 值：Future<void>
  说    明：包含协议勾选校验、字段校验、密码一致性检查，调用 `ApiService.register` 并根据结果提示与跳转
***************************************************************************/
  Future<void> _register() async {
    // 注册逻辑主流程：
    // 1. 协议勾选校验（双保险，与 UI gating 保持一致）
    // 2. 基础字段校验（邮箱/密码/用户名/验证码）
    // 3. 再次协议确认（可根据需要去重，这里保留容错）
    // 4. 确认密码一致性
    // 5. 调用后端注册 API，并根据返回弹窗提示
    // 协议同意校验：仅在中文或英文本地语言时生效
    if ((selectedLanguage == 'zh' || selectedLanguage == 'en') &&
        !agreeProtocol) {
      if (!mounted) return;
      await showDialog(
        context: context,
        builder: (context) => AlertDialog(
          content: Text(LangManager.get('pleaseAgree')),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: Text('OK'),
            ),
          ],
        ),
      );
      return;
    }
    // 表单校验
    String? emailError = Validators.email(emailController.text);
    String? pwdError = Validators.password(passwordController.text);
    String? usernameError = Validators.username(usernameController.text);
    String? codeError = Validators.code(codeController.text);
    if (emailError != null ||
        pwdError != null ||
        usernameError != null ||
        codeError != null) {
      if (!mounted) return;
      await showDialog(
        context: context,
        builder: (context) => AlertDialog(
          content: Text(
            emailError ?? pwdError ?? usernameError ?? codeError ?? '',
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: Text('OK'),
            ),
          ],
        ),
      );
      return;
    }
    // 双重协议校验：仅在中文或英文时生效
    if ((selectedLanguage == 'zh' || selectedLanguage == 'en') &&
        !agreeProtocol) {
      if (!mounted) return;
      await showDialog(
        context: context,
        builder: (context) => AlertDialog(
          content: Text(LangManager.get('pleaseAgree')),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: Text('OK'),
            ),
          ],
        ),
      );
      return;
    }
    // 密码确认校验
    if (passwordController.text != confirmController.text) {
      if (!mounted) return;
      await showDialog(
        context: context,
        builder: (context) => AlertDialog(
          content: Text(LangManager.get('passwordNotMatch')),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: Text('OK'),
            ),
          ],
        ),
      );
      return;
    }
    // 调用注册接口
    try {
      var res = await ApiService.register(
        emailController.text,
        passwordController.text,
        codeController.text,
      );
      if (!mounted) return;
      if (res['success'] == true) {
        await showDialog(
          context: context,
          builder: (context) => AlertDialog(
            content: Text(LangManager.get('success')),
            actions: [
              TextButton(
                onPressed: () {
                  Navigator.pop(context); // 关闭弹窗
                  Navigator.pushReplacementNamed(context, '/login'); // 跳转到登录页面
                },
                child: Text('OK'),
              ),
            ],
          ),
        );
      } else {
        await showDialog(
          context: context,
          builder: (context) => AlertDialog(
            content: Text(res['message'] ?? LangManager.get('fail')),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(context),
                child: Text('OK'),
              ),
            ],
          ),
        );
      }
    } catch (e) {
      if (!mounted) return;
      await showDialog(
        context: context,
        builder: (context) => AlertDialog(
          content: Text(LangManager.get('networkError')),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: Text('OK'),
            ),
          ],
        ),
      );
    }
  }

  /***************************************************************************
  函数名称：_showProtocol
  功    能：显示服务协议或隐私政策弹窗对话框
  输入参数：type - 协议类型（"service"服务协议，"privacy"隐私政策）
  返 回 值：Future<void>
  说    明：使用当前选择的语言显示相应的协议内容弹窗
***************************************************************************/
  void _showProtocol(String type) async {
    // 打开服务协议/隐私政策弹窗
    showDialog(
      context: context,
      builder: (context) =>
          ProtocolDialog(type: type, language: selectedLanguage),
    );
  }

  @override
  /***************************************************************************
  函数名称：dispose
  功    能：释放页面资源，取消计时器与释放输入控制器
  输入参数：无
  返 回 值：void
  说    明：清理Timer与TextEditingController，Hive Box由全局管理无需在此关闭
***************************************************************************/
  void dispose() {
    // 资源释放：
    // - 计时器取消
    // - 所有 TextEditingController 释放
    // - Hive Box 不主动关闭（全局生命周期由 Hive 管理）
    _countdownTimer?.cancel();
    emailController.dispose();
    usernameController.dispose();
    passwordController.dispose();
    confirmController.dispose();
    codeController.dispose();
    // 不主动关闭 box，交给 Hive 统一管理；只清理过期键
    _registerStateBox = null;
    super.dispose();
  }

  @override
  /***************************************************************************
  函数名称：build
  功    能：构建注册页面的完整UI布局
  输入参数：context-构建上下文BuildContext
  返 回 值：Widget-页面的根Widget
  说    明：包含背景图、输入表单、验证码发送、协议勾选与注册按钮
***************************************************************************/
  Widget build(BuildContext context) {
    // 页面整体采用背景图 + 可滚区域；所有输入逻辑集中于列结构中
    return Scaffold(
      appBar: AppBar(
        backgroundColor: Color(0xFF7C4DFF),
        elevation: 0,
        title: Row(
          children: [
            Flexible(
              child: Text(
                'Yuefu Interpretation',
                style: TextStyle(
                  color: Colors.white,
                  fontWeight: FontWeight.bold,
                  fontSize: 22,
                ),
                overflow: TextOverflow.ellipsis,
              ),
            ),
          ],
        ),
        automaticallyImplyLeading: false,
      ),
      body: Column(
        children: [
          Expanded(
            child: SizedBox.expand(
              child: FutureBuilder(
                future: AssetManager.getAssetPath('pc_bg_signup.jpg'),
                builder: (context, snapshot) {
                  if (snapshot.connectionState == ConnectionState.done &&
                      snapshot.hasData) {
                    return Container(
                      decoration: BoxDecoration(
                        image: DecorationImage(
                          image: AssetImage(snapshot.data!),
                          fit: BoxFit.cover,
                        ),
                      ),
                      child: SingleChildScrollView(
                        child: Padding(
                          padding: EdgeInsets.all(40),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.center,
                            children: [
                              // 顶部返回按钮和“已有账号”
                              Row(
                                children: [
                                  FutureBuilder(
                                    future: AssetManager.getAssetPath(
                                      'pc_go_back.png',
                                    ),
                                    builder: (context, snapshot) {
                                      if (snapshot.connectionState ==
                                              ConnectionState.done &&
                                          snapshot.hasData) {
                                        return IconButton(
                                          icon: Image.asset(
                                            snapshot.data!,
                                            width: 30,
                                            height: 30,
                                          ),
                                          onPressed: () =>
                                              Navigator.pushReplacementNamed(
                                                context,
                                                '/login',
                                              ),
                                        );
                                      }
                                      return CircularProgressIndicator();
                                    },
                                  ),
                                  SizedBox(width: 4),
                                  Text(
                                    LangManager.get('haveAccount'),
                                    style: TextStyle(
                                      fontSize: 20,
                                      fontWeight: FontWeight.bold,
                                    ),
                                  ),
                                ],
                              ),
                              SizedBox(height: 30),
                              // 用户头像
                              FutureBuilder(
                                future: AssetManager.getAssetPath(
                                  'pc_user.png',
                                ),
                                builder: (context, snapshot) {
                                  if (snapshot.connectionState ==
                                          ConnectionState.done &&
                                      snapshot.hasData) {
                                    return Image.asset(
                                      snapshot.data!,
                                      width: 202,
                                      height: 96,
                                    );
                                  }
                                  return CircularProgressIndicator();
                                },
                              ),
                              SizedBox(height: 10),
                              // 新用户标题
                              Text(
                                LangManager.get('new_user'),
                                style: TextStyle(
                                  fontSize: 40,
                                  color: Colors.black,
                                  fontWeight: FontWeight.bold,
                                ),
                                textAlign: TextAlign.center,
                              ),
                              SizedBox(height: 20),
                              // 邮箱输入带图标
                              Row(
                                crossAxisAlignment: CrossAxisAlignment.center,
                                children: [
                                  FutureBuilder(
                                    future: AssetManager.getAssetPath(
                                      'mail.svg',
                                    ),
                                    builder: (context, snapshot) {
                                      if (snapshot.connectionState ==
                                              ConnectionState.done &&
                                          snapshot.hasData) {
                                        return SvgPicture.asset(
                                          snapshot.data!,
                                          width: 45,
                                        );
                                      }
                                      return CircularProgressIndicator();
                                    },
                                  ),
                                  SizedBox(width: 10),
                                  Expanded(
                                    child: TextField(
                                      controller: emailController,
                                      decoration: InputDecoration(
                                        hintText: LangManager.get(
                                          'enter_your_email',
                                        ),
                                        border: UnderlineInputBorder(),
                                      ),
                                      keyboardType: TextInputType.emailAddress,
                                      style: TextStyle(fontSize: 20),
                                    ),
                                  ),
                                ],
                              ),
                              SizedBox(height: 10),
                              // 用户名输入带图标
                              Row(
                                crossAxisAlignment: CrossAxisAlignment.center,
                                children: [
                                  FutureBuilder(
                                    future: AssetManager.getAssetPath(
                                      'account_circle.svg',
                                    ),
                                    builder: (context, snapshot) {
                                      if (snapshot.connectionState ==
                                              ConnectionState.done &&
                                          snapshot.hasData) {
                                        return SvgPicture.asset(
                                          snapshot.data!,
                                          width: 45,
                                        );
                                      }
                                      return CircularProgressIndicator();
                                    },
                                  ),
                                  SizedBox(width: 10),
                                  Expanded(
                                    child: TextField(
                                      controller: usernameController,
                                      decoration: InputDecoration(
                                        hintText: LangManager.get(
                                          'enter_username',
                                        ),
                                        border: UnderlineInputBorder(),
                                      ),
                                      style: TextStyle(fontSize: 20),
                                    ),
                                  ),
                                ],
                              ),
                              SizedBox(height: 10),
                              // 密码输入带图标+可见按钮
                              Row(
                                crossAxisAlignment: CrossAxisAlignment.center,
                                children: [
                                  FutureBuilder(
                                    future: AssetManager.getAssetPath(
                                      'key.svg',
                                    ),
                                    builder: (context, snapshot) {
                                      if (snapshot.connectionState ==
                                              ConnectionState.done &&
                                          snapshot.hasData) {
                                        return SvgPicture.asset(
                                          snapshot.data!,
                                          width: 45,
                                        );
                                      }
                                      return CircularProgressIndicator();
                                    },
                                  ),
                                  SizedBox(width: 10),
                                  Expanded(
                                    child: StatefulBuilder(
                                      builder: (context, setPwdState) =>
                                          TextField(
                                            controller: passwordController,
                                            decoration: InputDecoration(
                                              hintText: LangManager.get(
                                                'enter_password',
                                              ),
                                              border: UnderlineInputBorder(),
                                              suffixIcon: IconButton(
                                                icon: Icon(
                                                  _obscurePwd
                                                      ? Icons.visibility_off
                                                      : Icons.visibility,
                                                ),
                                                onPressed: () {
                                                  setPwdState(
                                                    () => _obscurePwd =
                                                        !_obscurePwd,
                                                  );
                                                },
                                              ),
                                            ),
                                            obscureText: _obscurePwd,
                                            style: TextStyle(fontSize: 20),
                                          ),
                                    ),
                                  ),
                                ],
                              ),
                              SizedBox(height: 10),
                              // 重复密码输入带图标+可见按钮
                              Row(
                                crossAxisAlignment: CrossAxisAlignment.center,
                                children: [
                                  FutureBuilder(
                                    future: AssetManager.getAssetPath(
                                      'lock.svg',
                                    ),
                                    builder: (context, snapshot) {
                                      if (snapshot.connectionState ==
                                              ConnectionState.done &&
                                          snapshot.hasData) {
                                        return SvgPicture.asset(
                                          snapshot.data!,
                                          width: 45,
                                        );
                                      }
                                      return CircularProgressIndicator();
                                    },
                                  ),
                                  SizedBox(width: 10),
                                  Expanded(
                                    child: StatefulBuilder(
                                      builder: (context, setConfirmState) =>
                                          TextField(
                                            controller: confirmController,
                                            decoration: InputDecoration(
                                              hintText: LangManager.get(
                                                'repeat_password',
                                              ),
                                              border: UnderlineInputBorder(),
                                              suffixIcon: IconButton(
                                                icon: Icon(
                                                  _obscureConfirm
                                                      ? Icons.visibility_off
                                                      : Icons.visibility,
                                                ),
                                                onPressed: () {
                                                  setConfirmState(
                                                    () => _obscureConfirm =
                                                        !_obscureConfirm,
                                                  );
                                                },
                                              ),
                                            ),
                                            obscureText: _obscureConfirm,
                                            style: TextStyle(fontSize: 20),
                                          ),
                                    ),
                                  ),
                                ],
                              ),
                              SizedBox(height: 10),
                              // 验证码输入带图标和发送按钮（局部刷新按钮）
                              Row(
                                crossAxisAlignment: CrossAxisAlignment.center,
                                children: [
                                  FutureBuilder(
                                    future: AssetManager.getAssetPath(
                                      'mark_email_read.svg',
                                    ),
                                    builder: (context, snapshot) {
                                      if (snapshot.connectionState ==
                                              ConnectionState.done &&
                                          snapshot.hasData) {
                                        return SvgPicture.asset(
                                          snapshot.data!,
                                          width: 45,
                                        );
                                      }
                                      return CircularProgressIndicator();
                                    },
                                  ),
                                  SizedBox(width: 10),
                                  Expanded(
                                    child: TextField(
                                      controller: codeController,
                                      decoration: InputDecoration(
                                        hintText: LangManager.get(
                                          'verification_code',
                                        ),
                                        border: UnderlineInputBorder(),
                                      ),
                                      style: TextStyle(fontSize: 20),
                                    ),
                                  ),
                                  SizedBox(width: 8),
                                  StatefulBuilder(
                                    builder: (context, setBtnState) {
                                      return ElevatedButton(
                                        onPressed:
                                            (isSendingCode || _countdown > 0)
                                            ? null
                                            : () async {
                                                // 协议勾选校验（仅在中文或英文时生效）
                                                if ((selectedLanguage == 'zh' ||
                                                        selectedLanguage ==
                                                            'en') &&
                                                    !agreeProtocol) {
                                                  if (!mounted) return;
                                                  await showDialog(
                                                    context: context,
                                                    builder: (context) =>
                                                        AlertDialog(
                                                          content: Text(
                                                            LangManager.get(
                                                              'pleaseAgree',
                                                            ),
                                                          ),
                                                          actions: [
                                                            TextButton(
                                                              onPressed: () =>
                                                                  Navigator.pop(
                                                                    context,
                                                                  ),
                                                              child: const Text(
                                                                'OK',
                                                              ),
                                                            ),
                                                          ],
                                                        ),
                                                  );
                                                  return;
                                                }
                                                // 邮箱校验
                                                String? emailError =
                                                    Validators.email(
                                                      emailController.text,
                                                    );
                                                if (emailController
                                                        .text
                                                        .isEmpty ||
                                                    emailError != null) {
                                                  if (!mounted) return;
                                                  await showDialog(
                                                    context: context,
                                                    builder: (context) =>
                                                        AlertDialog(
                                                          content: Text(
                                                            emailError ??
                                                                LangManager.get(
                                                                  'email',
                                                                ),
                                                          ),
                                                          actions: [
                                                            TextButton(
                                                              onPressed: () =>
                                                                  Navigator.pop(
                                                                    context,
                                                                  ),
                                                              child: Text('OK'),
                                                            ),
                                                          ],
                                                        ),
                                                  );
                                                  return;
                                                }
                                                setBtnState(() {
                                                  isSendingCode = true;
                                                });
                                                try {
                                                  var res =
                                                      await ApiService.verify(
                                                        emailController.text,
                                                        LangManager.currentLang,
                                                      );
                                                  if (!mounted) return;
                                                  setBtnState(() {
                                                    isSendingCode = false;
                                                  });
                                                  final bool success =
                                                      _isVerifySuccess(
                                                        res as Map,
                                                      );
                                                  if (success &&
                                                      _countdown == 0) {
                                                    // 成功才启动倒计时（重复点击已被禁用，这里双重保护）
                                                    _startCountdown(
                                                      setBtnState,
                                                    );
                                                    // 保险：父级 rebuild，避免仅局部刷新某些情况下未更新
                                                    if (mounted) {
                                                      setState(() {});
                                                    }
                                                  }
                                                  if (!mounted) return;
                                                  await showDialog(
                                                    context: this.context,
                                                    builder: (context) => AlertDialog(
                                                      content: Text(
                                                        success
                                                            ? LangManager.get(
                                                                'codeSent',
                                                              )
                                                            : (res['message'] ??
                                                                  LangManager.get(
                                                                    'networkError',
                                                                  )),
                                                      ),
                                                      actions: [
                                                        TextButton(
                                                          onPressed: () =>
                                                              Navigator.pop(
                                                                this.context,
                                                              ),
                                                          child: Text('OK'),
                                                        ),
                                                      ],
                                                    ),
                                                  );
                                                } catch (e) {
                                                  if (!mounted) return;
                                                  setBtnState(() {
                                                    isSendingCode = false;
                                                  });
                                                  if (!mounted) return;
                                                  await showDialog(
                                                    context: this.context,
                                                    builder: (context) =>
                                                        AlertDialog(
                                                          content: Text(
                                                            LangManager.get(
                                                              'networkError',
                                                            ),
                                                          ),
                                                          actions: [
                                                            TextButton(
                                                              onPressed: () =>
                                                                  Navigator.pop(
                                                                    this.context,
                                                                  ),
                                                              child: Text('OK'),
                                                            ),
                                                          ],
                                                        ),
                                                  );
                                                }
                                              },
                                        style: ElevatedButton.styleFrom(
                                          backgroundColor: Color(0xFF7C4DFF),
                                          minimumSize: Size(80, 40),
                                          shape: RoundedRectangleBorder(
                                            borderRadius: BorderRadius.circular(
                                              20,
                                            ),
                                          ),
                                        ),
                                        child: Text(
                                          isSendingCode
                                              ? LangManager.get('sending')
                                              : (_countdown > 0
                                                    ? '${_countdown}s'
                                                    : LangManager.get(
                                                        'getCode',
                                                      )),
                                          style: TextStyle(
                                            fontSize: 14,
                                            color: Colors.white,
                                          ),
                                        ),
                                      );
                                    },
                                  ),
                                ],
                              ),
                              SizedBox(height: 10),
                              // 协议勾选、服务协议、隐私政策（仅在本地语言为中文或英文时显示）
                              if (selectedLanguage == 'zh' ||
                                  selectedLanguage == 'en')
                                Row(
                                  mainAxisAlignment: MainAxisAlignment.center,
                                  children: [
                                    StatefulBuilder(
                                      builder: (context, setLocalState) =>
                                          Checkbox(
                                            value: agreeProtocol,
                                            onChanged: (v) => setLocalState(
                                              () => agreeProtocol = v!,
                                            ),
                                          ),
                                    ),
                                    Text(
                                      LangManager.get('agree'),
                                      style: TextStyle(
                                        fontSize: 15,
                                        fontWeight: FontWeight.bold,
                                      ),
                                    ),
                                    TextButton(
                                      onPressed: () => _showProtocol('service'),
                                      child: Text(
                                        LangManager.get('service'),
                                        style: TextStyle(
                                          fontStyle: FontStyle.italic,
                                          color: Colors.black,
                                        ),
                                      ),
                                    ),
                                    TextButton(
                                      onPressed: () => _showProtocol('privacy'),
                                      child: Text(
                                        LangManager.get('privacy'),
                                        style: TextStyle(
                                          fontStyle: FontStyle.italic,
                                          color: Colors.black,
                                        ),
                                        overflow:
                                            TextOverflow.visible, // 确保文本不会溢出
                                        softWrap: true, // 启用自动换行
                                      ),
                                    ),
                                  ],
                                ),
                              SizedBox(height: 18),
                              // 注册按钮
                              ElevatedButton(
                                onPressed: _register,
                                style: ElevatedButton.styleFrom(
                                  backgroundColor: Color(0xFF7C4DFF),
                                  minimumSize: Size(90, 50),
                                  shape: RoundedRectangleBorder(
                                    borderRadius: BorderRadius.circular(25),
                                  ),
                                ),
                                child: Text(
                                  LangManager.get('register'),
                                  style: TextStyle(
                                    fontSize: 18,
                                    fontWeight: FontWeight.bold,
                                    color: Colors.white,
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                    );
                  }
                  return Container(color: Colors.white);
                },
              ),
            ),
          ),
        ],
      ),
    );
  }
}
