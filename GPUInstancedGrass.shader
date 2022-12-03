Shader "Custom/GPUInstancedGrass" {
    Properties
    {
        _ColorTop ("Top Color", Color) = (1,1,1,1)
        _ColorBottom ("Bottom Color", Color) = (1,1,1,1)

        _ColorStart ("Color Start", Range(-2, 2)) = 0
        _ColorEnd ("Color End", Range(-2, 2)) = 1

        _Amp ("Amplitude", Range(0, 0.5)) = 0.1
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
            };

            fixed4 _ColorTop;
            fixed4 _ColorBottom;

            float _ColorStart;
            float _ColorEnd;

            float _Amp;

            StructuredBuffer<MeshData> _Properties;

            v2f vert(appdata_t i, uint instanceID: SV_InstanceID) {
                v2f o;

                // float wave = cos((i.uv.x - _Time.y * 0.1f) * TAU * 2);
                // i.vertex.x = ((wave * _Amp) + 0.1f) * i.color.r;

                float4 pos = mul(_Properties[instanceID].mat, i.vertex);
                o.vertex = UnityObjectToClipPos(pos);

                // o.color = _Properties[instanceID].color;
                // o.color = i.color;

                o.uv = i.uv;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target {

                float t = smoothstep(_ColorStart, _ColorEnd, i.uv.y);
                float4 col = lerp(_ColorBottom, _ColorTop, t);

                return col;
            }

            ENDCG
        }
    }
}