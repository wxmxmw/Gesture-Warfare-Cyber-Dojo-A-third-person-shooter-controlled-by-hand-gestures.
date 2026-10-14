Shader "Custom/AlphaGradientShader"
{
    Properties
    {
        // 主贴图
        _MainTex ("主贴图 (RGBA)", 2D) = "white" {}
        
        [Header(Alpha Gradient Settings)]
        // 渐变方向设置
        _GradientDirection ("渐变方向", Range(0, 3)) = 0  // 0=从上到下, 1=从下到上, 2=从左到右, 3=从右到左
        
        [Header(Gradient Control)]
        // 渐变控制参数
        _GradientStart ("渐变起始位置", Range(0, 1)) = 0.0    // 渐变开始的位置
        _GradientEnd ("渐变结束位置", Range(0, 1)) = 1.0      // 渐变结束的位置
        _GradientSoftness ("渐变柔化", Range(0.001, 0.5)) = 0.1  // 渐变边缘的柔化程度
        
        [Header(Alpha Control)]
        // 透明度控制
        _StartAlpha ("起始透明度", Range(0, 1)) = 1.0         // 渐变起始位置的透明度
        _EndAlpha ("结束透明度", Range(0, 1)) = 0.0           // 渐变结束位置的透明度
        _BaseAlpha ("基础透明度", Range(0, 1)) = 1.0          // 整体基础透明度
        
        [Header(Advanced Settings)]
        // 高级设置
        _InvertGradient ("反转渐变", Range(0, 1)) = 0         // 是否反转渐变方向
        _UseCustomMask ("使用自定义遮罩", Range(0, 1)) = 0    // 是否使用自定义遮罩
        _CustomMask ("自定义遮罩", 2D) = "white" {}           // 自定义遮罩贴图
        
        [Header(Animation)]
        // 动画效果
        [Toggle] _EnableAnimation ("启用动画", Float) = 0
        _AnimationSpeed ("动画速度", Range(0, 5)) = 1
        _AnimationType ("动画类型", Range(0, 2)) = 0          // 0=渐变位置移动, 1=透明度脉冲, 2=渐变方向旋转
        
        [Header(Color Tint)]
        // 颜色调节
        _ColorTint ("颜色调节", Color) = (1, 1, 1, 1)        // 整体颜色调节
        _EmissionColor ("发光颜色", Color) = (0, 0, 0, 1)     // 发光颜色
        _EmissionIntensity ("发光强度", Range(0, 5)) = 0      // 发光强度
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
        }
        
        LOD 100
        
        // 开启透明混合
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma shader_feature _ENABLEANIMATION_ON
            #pragma shader_feature _USECUSTOMMASK_ON
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 worldUV : TEXCOORD1;  // 用于世界坐标UV
                UNITY_FOG_COORDS(2)
            };
            
            // 属性声明
            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            // 渐变设置
            float _GradientDirection;
            float _GradientStart;
            float _GradientEnd;
            float _GradientSoftness;
            
            // 透明度控制
            float _StartAlpha;
            float _EndAlpha;
            float _BaseAlpha;
            
            // 高级设置
            float _InvertGradient;
            float _UseCustomMask;
            sampler2D _CustomMask;
            
            // 动画设置
            float _EnableAnimation;
            float _AnimationSpeed;
            float _AnimationType;
            
            // 颜色调节
            fixed4 _ColorTint;
            fixed4 _EmissionColor;
            float _EmissionIntensity;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                o.worldUV = v.uv;  // 保存原始UV用于世界坐标计算
                
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }
            
            // 计算渐变因子 - 这是核心函数
            float CalculateGradientFactor(float2 uv, float direction, float start, float end, float softness)
            {
                float factor = 0.0;
                
                // 根据方向计算渐变因子
                if (direction < 0.5)
                {
                    // 从上到下 (0)
                    factor = uv.y;
                }
                else if (direction < 1.5)
                {
                    // 从下到上 (1)
                    factor = 1.0 - uv.y;
                }
                else if (direction < 2.5)
                {
                    // 从左到右 (2)
                    factor = uv.x;
                }
                else
                {
                    // 从右到左 (3)
                    factor = 1.0 - uv.x;
                }
                
                // 应用渐变范围
                factor = (factor - start) / (end - start);
                factor = saturate(factor);
                
                // 应用柔化
                if (softness > 0.001)
                {
                    factor = smoothstep(0.0, softness, factor) * smoothstep(1.0, 1.0 - softness, factor);
                }
                
                return factor;
            }
            
            // 动画效果
            float GetAnimationEffect(float2 uv, float time, float speed, float type)
            {
                float animTime = time * speed;
                
                if (type < 0.5)
                {
                    // 渐变位置移动动画
                    return sin(animTime) * 0.3 + 0.5;  // 在0.2到0.8之间移动
                }
                else if (type < 1.5)
                {
                    // 透明度脉冲动画
                    return sin(animTime * 2.0) * 0.5 + 0.5;
                }
                else
                {
                    // 渐变方向旋转动画
                    return fmod(animTime * 0.5, 4.0);  // 在0-4之间循环
                }
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // 采样主贴图
                fixed4 mainTex = tex2D(_MainTex, i.uv);
                
                // 计算动画效果
                float animEffect = 0.0;
                float currentGradientStart = _GradientStart;
                float currentGradientEnd = _GradientEnd;
                float currentGradientDirection = _GradientDirection;
                
                #ifdef _ENABLEANIMATION_ON
                if (_EnableAnimation > 0.5)
                {
                    animEffect = GetAnimationEffect(i.uv, _Time.y, _AnimationSpeed, _AnimationType);
                    
                    if (_AnimationType < 0.5)
                    {
                        // 渐变位置移动
                        currentGradientStart = lerp(0.2, 0.8, animEffect);
                        currentGradientEnd = currentGradientStart + 0.3;
                    }
                    else if (_AnimationType < 1.5)
                    {
                        // 透明度脉冲 - 在片段着色器中处理
                    }
                    else
                    {
                        // 渐变方向旋转
                        currentGradientDirection = animEffect;
                    }
                }
                #endif
                
                // 计算渐变因子
                float gradientFactor = CalculateGradientFactor(
                    i.uv, 
                    currentGradientDirection, 
                    currentGradientStart, 
                    currentGradientEnd, 
                    _GradientSoftness
                );
                
                // 应用反转
                if (_InvertGradient > 0.5)
                {
                    gradientFactor = 1.0 - gradientFactor;
                }
                
                // 计算最终透明度
                float finalAlpha = lerp(_StartAlpha, _EndAlpha, gradientFactor);
                
                // 应用动画透明度脉冲
                #ifdef _ENABLEANIMATION_ON
                if (_EnableAnimation > 0.5 && _AnimationType >= 0.5 && _AnimationType < 1.5)
                {
                    finalAlpha *= animEffect;
                }
                #endif
                
                // 应用基础透明度
                finalAlpha *= _BaseAlpha;
                
                // 应用自定义遮罩
                #ifdef _USECUSTOMMASK_ON
                if (_UseCustomMask > 0.5)
                {
                    fixed4 mask = tex2D(_CustomMask, i.uv);
                    finalAlpha *= mask.a;
                }
                #endif
                
                // 组合最终颜色
                fixed4 finalColor = mainTex * _ColorTint * i.color;
                
                // 应用发光效果
                if (_EmissionIntensity > 0.001)
                {
                    finalColor.rgb += _EmissionColor.rgb * _EmissionIntensity * finalAlpha;
                }
                
                // 应用计算出的透明度
                finalColor.a = finalAlpha;
                
                // 应用雾效
                UNITY_APPLY_FOG(i.fogCoord, finalColor);
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    // 后备着色器
    FallBack "Sprites/Default"
}
