Shader "Custom/BeamShader"
{
    Properties
    {
        _MainTex ("Beam Texture", 2D) = "white" {}
        _Color ("Beam Color", Color) = (1, 0, 0, 1)
        _Intensity ("Intensity", Range(0, 5)) = 1
        _Length ("Beam Length", Range(0, 1)) = 1
        _Width ("Beam Width", Range(0, 1)) = 0.1
        _Speed ("Animation Speed", Range(0, 10)) = 1
        _FadeIn ("Fade In", Range(0, 1)) = 0.1
        _FadeOut ("Fade Out", Range(0, 1)) = 0.1
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
            fixed4 _Color;
            float _Intensity;
            float _Length;
            float _Width;
            float _Speed;
            float _FadeIn;
            float _FadeOut;
            
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
                // 计算时间动画
                float time = _Time.y * _Speed;
                
                // 计算UV坐标
                float2 uv = i.uv;
                
                // 计算光束长度动画
                float lengthAnim = sin(time) * 0.5 + 0.5;
                float currentLength = _Length * lengthAnim;
                
                // 计算光束宽度
                float width = _Width;
                
                // 计算距离中心的距离
                float distFromCenter = abs(uv.x - 0.5);
                
                // 计算光束强度
                float beamIntensity = 1.0 - smoothstep(0, width, distFromCenter);
                
                // 计算长度衰减
                float lengthFade = 1.0 - smoothstep(currentLength - _FadeOut, currentLength, uv.y);
                lengthFade *= smoothstep(0, _FadeIn, uv.y);
                
                // 计算整体透明度
                float alpha = beamIntensity * lengthFade * _Intensity;
                
                // 添加闪烁效果
                float flicker = sin(time * 10) * 0.1 + 0.9;
                alpha *= flicker;
                
                // 计算最终颜色
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                col.a *= alpha;
                
                return col;
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}
