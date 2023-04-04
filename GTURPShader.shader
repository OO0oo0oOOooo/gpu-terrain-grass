Shader "Unlit/GTURPShader" {
    Properties {
        [Header(Surface options)]
        _TopTint("Top Tint", Color) = (1, 1, 1, 1)
        _BottomTint("Bottom Tint", Color) = (1, 1, 1, 1)
        _Fade("Top Fade Offset", Range(-1,10)) = 0
    }

    SubShader {
        Tags{"RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque"}

        Pass {
            Name "ForwardLit"
            Tags{"LightMode" = "UniversalForward"}

            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"

            float4 _TopTint;
            float4 _BottomTint;
            float _Fade;

            struct MeshProperties {
                float4x4 mat;
                float4 color;
            };

            StructuredBuffer<MeshProperties> _Properties;

            struct appdata_t {
                float4 vertex   : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 vertex   : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 color    : COLOR;
            };

            v2f vert(appdata_t i, uint instanceID: SV_InstanceID) {
                v2f o;

                float4 pos = mul(_Properties[instanceID].mat, i.vertex);
                VertexPositionInputs posnInputs = GetVertexPositionInputs(pos);

                o.vertex = posnInputs.positionCS;
                o.positionWS = posnInputs.positionWS;
                o.color = _Properties[instanceID].color;
                o.uv = i.uv;

                return o;
            }

            float4 frag(v2f i) : SV_Target {

                // return i.color;
                float4 ambient = float4(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w, 1); //float4(ShadeSH9(float4(0,0,1,1)),0);
                float shadow = 0;
                
                half4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                
                #if _MAIN_LIGHT_SHADOWS_CASCADE || _MAIN_LIGHT_SHADOWS
                    Light mainLight = GetMainLight(shadowCoord);
                    shadow = mainLight.shadowAttenuation;
                #else
                    Light mainLight = GetMainLight();
                #endif
                
                // extra point lights support
                float3 extraLights;
                int pixelLightCount = GetAdditionalLightsCount();
                for (int j = 0; j < pixelLightCount; ++j) {
                    Light light = GetAdditionalLight(j, i.positionWS, half4(1, 1, 1, 1));
                    float3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
                    extraLights += attenuatedLightColor;
                }

                // fade over the length of the grass
                float verticalFade = saturate(i.uv.y + _Fade);
                extraLights *= verticalFade;

                // colors from the tool with tinting from the grass script
                float4 baseColor = lerp(_BottomTint, _TopTint, verticalFade) * float4(i.color);
                
                float4 final = float4(0, 0, 0, 0);
                final = baseColor;

                //add in shadows
                final *= shadow;

                // if theres a main light, multiply with its color and intensity
                final *= float4(mainLight.color, 1);
                
                // add in ambient
                final += (ambient * baseColor) ;

                final += float4(extraLights, 1);
                return final;
            }

            ENDHLSL
        }
    }
}