Shader "Custom/DirectShotTrajectory"
{
    Properties
    {
        _MainTex ("主贴图", 2D) = "white" {}
        _Color ("基础颜色", Color) = (1, 1, 1, 1)
        _Brightness ("亮度倍数", Range(0, 5)) = 1
        _Alpha ("透明度", Range(0, 1)) = 1
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
            float _Brightness;
            float _Alpha;
            
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
                // 采样主贴图
                fixed4 tex = tex2D(_MainTex, i.uv);
                
                // 组合基础颜色
                fixed4 finalColor = _Color * tex * i.color;
                
                // 应用亮度
                finalColor.rgb *= _Brightness;
                
                // 应用透明度
                finalColor.a *= _Alpha;
                
                // 应用雾效
                UNITY_APPLY_FOG(i.fogCoord, finalColor);
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    FallBack "Sprites/Default"
}
