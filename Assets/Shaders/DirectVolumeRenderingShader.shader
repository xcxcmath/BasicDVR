Shader "VolumeRendering/DirectVolumeRenderingShader"
{
    Properties
    {
        _DataTex ("Data Texture (Generated)", 3D) = "" {}
        _NoiseTex("Noise Texture (Generated)", 2D) = "white" {}
        _TFTex ("Transfer Function Texture (Generated)", 2D) = "" {}
        _MinVal("Min val", Range(0.0, 1.0)) = 0.0
        _MaxVal("Max val", Range(0.0, 1.0)) = 1.0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType"="Transparent" }
        LOD 100
        Cull Back
        //ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma multi_compile __ TF2D_ON
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            #define ITERATIONS 100
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 world : TEXCOORD1;
                float3 local : TEXCOORD2;
            };
            
            struct Ray
            {
                float3 origin;
                float3 dir;
            };
            
            struct AABB
            {
                float3 min;
                float3 max;
            };
            
            sampler3D _DataTex;
            sampler2D _NoiseTex;
            sampler2D _TFTex;
            float _MinVal;
            float _MaxVal;
            
            float4 getTFColor(float density)
            {
                return tex2D(_TFTex, float2(density, 0));
            }
            
            float4 getTF2DColor(float density, float gradMag)
            {
                return tex2D(_TFTex, float2(density, gradMag));
            }
            
            float4 getGradDensity(float3 pos)
            {
                return tex3D(_DataTex, pos);
            }
            
            bool intersect(Ray r, AABB aabb, out float t0, out float t1)
            {
                float3 invR = 1.0 / r.dir;
                float3 tbot = invR * (aabb.min - r.origin);
                float3 ttop = invR * (aabb.max - r.origin);
                float3 tmin = min(ttop, tbot);
                float3 tmax = max(ttop, tbot);
                float2 t = max(tmin.xx, tmin.yz);
                t0 = max(t.x, t.y);
                t = min(tmax.xx, tmax.yz);
                t1 = min(t.x, t.y);
                return t0 <= t1;
            }
            
            float3 get_uv(float3 p){
                return (p + 0.5);
            }
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.local = v.vertex.xyz;
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                Ray ray;
                ray.origin = i.local;
                
                float3 dir = (i.world - _WorldSpaceCameraPos);
                ray.dir = normalize(mul(unity_WorldToObject, dir));
                
                // create random offset
                ray.origin += (2.0f * ray.dir / ITERATIONS) * tex2D(_NoiseTex, i.uv);
                
                AABB aabb; aabb.min = float3(-0.5, -0.5, -0.5); aabb.max = float3(0.5, 0.5, 0.5);
                
                float tnear, tfar;
                intersect(ray, aabb, tnear, tfar);
                
                tnear = max(0.0, tnear);
                float3 start = ray.origin + ray.dir * tnear;
                float3 end = ray.origin + ray.dir * tfar;
                
                float4 dst = float4(0,0,0,0);
                
                [unroll]
                for(int iter = 0; iter < ITERATIONS; ++iter){
                    float iter_point = float(iter) / float(ITERATIONS);
                    float3 obj_pos = lerp(start, end, iter_point);
                    float3 uv = get_uv(obj_pos);
                    
                    float4 here = getGradDensity(uv);
                    float density = here.a;
                    float3 gradient = here.xyz;
                    #if TF2D_ON
                    float mag = length(gradient) / 1.75f;
                    float4 src = getTF2DColor(density, mag);
                    #else
                    float4 src = getTFColor(density);
                    #endif
                    if(density < _MinVal || density > _MaxVal)
                        src.a = 0.0f;
                    src.a *= 0.5;
                    src.rgb *= src.a;
                    float alpha_delta = (1 - dst.a) * src.a;
                    dst += (1.0 - dst.a) * src;
                    if(dst.a > 1.0f)
                        break;
                }
                return saturate(dst);
            }
            
            ENDCG
        }
    }
    FallBack "Diffuse"
}
