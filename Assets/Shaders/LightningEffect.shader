Shader "Custom/LightningEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Base Color", Color) = (1,1,1,1)
        _Intensity ("Intensity", Range(0, 10)) = 1
        _GlowColor ("Glow Color", Color) = (0.5, 0.8, 1, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 20)) = 5
        _Brightness ("Brightness", Range(0, 50)) = 10
        _FlickerSpeed ("Flicker Speed", Range(0, 20)) = 5
        _FlickerIntensity ("Flicker Intensity", Range(0, 1)) = 0.8
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 2
        _PulseAmplitude ("Pulse Amplitude", Range(0, 2)) = 0.5
        _EdgeFade ("Edge Fade", Range(0, 1)) = 0.3
        _NoiseScale ("Noise Scale", Range(0, 20)) = 5
        _NoiseIntensity ("Noise Intensity", Range(0, 1)) = 0.4
        _SparkleIntensity ("Sparkle Intensity", Range(0, 5)) = 2
        _SparkleSpeed ("Sparkle Speed", Range(0, 20)) = 8
        _CoreBrightness ("Core Brightness", Range(0, 100)) = 20
        _RimGlow ("Rim Glow", Range(0, 5)) = 1
        _Distortion ("Distortion", Range(0, 1)) = 0.1
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
        }
        
        LOD 200
        
        Blend One One  // 加法混合，增强发光效果
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 worldPos : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Intensity;
            float4 _GlowColor;
            float _GlowIntensity;
            float _Brightness;
            float _FlickerSpeed;
            float _FlickerIntensity;
            float _PulseSpeed;
            float _PulseAmplitude;
            float _EdgeFade;
            float _NoiseScale;
            float _NoiseIntensity;
            float _SparkleIntensity;
            float _SparkleSpeed;
            float _CoreBrightness;
            float _RimGlow;
            float _Distortion;
            
            // 改进的噪声函数
            float noise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            // 分形噪声，用于更复杂的闪电效果
            float fractalNoise(float2 uv)
            {
                float value = 0.0;
                float amplitude = 1.0;
                float frequency = 1.0;
                
                for(int i = 0; i < 4; i++)
                {
                    value += amplitude * noise(uv * frequency);
                    amplitude *= 0.5;
                    frequency *= 2.0;
                }
                
                return value;
            }
            
            // 闪电闪烁效果
            float lightningFlicker(float2 uv, float time)
            {
                float flicker = 0.0;
                
                // 主要闪烁
                flicker += sin(time * _FlickerSpeed) * 0.5 + 0.5;
                
                // 随机闪烁
                float2 flickerUV = uv * 10.0 + time * 2.0;
                flicker += noise(flickerUV) * _FlickerIntensity;
                
                // 高频闪烁
                flicker += sin(time * _FlickerSpeed * 3.0) * 0.3;
                
                return saturate(flicker);
            }
            
            // 耀斑效果
            float sparkleEffect(float2 uv, float time)
            {
                float2 sparkleUV = uv * _NoiseScale * 2.0 + time * _SparkleSpeed;
                float sparkle = fractalNoise(sparkleUV);
                
                // 创建闪烁的耀斑点
                sparkle = pow(sparkle, 8.0) * _SparkleIntensity;
                
                return sparkle;
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xy;
                
                // 计算视角方向
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = normalize(_WorldSpaceCameraPos - worldPos);
                
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                
                float time = _Time.y;
                
                // 基础纹理采样
                float2 scrollUV = i.uv + float2(time * 0.5, 0);
                fixed4 tex = tex2D(_MainTex, scrollUV);
                
                // 闪电闪烁效果
                float flicker = lightningFlicker(i.uv, time);
                
                // 脉冲效果
                float pulse = sin(time * _PulseSpeed) * _PulseAmplitude + (1.0 - _PulseAmplitude);
                
                // 边缘渐变效果
                float edgeFactor = 1.0;
                if(_EdgeFade > 0)
                {
                    float distFromCenter = abs(i.uv.y - 0.5) * 2.0;
                    edgeFactor = 1.0 - smoothstep(1.0 - _EdgeFade, 1.0, distFromCenter);
                }
                
                // 噪声效果
                float2 noiseUV = i.worldPos * _NoiseScale + time * 0.5;
                float noiseValue = fractalNoise(noiseUV);
                float noiseFactor = lerp(1.0, noiseValue, _NoiseIntensity);
                
                // 耀斑效果
                float sparkle = sparkleEffect(i.uv, time);
                
                // 核心亮度效果
                float coreBrightness = _CoreBrightness * flicker * pulse;
                
                // 边缘发光效果
                float rimGlow = 1.0 - saturate(dot(i.viewDir, float3(0, 0, 1)));
                rimGlow = pow(rimGlow, 2.0) * _RimGlow;
                
                // 组合所有效果
                float4 baseColor = _Color * i.color * tex;
                float4 glowColor = _GlowColor * _GlowIntensity;
                
                // 最终颜色计算
                float4 finalColor = baseColor + glowColor;
                finalColor.rgb *= _Intensity * _Brightness * flicker * pulse * edgeFactor * noiseFactor;
                finalColor.rgb += sparkle * _GlowColor.rgb * _SparkleIntensity;
                finalColor.rgb += rimGlow * _GlowColor.rgb * _RimGlow;
                finalColor.rgb *= coreBrightness;
                
                // 保持透明度
                finalColor.a *= edgeFactor * i.color.a;
                
                // 应用雾效
                UNITY_APPLY_FOG(i.fogCoord, finalColor);
                
                return finalColor;
            }
            ENDCG
        }
        
        // 第二个Pass用于增强发光效果
        Pass
        {
            Blend One One
            ZWrite Off
            Cull Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 worldPos : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Intensity;
            float4 _GlowColor;
            float _GlowIntensity;
            float _Brightness;
            float _FlickerSpeed;
            float _FlickerIntensity;
            float _PulseSpeed;
            float _PulseAmplitude;
            float _EdgeFade;
            float _NoiseScale;
            float _NoiseIntensity;
            float _SparkleIntensity;
            float _SparkleSpeed;
            float _CoreBrightness;
            float _RimGlow;
            float _Distortion;
            
            float noise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            float fractalNoise(float2 uv)
            {
                float value = 0.0;
                float amplitude = 1.0;
                float frequency = 1.0;
                
                for(int i = 0; i < 4; i++)
                {
                    value += amplitude * noise(uv * frequency);
                    amplitude *= 0.5;
                    frequency *= 2.0;
                }
                
                return value;
            }
            
            float lightningFlicker(float2 uv, float time)
            {
                float flicker = 0.0;
                flicker += sin(time * _FlickerSpeed) * 0.5 + 0.5;
                float2 flickerUV = uv * 10.0 + time * 2.0;
                flicker += noise(flickerUV) * _FlickerIntensity;
                flicker += sin(time * _FlickerSpeed * 3.0) * 0.3;
                return saturate(flicker);
            }
            
            float sparkleEffect(float2 uv, float time)
            {
                float2 sparkleUV = uv * _NoiseScale * 2.0 + time * _SparkleSpeed;
                float sparkle = fractalNoise(sparkleUV);
                sparkle = pow(sparkle, 8.0) * _SparkleIntensity;
                return sparkle;
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xy;
                
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                
                float time = _Time.y;
                
                // 基础纹理采样
                float2 scrollUV = i.uv + float2(time * 0.5, 0);
                fixed4 tex = tex2D(_MainTex, scrollUV);
                
                // 闪电闪烁效果
                float flicker = lightningFlicker(i.uv, time);
                
                // 脉冲效果
                float pulse = sin(time * _PulseSpeed) * _PulseAmplitude + (1.0 - _PulseAmplitude);
                
                // 边缘渐变效果
                float edgeFactor = 1.0;
                if(_EdgeFade > 0)
                {
                    float distFromCenter = abs(i.uv.y - 0.5) * 2.0;
                    edgeFactor = 1.0 - smoothstep(1.0 - _EdgeFade, 1.0, distFromCenter);
                }
                
                // 噪声效果
                float2 noiseUV = i.worldPos * _NoiseScale + time * 0.5;
                float noiseValue = fractalNoise(noiseUV);
                float noiseFactor = lerp(1.0, noiseValue, _NoiseIntensity);
                
                // 耀斑效果
                float sparkle = sparkleEffect(i.uv, time);
                
                // 核心亮度效果
                float coreBrightness = _CoreBrightness * flicker * pulse;
                
                // 第二个Pass只输出发光效果
                float4 glowColor = _GlowColor * _GlowIntensity * 0.5;
                glowColor.rgb *= flicker * pulse * edgeFactor * noiseFactor;
                glowColor.rgb += sparkle * _GlowColor.rgb * _SparkleIntensity * 0.5;
                glowColor.rgb *= coreBrightness * 0.3;
                
                glowColor.a *= edgeFactor * i.color.a * 0.5;
                
                UNITY_APPLY_FOG(i.fogCoord, glowColor);
                
                return glowColor;
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}
