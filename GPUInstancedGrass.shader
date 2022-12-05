Shader "Custom/GPUInstancedGrass" {
    Properties
    {
        _ColorTop ("Top Color", Color) = (1,1,1,1)
        _ColorBottom ("Bottom Color", Color) = (1,1,1,1)
        _AmbiantColor("Ambiant Color", Color) = (0.2,0.2,1,1)

        _ColorStart ("Color Start", Range(-2, 2)) = 0
        _ColorEnd ("Color End", Range(-2, 2)) = 1

        // _ColorSmall ("_ColorSmall", color) = (0,0,0,1)
        // _ColorBig ("_ColorBig", color) = (1,1,1,1)
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


            float _ColorStart;
            float _ColorEnd;

            // fixed4 _ColorSmall;
            // fixed4 _ColorBig;

            StructuredBuffer<MeshData> _Properties;

            v2f vert(appdata_t i, uint instanceID: SV_InstanceID) {
                v2f o;

                float4 pos = mul(_Properties[instanceID].mat, i.vertex);
                o.vertex = UnityObjectToClipPos(pos);

                o.color = _Properties[instanceID].color;
                // o.color = i.color;

                o.uv = i.uv;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target {

                float t = InverseLerp(_ColorStart, _ColorEnd, i.uv.y);
                float t1 = smoothstep(_ColorStart, _ColorEnd, i.uv.y);

                float4 col = lerp(_ColorBottom, _ColorTop, t);
                float4 col1 = lerp(_AmbiantColor, col, i.color.x);

                float4 col2 = col * col1;

                float4 colo = lerp(0.1, 1, i.color.x);

                // col = col * i.color;

                return col1 * colo;
            }

            ENDCG
        }
    }
}