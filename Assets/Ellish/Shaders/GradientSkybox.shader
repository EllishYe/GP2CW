Shader "Skybox/Ellish Gradient Skybox"
{
    Properties
    {
        [HDR]_TopColor ("Top Color", Color) = (0.12, 0.28, 0.62, 1)
        [HDR]_HorizonColor ("Horizon Color", Color) = (0.95, 0.54, 0.32, 1)
        [HDR]_BottomColor ("Bottom Color", Color) = (0.03, 0.035, 0.07, 1)
        _HorizonHeight ("Horizon Height", Range(-1, 1)) = 0
        _BlendPower ("Horizon Falloff", Range(0.25, 8)) = 1
        _Intensity ("Intensity", Range(0, 8)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "PreviewType" = "Skybox"
        }

        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _HorizonColor;
                half4 _BottomColor;
                half _HorizonHeight;
                half _BlendPower;
                half _Intensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 directionOS : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.directionOS = normalize(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half y = normalize(input.directionOS).y;
                half topRange = max(0.001h, 1.0h - _HorizonHeight);
                half bottomRange = max(0.001h, _HorizonHeight + 1.0h);
                half topBlend = pow(saturate((y - _HorizonHeight) / topRange), _BlendPower);
                half bottomBlend = pow(saturate((_HorizonHeight - y) / bottomRange), _BlendPower);

                half4 upperGradient = lerp(_HorizonColor, _TopColor, topBlend);
                half4 lowerGradient = lerp(_HorizonColor, _BottomColor, bottomBlend);
                half horizonMask = step(_HorizonHeight, y);
                half4 color = lerp(lowerGradient, upperGradient, horizonMask);

                color.rgb *= _Intensity;
                color.a = 1.0h;
                return color;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
