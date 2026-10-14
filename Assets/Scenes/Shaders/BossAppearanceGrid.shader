Shader "Custom/BossAppearanceGrid"
{
    Properties
    {
        // 主贴图
        _MainTex ("主贴图 (RGBA)", 2D) = "white" {}
        
        // 网格线设置
        _GridColor ("网格线颜色", Color) = (0, 1, 1, 1)
        _GridThickness ("网格线粗细", Range(0.001, 0.1)) = 0.02
        _GridScale ("网格大小", Range(0.1, 10)) = 1
        
        // 透明度控制
        _MainAlpha ("主贴图透明度", Range(0, 1)) = 1
        _GridAlpha ("网格线透明度", Range(0, 1)) = 1
        _OverallAlpha ("整体透明度", Range(0, 1)) = 1
        
        // 流动效果
        [Toggle] _EnableFlow ("启用流动效果", Float) = 0
        _FlowSpeed ("流动速度", Range(0, 10)) = 1
        _FlowDirection ("流动方向", Vector) = (1, 0, 0, 0)
        
        // 网格线强度和发光
        _GridIntensity ("网格线强度", Range(0, 5)) = 1
        _GridGlow ("网格线发光强度", Range(0, 3)) = 0
        
        // 抗锯齿
        _AntiAliasing ("抗锯齿强度", Range(0.001, 0.01)) = 0.005
        
        // 混合模式控制
        _BlendMode ("混合模式", Range(0, 1)) = 0.5
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
            #pragma shader_feature _ENABLEFLOW_ON
            
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
                UNITY_FOG_COORDS(1)
                float2 gridUV : TEXCOORD2;
            };
            
            // 属性声明
            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            fixed4 _GridColor;
            float _GridThickness;
            float _GridScale;
            
            float _MainAlpha;
            float _GridAlpha;
            float _OverallAlpha;
            
            float _EnableFlow;
            float _FlowSpeed;
            float4 _FlowDirection;
            
            float _GridIntensity;
            float _GridGlow;
            float _AntiAliasing;
            float _BlendMode;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                
                // 计算网格UV坐标
                o.gridUV = v.uv * _GridScale;
                
                // 如果启用流动效果，添加时间偏移
                #ifdef _ENABLEFLOW_ON
                if (_EnableFlow > 0.5)
                {
                    float2 flowOffset = _FlowDirection.xy * _Time.y * _FlowSpeed;
                    o.gridUV += flowOffset;
                }
                #endif
                
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }
            
            // 网格线计算函数
            float GetGridLine(float2 uv, float thickness, float antiAlias)
            {
                // 计算到网格线的距离
                float2 grid = abs(frac(uv) - 0.5);
                float2 gridDist = min(grid, 1.0 - grid);
                
                // 计算网格线强度
                float2 gridLine = smoothstep(thickness - antiAlias, thickness + antiAlias, gridDist);
                
                // 返回网格线的强度（1为网格线，0为空白）
                return 1.0 - min(gridLine.x, gridLine.y);
            }
            
            // 增强的网格线函数，支持不同粗细的主次网格
            float GetEnhancedGridLine(float2 uv, float thickness, float antiAlias)
            {
                // 主网格线
                float mainGrid = GetGridLine(uv, thickness, antiAlias);
                
                // 次网格线（更细，用于细节）
                float subGrid = GetGridLine(uv * 5.0, thickness * 0.3, antiAlias);
                subGrid *= 0.3; // 降低次网格线的强度
                
                // 合并主次网格线
                return saturate(mainGrid + subGrid);
            }
            
            // 发光效果函数
            float GetGlowEffect(float gridLine, float glowIntensity)
            {
                if (glowIntensity <= 0) return gridLine;
                
                // 创建发光效果
                float glow = pow(gridLine, 0.5) * glowIntensity;
                return saturate(gridLine + glow);
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // 采样主贴图
                fixed4 mainTex = tex2D(_MainTex, i.uv);
                
                // 计算网格线
                float gridLine = GetEnhancedGridLine(i.gridUV, _GridThickness, _AntiAliasing);
                
                // 应用网格线强度
                gridLine *= _GridIntensity;
                
                // 应用发光效果
                gridLine = GetGlowEffect(gridLine, _GridGlow);
                
                // 创建网格线颜色
                fixed4 gridColor = _GridColor;
                gridColor.a *= gridLine * _GridAlpha;
                
                // 应用主贴图透明度
                mainTex.a *= _MainAlpha;
                
                // 混合主贴图和网格线
                fixed4 finalColor;
                
                // 根据混合模式进行混合
                if (_BlendMode < 0.5)
                {
                    // 加法混合模式 - 网格线叠加在主贴图上
                    finalColor.rgb = mainTex.rgb + gridColor.rgb * gridColor.a;
                    finalColor.a = saturate(mainTex.a + gridColor.a);
                }
                else
                {
                    // 插值混合模式 - 网格线和主贴图插值混合
                    float gridStrength = gridColor.a;
                    finalColor.rgb = lerp(mainTex.rgb, gridColor.rgb, gridStrength);
                    finalColor.a = saturate(mainTex.a + gridColor.a);
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
