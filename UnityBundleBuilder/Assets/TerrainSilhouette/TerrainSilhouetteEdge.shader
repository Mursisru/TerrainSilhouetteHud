Shader "Hidden/at747/TerrainSilhouette/Edge"
{
    Properties
    {
        _MainTex ("Terrain", 2D) = "black" {}
        _HudColor ("HUD tint", Color) = (0, 1, 0, 1)
        _NearColor ("Near highlight", Color) = (1, 0.2, 0.1, 1)
        _EdgeThreshold ("Edge threshold", Range(0.001, 0.5)) = 0.08
        _EdgeStrength ("Edge strength", Range(0, 4)) = 1.4
        _NearDepthBias ("Near depth bias", Range(0, 0.01)) = 0.002
        _NearDepthScale ("Near depth scale", Range(1, 8000)) = 2200
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off
            Blend One Zero

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _HudColor;
            fixed4 _NearColor;
            float _EdgeThreshold;
            float _EdgeStrength;
            float _NearDepthBias;
            float _NearDepthScale;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float SampleLum(float2 uv)
            {
                fixed3 c = tex2D(_MainTex, uv).rgb;
                return dot(c, float3(0.299, 0.587, 0.114));
            }

            float SampleDepth01(float2 uv)
            {
                float d = tex2D(_MainTex, uv).r;
                return saturate((_NearDepthBias + (1.0 - d)) * _NearDepthScale * 0.00015);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 t = _MainTex_TexelSize.xy;
                float lumC = SampleLum(i.uv);
                float lumL = SampleLum(i.uv + float2(-t.x, 0));
                float lumR = SampleLum(i.uv + float2(t.x, 0));
                float lumU = SampleLum(i.uv + float2(0, t.y));
                float lumD = SampleLum(i.uv + float2(0, -t.y));

                float gx = lumR - lumL;
                float gy = lumU - lumD;
                float edge = sqrt(gx * gx + gy * gy);
                edge = saturate((edge - _EdgeThreshold) * _EdgeStrength);

                float depthNear = SampleDepth01(i.uv);
                fixed3 rgb = lerp(_HudColor.rgb, _NearColor.rgb, depthNear * edge);
                fixed4 outCol;
                outCol.rgb = rgb * edge;
                outCol.a = edge * _HudColor.a;
                return outCol;
            }
            ENDCG
        }
    }
    Fallback Off
}
