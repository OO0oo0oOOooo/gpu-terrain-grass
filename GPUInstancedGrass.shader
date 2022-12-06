Shader "Custom/GPUInstancedGrass" {
    Properties
    {
        _ColorTop ("Top Color", Color) = (1,1,1,1)
        _ColorBottom ("Bottom Color", Color) = (1,1,1,1)
        // _AmbiantColor("Ambiant Color", Color) = (0.2, 0.2, 0.65, 1)
        _HeightColor1("Height Top Color", Color) = (0.03, 0.73, 0.13, 1)
        _HeightColor2("Height Bottom Color", Color) = (0.03, 0.73, 0.13, 1)

        // _AmbientStart ("Ambiant Color Start", Range(0, 1)) = 0
        _ColorStart ("Color Start", Range(0, 1)) = 0
        _ColorEnd ("Color End", Range(0, 1)) = 1

        _Mask ("Mask", 2D) = "white" {}
        _AlphaClip ("Alpha Clip", Range(0, 1)) = 0.5
    }
    SubShader {
        Tags { "RenderType" = "Opaque" }
        Cull off

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #define TAU 6.28318530718

            struct appdata_t {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 uv       : TEXCOORD0;
            };

            struct v2f {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uv       : TEXCOORD0;
            };

            struct MeshData {
                float4x4 mat;
                float4 color;
            };

            float InverseLerp(float a, float b, float v)
            {
                return (v-a) / (b-a);
            }

            fixed4 _ColorTop;
            fixed4 _ColorBottom;
            fixed4 _AmbiantColor;
            fixed4 _HeightColor1;
            fixed4 _HeightColor2;

            float _AmbientStart;
            float _ColorStart;
            float _ColorEnd;
            float _AlphaClip;

            sampler2D _Mask;

            StructuredBuffer<MeshData> _Properties;

            v2f vert(appdata_t i, uint instanceID: SV_InstanceID) {
                v2f o;

                float4 pos = mul(_Properties[instanceID].mat, i.vertex);
                o.vertex = UnityObjectToClipPos(pos);
                
                o.color = _Properties[instanceID].color;
                o.uv = i.uv;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                // float t2 = saturate(InverseLerp(_ColorStart, _ColorEnd, i.uv.y));
                float t = smoothstep(_ColorStart, _ColorEnd, i.uv.y);

                float4 col1 = lerp(_ColorBottom, _ColorTop, t);
                float4 col2 = lerp(_HeightColor2, _HeightColor1, t);
                
                float4 clr = lerp(col1, col2, i.color.x);

                float mask = tex2D(_Mask, i.uv);
                clip(mask - _AlphaClip);

                return clr * mask;
            }

            ENDCG
        }
    }
}