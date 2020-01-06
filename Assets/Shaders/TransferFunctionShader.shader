Shader "VolumeRendering/TransferFunctionShader"
{
    Properties
    {
        _HistTex("Histogram Texture", 2D) = "white" {}
        _TFTex("Transfer Function Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        
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
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _HistTex;
            sampler2D _TFTex;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float density = i.uv.x;
                float histY = tex2D(_HistTex, float2(density, 0.0f)).r;
                float4 tfColor = tex2D(_TFTex, float2(density, 0.0f));
                float4 histColor = histY > i.uv.y ? float4(1.0f, 1.0f, 1.0f, 1.0f) : float4(0.0f, 0.0f, 0.0f, 0.0f);
                
                float alpha = tfColor.a;
                if(i.uv.y > alpha) tfColor.a = 0.0f;
                
                float4 color = histColor * 0.5f + tfColor * 0.7f;
                
                return color;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
