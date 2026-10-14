Shader "Custom/SimpleMeteorShader"
{
    Properties
    {
        _MainTex ("Background Texture", 2D) = "white" {}
        _MeteorColor ("Meteor Color", Color) = (1, 0.8, 0.2, 1)
        _MeteorSpeed ("Meteor Speed", Range(0.1, 5)) = 1
        _MeteorDirection ("Meteor Direction", Vector) = (1, -0.5, 0, 0)
        _MeteorCount ("Meteor Count", Range(1, 10)) = 3
        _MeteorLength ("Meteor Length", Range(0.1, 1)) = 0.3
        _MeteorWidth ("Meteor Width", Range(0.01, 0.2)) = 0.05
        _MeteorIntensity ("Meteor Intensity", Range(0, 3)) = 1.5
        _BackgroundAlpha ("Background Alpha", Range(0, 1)) = 0.8
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        
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
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MeteorColor;
            float _MeteorSpeed;
            float4 _MeteorDirection;
            float _MeteorCount;
            float _MeteorLength;
            float _MeteorWidth;
            float _MeteorIntensity;
            float _BackgroundAlpha;
            
            // 简单的随机函数
            float random(float2 st)
            {
                return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
            }
            
            // 流星函数
            float createMeteor(float2 uv, float2 direction, float time, float seed)
            {
                // 流星位置
                float2 meteorPos = direction * time * _MeteorSpeed + seed;
                meteorPos = frac(meteorPos);
                
                // 计算距离
                float2 toMeteor = uv - meteorPos;
                float dist = length(toMeteor);
                
                // 流星方向
                float2 meteorDir = normalize(direction);
                float projection = dot(toMeteor, meteorDir);
                
                // 流星形状
                float meteor = 0;
                if (projection >= 0 && projection <= _MeteorLength)
                {
                    float2 perp = toMeteor - meteorDir * projection;
                    float perpDist = length(perp);
                    
                    if (perpDist <= _MeteorWidth)
                    {
                        // 头部更亮
                        float headBrightness = 1.0 - projection / _MeteorLength;
                        meteor = headBrightness * (1.0 - perpDist / _MeteorWidth);
                    }
                }
                
                return meteor;
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // 背景纹理
                fixed4 bgColor = tex2D(_MainTex, i.uv);
                
                // 时间
                float time = _Time.y;
                
                // 流星效果
                float meteorEffect = 0;
                
                // 生成多个流星
                for (int j = 0; j < (int)_MeteorCount; j++)
                {
                    float seed = (float)j / _MeteorCount;
                    float meteorTime = time + seed * 5.0;
                    
                    // 添加随机偏移
                    float2 randomOffset = float2(
                        random(float2(seed, 0.3)) - 0.5,
                        random(float2(seed, 0.7)) - 0.5
                    ) * 0.2;
                    
                    float2 direction = normalize(_MeteorDirection.xy + randomOffset);
                    
                    meteorEffect += createMeteor(i.uv, direction, meteorTime, seed);
                }
                
                // 限制强度
                meteorEffect = saturate(meteorEffect * _MeteorIntensity);
                
                // 混合颜色
                fixed4 meteorColor = _MeteorColor * meteorEffect;
                fixed4 finalColor = lerp(bgColor * _BackgroundAlpha, meteorColor, meteorEffect);
                
                // 设置透明度
                finalColor.a = max(bgColor.a * _BackgroundAlpha, meteorEffect);
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}
