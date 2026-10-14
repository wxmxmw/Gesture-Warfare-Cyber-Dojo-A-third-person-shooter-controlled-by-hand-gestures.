Shader "Custom/LaserEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Intensity ("Intensity", Range(0, 5)) = 1
        _GlowColor ("Glow Color", Color) = (1,1,1,1)
        _GlowIntensity ("Glow Intensity", Range(0, 3)) = 1
        _ScrollSpeed ("Scroll Speed", Range(-5, 5)) = 1
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 2
        _PulseAmplitude ("Pulse Amplitude", Range(0, 1)) = 0.3
        _EdgeFade ("Edge Fade", Range(0, 1)) = 0.5
        _NoiseScale ("Noise Scale", Range(0, 10)) = 1
        _NoiseIntensity ("Noise Intensity", Range(0, 1)) = 0.2
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "IgnoreProjector"="True"
        }
        
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
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
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 worldPos : TEXCOORD2;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Intensity;
            float4 _GlowColor;
            float _GlowIntensity;
            float _ScrollSpeed;
            float _PulseSpeed;
            float _PulseAmplitude;
            float _EdgeFade;
            float _NoiseScale;
            float _NoiseIntensity;
            
            // 简单噪声函数
            float noise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            // 分形噪声
            float fractalNoise(float2 uv)
            {
                float value = 0.0;
                float amplitude = 1.0;
                float frequency = 1.0;
                
                for(int i = 0; i < 3; i++)
                {
                    value += amplitude * noise(uv * frequency);
                    amplitude *= 0.5;
                    frequency *= 2.0;
                }
                
                return value;
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xy;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // 基础纹理采样
                float2 scrollUV = i.uv + float2(_ScrollSpeed * _Time.y, 0);
                fixed4 tex = tex2D(_MainTex, scrollUV);
                
                // 脉冲效果
                float pulse = sin(_Time.y * _PulseSpeed) * _PulseAmplitude + (1.0 - _PulseAmplitude);
                
                // 边缘渐变效果
                float edgeFactor = 1.0;
                if(_EdgeFade > 0)
                {
                    float distFromCenter = abs(i.uv.y - 0.5) * 2.0; // 0到1
                    edgeFactor = 1.0 - smoothstep(1.0 - _EdgeFade, 1.0, distFromCenter);
                }
                
                // 噪声效果
                float2 noiseUV = i.worldPos * _NoiseScale + _Time.y * 0.5;
                float noiseValue = fractalNoise(noiseUV);
                float noiseFactor = lerp(1.0, noiseValue, _NoiseIntensity);
                
                // 组合所有效果
                float4 baseColor = _Color * i.color * tex;
                float4 glowColor = _GlowColor * _GlowIntensity;
                
                // 最终颜色计算
                float4 finalColor = baseColor + glowColor;
                finalColor.rgb *= _Intensity * pulse * edgeFactor * noiseFactor;
                finalColor.a *= edgeFactor * i.color.a;
                
                // 应用雾效
                UNITY_APPLY_FOG(i.fogCoord, finalColor);
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}
