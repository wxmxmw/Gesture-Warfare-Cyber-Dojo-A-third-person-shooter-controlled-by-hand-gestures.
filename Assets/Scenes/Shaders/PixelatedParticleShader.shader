Shader "Custom/PixelatedParticleShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ParticleSize ("Particle Size", Range(0.01, 0.1)) = 0.05
        _FlickerSpeed ("Flicker Speed", Range(0.1, 10.0)) = 2.0
        _WaveAmplitude ("Wave Amplitude", Range(0.1, 2.0)) = 1.0
        _WaveFrequency ("Wave Frequency", Range(0.1, 5.0)) = 1.0
        _MinAlpha ("Min Alpha", Range(0.0, 1.0)) = 0.0
        _MaxAlpha ("Max Alpha", Range(0.0, 1.0)) = 1.0
        _ParticleColor ("Particle Color", Color) = (1,1,1,1)
        _BackgroundAlpha ("Background Alpha", Range(0.0, 1.0)) = 0.3
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _ParticleSize;
            float _FlickerSpeed;
            float _WaveAmplitude;
            float _WaveFrequency;
            float _MinAlpha;
            float _MaxAlpha;
            fixed4 _ParticleColor;
            float _BackgroundAlpha;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            // 生成伪随机数
            float random(float2 st)
            {
                return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
            }

            // 生成噪声
            float noise(float2 st)
            {
                float2 i = floor(st);
                float2 f = frac(st);
                
                float a = random(i);
                float b = random(i + float2(1.0, 0.0));
                float c = random(i + float2(0.0, 1.0));
                float d = random(i + float2(1.0, 1.0));
                
                float2 u = f * f * (3.0 - 2.0 * f);
                
                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 获取原始纹理颜色
                fixed4 texColor = tex2D(_MainTex, i.uv);
                
                // 获取纹理的宽高比，用于修正像素点形状
                float2 texelSize = float2(ddx(i.uv.x), ddy(i.uv.y));
                float textureAspect = abs(texelSize.y / texelSize.x);
                
                // 计算修正后的UV坐标，确保像素点为正圆
                float2 correctedUV = float2(i.uv.x, i.uv.y * textureAspect);
                
                // 计算像素网格坐标（基于纹理空间，但大小更小）
                float2 pixelUV = floor(correctedUV / _ParticleSize) * _ParticleSize;
                
                // 计算当前像素在网格中的位置
                float2 pixelCenter = pixelUV + _ParticleSize * 0.5;
                
                // 计算距离像素中心的距离
                float2 dist = abs(correctedUV - pixelCenter);
                
                // 创建圆形粒子效果
                float particleMask = 1.0 - smoothstep(_ParticleSize * 0.3, _ParticleSize * 0.5, length(dist));
                
                // 基于位置和时间的波动闪烁效果
                float timeOffset = _Time.y * _FlickerSpeed;
                float waveX = sin(pixelCenter.x * _WaveFrequency * 10.0 + timeOffset) * _WaveAmplitude;
                float waveY = cos(pixelCenter.y * _WaveFrequency * 10.0 + timeOffset * 1.3) * _WaveAmplitude;
                float wave = (waveX + waveY) * 0.5;
                
                // 添加噪声变化
                float noiseValue = noise(pixelCenter * 20.0 + _Time.y * 0.5);
                
                // 组合波动和噪声
                float flicker = (sin(timeOffset + wave + noiseValue * 3.14159) + 1.0) * 0.5;
                
                // 应用闪烁范围
                float alpha = lerp(_MinAlpha, _MaxAlpha, flicker);
                
                // 粒子颜色混合
                fixed4 particleColor = _ParticleColor * texColor;
                particleColor.a *= alpha * particleMask;
                
                // 背景颜色（原图的淡化版本）
                fixed4 backgroundColor = texColor;
                backgroundColor.a *= _BackgroundAlpha;
                
                // 混合粒子和背景
                fixed4 finalColor = lerp(backgroundColor, particleColor, particleMask * alpha);
                
                return finalColor;
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}
