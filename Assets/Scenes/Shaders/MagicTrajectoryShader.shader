Shader "Custom/MagicTrajectory"
{
    Properties
    {
        _MainTex ("主贴图", 2D) = "white" {}
        _Color ("基础颜色", Color) = (0, 1, 1, 1)
        _EmissionColor ("发光颜色", Color) = (0, 1, 1, 1)
        _EmissionIntensity ("发光强度", Range(0, 5)) = 2
        
        [Header(Flow Effect)]
        _FlowSpeed ("流动速度", Range(0, 10)) = 2
        _FlowDirection ("流动方向", Vector) = (1, 0, 0, 0)
        _FlowTiling ("流动平铺", Range(0.1, 5)) = 1
        
        [Header(Magic Effect)]
        _NoiseScale ("噪点缩放", Range(0.1, 10)) = 1
        _NoiseSpeed ("噪点速度", Range(0, 5)) = 1
        _NoiseIntensity ("噪点强度", Range(0, 1)) = 0.3
        
        [Header(Transparency)]
        _Alpha ("透明度", Range(0, 1)) = 1
        _EdgeFade ("边缘渐变", Range(0, 1)) = 0.5
        
        [Header(Animation)]
        _PulseSpeed ("脉冲速度", Range(0, 10)) = 3
        _PulseIntensity ("脉冲强度", Range(0, 2)) = 0.5
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
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
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float3 worldPos : TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            fixed4 _Color;
            fixed4 _EmissionColor;
            float _EmissionIntensity;
            
            float _FlowSpeed;
            float4 _FlowDirection;
            float _FlowTiling;
            
            float _NoiseScale;
            float _NoiseSpeed;
            float _NoiseIntensity;
            
            float _Alpha;
            float _EdgeFade;
            
            float _PulseSpeed;
            float _PulseIntensity;
            
            // 简单噪声函数
            float noise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            // 更复杂的噪声
            float smoothNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = noise(i);
                float b = noise(i + float2(1.0, 0.0));
                float c = noise(i + float2(0.0, 1.0));
                float d = noise(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            // 分形噪声
            float fractalNoise(float2 uv)
            {
                float value = 0.0;
                float amplitude = 0.5;
                
                for (int i = 0; i < 4; i++)
                {
                    value += amplitude * smoothNoise(uv);
                    uv *= 2.0;
                    amplitude *= 0.5;
                }
                
                return value;
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                
                // 流动效果
                float2 flowUV = uv * _FlowTiling;
                flowUV += _FlowDirection.xy * _Time.y * _FlowSpeed;
                
                // 采样主贴图
                fixed4 tex = tex2D(_MainTex, flowUV);
                
                // 魔法噪声效果
                float2 noiseUV = i.worldPos.xy * _NoiseScale + _Time.y * _NoiseSpeed;
                float noiseValue = fractalNoise(noiseUV);
                
                // 脉冲效果
                float pulse = sin(_Time.y * _PulseSpeed) * _PulseIntensity + 1.0;
                
                // 边缘渐变（基于UV距离中心的距离）
                float distanceFromCenter = abs(uv.y - 0.5) * 2.0;
                float edgeFade = 1.0 - smoothstep(1.0 - _EdgeFade, 1.0, distanceFromCenter);
                
                // 组合颜色
                fixed4 finalColor = _Color * tex * i.color;
                
                // 发光效果
                fixed4 emission = _EmissionColor * _EmissionIntensity * pulse;
                emission *= (1.0 + noiseValue * _NoiseIntensity);
                
                finalColor.rgb += emission.rgb;
                
                // 应用透明度和边缘渐变
                finalColor.a *= _Alpha * edgeFade;
                
                // 魔法闪烁效果
                float sparkle = step(0.95, noiseValue) * 0.5;
                finalColor.rgb += sparkle * _EmissionColor.rgb;
                
                // 应用雾效
                UNITY_APPLY_FOG(i.fogCoord, finalColor);
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    FallBack "Sprites/Default"
}
