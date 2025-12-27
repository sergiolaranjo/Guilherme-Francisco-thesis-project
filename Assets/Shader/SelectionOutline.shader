// ============================================================================
// CardiacVR - VR Platform for Cardiac Surgery Planning
// Copyright (c) 2024 Sérgio Laranjo
// Computational Cardiology, AI and Data Science for Health Lab
// Nova Medical School, Universidade Nova de Lisboa
// All rights reserved.
// ============================================================================

Shader "CardiacVR/SelectionOutline"
{
    Properties
    {
        [Header(Outline)]
        _OutlineColor ("Outline Color", Color) = (1, 0.85, 0.2, 1)
        _OutlineWidth ("Outline Width", Range(0.001, 0.1)) = 0.02
        _OutlineGlow ("Outline Glow", Range(0, 2)) = 0.5

        [Header(Pulse Animation)]
        [Toggle] _EnablePulse ("Enable Pulse", Float) = 1
        _PulseSpeed ("Pulse Speed", Range(0.5, 5)) = 2
        _PulseMin ("Pulse Min Intensity", Range(0, 1)) = 0.5
        _PulseMax ("Pulse Max Intensity", Range(0, 2)) = 1.2

        [Header(Fill)]
        [Toggle] _EnableFill ("Enable Fill", Float) = 0
        _FillColor ("Fill Color", Color) = (1, 0.9, 0.3, 0.2)
        _FresnelPower ("Fresnel Power", Range(1, 10)) = 3
        _FresnelBoost ("Fresnel Boost", Range(0, 2)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+100"
        }

        // Outline pass (extruded mesh)
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
                float _OutlineGlow;
                float _EnablePulse;
                float _PulseSpeed;
                float _PulseMin;
                float _PulseMax;
                float _EnableFill;
                float4 _FillColor;
                float _FresnelPower;
                float _FresnelBoost;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // Calculate animated width
                float pulse = 1.0;
                if (_EnablePulse > 0.5)
                {
                    pulse = lerp(_PulseMin, _PulseMax, (sin(_Time.y * _PulseSpeed * 3.14159) * 0.5 + 0.5));
                }

                // Extrude along normal
                float3 extrudedPos = IN.positionOS.xyz + IN.normalOS * _OutlineWidth * pulse;

                OUT.positionCS = TransformObjectToHClip(extrudedPos);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = GetWorldSpaceNormalizeViewDir(TransformObjectToWorld(IN.positionOS.xyz));

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Pulse intensity
                float pulse = 1.0;
                if (_EnablePulse > 0.5)
                {
                    pulse = lerp(_PulseMin, _PulseMax, (sin(_Time.y * _PulseSpeed * 3.14159) * 0.5 + 0.5));
                }

                // Fresnel for edge softness
                float NdotV = saturate(dot(normalize(IN.normalWS), normalize(IN.viewDirWS)));
                float fresnel = pow(1.0 - NdotV, 2.0);

                // Final color with glow
                float3 color = _OutlineColor.rgb * (1.0 + _OutlineGlow * pulse);

                // Alpha based on fresnel (softer edges)
                float alpha = _OutlineColor.a * lerp(0.7, 1.0, fresnel) * pulse;

                return half4(color, alpha);
            }
            ENDHLSL
        }

        // Fill pass (inner glow)
        Pass
        {
            Name "Fill"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
                float _OutlineGlow;
                float _EnablePulse;
                float _PulseSpeed;
                float _PulseMin;
                float _PulseMax;
                float _EnableFill;
                float4 _FillColor;
                float _FresnelPower;
                float _FresnelBoost;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = GetWorldSpaceNormalizeViewDir(TransformObjectToWorld(IN.positionOS.xyz));

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                if (_EnableFill < 0.5)
                    discard;

                // Fresnel effect
                float NdotV = saturate(dot(normalize(IN.normalWS), normalize(IN.viewDirWS)));
                float fresnel = pow(1.0 - NdotV, _FresnelPower);

                // Pulse
                float pulse = 1.0;
                if (_EnablePulse > 0.5)
                {
                    pulse = lerp(_PulseMin, _PulseMax, (sin(_Time.y * _PulseSpeed * 3.14159) * 0.5 + 0.5));
                }

                // Color with fresnel boost at edges
                float3 color = _FillColor.rgb * (1.0 + fresnel * _FresnelBoost);

                // Alpha - stronger at edges
                float alpha = _FillColor.a * (fresnel + 0.1) * pulse;

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
