Shader "Custom/BorderGradientShader"
{
    Properties
    {
        // 主贴图
        _MainTex ("主贴图 (RGBA)", 2D) = "white" {}
        
        [Header(Border Settings)]
        // 边框基础设置
        _BorderWidth ("边框宽度", Range(0.001, 0.5)) = 0.05
        _BorderSoftness ("边框柔化", Range(0.001, 0.1)) = 0.01
        
        [Header(Gradient Colors)]
        // 渐变颜色设置
        _Color1 ("边缘颜色", Color) = (1, 1, 1, 1)  // 白色 - 最外层
        _Color2 ("中间颜色", Color) = (0, 0, 1, 1)  // 蓝色 - 中间层
        _Color3 ("内部颜色", Color) = (0, 0, 0, 1)  // 黑色 - 最内层
        
        [Header(Gradient Control)]
        // 渐变控制
        _GradientIntensity ("渐变强度", Range(0, 2)) = 1
        _GradientBlend ("渐变混合", Range(0, 1)) = 0.5  // 控制渐变过渡的平滑度
        
        [Header(Animation)]
        // 动画效果
        [Toggle] _EnableAnimation ("启用动画", Float) = 0
        _AnimationSpeed ("动画速度", Range(0, 5)) = 1
        _AnimationType ("动画类型", Range(0, 3)) = 0  // 0=旋转, 1=脉冲, 2=流动, 3=闪烁
        
        [Header(Advanced)]
        // 高级设置
        _BorderOffset ("边框偏移", Vector) = (0, 0, 0, 0)
        _BorderMask ("边框遮罩", 2D) = "white" {}
        [Toggle] _UseMask ("使用遮罩", Float) = 0
        
        // 透明度控制
        _MainAlpha ("主贴图透明度", Range(0, 1)) = 1
        _BorderAlpha ("边框透明度", Range(0, 1)) = 1
        _OverallAlpha ("整体透明度", Range(0, 1)) = 1
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
            #pragma shader_feature _USEANIMATION_ON
            #pragma shader_feature _USEMASK_ON
            
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
                float2 borderUV : TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };
            
            // 属性声明
            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            // 边框设置
            float _BorderWidth;
            float _BorderSoftness;
            
            // 渐变颜色
            fixed4 _Color1;  // 边缘颜色
            fixed4 _Color2;  // 中间颜色
            fixed4 _Color3;  // 内部颜色
            
            // 渐变控制
            float _GradientIntensity;
            float _GradientBlend;
            
            // 动画设置
            float _EnableAnimation;
            float _AnimationSpeed;
            float _AnimationType;
            
            // 高级设置
            float4 _BorderOffset;
            sampler2D _BorderMask;
            float _UseMask;
            
            // 透明度控制
            float _MainAlpha;
            float _BorderAlpha;
            float _OverallAlpha;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                
                // 计算边框UV坐标
                o.borderUV = v.uv + _BorderOffset.xy;
                
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }
            
            // 计算到边缘的距离
            float GetDistanceToEdge(float2 uv)
            {
                // 计算到四个边缘的最小距离
                float distToLeft = uv.x;
                float distToRight = 1.0 - uv.x;
                float distToTop = uv.y;
                float distToBottom = 1.0 - uv.y;
                
                return min(min(distToLeft, distToRight), min(distToTop, distToBottom));
            }
            
            // 计算边框渐变颜色 - 这是关键函数
            fixed4 GetBorderGradientColor(float2 uv, float borderWidth, float softness)
            {
                float distToEdge = GetDistanceToEdge(uv);
                
                // 计算边框强度 (0 = 完全在边框内, 1 = 完全在边框外)
                float borderStrength = 1.0 - smoothstep(borderWidth - softness, borderWidth + softness, distToEdge);
                
                // 如果不在边框区域内，返回透明
                if (borderStrength <= 0)
                    return fixed4(0, 0, 0, 0);
                
                // 计算边框内的相对位置 (0 = 边缘, 1 = 内部边界)
                float borderProgress = distToEdge / borderWidth;
                borderProgress = saturate(borderProgress);
                
                // 应用渐变混合控制
                borderProgress = pow(borderProgress, _GradientBlend * 2.0 + 0.1);
                
                // 三色渐变插值
                fixed4 finalColor;
                if (borderProgress < 0.5)
                {
                    // 在边缘颜色和中间颜色之间插值
                    float t = borderProgress * 2.0;
                    finalColor = lerp(_Color1, _Color2, t);
                }
                else
                {
                    // 在中间颜色和内部颜色之间插值
                    float t = (borderProgress - 0.5) * 2.0;
                    finalColor = lerp(_Color2, _Color3, t);
                }
                
                // 应用边框强度到透明度
                finalColor.a *= borderStrength;
                
                return finalColor;
            }
            
            // 动画效果
            float GetAnimationEffect(float2 uv, float time, float speed, float type)
            {
                float animTime = time * speed;
                
                if (type < 0.25)
                {
                    // 旋转动画 - 渐变颜色旋转
                    float2 center = float2(0.5, 0.5);
                    float2 offset = uv - center;
                    float angle = animTime;
                    float cosA = cos(angle);
                    float sinA = sin(angle);
                    float2 rotated = float2(
                        offset.x * cosA - offset.y * sinA,
                        offset.x * sinA + offset.y * cosA
                    );
                    return length(rotated) * 0.5 + 0.5;
                }
                else if (type < 0.5)
                {
                    // 脉冲动画 - 边框强度脉冲
                    return sin(animTime * 3.14159) * 0.5 + 0.5;
                }
                else if (type < 0.75)
                {
                    // 流动动画 - 渐变位置流动
                    return sin(uv.x * 10.0 + animTime) * 0.5 + 0.5;
                }
                else
                {
                    // 闪烁动画 - 边框闪烁
                    return step(0.5, sin(animTime * 10.0));
                }
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // 采样主贴图
                fixed4 mainTex = tex2D(_MainTex, i.uv);
                
                // 计算边框渐变颜色
                fixed4 borderColor = GetBorderGradientColor(i.borderUV, _BorderWidth, _BorderSoftness);
                
                // 应用遮罩（如果启用）
                #ifdef _USEMASK_ON
                if (_UseMask > 0.5)
                {
                    fixed4 mask = tex2D(_BorderMask, i.borderUV);
                    borderColor *= mask;
                }
                #endif
                
                // 应用动画效果（如果启用）
                #ifdef _USEANIMATION_ON
                if (_EnableAnimation > 0.5)
                {
                    float animEffect = GetAnimationEffect(i.borderUV, _Time.y, _AnimationSpeed, _AnimationType);
                    
                    if (_AnimationType < 0.25)
                    {
                        // 旋转动画影响渐变位置
                        float2 center = float2(0.5, 0.5);
                        float2 offset = i.borderUV - center;
                        float angle = _Time.y * _AnimationSpeed;
                        float cosA = cos(angle);
                        float sinA = sin(angle);
                        float2 rotated = float2(
                            offset.x * cosA - offset.y * sinA,
                            offset.x * sinA + offset.y * cosA
                        );
                        float2 rotatedUV = rotated + center;
                        borderColor = GetBorderGradientColor(rotatedUV, _BorderWidth, _BorderSoftness);
                    }
                    else
                    {
                        // 其他动画影响边框强度
                        borderColor.a *= animEffect;
                    }
                }
                #endif
                
                // 应用渐变强度
                borderColor.rgb *= _GradientIntensity;
                
                // 应用边框透明度
                borderColor.a *= _BorderAlpha;
                
                // 应用主贴图透明度
                mainTex.a *= _MainAlpha;
                
                // 混合主贴图和边框
                fixed4 finalColor;
                
                // 边框叠加在主贴图上
                finalColor.rgb = mainTex.rgb + borderColor.rgb * borderColor.a;
                finalColor.a = saturate(mainTex.a + borderColor.a);
                
                // 应用顶点颜色
                finalColor *= i.color;
                
                // 应用整体透明度
                finalColor.a *= _OverallAlpha;
                
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