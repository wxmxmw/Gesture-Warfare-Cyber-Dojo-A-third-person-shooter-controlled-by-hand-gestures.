Shader "Custom/MeteorShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MeteorColor ("Meteor Color", Color) = (1, 0.5, 0, 1)
        _MeteorIntensity ("Meteor Intensity", Range(0, 5)) = 2
        _MeteorSpeed ("Meteor Speed", Range(0, 10)) = 1
        _MeteorDirection ("Meteor Direction", Vector) = (1, -1, 0, 0)
        _MeteorLength ("Meteor Length", Range(0.1, 2)) = 0.5
        _MeteorWidth ("Meteor Width", Range(0.01, 0.5)) = 0.1
        _MeteorCount ("Meteor Count", Range(1, 20)) = 5
        _MeteorOpacity ("Meteor Opacity", Range(0, 1)) = 0.8
        _BackgroundOpacity ("Background Opacity", Range(0, 1)) = 0.3
        _TimeScale ("Time Scale", Range(0, 5)) = 1
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
            #pragma multi_compile_instancing
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 worldPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MeteorColor;
            float _MeteorIntensity;
            float _MeteorSpeed;
            float4 _MeteorDirection;
            float _MeteorLength;
            float _MeteorWidth;
            float _MeteorCount;
            float _MeteorOpacity;
            float _BackgroundOpacity;
            float _TimeScale;
            
            // 随机函数
            float random(float2 st)
            {
                return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
            }
            
            // 噪声函数
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
            
            // 流星生成函数
            float meteor(float2 uv, float2 direction, float time, float seed)
            {
                // 计算流星位置
                float2 meteorPos = direction * time * _MeteorSpeed + seed;
                meteorPos = frac(meteorPos); // 循环
                
                // 计算到流星中心的距离
                float2 toMeteor = uv - meteorPos;
                float dist = length(toMeteor);
                
                // 流星方向
                float2 meteorDir = normalize(direction);
                
                // 计算在流星方向上的投影
                float projection = dot(toMeteor, meteorDir);
                
                // 流星形状：沿方向的长条
                float meteorShape = 0;
                if (projection >= 0 && projection <= _MeteorLength)
                {
                    float2 perpendicular = toMeteor - meteorDir * projection;
                    float perpDist = length(perpendicular);
                    
                    if (perpDist <= _MeteorWidth)
                    {
                        // 流星头部更亮
                        float headIntensity = 1.0 - projection / _MeteorLength;
                        meteorShape = headIntensity * (1.0 - perpDist / _MeteorWidth);
                    }
                }
                
                return meteorShape;
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // 获取背景纹理
                fixed4 bgColor = tex2D(_MainTex, i.uv);
                
                // 时间计算
                float time = _Time.y * _TimeScale;
                
                // 流星效果
                float meteorEffect = 0;
                
                // 生成多个流星
                for (int j = 0; j < (int)_MeteorCount; j++)
                {
                    float seed = (float)j / _MeteorCount;
                    float meteorTime = time + seed * 10.0; // 错开时间
                    
                    // 添加一些随机性
                    float2 randomOffset = float2(
                        random(float2(seed, 0.5)) - 0.5,
                        random(float2(seed, 1.5)) - 0.5
                    ) * 0.3;
                    
                    float2 meteorDirection = normalize(_MeteorDirection.xy + randomOffset);
                    
                    // 生成单个流星
                    float singleMeteor = meteor(i.uv, meteorDirection, meteorTime, seed);
                    
                    // 添加拖尾效果
                    float tailLength = _MeteorLength * 2.0;
                    float2 tailDirection = meteorDirection;
                    float tailTime = meteorTime - 0.1;
                    float tailMeteor = meteor(i.uv, tailDirection, tailTime, seed) * 0.3;
                    
                    meteorEffect += singleMeteor + tailMeteor;
                }
                
                // 添加一些闪烁效果
                float flicker = sin(time * 10.0 + i.uv.x * 50.0) * 0.1 + 0.9;
                meteorEffect *= flicker;
                
                // 限制流星效果强度
                meteorEffect = saturate(meteorEffect * _MeteorIntensity);
                
                // 混合背景和流星
                fixed4 meteorColor = _MeteorColor * meteorEffect * _MeteorOpacity;
                fixed4 finalColor = lerp(bgColor * _BackgroundOpacity, meteorColor, meteorEffect);
                
                // 保持背景透明度
                finalColor.a = max(bgColor.a, meteorEffect * _MeteorOpacity);
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}
