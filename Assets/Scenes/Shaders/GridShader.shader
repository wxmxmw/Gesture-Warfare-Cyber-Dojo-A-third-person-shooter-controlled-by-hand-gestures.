Shader "Custom/GridShader"
{
    Properties
    {
        // 网格背景设置
        [Header(Grid Background)]
        _GridBackgroundColor ("网格背景颜色", Color) = (0.1, 0.1, 0.1, 1)
        _GridBackgroundColor2 ("网格背景颜色2", Color) = (0.2, 0.2, 0.2, 1)
        _GridBackgroundGradient ("背景渐变强度", Range(0, 1)) = 0.5
        _GridBackgroundGradientDirection ("背景渐变方向", Range(0, 3)) = 0  // 0=上下, 1=左右, 2=对角, 3=径向
        
        // 网格线设置
        [Header(Grid Lines)]
        _GridLineColor ("网格线颜色", Color) = (1, 1, 1, 1)
        _GridLineColor2 ("网格线颜色2", Color) = (0.8, 0.8, 0.8, 1)
        _GridLineGradient ("网格线渐变强度", Range(0, 1)) = 0.3
        _GridLineGradientDirection ("网格线渐变方向", Range(0, 3)) = 1  // 0=上下, 1=左右, 2=对角, 3=径向
        
        // 网格参数
        [Header(Grid Settings)]
        _GridSize ("网格大小", Range(0.01, 1)) = 0.1
        _GridLineWidth ("网格线宽度", Range(0.001, 0.1)) = 0.01
        _GridLineSoftness ("网格线柔化", Range(0.001, 0.05)) = 0.005
        
        // 透明度控制
        [Header(Transparency)]
        _BackgroundAlpha ("背景透明度", Range(0, 1)) = 1
        _LineAlpha ("网格线透明度", Range(0, 1)) = 1
        _OverallAlpha ("整体透明度", Range(0, 1)) = 1
        
        // 移动动画
        [Header(Animation)]
        [Toggle] _EnableMovement ("启用移动动画", Float) = 0
        _MovementSpeed ("移动速度", Range(0, 10)) = 1
        _MovementDirection ("移动方向", Vector) = (1, 0, 0, 0)  // X, Y方向移动
        _MovementType ("移动类型", Range(0, 2)) = 0  // 0=平移, 1=旋转, 2=缩放
        
        // 高级设置
        [Header(Advanced)]
        _GridOffset ("网格偏移", Vector) = (0, 0, 0, 0)
        _GridRotation ("网格旋转", Range(0, 360)) = 0
        _GridScale ("网格缩放", Range(0.1, 5)) = 1
        
        // 混合模式
        [Header(Blend Mode)]
        _BlendMode ("混合模式", Range(0, 2)) = 0  // 0=正常, 1=叠加, 2=屏幕
        
        // 主贴图（可选）
        [Header(Texture)]
        _MainTex ("主贴图", 2D) = "white" {}
        _TextureBlend ("贴图混合", Range(0, 1)) = 0
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
            #pragma shader_feature _ENABLEMOVEMENT_ON
            
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
                float2 worldUV : TEXCOORD1;  // 用于世界坐标计算
                UNITY_FOG_COORDS(2)
            };
            
            // 属性声明
            // 网格背景
            fixed4 _GridBackgroundColor;
            fixed4 _GridBackgroundColor2;
            float _GridBackgroundGradient;
            float _GridBackgroundGradientDirection;
            
            // 网格线
            fixed4 _GridLineColor;
            fixed4 _GridLineColor2;
            float _GridLineGradient;
            float _GridLineGradientDirection;
            
            // 网格参数
            float _GridSize;
            float _GridLineWidth;
            float _GridLineSoftness;
            
            // 透明度
            float _BackgroundAlpha;
            float _LineAlpha;
            float _OverallAlpha;
            
            // 动画
            float _EnableMovement;
            float _MovementSpeed;
            float4 _MovementDirection;
            float _MovementType;
            
            // 高级设置
            float4 _GridOffset;
            float _GridRotation;
            float _GridScale;
            
            // 混合模式
            float _BlendMode;
            
            // 主贴图
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _TextureBlend;
            
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
            
            // 计算渐变因子
            float CalculateGradientFactor(float2 uv, float direction)
            {
                float factor = 0.0;
                
                if (direction < 0.5)
                {
                    // 上下渐变
                    factor = uv.y;
                }
                else if (direction < 1.5)
                {
                    // 左右渐变
                    factor = uv.x;
                }
                else if (direction < 2.5)
                {
                    // 对角渐变
                    factor = (uv.x + uv.y) * 0.5;
                }
                else
                {
                    // 径向渐变
                    float2 center = float2(0.5, 0.5);
                    factor = distance(uv, center) * 2.0;
                }
                
                return saturate(factor);
            }
            
            // 计算网格
            float CalculateGrid(float2 uv, float gridSize, float lineWidth, float softness)
            {
                // 应用网格变换
                float2 gridUV = uv * _GridScale;
                
                // 应用旋转
                if (_GridRotation > 0.001)
                {
                    float angle = _GridRotation * 3.14159 / 180.0;
                    float cosA = cos(angle);
                    float sinA = sin(angle);
                    float2 center = float2(0.5, 0.5);
                    float2 offset = gridUV - center;
                    gridUV = float2(
                        offset.x * cosA - offset.y * sinA,
                        offset.x * sinA + offset.y * cosA
                    ) + center;
                }
                
                // 应用偏移
                gridUV += _GridOffset.xy;
                
                // 应用移动动画
                #ifdef _ENABLEMOVEMENT_ON
                if (_EnableMovement > 0.5)
                {
                    float animTime = _Time.y * _MovementSpeed;
                    
                    if (_MovementType < 0.5)
                    {
                        // 平移移动
                        gridUV += _MovementDirection.xy * animTime;
                    }
                    else if (_MovementType < 1.5)
                    {
                        // 旋转移动
                        float angle = animTime;
                        float cosA = cos(angle);
                        float sinA = sin(angle);
                        float2 center = float2(0.5, 0.5);
                        float2 offset = gridUV - center;
                        gridUV = float2(
                            offset.x * cosA - offset.y * sinA,
                            offset.x * sinA + offset.y * cosA
                        ) + center;
                    }
                    else
                    {
                        // 缩放移动
                        float scale = 1.0 + sin(animTime) * 0.2;
                        float2 center = float2(0.5, 0.5);
                        gridUV = (gridUV - center) * scale + center;
                    }
                }
                #endif
                
                // 计算网格线
                float2 grid = abs(frac(gridUV / gridSize) - 0.5) / (lineWidth / gridSize);
                float Line = min(grid.x, grid.y);
                
                // 应用柔化
                return 1.0 - smoothstep(0.0, softness / lineWidth, Line);
            }
            
            // 颜色混合函数
            fixed4 BlendColors(fixed4 baseColor, fixed4 blendColor, float blendMode)
            {
                if (blendMode < 0.5)
                {
                    // 正常混合
                    return lerp(baseColor, blendColor, blendColor.a);
                }
                else if (blendMode < 1.5)
                {
                    // 叠加混合
                    fixed4 result;
                    result.rgb = baseColor.rgb * (1.0 - blendColor.a) + 
                                (baseColor.rgb * blendColor.rgb * 2.0) * blendColor.a;
                    result.a = baseColor.a;
                    return result;
                }
                else
                {
                    // 屏幕混合
                    fixed4 result;
                    result.rgb = baseColor.rgb * (1.0 - blendColor.a) + 
                                (1.0 - (1.0 - baseColor.rgb) * (1.0 - blendColor.rgb)) * blendColor.a;
                    result.a = baseColor.a;
                    return result;
                }
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                
                // 计算网格
                float gridMask = CalculateGrid(uv, _GridSize, _GridLineWidth, _GridLineSoftness);
                
                // 计算背景渐变
                float backgroundGradientFactor = CalculateGradientFactor(uv, _GridBackgroundGradientDirection);
                fixed4 backgroundColor = lerp(_GridBackgroundColor, _GridBackgroundColor2, 
                                            backgroundGradientFactor * _GridBackgroundGradient);
                backgroundColor.a *= _BackgroundAlpha;
                
                // 计算网格线渐变
                float lineGradientFactor = CalculateGradientFactor(uv, _GridLineGradientDirection);
                fixed4 lineColor = lerp(_GridLineColor, _GridLineColor2, 
                                      lineGradientFactor * _GridLineGradient);
                lineColor.a *= _LineAlpha;
                
                // 组合颜色
                fixed4 finalColor = backgroundColor;
                
                // 添加网格线
                finalColor.rgb = lerp(finalColor.rgb, lineColor.rgb, gridMask * lineColor.a);
                finalColor.a = max(finalColor.a, gridMask * lineColor.a);
                
                // 应用主贴图（如果启用）
                if (_TextureBlend > 0.001)
                {
                    fixed4 texColor = tex2D(_MainTex, i.uv);
                    finalColor = lerp(finalColor, texColor, _TextureBlend);
                }
                
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
