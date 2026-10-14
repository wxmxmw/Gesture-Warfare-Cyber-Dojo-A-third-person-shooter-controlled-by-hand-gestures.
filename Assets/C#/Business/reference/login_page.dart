// ignore_for_file: slash_for_doc_comments, unintended_html_in_doc_comment

import 'package:flutter/material.dart'; // Flutter UI 核心库
import 'package:logging/logging.dart'; // 日志记录库
import '../widgets/language_dropdown.dart';
import '../widgets/protocol_dialog.dart';
import '../services/api_service.dart';
import '../utils/validators.dart';
import '../utils/lang_manager.dart';
import '../utils/asset_manager.dart';

final Logger _loginLogger = Logger('LoginPage');

/***************************************************************************
  类名称：LoginPage
  功    能：登录页面组件，提供用户登录界面和相关功能
  输入参数：Key? key - Widget键值，可选参数
  返 回 值：LoginPage实例
  说    明：包含背景图片、用户头像、语言选择、表单输入、协议确认、功能选择等完整登录流程
***************************************************************************/
class LoginPage extends StatefulWidget {
  const LoginPage({super.key});

  @override
  State<LoginPage> createState() => _LoginPageState();
}

/***************************************************************************
  类名称：_LoginPageState
  功    能：登录页面状态管理类，管理表单数据、语言设置、协议状态等
  输入参数：无
  返 回 值：_LoginPageState实例
  说    明：维护登录表单的所有状态变量，处理用户交互和页面更新逻辑
***************************************************************************/
class _LoginPageState extends State<LoginPage> {
  // 滚动控制器，防止setState后回到顶部
  final ScrollController _scrollController = ScrollController();
  bool _obscurePwd = true;
  // 当前选择的语言
  String selectedLanguage = LangManager.currentLang;
  // 是否记住密码
  bool rememberMe = false;
  // 是否自动登录
  bool autoLogin = false;
  // 是否同意协议
  bool agreeProtocol = false;
  // 邮箱输入控制器
  TextEditingController emailController = TextEditingController();
  // 用户名输入控制器（可选）
  TextEditingController usernameController = TextEditingController();
  // 密码输入控制器
  TextEditingController passwordController = TextEditingController();
  // 功能选择：voice 或 ocr
  String selectedFunc = 'voice';

  /***************************************************************************
  函数名称：_changeLanguage
  功    能：切换本地语言并更新界面显示
  输入参数：String lang - 目标语言代码（如'zh', 'en'等）
  返 回 值：Future<void>
  说    明：异步方法，保持滚动位置不变，更新语言设置后输出调试信息
***************************************************************************/
  Future<void> _changeLanguage(String lang) async {
    try {
      await LangManager.load(lang);
      final oldOffset = _scrollController.hasClients
          ? _scrollController.offset
          : 0.0;
      setState(() {
        selectedLanguage = lang;
      });
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (_scrollController.hasClients) {
          _scrollController.jumpTo(oldOffset);
        }
      });
      _loginLogger.info('Language changed to: $lang');
      // 文本调试信息
      _loginLogger.info('[Text] localLang: ${LangManager.get('localLang')}');
      _loginLogger.info('[Text] welcome: ${LangManager.get('welcome')}');
      _loginLogger.info('[Text] email: ${LangManager.get('email')}');
      // 图片资源调试信息
      final bgPath = await AssetManager.getAssetPath('pc_bg_login.jpg');
      final userPath = await AssetManager.getAssetPath('pc_user.png');
      _loginLogger.info('[Image] pc_bg_login.jpg 路径: $bgPath');
      _loginLogger.info('[Image] pc_user.png 路径: $userPath');
    } catch (e) {
      _loginLogger.severe('Failed to change language: $e');
    }
  }

  /***************************************************************************
  函数名称：_showProtocol
  功    能：根据类型显示对应的协议或隐私政策弹窗
  输入参数：type - 协议类型（"service"服务协议，"privacy"隐私政策）
  返 回 值：Future<void>
  说    明：使用当前选择的语言显示相应的协议内容对话框
***************************************************************************/
  void _showProtocol(String type) async {
    showDialog(
      context: context,
      builder: (context) =>
          ProtocolDialog(type: type, language: selectedLanguage),
    );
  }

  /***************************************************************************
  函数名称：_login
  功    能：执行用户登录逻辑，包括表单验证、API调用和页面跳转
  输入参数：无
  返 回 值：Future<void>
  说    明：验证协议同意状态、表单数据格式，调用登录API，根据结果跳转或显示错误提示
***************************************************************************/
  Future<void> _login() async {
    // 协议同意校验：仅在中文或英文本地语言时生效
    if ((selectedLanguage == 'zh' || selectedLanguage == 'en') &&
        !agreeProtocol) {
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
    // 校验邮箱、密码、用户名格式
    String? emailError = Validators.email(emailController.text);
    String? pwdError = Validators.password(passwordController.text);
    String? usernameError = Validators.username(usernameController.text);
    if (emailError != null || pwdError != null || usernameError != null) {
      await showDialog(
        context: context,
        builder: (context) => AlertDialog(
          content: Text(emailError ?? pwdError ?? usernameError ?? ''),
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
    try {
      // 调用登录接口
      var res = await ApiService.login(
        emailController.text,
        passwordController.text,
      );
      _loginLogger.info('登录接口返回: $res');
      // 判断登录是否成功
      if (res['message'] == 'Login successful') {
        _loginLogger.info('登录成功，跳转到对应页面');
        if (!mounted) return;
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              '${LangManager.get('login')}${LangManager.get('success')}',
            ), // "登录成功"
            duration: Duration(seconds: 2),
            behavior: SnackBarBehavior.floating,
          ),
        );
        await Future.delayed(Duration(seconds: 2));
        if (!mounted) return;
        if (selectedFunc == 'voice') {
          Navigator.pushReplacementNamed(context, '/home');
        } else {
          Navigator.pushReplacementNamed(context, '/ocr');
        }
      } else {
        _loginLogger.info(
          '登录失败，弹窗提示: ${res['message'] ?? LangManager.get('fail')}',
        );
        // 登录失败弹窗
        if (!mounted) return;
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
      // 网络异常弹窗
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

  @override
  /***************************************************************************
  函数名称：initState
  功    能：初始化登录页面状态
  输入参数：无
  返 回 值：void
  说    明：Widget生命周期方法，在组件创建时调用，进行必要的初始化设置
***************************************************************************/
  void initState() {
    super.initState();
  }

  @override
  /***************************************************************************
  函数名称：build
  功    能：构建登录页面的UI界面
  输入参数：BuildContext context - 构建上下文
  返 回 值：Widget - 完整的登录页面界面
  说    明：创建包含背景图、表单输入、按钮等的完整登录界面，支持多语言和主题适配
***************************************************************************/
  Widget build(BuildContext context) {
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
            // Debug按钮: 直接进入HomePage
            SizedBox(width: 12),
            ElevatedButton(
              onPressed: () {
                Navigator.pushNamed(context, '/home');
              },
              style: ElevatedButton.styleFrom(
                backgroundColor: Colors.red,
                minimumSize: Size(10, 36),
                padding: EdgeInsets.symmetric(horizontal: 10),
              ),
              child: Text('Debug: Go to Home'),
            ),
            // Debug按钮结束
          ],
        ),
        automaticallyImplyLeading: false,
      ),
      body: Stack(
        children: [
          // 背景图片容器
          FutureBuilder(
            future: AssetManager.getAssetPath('pc_bg_login.jpg'),
            builder: (context, snapshot) {
              if (snapshot.connectionState == ConnectionState.done &&
                  snapshot.hasData) {
                return Container(
                  width: double.infinity,
                  height: double.infinity,
                  decoration: BoxDecoration(
                    image: DecorationImage(
                      image: AssetImage(snapshot.data!),
                      fit: BoxFit.cover,
                    ),
                  ),
                  child: SingleChildScrollView(
                    controller: _scrollController,
                    child: Padding(
                      padding: EdgeInsets.all(20),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.center,
                        children: [
                          SizedBox(height: 5),
                          // 欢迎语
                          Text(
                            LangManager.get('welcome'),
                            style: TextStyle(
                              fontSize: 22,
                              color: Colors.black,
                              fontWeight: FontWeight.bold,
                            ),
                            textAlign: TextAlign.center,
                          ),
                          SizedBox(height: 18),
                          // 用户头像图片
                          FutureBuilder(
                            future: AssetManager.getAssetPath('pc_user.png'),
                            builder: (context, snapshot) {
                              if (snapshot.connectionState ==
                                      ConnectionState.done &&
                                  snapshot.hasData) {
                                return Image.asset(
                                  snapshot.data!,
                                  width: 221,
                                  height: 199,
                                );
                              }
                              return CircularProgressIndicator();
                            },
                          ),
                          SizedBox(height: 18),
                          // 本地语言选择
                          Row(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              Text(
                                LangManager.get('localLang'),
                                style: TextStyle(
                                  fontSize: 22,
                                  color: Colors.black,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                              SizedBox(width: 10),
                              // 语言选择下拉框
                              LanguageDropdown(
                                value: selectedLanguage,
                                onChanged: (lang) =>
                                    _changeLanguage(lang ?? 'zh'),
                                items: [
                                  DropdownMenuItem(
                                    value: 'zh',
                                    child: Text('中文'),
                                  ),
                                  DropdownMenuItem(
                                    value: 'en',
                                    child: Text('English'),
                                  ),
                                  DropdownMenuItem(
                                    value: 'ru',
                                    child: Text('Русский'),
                                  ),
                                  DropdownMenuItem(
                                    value: 'ja',
                                    child: Text('日本語'),
                                  ),
                                  DropdownMenuItem(
                                    value: 'fr',
                                    child: Text('Français'),
                                  ),
                                  DropdownMenuItem(
                                    value: 'ar',
                                    child: Text('العربية'),
                                  ),
                                  DropdownMenuItem(
                                    value: 'de',
                                    child: Text('Deutsch'),
                                  ),
                                  DropdownMenuItem(
                                    value: 'es',
                                    child: Text('Español'),
                                  ),
                                  DropdownMenuItem(
                                    value: 'fa',
                                    child: Text('فارسی'),
                                  ),
                                  DropdownMenuItem(
                                    value: 'in',
                                    child: Text('Bahasa Indonesia'),
                                  ),
                                  DropdownMenuItem(
                                    value: 'it',
                                    child: Text('Italiano'),
                                  ),
                                  DropdownMenuItem(
                                    value: 'ko',
                                    child: Text('한국어'),
                                  ),
                                  DropdownMenuItem(
                                    value: 'pt',
                                    child: Text('Português'),
                                  ),
                                  DropdownMenuItem(
                                    value: 'th',
                                    child: Text('ไทย'),
                                  ),
                                  DropdownMenuItem(
                                    value: 'tr',
                                    child: Text('Türkçe'),
                                  ),
                                  DropdownMenuItem(
                                    value: 'ur',
                                    child: Text('اردو'),
                                  ),
                                ],
                              ),
                            ],
                          ),
                          SizedBox(height: 18),
                          // 邮箱输入框
                          TextField(
                            controller: emailController,
                            decoration: InputDecoration(
                              hintText: LangManager.get('email'),
                              contentPadding: EdgeInsets.symmetric(
                                vertical: 15,
                              ),
                              border: UnderlineInputBorder(),
                            ),
                            style: TextStyle(fontSize: 20, color: Colors.black),
                            textAlign: TextAlign.center,
                          ),
                          SizedBox(height: 5),
                          // 用户名输入框
                          TextField(
                            controller: usernameController,
                            decoration: InputDecoration(
                              hintText: LangManager.get('username'),
                              contentPadding: EdgeInsets.symmetric(
                                vertical: 15,
                              ),
                              border: UnderlineInputBorder(),
                            ),
                            style: TextStyle(fontSize: 20, color: Colors.black),
                            textAlign: TextAlign.center,
                          ),
                          SizedBox(height: 5),
                          // 密码输入框（横线+可见切换，局部刷新）
                          StatefulBuilder(
                            builder: (context, setPwdState) => TextField(
                              controller: passwordController,
                              obscureText: _obscurePwd,
                              decoration: InputDecoration(
                                hintText: LangManager.get('password'),
                                contentPadding: EdgeInsets.symmetric(
                                  vertical: 15,
                                ),
                                border: UnderlineInputBorder(),
                                suffixIcon: IconButton(
                                  icon: Icon(
                                    _obscurePwd
                                        ? Icons.visibility_off
                                        : Icons.visibility,
                                  ),
                                  onPressed: () {
                                    setPwdState(() {
                                      _obscurePwd = !_obscurePwd;
                                    });
                                  },
                                ),
                              ),
                              style: TextStyle(
                                fontSize: 20,
                                color: Colors.black,
                              ),
                              textAlign: TextAlign.center,
                            ),
                          ),
                          SizedBox(height: 18),
                          // 记住密码、自动登录选项
                          Row(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              StatefulBuilder(
                                builder: (context, setLocalState) => Theme(
                                  data: Theme.of(context).copyWith(
                                    splashColor: Colors.transparent,
                                    highlightColor: Colors.transparent,
                                  ),
                                  child: Checkbox(
                                    value: rememberMe,
                                    onChanged: (v) {
                                      setLocalState(() => rememberMe = v!);
                                    },
                                    materialTapTargetSize:
                                        MaterialTapTargetSize.shrinkWrap,
                                  ),
                                ),
                              ),
                              Text(
                                LangManager.get('rememberPwd'),
                                style: TextStyle(
                                  fontSize: 15,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                              SizedBox(width: 10),
                              StatefulBuilder(
                                builder: (context, setLocalState) => Theme(
                                  data: Theme.of(context).copyWith(
                                    splashColor: Colors.transparent,
                                    highlightColor: Colors.transparent,
                                  ),
                                  child: Checkbox(
                                    value: autoLogin,
                                    onChanged: (v) {
                                      setLocalState(() => autoLogin = v!);
                                    },
                                    materialTapTargetSize:
                                        MaterialTapTargetSize.shrinkWrap,
                                  ),
                                ),
                              ),
                              Text(
                                LangManager.get('autoLogin'),
                                style: TextStyle(
                                  fontSize: 15,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                            ],
                          ),
                          // 协议勾选、服务协议、隐私政策（仅在本地语言为中文或英文时显示）
                          if (selectedLanguage == 'zh' ||
                              selectedLanguage == 'en')
                            Row(
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                StatefulBuilder(
                                  builder: (context, setLocalState) => Theme(
                                    data: Theme.of(context).copyWith(
                                      splashColor: Colors.transparent,
                                      highlightColor: Colors.transparent,
                                    ),
                                    child: Checkbox(
                                      value: agreeProtocol,
                                      onChanged: (v) {
                                        setLocalState(() => agreeProtocol = v!);
                                      },
                                      materialTapTargetSize:
                                          MaterialTapTargetSize.shrinkWrap,
                                    ),
                                  ),
                                ),
                                Text(
                                  LangManager.get('loginAgree'),
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
                                    overflow: TextOverflow.visible, // 确保文本不会溢出
                                    softWrap: true, // 启用自动换行
                                  ),
                                ),
                              ],
                            ),
                          SizedBox(height: 16),
                          // 登录按钮
                          ElevatedButton(
                            onPressed: _login,
                            style: ElevatedButton.styleFrom(
                              minimumSize: Size(120, 50),
                              backgroundColor: Color(0xFF7C4DFF),
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(25),
                              ),
                            ),
                            child: Text(
                              LangManager.get('login'),
                              style: TextStyle(
                                fontSize: 18,
                                fontWeight: FontWeight.bold,
                                color: Colors.white,
                              ),
                            ),
                          ),
                          SizedBox(height: 16),
                          // 注册跳转按钮
                          TextButton(
                            onPressed: () => Navigator.pushReplacementNamed(
                              context,
                              '/register',
                            ),
                            child: Text(
                              LangManager.get('registerTip'),
                              style: TextStyle(
                                color: Color(0xFF00E2FF),
                                fontSize: 15,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                          ),
                          SizedBox(height: 20),
                          // 功能选择（语音同传/文字识别）
                          StatefulBuilder(
                            builder: (context, setLocalState) => Row(
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                Radio<String>(
                                  value: 'voice',
                                  groupValue: selectedFunc,
                                  onChanged: (v) {
                                    setLocalState(() {
                                      selectedFunc = v!;
                                    });
                                  },
                                ),
                                Text(
                                  LangManager.get(
                                    'simultaneous_interpretation_func',
                                  ),
                                  style: TextStyle(
                                    fontSize: 15,
                                    color: Colors.black,
                                  ),
                                ),
                                SizedBox(width: 20),
                                Radio<String>(
                                  value: 'ocr',
                                  groupValue: selectedFunc,
                                  onChanged: (v) {
                                    setLocalState(() {
                                      selectedFunc = v!;
                                    });
                                  },
                                ),
                                Text(
                                  LangManager.get('ocr_func'),
                                  style: TextStyle(
                                    fontSize: 15,
                                    color: Colors.black,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                );
              }
              return Container(color: Colors.white); // 加载中显示空白背景
            },
          ),
        ],
      ),
    );
  }
}
